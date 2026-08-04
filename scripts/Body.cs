using Godot;

/// <summary>
/// A body. It is not chasing — it is falling.
///
/// This class owns everything every kind shares: gravity, damage, knockback,
/// flashing and culling. What differs per kind lives in a <see cref="BodyBehaviour"/>,
/// which this hands the wheel to once a frame.
/// </summary>
public partial class Body : CharacterBody2D, IShootable
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

	[ExportGroup("Relics")]
	[Export] public float SlowAuraRadius { get; set; } = 340.0f;
	[Export] public float SlowAuraStrength { get; set; } = 3.2f;

	[ExportGroup("Arena events")]
	/// <summary>Push per second during Solar Wind.</summary>
	[Export] public float SolarWindForce { get; set; } = 340.0f;
	/// <summary>How hard Inversion pushes, relative to the pull it replaces.</summary>
	[Export] public float InversionStrength { get; set; } = 0.75f;
	/// <summary>Size and slowness multipliers during Heavy Weather.</summary>
	[Export] public float GiantScale { get; set; } = 1.55f;
	[Export] public float GiantSlowdown { get; set; } = 0.55f;

	[ExportGroup("Armed bodies")]
	[Export] public PackedScene BulletScene { get; set; }
	[Export] public float BulletSpeed { get; set; } = 420.0f;

	/// <summary>This same scene, for kinds that break apart into more of themselves.</summary>
	[Export] public PackedScene BodyScene { get; set; }

	/// <summary>
	/// The nine face sprites, loaded once and shared by every body. Centralised
	/// here rather than kept per-spawner, so every path that creates a body —
	/// the spawner, a Fracture's splinters, a boss's broodlings — gets the same
	/// correct face instead of only the spawner's own spawns being dressed.
	/// </summary>
	private static Texture2D[] faceTextures;

	private static void EnsureFacesLoaded()
	{
		if (faceTextures != null)
			return;

		faceTextures = new Texture2D[9];
		for (int i = 0; i < faceTextures.Length; i++)
			faceTextures[i] = GD.Load<Texture2D>($"res://sprites/enemy {i + 1}.png");
	}

	private Node2D world;
	private RunState run;
	private Sprite2D sprite;
	private Vector2 spriteBaseScale = Vector2.One;
	private Vector2 knockback = Vector2.Zero;
	private BodyBehaviour behaviour = BodyBehaviours.For(BodyKind.Drifter);
	private int health = 1;
	private int maxHealth = 1;
	private bool destroyed;
	private Tween hitFlash;

	public BodyKind Kind { get; private set; } = BodyKind.Drifter;

	// Written by the behaviour in Apply, read by the shared code below.
	public float SpeedMultiplier { get; set; } = 1.0f;
	public float AccelMultiplier { get; set; } = 1.0f;
	public Vector2 BaseScale { get; set; } = Vector2.One;
	public Color BaseTint { get; set; } = Colors.White;
	public float KnockbackStrength { get; set; } = 260.0f;
	public int DebrisCount { get; set; } = 2;

	/// <summary>
	/// Which face sprite this kind wears. Fixed per kind so a Bulwark always
	/// looks like a Bulwark; -1 leaves it to the spawner to pick at random.
	/// </summary>
	public int TextureIndex { get; set; } = -1;

	/// <summary>Current velocity under gravity, before knockback is added.</summary>
	public Vector2 Drift { get; set; } = Vector2.Zero;

	/// <summary>One float of scratch space for the behaviour. Cadences, mostly.</summary>
	public float BehaviourTimer { get; set; }

	/// <summary>Which way this body circles, for kinds that hold an orbit.</summary>
	public float OrbitDirection { get; private set; } = 1.0f;

	/// <summary>Vector from this body to the world.</summary>
	public Vector2 WorldOffset => HasWorld ? world.GlobalPosition - GlobalPosition : Vector2.Zero;
	public Vector2 WorldPosition => HasWorld ? world.GlobalPosition : GlobalPosition;
	public bool HasWorld => world != null && IsInstanceValid(world);

	/// <summary>The direction this body is travelling — and therefore facing.</summary>
	public Vector2 Forward => Drift.LengthSquared() > 1f ? Drift.Normalized() : Vector2.Right;

	/// <summary>Death burst tuning, so a Shard does not pop like a Planetoid.</summary>
	public int BurstAmount { get; private set; } = 55;
	public float BurstScale { get; private set; } = 1.0f;
	public Color BurstColor { get; private set; } = new Color(0.91f, 0.35f, 0.45f);

	/// <summary>
	/// What a body leaves behind. Captured before the killing blow, because that
	/// blow queues the body for deletion.
	/// </summary>
	public readonly struct Remains
	{
		public Remains(BodyKind kind, int debrisCount, int burstAmount, float burstScale, Color burstColor)
		{
			Kind = kind;
			DebrisCount = debrisCount;
			BurstAmount = burstAmount;
			BurstScale = burstScale;
			BurstColor = burstColor;
		}

		public BodyKind Kind { get; }
		public int DebrisCount { get; }
		public int BurstAmount { get; }
		public float BurstScale { get; }
		public Color BurstColor { get; }
	}

	public Remains GetRemains() => new Remains(Kind, DebrisCount, BurstAmount, BurstScale, BurstColor);

	public void SetHealth(int value)
	{
		health = Mathf.Max(value, 1);
		maxHealth = health;
	}

	public void SetBurst(int amount, float scale, Color color)
	{
		BurstAmount = amount;
		BurstScale = scale;
		BurstColor = color;
	}

	public override void _Ready()
	{
		var gameManager = GameManager.Of(this);
		world = gameManager?.GetNodeOrNull<Node2D>("player");
		run = gameManager?.Run;
		BulletScene ??= GD.Load<PackedScene>("res://scenes/bullet.tscn");

		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null)
			spriteBaseScale = sprite.Scale;

		// Difficulty's contact radius scales the hitbox alone — the collision
		// shape's own local scale, not the body's — so a Hard-mode Drifter looks
		// exactly like a Normal one but is less forgiving to graze.
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
			collisionShape.Scale *= Loadout.DifficultyProfile.ContactRadiusMultiplier;

		ApplyFace();

		OrbitDirection = RunState.Rng.Randf() < 0.5f ? -1.0f : 1.0f;

		// Heavy Weather applies at birth, not continuously, so the bodies it
		// produced stay giant for their whole life and the change is legible.
		if (run != null && run.During(ArenaEventId.GiantSlugs))
		{
			BaseScale *= GiantScale;
			SpeedMultiplier *= GiantSlowdown;
			AccelMultiplier *= GiantSlowdown;
			Scale = BaseScale;
		}

		LaunchIntoOrbit();
	}

	/// <summary>
	/// Picks this body's face. Most kinds pin one via <see cref="TextureIndex"/>
	/// so they stay recognisable between orbits; only the baseline Drifter is
	/// left to vary. Runs in _Ready, so GlobalPosition must already be set —
	/// every spawn path sets it before adding the body to the tree.
	/// </summary>
	private void ApplyFace()
	{
		if (sprite == null)
			return;

		EnsureFacesLoaded();

		int index = TextureIndex >= 0
			? Mathf.Min(TextureIndex, faceTextures.Length - 1)
			: RunState.Rng.RandiRange(0, Mathf.Min(2, faceTextures.Length - 1));
		sprite.Texture = faceTextures[index];

		// Bodies spawned past the bottom-right edge get flipped, purely for
		// variety — otherwise every off-screen arrival on that side looks identical.
		Vector2 viewportSize = GetViewportRect().Size;
		if (GlobalPosition.X > viewportSize.X || GlobalPosition.Y > viewportSize.Y)
		{
			sprite.FlipV = true;
			sprite.FlipH = true;
		}
	}

	/// <summary>
	/// A body dropped in at rest falls dead straight and never orbits. A small
	/// sideways push at birth is what turns the arena into a gravity well.
	/// </summary>
	private void LaunchIntoOrbit()
	{
		if (!HasWorld || Drift != Vector2.Zero)
			return;

		Vector2 toWorld = WorldOffset;
		if (toWorld.LengthSquared() < 1f)
			return;

		Vector2 tangent = toWorld.Normalized().Orthogonal() * OrbitDirection;
		Drift = tangent * BodySpawner.CurrentSpeed * SpeedMultiplier * RunState.Rng.RandfRange(0.35f, 0.95f);
	}

	/// <summary>Applies the stats and look for a kind. Call before adding to the tree.</summary>
	public void Configure(BodyKind kind)
	{
		Kind = kind;
		behaviour = BodyBehaviours.For(kind);
		behaviour.Apply(this);

		Scale = BaseScale;
		Modulate = BaseTint;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!HasWorld)
			return;

		float step = (float)delta;

		// Freeze stops the steering, not the node: knockback still resolves, so
		// a frozen body being shot still visibly takes the hit.
		if (run == null || !run.Frozen)
			behaviour.Steer(this, step);
		else
			Drift = Drift.MoveToward(Vector2.Zero, 900f * step);

		ApplySlowAura(step);

		Velocity = Drift + knockback;
		FaceTravel();
		MoveAndSlide();

		knockback = knockback.MoveToward(Vector2.Zero, KnockbackDecay * step);

		CullIfLost();
	}

	/// <summary>
	/// Deep Well relic: bodies bog down as they close. It makes the last stretch
	/// before contact readable, which is exactly where the game is hardest.
	/// </summary>
	private void ApplySlowAura(float delta)
	{
		if (run == null || !run.Has(RelicId.SlowAura))
			return;

		float distance = WorldOffset.Length();
		if (distance > SlowAuraRadius)
			return;

		// Strongest at the centre, nothing at the rim, so there is no edge to it.
		float bite = 1f - distance / SlowAuraRadius;
		Drift *= Mathf.Max(1f - SlowAuraStrength * bite * delta, 0f);
	}

	/// <summary>The default motion: accelerate toward the world under its gravity.</summary>
	public void FallTowardWorld(float delta)
	{
		Vector2 toWorld = WorldOffset;
		float distance = Mathf.Max(toWorld.Length(), 1.0f);
		Vector2 towards = toWorld / distance;

		// Softened inverse falloff: real inverse-square explodes on contact and
		// leaves distant bodies barely moving. This keeps both ends playable.
		float falloff = FalloffDistance / (distance + FalloffDistance);
		float massPull = Mathf.Lerp(1.0f, HeavyPullMultiplier, run?.MassNormalised ?? 0f);
		float acceleration = BaseAcceleration * AccelMultiplier * massPull * falloff
			* BodySpawner.SpeedScale;

		// Inversion flips the sign of the one force the whole game is built on.
		bool inverted = run != null && run.During(ArenaEventId.InvertedGravity);
		if (inverted)
			acceleration = -acceleration * InversionStrength;

		Vector2 drift = Drift + towards * acceleration * delta;

		if (run != null && run.During(ArenaEventId.SolarWind))
			drift += run.WindDirection * SolarWindForce * delta;

		drift *= Mathf.Max(1.0f - Drag * delta, 0f);

		float baseSpeed = BodySpawner.CurrentSpeed * SpeedMultiplier;

		// Far out, orbiting forever would just mean drifting off-screen, so a
		// minimum closing speed is enforced. Inside that range the body is left
		// alone: overshooting and swinging back is the behaviour we want.
		// Suspended during Inversion, which is meant to scatter bodies outward.
		if (distance > StrandingDistance && !inverted)
		{
			float approach = drift.Dot(towards);
			float minApproach = baseSpeed * 0.5f;
			if (approach < minApproach)
				drift += towards * (minApproach - approach);
		}

		Drift = drift.LimitLength(baseSpeed * MaxSpeedFactor);
	}

	/// <summary>Spawns another body of <paramref name="kind"/> with an outward push.</summary>
	public void SpawnChild(BodyKind kind, Vector2 launch)
	{
		var manager = GameManager.Of(this);
		if (manager == null)
			return;

		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");

		var child = BodyScene.Instantiate<Body>();
		child.Configure(kind);
		child.GlobalPosition = GlobalPosition;
		child.Drift = Drift * 0.4f + launch;

		// Splits happen inside a collision callback, and inserting a physics body
		// while the server is flushing queries is an error. The add waits for idle.
		Callable.From(() =>
		{
			if (IsInstanceValid(manager) && IsInstanceValid(child))
				manager.AddEntity(child);
			else
				child.QueueFree();
		}).CallDeferred();
	}

	/// <summary>Fires this body's own projectile at a point.</summary>
	public void FireAt(Vector2 target)
	{
		if (BulletScene == null)
			return;

		var shot = BulletScene.Instantiate<Bullet>();
		shot.GlobalPosition = GlobalPosition;
		shot.Direction = (target - GlobalPosition).Normalized();
		shot.Speed = BulletSpeed;
		shot.MakeHostile();
		GameManager.Spawn(this, shot);
	}

	/// <summary>
	/// Blows up everything nearby, including the world. Chained bodies do not
	/// score, so two Flares next to each other cannot cascade into free points.
	/// </summary>
	public void Detonate(float radius)
	{
		GameManager.Of(this)?.SpawnBlast(GlobalPosition, radius, BurstColor);

		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body other || other == this || !IsInstanceValid(other))
				continue;

			if (GlobalPosition.DistanceTo(other.GlobalPosition) <= radius)
				other.TakeDamage(9999, (other.GlobalPosition - GlobalPosition).Normalized());
		}

		if (HasWorld && GlobalPosition.DistanceTo(world.GlobalPosition) <= radius)
			(world as Player)?.KillByBlast();
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
		if (destroyed)
			return false;

		if (behaviour.Deflects(this, impactDirection))
		{
			FlashDeflect();
			return false;
		}

		health -= amount;

		if (health > 0)
		{
			knockback += impactDirection.Normalized() * KnockbackStrength;
			FlashHit();
			return false;
		}

		destroyed = true;
		behaviour.OnDestroyed(this);
		QueueFree();
		return true;
	}

	/// <summary>
	/// White flash plus a squash on any survivable hit, so chip damage reads even
	/// on a body that is barely dented.
	/// </summary>
	private void FlashHit()
	{
		// Bodies with more health left flash back to tint faster; a nearly-dead
		// one lingers pale, which telegraphs the last hit.
		float lingerFactor = 1.0f - (float)health / Mathf.Max(maxHealth, 1);
		Flash(Colors.White, Mathf.Lerp(0.12f, 0.26f, lingerFactor), 1.2f);
	}

	/// <summary>A bounce off armour has to look different from a wound.</summary>
	private void FlashDeflect()
	{
		Flash(new Color(0.75f, 0.9f, 1.0f), 0.1f, 0.9f);
	}

	private void Flash(Color colour, float duration, float spritePunch)
	{
		hitFlash?.Kill();
		Modulate = colour;

		hitFlash = CreateTween().SetParallel();
		hitFlash.TweenProperty(this, "modulate", BaseTint, duration);

		// Punch the sprite rather than the body, so the collision shape stays honest.
		if (sprite != null)
		{
			sprite.Scale = spriteBaseScale * spritePunch;
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
