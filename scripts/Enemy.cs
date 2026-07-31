using Godot;

public enum EnemyKind
{
	/// <summary>Baseline: falls straight in.</summary>
	Chaser,

	/// <summary>Small and quick, arrives in packs. Dies to one hit.</summary>
	Swarmer,

	/// <summary>Large and slow, soaks several hits and has a big contact radius.</summary>
	Tank
}

/// <summary>
/// A body. It is not chasing — it is falling.
///
/// Acceleration toward the world scales with the world's mass and with how close
/// the body already is, so bodies build momentum, overshoot, swing back and
/// clump. That behaviour is the whole point of the game; a straight-line chase
/// would make this a re-skinned arena shooter.
/// </summary>
public partial class Enemy : CharacterBody2D
{
	// How far outside the viewport a body may drift before it is culled.
	private const float CullMargin = 700.0f;

	/// <summary>Speed a knockback impulse bleeds off at, in units per second.</summary>
	private const float KnockbackDecay = 1400.0f;

	[ExportGroup("Gravity")]
	[Export] public float BaseAcceleration { get; set; } = 640.0f;
	/// <summary>Distance at which pull is half its close-range strength.</summary>
	[Export] public float FalloffDistance { get; set; } = 430.0f;
	/// <summary>Pull multiplier at full world mass. 1.0 at zero mass.</summary>
	[Export] public float HeavyPullMultiplier { get; set; } = 2.5f;
	/// <summary>Velocity bled off per second. Without it, orbits never decay inward.</summary>
	[Export] public float Drag { get; set; } = 0.55f;
	/// <summary>Beyond this range a body is forced to keep closing, so nothing strands.</summary>
	[Export] public float StrandingDistance { get; set; } = 700.0f;
	/// <summary>Ceiling on drift speed, as a multiple of the ramped base speed.</summary>
	[Export] public float MaxSpeedFactor { get; set; } = 2.3f;

	private Node2D player;
	private RunState run;
	private Sprite2D sprite;
	private Vector2 spriteBaseScale = Vector2.One;
	private Vector2 drift = Vector2.Zero;
	private Vector2 knockback = Vector2.Zero;
	private float speedMultiplier = 1.0f;
	private float accelMultiplier = 1.0f;
	private int health = 1;
	private int maxHealth = 1;
	private Color baseTint = Colors.White;
	private Vector2 baseScale = Vector2.One;
	private Tween hitFlash;

	/// <summary>
	/// What a body leaves behind. Captured before the killing blow, because that
	/// blow queues the body for deletion.
	/// </summary>
	public readonly struct Remains
	{
		public Remains(EnemyKind kind, int debrisCount, int burstAmount, float burstScale, Color burstColor)
		{
			Kind = kind;
			DebrisCount = debrisCount;
			BurstAmount = burstAmount;
			BurstScale = burstScale;
			BurstColor = burstColor;
		}

		public EnemyKind Kind { get; }
		public int DebrisCount { get; }
		public int BurstAmount { get; }
		public float BurstScale { get; }
		public Color BurstColor { get; }
	}

	public Remains GetRemains() => new Remains(Kind, DebrisCount, BurstAmount, BurstScale, BurstColor);

	public EnemyKind Kind { get; private set; } = EnemyKind.Chaser;

	/// <summary>How hard a hit on this body knocks it back.</summary>
	public float KnockbackStrength { get; private set; } = 260.0f;

	/// <summary>Motes shed on death, for the world to pull in and absorb.</summary>
	public int DebrisCount { get; private set; } = 2;

	/// <summary>Death burst tuning, so a tank does not pop like a swarmer.</summary>
	public int BurstAmount { get; private set; } = 55;
	public float BurstScale { get; private set; } = 1.0f;
	public Color BurstColor { get; private set; } = new Color(0.91f, 0.35f, 0.45f);

	public override void _Ready()
	{
		var gameManager = GameManager.Of(this);
		player = gameManager?.GetNodeOrNull<Node2D>("player");
		run = gameManager?.Run;

		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null)
			spriteBaseScale = sprite.Scale;

		LaunchIntoOrbit();
	}

	/// <summary>
	/// A body dropped in at rest falls dead straight and never orbits. A small
	/// sideways push at birth is what turns the arena into a gravity well.
	/// </summary>
	private void LaunchIntoOrbit()
	{
		if (player == null || !IsInstanceValid(player))
			return;

		Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
		if (toPlayer.LengthSquared() < 1f)
			return;

		Vector2 tangent = toPlayer.Normalized().Orthogonal();
		if (GD.Randf() < 0.5f)
			tangent = -tangent;

		float base_ = EnemySpawner.CurrentSpeed * speedMultiplier;
		drift = tangent * base_ * (float)GD.RandRange(0.35, 0.95);
	}

	/// <summary>Applies the stats and look for a kind. Call before adding to the tree.</summary>
	public void Configure(EnemyKind kind)
	{
		Kind = kind;

		switch (kind)
		{
			case EnemyKind.Swarmer:
				speedMultiplier = 1.7f;
				accelMultiplier = 1.55f;
				health = 1;
				baseScale = new Vector2(1.2f, 1.2f);
				baseTint = new Color(0.72f, 1.0f, 0.85f);
				KnockbackStrength = 420.0f;
				DebrisCount = 1;
				BurstAmount = 26;
				BurstScale = 0.65f;
				BurstColor = new Color(0.62f, 1.0f, 0.78f);
				break;

			case EnemyKind.Tank:
				speedMultiplier = 0.5f;
				// Heavy bodies answer gravity slowly, which is what makes them
				// read as heavy rather than merely slow.
				accelMultiplier = 0.45f;
				health = 4;
				baseScale = new Vector2(3.0f, 3.0f);
				baseTint = new Color(0.7f, 0.78f, 1.0f);
				KnockbackStrength = 90.0f;
				DebrisCount = 6;
				BurstAmount = 120;
				BurstScale = 2.1f;
				BurstColor = new Color(0.66f, 0.76f, 1.0f);
				break;

			default:
				speedMultiplier = 1.0f;
				accelMultiplier = 1.0f;
				health = 1;
				baseScale = new Vector2(2.0f, 2.0f);
				baseTint = Colors.White;
				KnockbackStrength = 240.0f;
				DebrisCount = 2;
				BurstAmount = 55;
				BurstScale = 1.0f;
				BurstColor = new Color(0.91f, 0.35f, 0.45f);
				break;
		}

		maxHealth = health;
		Scale = baseScale;
		Modulate = baseTint;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (player == null || !IsInstanceValid(player))
			return;

		float step = (float)delta;
		ApplyPull(step);

		Velocity = drift + knockback;
		FaceTravel();
		MoveAndSlide();

		knockback = knockback.MoveToward(Vector2.Zero, KnockbackDecay * step);

		CullIfLost();
	}

	private void ApplyPull(float delta)
	{
		Vector2 toPlayer = player.GlobalPosition - GlobalPosition;
		float distance = Mathf.Max(toPlayer.Length(), 1.0f);
		Vector2 towards = toPlayer / distance;

		// Softened inverse falloff: real inverse-square explodes on contact and
		// leaves distant bodies barely moving. This keeps both ends playable.
		float falloff = FalloffDistance / (distance + FalloffDistance);
		float massPull = Mathf.Lerp(1.0f, HeavyPullMultiplier, run?.MassNormalised ?? 0f);
		float acceleration = BaseAcceleration * accelMultiplier * massPull * falloff
			* EnemySpawner.SpeedScale;

		drift += towards * acceleration * delta;
		drift *= Mathf.Max(1.0f - Drag * delta, 0f);

		float baseSpeed = EnemySpawner.CurrentSpeed * speedMultiplier;

		// Far out, orbiting forever would just mean drifting off-screen, so a
		// minimum closing speed is enforced. Inside that range the body is left
		// alone: overshooting and swinging back is the behaviour we want.
		if (distance > StrandingDistance)
		{
			float approach = drift.Dot(towards);
			float minApproach = baseSpeed * 0.5f;
			if (approach < minApproach)
				drift += towards * (minApproach - approach);
		}

		drift = drift.LimitLength(baseSpeed * MaxSpeedFactor);
	}

	// Bodies are falling, so they should point where they are going. Below a
	// crawl there is no meaningful heading, so the last one is kept.
	private void FaceTravel()
	{
		if (Velocity.LengthSquared() > 100.0f)
			Rotation = Velocity.Angle();
	}

	/// <param name="impactDirection">Travel direction of whatever hit it, for knockback.</param>
	/// <returns>True if this hit destroyed the body.</returns>
	public bool TakeDamage(int amount, Vector2 impactDirection = default)
	{
		health -= amount;

		if (health > 0)
		{
			knockback += impactDirection.Normalized() * KnockbackStrength;
			FlashHit();
			return false;
		}

		QueueFree();
		return true;
	}

	/// <summary>
	/// White flash plus a squash on any survivable hit, so chip damage reads even
	/// on a body that is barely dented.
	/// </summary>
	private void FlashHit()
	{
		hitFlash?.Kill();
		Modulate = Colors.White;

		// Bodies with more health left flash back to tint faster; a nearly-dead
		// tank lingers pale, which telegraphs the last hit.
		float lingerFactor = 1.0f - (float)health / Mathf.Max(maxHealth, 1);
		float duration = Mathf.Lerp(0.12f, 0.26f, lingerFactor);

		hitFlash = CreateTween().SetParallel();
		hitFlash.TweenProperty(this, "modulate", baseTint, duration);

		// Punch the sprite rather than the body, so the collision shape stays honest.
		if (sprite != null)
		{
			sprite.Scale = spriteBaseScale * 1.2f;
			hitFlash.TweenProperty(sprite, "scale", spriteBaseScale, 0.15f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}
	}

	/// <summary>Safety net so a body that somehow drifts away cannot leak forever.</summary>
	private void CullIfLost()
	{
		var bounds = GetViewportRect().Size;
		if (GlobalPosition.X < -CullMargin || GlobalPosition.X > bounds.X + CullMargin ||
			GlobalPosition.Y < -CullMargin || GlobalPosition.Y > bounds.Y + CullMargin)
		{
			QueueFree();
		}
	}
}
