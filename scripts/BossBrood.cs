using Godot;

/// <summary>
/// The Brood — second boss. A DPS and movement check, not a pattern check —
/// see <see cref="BossCoil"/> for the other half.
///
/// It spawns Shards continuously for as long as it lives. There is no gap to
/// read and no telegraph to time; the only way to stop the flood is to keep
/// damage on the Brood itself while staying alive among what it has already
/// spawned, which means never standing still.
/// </summary>
public partial class BossBrood : Boss
{
	public BossBrood()
	{
		BossName = "THE BROOD";
		ArrivalLine = TranslationServer.Translate("BOSS_Brood_ARRIVAL");
		BossColor = new Color(0.58f, 0.82f, 0.4f);
		MaxHealth = 140;
	}

	[Export] public float ChaseSpeed { get; set; } = 62.0f;
	/// <summary>Seconds between spawns. Falls toward the floor as health drops.</summary>
	[Export] public float SpawnInterval { get; set; } = 1.6f;
	[Export] public float MinSpawnInterval { get; set; } = 0.55f;
	[Export] public float SpawnRadius { get; set; } = 90.0f;
	/// <summary>Local cap on live Shards from this boss, on top of the arena's own spawn cap.</summary>
	[Export] public int MaxBroodlings { get; set; } = 14;
	[Export] public PackedScene BodyScene { get; set; }

	private float spawnTimer;
	private int liveBroodlings;
	private Player world;

	protected override void OnBossReady()
	{
		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");
		world = GameManager.Of(this)?.GetNodeOrNull<Player>("player");
		spawnTimer = 1.0f;
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;

		// Chases, slowly — fast enough that ignoring it is not an option, slow
		// enough that it is always the Shards, not the Brood itself, that force
		// the player to move.
		if (world != null && IsInstanceValid(world))
		{
			Vector2 toWorld = world.GlobalPosition - GlobalPosition;
			if (toWorld.LengthSquared() > 4900f)
				Velocity = toWorld.Normalized() * ChaseSpeed;
			else
				Velocity = Vector2.Zero;
		}

		MoveAndSlide();

		spawnTimer -= step;
		if (spawnTimer <= 0f)
		{
			spawnTimer = Mathf.Lerp(MinSpawnInterval, SpawnInterval, HealthFraction);
			SpawnBroodling();
		}
	}

	private void SpawnBroodling()
	{
		if (BodyScene == null || liveBroodlings >= MaxBroodlings)
			return;

		// The arena's own cap still applies — a boss fight flooding past it would
		// undo the measurement in ROADMAP.md that said pooling wasn't needed yet.
		if (GetTree().GetNodeCountInGroup("bodies") >= 90)
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
