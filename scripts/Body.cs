using System.Collections.Generic;
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
	/// <summary>Speed a knockback impulse bleeds off at, in units per second.</summary>
	private const float KnockbackDecay = 1400.0f;

	/// <summary>No matter what spawns children, the arena never holds more than this.</summary>
	public const int HardCap = 90;

	[ExportGroup("Gravity")]
	[Export] public float BaseAcceleration { get; set; } = 640.0f;
	/// <summary>Distance at which pull is half its close-range strength.</summary>
	[Export] public float FalloffDistance { get; set; } = 430.0f;
	/// <summary>Velocity bled off per second. Without it, orbits never decay inward.</summary>
	[Export] public float Drag { get; set; } = 0.55f;
	/// <summary>Beyond this range a body is forced to keep closing, so nothing strands.</summary>
	[Export] public float StrandingDistance { get; set; } = 700.0f;
	/// <summary>Ceiling on drift speed, as a multiple of the ramped base speed.</summary>
	[Export] public float MaxSpeedFactor { get; set; } = 2.3f;

	[ExportGroup("Arena events")]
	/// <summary>Push per second during Solar Wind.</summary>
	[Export] public float SolarWindForce { get; set; } = 340.0f;
	/// <summary>How hard Inversion pushes, relative to the pull it replaces.</summary>
	[Export] public float InversionStrength { get; set; } = 0.75f;
	/// <summary>Size and slowness multipliers during Heavy Weather.</summary>
	[Export] public float GiantScale { get; set; } = 1.55f;
	[Export] public float GiantSlowdown { get; set; } = 0.55f;

	/// <summary>This same scene, for kinds that break apart into more of themselves.</summary>
	[Export] public PackedScene BodyScene { get; set; }

	/// <summary>
	/// One distinct silhouette per kind, loaded once and shared by every body.
	/// Centralised here rather than kept per-spawner, so every path that creates
	/// a body — the spawner, a Fracture's splinters, a boss's broodlings — gets
	/// the same correct face instead of only the spawner's own spawns being dressed.
	///
	/// Each kind wears one negative emotion, more intense the more dangerous the
	/// kind (see tools/make_moods.py), in a few expressions picked at random per
	/// body, so a crowd of them does not look like one face copied.
	/// </summary>
	private static Dictionary<BodyKind, Texture2D[]> faceTextures;
	/// <summary>Each face with its eyes closed, same order as <see cref="faceTextures"/>.</summary>
	private static Dictionary<BodyKind, Texture2D[]> blinkTextures;

	/// <summary>Splinter is drawn at half the family canvas — see ASSETS.md.</summary>
	private static readonly Dictionary<BodyKind, string[]> FaceFiles = new()
	{
		[BodyKind.Drifter] = new[] { "body_drifter", "body_drifter_2", "body_drifter_3" },
		// Irritability, in three four-cornered crystal shapes.
		[BodyKind.Shard] = Mix("body_shard", new[] { "kite", "blade", "arrow" }, new[] { "huffy", "twitchy", "snappy", "scowling" }),
		// Self-doubt, frustration, shame, resentment and anger.
		[BodyKind.Splinter] = Faces("body_splinter", "unsure", "timid", "shrinking"),
		[BodyKind.Fracture] = Faces("body_fracture", "fed_up", "exasperated", "strained"),
		[BodyKind.Bulwark] = Faces("body_bulwark", "hiding", "cringing", "ashamed"),
		[BodyKind.Planetoid] = Faces("body_planetoid", "grudging", "bitter", "brooding"),
		[BodyKind.Flare] = Faces("body_flare", "furious", "seething", "roaring")
	};

	/// <summary>One file per expression: <c>{prefix}_{mood}</c>.</summary>
	private static string[] Faces(string prefix, params string[] moods) => System.Array.ConvertAll(moods, mood => $"{prefix}_{mood}");

	/// <summary>Every shape wearing every face: <c>{prefix}_{shape}_{mood}</c>.</summary>
	private static string[] Mix(string prefix, string[] shapes, string[] moods)
	{
		var files = new List<string>();
		foreach (string shape in shapes)
			foreach (string mood in moods)
				files.Add($"{prefix}_{shape}_{mood}");
		return files.ToArray();
	}

	private static void EnsureFacesLoaded()
	{
		if (faceTextures != null)
			return;

		faceTextures = new Dictionary<BodyKind, Texture2D[]>();
		blinkTextures = new Dictionary<BodyKind, Texture2D[]>();
		foreach (var (kind, files) in FaceFiles)
		{
			faceTextures[kind] = System.Array.ConvertAll(files, file => GD.Load<Texture2D>($"res://art/cosmic/{file}.svg"));
			blinkTextures[kind] = System.Array.ConvertAll(files, file => GD.Load<Texture2D>($"res://art/cosmic/{file}_blink.svg"));
		}
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
    private float visualTime;

	public BodyKind Kind { get; private set; } = BodyKind.Drifter;

	// Written by the behaviour in Apply, read by the shared code below.
	public float SpeedMultiplier { get; set; } = 1.0f;
	public float AccelMultiplier { get; set; } = 1.0f;
	public Vector2 BaseScale { get; set; } = Vector2.One;
	public Color BaseTint { get; set; } = Colors.White;
	public float KnockbackStrength { get; set; } = 260.0f;
	public int DebrisCount { get; set; } = 2;

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

	/// <summary>Pops in this colour instead of its kind's: a Drifter pops in its own rock's colour.</summary>
	public void SetBurstColour(Color colour) => BurstColor = colour;

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

	/// <summary>True from the killing blow on, even before the node is freed.</summary>
	public bool IsDestroyed => destroyed;

	/// <summary>How much room this body takes in a crowd: its drawn size, not the difficulty-scaled hitbox.</summary>
	public float CrowdRadius => crowdBase * Mathf.Abs(Scale.X);
	private float crowdBase = 25f;

	/// <summary>A Drifter's living face (see <see cref="DrifterFace"/>); null on every other kind.</summary>
	public DrifterFace Face { get; private set; }

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

		sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
		if (sprite != null)
		{
			spriteBaseScale = sprite.Scale;
			if (GameSettings.Instance?.HighContrastOutlines == true)
				sprite.Material = OutlineMaterial.Get();
		}

		// Difficulty's contact radius scales the hitbox alone — the collision
		// shape's own local scale, not the body's — so a Hard-mode Drifter looks
		// exactly like a Normal one but is less forgiving to graze.
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
		{
			if (collisionShape.Shape is CircleShape2D circle)
				crowdBase = circle.Radius * collisionShape.Scale.X;
			collisionShape.Scale *= Loadout.DifficultyProfile.ContactRadiusMultiplier;
		}

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
	/// Picks this body's face. Every kind wears its own distinct silhouette and
	/// mood, so it stays recognisable at a glance in a busy fight. Runs in _Ready, so
	/// GlobalPosition must already be set — every spawn path sets it before
	/// adding the body to the tree.
	/// </summary>
	private void ApplyFace()
	{
		if (sprite == null)
			return;

		// Looks only, so it stays off the run's seeded generator.
		if (Kind == BodyKind.Drifter)
		{
			// The crowd enemy is built from parts, so a crowd of them is not one rock repeated.
			Face = DrifterFace.Dress(this, sprite);
		}
		else
		{
			EnsureFacesLoaded();
			Texture2D[] faces = faceTextures[Kind];
			int pick = (int)(GD.Randi() % (uint)faces.Length);
			sprite.Texture = openFace = faces[pick];
			closedFace = blinkTextures[Kind][pick];
			blinkIn = (float)GD.RandRange(1.0, 5.0);
		}

		// Bodies arriving from the right are mirrored, purely for variety —
		// otherwise every arrival looks identical.
		if (HasWorld && GlobalPosition.X > world.GlobalPosition.X)
		{
			sprite.FlipV = false;
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
        BaseTint=Colors.White;

		if (GameSettings.Instance?.ColourblindMode == true)
		{
			BaseTint = ColourblindPalette.Tint(kind, BaseTint);
			BurstColor = ColourblindPalette.Burst(kind, BurstColor);
		}

		Scale = BaseScale;
		Modulate = BaseTint;
	}

	/// <summary>This body's face and the same face mid-blink. Drifters blink through their own <see cref="DrifterFace"/>.</summary>
	private Texture2D openFace, closedFace;
	private float blinkIn, blinkLeft;

    public override void _Process(double delta)
    {
        visualTime+=(float)delta;
        Blink((float)delta);
        if(sprite!=null)
        {
            sprite.Rotation=Kind==BodyKind.Bulwark?0:-Rotation+Mathf.Sin(visualTime*2+GetInstanceId()%19)*.09f;
            if(hitFlash==null||!hitFlash.IsRunning())
            {float squash=1+.025f*Mathf.Sin(visualTime*3+GetInstanceId()%13);sprite.Scale=spriteBaseScale*new Vector2(1/squash,squash);}
        }
    }
	/// <summary>Closes the eyes for a moment every few seconds, each body on its own clock.</summary>
	private void Blink(float step)
	{
		if (closedFace == null || sprite == null)
			return;
		if (blinkLeft > 0f)
		{
			if ((blinkLeft -= step) <= 0f)
				sprite.Texture = openFace;
		}
		else if ((blinkIn -= step) <= 0f)
		{
			blinkLeft = 0.13f;
			blinkIn = (float)GD.RandRange(2.5, 6.0);
			sprite.Texture = closedFace;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!HasWorld)
			return;

		float step = (float)delta;

		behaviour.Steer(this, step);

		// Personal space rides on top of the steering, so a crowd spreads out without
		// any body steering differently or arriving any slower.
		Velocity = Drift + knockback + Crowd.Separation(this);
		FaceTravel();
		MoveAndSlide();
		StayInArena();

		knockback = knockback.MoveToward(Vector2.Zero, KnockbackDecay * step);

		Leash();
	}

	/// <summary>
	/// Enemies may cross the perimeter belt on the way in, but never leave the
	/// world. Once inside the playable area they stay in it.
	/// </summary>
	private void StayInArena()
	{
		Rect2 playable = Arena.Playable;
		if (playable.HasPoint(GlobalPosition))
		{
			insideArena = true;
			return;
		}

		GlobalPosition = insideArena
			? Arena.ClampToPlayable(GlobalPosition)
			: GlobalPosition.Clamp(Arena.World.Position, Arena.World.End);
	}

	private bool insideArena;

	/// <summary>The default motion: accelerate toward the world under its gravity.</summary>
	public void FallTowardWorld(float delta)
	{
		Vector2 toWorld = WorldOffset;
		float distance = Mathf.Max(toWorld.Length(), 1.0f);
		Vector2 towards = toWorld / distance;

		// Softened inverse falloff: real inverse-square explodes on contact and
		// leaves distant bodies barely moving. This keeps both ends playable.
		float falloff = FalloffDistance / (distance + FalloffDistance);
		float acceleration = BaseAcceleration * AccelMultiplier * falloff * BodySpawner.SpeedScale;

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
		// By then this body may already be freed, so nothing below may touch it.
		Callable.From(() =>
		{
			if (IsInstanceValid(manager) && IsInstanceValid(child) && manager.GetTree().GetNodeCountInGroup("bodies") < HardCap)
				manager.AddEntity(child);
			else
				child.QueueFree();
		}).CallDeferred();
	}

	/// <summary>
	/// Blows up everything nearby, including the world, after a short fuse: the
	/// flash goes off at once so the danger is seen, and the damage lands a
	/// moment later so a quick step or a dash can still get clear. Chained
	/// bodies do not count as kills, so two Flares side by side cannot cascade
	/// into free CORE.
	/// </summary>
	public void Detonate(float radius)
	{
		GameManager.Of(this)?.SpawnBlast(GlobalPosition, radius, BurstColor);

		Vector2 centre = GlobalPosition;
		SceneTree tree = GetTree();
		Player player = HasWorld ? world as Player : null;
		tree.CreateTimer(FlareBehaviour.Fuse, processAlways: false).Timeout += () =>
		{
			foreach (Node node in tree.GetNodesInGroup("bodies"))
			{
				if (node is not Body other || other == this || !IsInstanceValid(other))
					continue;

				if (centre.DistanceTo(other.GlobalPosition) <= radius)
					other.TakeDamage(9999, (other.GlobalPosition - centre).Normalized());
			}

			if (player != null && IsInstanceValid(player) && centre.DistanceTo(player.GlobalPosition) <= radius)
				player.KillByBlast(TranslationServer.Translate("DEATH_CAUSE_FlareBlast"));
		};
	}

	// Bodies are falling, so they should point where they are going. Below a
	// crawl there is no meaningful heading, so the last one is kept.
	private void FaceTravel()
	{
		// Its own steering, not the crowd's nudges, decides which way it faces:
		// a Bulwark's armour must not swing about as it jostles.
		Vector2 heading = Drift + knockback;
		if (heading.LengthSquared() > 100.0f)
			Rotation = heading.Angle();
	}

	/// <param name="impactDirection">Travel direction of whatever hit it, for knockback.</param>
	/// <param name="ignoreArmour">A dash or a Nova goes through plating. Only shots can be deflected.</param>
	/// <returns>True if this hit destroyed the body.</returns>
	public bool TakeDamage(int amount, Vector2 impactDirection = default) => TakeDamage(amount, impactDirection, false);

	public bool TakeDamage(int amount, Vector2 impactDirection, bool ignoreArmour)
	{
		if (destroyed)
			return false;

		if (!ignoreArmour && behaviour.Deflects(this, impactDirection))
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
		DrifterFace.StartleAround(this);
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

	/// <summary>
	/// A body left far behind is brought back in just outside the screen, rather
	/// than piling up somewhere the player never goes. Running away does not
	/// thin the field; it only reshuffles where the pressure comes from.
	/// </summary>
	private void Leash()
	{
		if (!HasWorld || GlobalPosition.DistanceSquaredTo(world.GlobalPosition) < Balance.LeashDistance * Balance.LeashDistance)
			return;

		if (!Arena.TryFindSpawnPoint(world.GlobalPosition, out Vector2 point))
			return;

		GlobalPosition = point;
		insideArena = Arena.Playable.HasPoint(point);
		Drift = Vector2.Zero;
		knockback = Vector2.Zero;
		LaunchIntoOrbit();
	}
}
