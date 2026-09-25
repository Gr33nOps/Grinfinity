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
		MaxHealth = 660;
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

	protected override float Size => 1.3f;

	protected override void OnBossReady()
	{
		// Shards are born just outside the bigger body, not inside it.
		SpawnRadius *= Size;
		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");
		world = World;
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
        Windup=Mathf.Clamp(1-spawnTimer/.45f,0,1);
		if (spawnTimer <= 0f)
		{
			spawnTimer = Mathf.Lerp(MinSpawnInterval, SpawnInterval, HealthFraction) * CycleTempo;
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
