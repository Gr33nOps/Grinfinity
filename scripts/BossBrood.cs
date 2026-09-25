using Godot;

/// <summary>
/// The Brood — second boss. A DPS and movement check, not a pattern check —
/// see <see cref="BossCoil"/> for the other half.
///
/// It floods the arena with Shards in bursts that grow as it gets hurt, and
/// every few seconds it stops, glows, and lunges at where the planet was. The
/// lunge is the part that has to be read: sidestep or dash through it. The
/// Shards are the part that punishes standing still to do so.
/// </summary>
public partial class BossBrood : Boss
{
	private enum Mode { Chasing, WindingUp, Lunging, Recovering }

	public BossBrood()
	{
		BossName = "THE BROOD";
		ArrivalLine = TranslationServer.Translate("BOSS_Brood_ARRIVAL");
		BossColor = new Color(0.58f, 0.82f, 0.4f);
		MaxHealth = 1100;
	}

	[Export] public float ChaseSpeed { get; set; } = 90.0f;
	/// <summary>Seconds between bursts of Shards. Falls toward the floor as health drops.</summary>
	[Export] public float SpawnInterval { get; set; } = 2.2f;
	[Export] public float MinSpawnInterval { get; set; } = 1.1f;
	[Export] public float SpawnRadius { get; set; } = 90.0f;
	/// <summary>Local cap on live Shards from this boss, on top of the arena's own spawn cap.</summary>
	[Export] public int MaxBroodlings { get; set; } = 18;

	/// <summary>Seconds between lunges, shrinking toward the floor as health drops.</summary>
	[Export] public float LungeInterval { get; set; } = 6.5f;
	[Export] public float MinLungeInterval { get; set; } = 4.2f;
	/// <summary>How long it stops and glows before a lunge. Long enough to see and react to.</summary>
	[Export] public float LungeWindup { get; set; } = 0.95f;
	[Export] public float LungeSpeed { get; set; } = 600.0f;
	[Export] public float LungeTime { get; set; } = 0.55f;
	[Export] public float LungeRecovery { get; set; } = 0.7f;
	[Export] public PackedScene BodyScene { get; set; }

	private float spawnTimer;
	private float lungeTimer;
	private float modeLeft;
	private Mode mode = Mode.Chasing;
	private Vector2 lungeDirection;
	private int liveBroodlings;
	private Player world;

	protected override float Size => 1.3f;

	protected override void OnBossReady()
	{
		// Shards are born just outside the bigger body, not inside it.
		SpawnRadius *= Size;
		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");
		world = World;
		spawnTimer = 1.0f;
		lungeTimer = 3.5f;
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		bool hasWorld = world != null && IsInstanceValid(world);
		Vector2 toWorld = hasWorld ? world.GlobalPosition - GlobalPosition : Vector2.Zero;

		switch (mode)
		{
			case Mode.Chasing:
				Velocity = toWorld.LengthSquared() > 4900f ? toWorld.Normalized() * ChaseSpeed : Vector2.Zero;
				lungeTimer -= step;
				if (lungeTimer <= 0f && hasWorld)
				{
					mode = Mode.WindingUp;
					modeLeft = LungeWindup;
				}
				Windup = 0f;
				break;

			case Mode.WindingUp:
				// Stops dead and glows brighter until it goes: the tell.
				Velocity = Vector2.Zero;
				modeLeft -= step;
				Windup = 1f - modeLeft / LungeWindup;
				if (modeLeft <= 0f)
				{
					// Aimed at where the planet is at the end of the tell, not
					// where it will be: moving during the glow is the way out.
					lungeDirection = toWorld.LengthSquared() > 1f ? toWorld.Normalized() : Vector2.Right;
					mode = Mode.Lunging;
					modeLeft = LungeTime;
				}
				break;

			case Mode.Lunging:
				Velocity = lungeDirection * LungeSpeed;
				Windup = 1f;
				modeLeft -= step;
				if (modeLeft <= 0f)
				{
					mode = Mode.Recovering;
					modeLeft = LungeRecovery;
				}
				break;

			case Mode.Recovering:
				Velocity = Velocity.MoveToward(Vector2.Zero, LungeSpeed * 3f * step);
				Windup = 0f;
				modeLeft -= step;
				if (modeLeft <= 0f)
				{
					mode = Mode.Chasing;
					lungeTimer = Mathf.Lerp(MinLungeInterval, LungeInterval, HealthFraction) * CycleTempo;
				}
				break;
		}

		MoveAndSlide();
		GlobalPosition = Arena.ClampToPlayable(GlobalPosition, Arena.RadiusOf(this));

		spawnTimer -= step;
		if (spawnTimer <= 0f)
		{
			spawnTimer = Mathf.Lerp(MinSpawnInterval, SpawnInterval, HealthFraction) * CycleTempo;
			// Two at a time, three once it is below half health.
			int burst = HealthFraction < 0.5f ? 3 : 2;
			for (int i = 0; i < burst; i++)
				SpawnBroodling();
		}
	}

	private void SpawnBroodling()
	{
		if (BodyScene == null || liveBroodlings >= MaxBroodlings)
			return;

		// The arena's own ceiling still applies on top of the Brood's.
		if (GetTree().GetNodeCountInGroup("bodies") >= Body.HardCap)
			return;

		if (BodyScene.Instantiate() is not Body shard)
			return;

		shard.Configure(BodyKind.Shard);
		shard.GlobalPosition = GlobalPosition + Vector2.FromAngle(RunState.Rng.Randf() * Mathf.Tau) * SpawnRadius;

		liveBroodlings++;
		shard.TreeExited += () => liveBroodlings--;

		GameManager.Spawn(this, shard);
	}
}
