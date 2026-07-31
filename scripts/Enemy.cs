using Godot;

public enum EnemyKind
{
	/// <summary>Baseline: walks straight at the player.</summary>
	Chaser,

	/// <summary>Small and quick, spawns in packs. Dies to one hit.</summary>
	Swarmer,

	/// <summary>Large and slow, soaks several hits and has a big contact radius.</summary>
	Tank
}

public partial class Enemy : CharacterBody2D
{
	// How far outside the viewport an enemy may drift before it is culled.
	private const float CullMargin = 600.0f;

	/// <summary>Speed a knockback impulse bleeds off at, in units per second.</summary>
	private const float KnockbackDecay = 1400.0f;

	private Node2D player;
	private Sprite2D sprite;
	private Vector2 spriteBaseScale = Vector2.One;
	private float speedMultiplier = 1.0f;
	private int health = 1;
	private int maxHealth = 1;
	private Color baseTint = Colors.White;
	private Vector2 baseScale = Vector2.One;
	private Vector2 knockback = Vector2.Zero;
	private Tween hitFlash;

	public EnemyKind Kind { get; private set; } = EnemyKind.Chaser;

	/// <summary>How hard a hit on this body knocks it back.</summary>
	public float KnockbackStrength { get; private set; } = 260.0f;

	/// <summary>Death burst tuning, so a tank does not pop like a swarmer.</summary>
	public int BurstAmount { get; private set; } = 50;
	public float BurstScale { get; private set; } = 1.0f;
	public Color BurstColor { get; private set; } = new Color(0.91f, 0.35f, 0.45f);

	public override void _Ready()
	{
		var gameManager = GetTree().GetFirstNodeInGroup("game_manager");
		player = gameManager?.GetNodeOrNull<Node2D>("player");
		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null)
			spriteBaseScale = sprite.Scale;
	}

	/// <summary>Applies the stats and look for a kind. Call before adding to the tree.</summary>
	public void Configure(EnemyKind kind)
	{
		Kind = kind;

		switch (kind)
		{
			case EnemyKind.Swarmer:
				speedMultiplier = 1.7f;
				health = 1;
				baseScale = new Vector2(1.2f, 1.2f);
				baseTint = new Color(0.72f, 1.0f, 0.85f);
				KnockbackStrength = 420.0f;
				BurstAmount = 26;
				BurstScale = 0.65f;
				BurstColor = new Color(0.62f, 1.0f, 0.78f);
				break;

			case EnemyKind.Tank:
				speedMultiplier = 0.5f;
				health = 4;
				baseScale = new Vector2(3.0f, 3.0f);
				baseTint = new Color(0.7f, 0.78f, 1.0f);
				KnockbackStrength = 90.0f;
				BurstAmount = 120;
				BurstScale = 2.1f;
				BurstColor = new Color(0.66f, 0.76f, 1.0f);
				break;

			default:
				speedMultiplier = 1.0f;
				health = 1;
				baseScale = new Vector2(2.0f, 2.0f);
				baseTint = Colors.White;
				KnockbackStrength = 240.0f;
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

		// Read the spawner's ramp each frame so living enemies speed up too,
		// then apply this kind's multiplier on top.
		float speed = EnemySpawner.CurrentSpeed * speedMultiplier;

		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * speed + knockback;
		LookAt(player.GlobalPosition);
		MoveAndSlide();

		knockback = knockback.MoveToward(Vector2.Zero, KnockbackDecay * (float)delta);

		CullIfLost();
	}

	/// <param name="impactDirection">Travel direction of whatever hit it, for knockback.</param>
	/// <returns>True if this hit destroyed the enemy.</returns>
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

	/// <summary>Safety net so an enemy that somehow drifts away cannot leak forever.</summary>
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
