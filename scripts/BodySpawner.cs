using System.Collections.Generic;
using Godot;

/// <summary>
/// Keeps enemies coming for as long as the planet survives. There are no waves
/// and nothing to clear: pressure is a function of survival time, read from the
/// tables in <see cref="Balance"/> — how often something arrives, how many can
/// be alive at once, how fast they fall, and which kinds are in the mix.
///
/// Every so often a rush arrives: a tight pack from one direction, so the arena
/// has peaks and lulls rather than one flat drizzle.
/// </summary>
public partial class BodySpawner : Node
{
	[Export] public PackedScene BodyScene { get; set; }
	[Export] public int ShardPackSize { get; set; } = 3;

	[ExportGroup("Rushes")]
	[Export] public float FirstRushAt { get; set; } = 110.0f;
	[Export] public float RushGap { get; set; } = 26.0f;

	/// <summary>Current ramped base speed, read by living bodies each frame.</summary>
	public static float CurrentSpeed { get; private set; } = 100.0f;

	/// <summary>The ramp as a multiplier on its starting value, for scaling pull.</summary>
	public static float SpeedScale { get; private set; } = 1.0f;

	/// <summary>
	/// Share of normal spawning that runs right now: 1 in open play, the cycle's
	/// support share during a boss, 0 while a first-time boss is announced.
	/// Set by <see cref="GameManager"/>.
	/// </summary>
	public float Support { get; set; } = 1.0f;

	private float spawnTimer = 1.2f;
	private float nextRushAt;
	private float assistScale = 1.0f;
	private readonly HashSet<BodyKind> introduced = new();

	public override void _Ready()
	{
		// Assist Mode is an accessibility preference: a flat speed cut a player
		// turns on for themselves. It never touches spawn rate or contact radius.
		if (GameSettings.Instance?.AssistMode == true)
			assistScale = 0.8f;

		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");
		nextRushAt = FirstRushAt;
		UpdateSpeed(0f);
	}

	/// <summary>Enemy speed at a moment in the run, including the endless creep.</summary>
	public static float SpeedAt(float time)
	{
		Vector2[] keys = Balance.EnemySpeed;
		float speed = Balance.Ramp(keys, time);
		if (time > keys[^1].X)
			speed += Balance.EnemySpeedCreepPerMinute * (time - keys[^1].X) / 60f;
		return Mathf.Min(speed, Balance.EnemySpeedCeiling);
	}

	private void UpdateSpeed(float time)
	{
		CurrentSpeed = SpeedAt(time) * assistScale;
		SpeedScale = CurrentSpeed / Mathf.Max(Balance.EnemySpeed[0].Y * assistScale, 1f);
	}

	/// <summary>How many enemies may be alive right now.</summary>
	public int Capacity(float time) =>
		Mathf.Max(1, Mathf.RoundToInt(Balance.Ramp(Balance.MaxAlive, time) * Mathf.Max(Support, 0.35f)));

	public override void _Process(double delta)
	{
		float time = RunState.ElapsedSeconds;
		UpdateSpeed(time);

		if (BodyScene == null || Support <= 0f)
			return;

		int alive = GetTree().GetNodeCountInGroup("bodies");
		int capacity = Capacity(time);

		if (time >= nextRushAt)
		{
			nextRushAt = time + RushGap * RunState.Rng.RandfRange(0.8f, 1.2f);
			if (Support >= 1f)
				SpawnRush(time, capacity + 6 - alive);
		}

		spawnTimer -= (float)delta * Support;
		if (spawnTimer > 0f)
			return;

		spawnTimer = Balance.Ramp(Balance.SpawnInterval, time);
		if (alive >= capacity)
			return;

		BodyKind kind = PickKind(time);
		Vector2? origin = FindOrigin();
		if (origin == null)
			return;

		// Shards are only threatening in numbers, so they arrive together.
		// Drifters come in twos and threes after the opening, so the screen
		// fills sooner without anything new to learn.
		int group = kind switch
		{
			BodyKind.Shard => time < 120f ? 2 : ShardPackSize,
			BodyKind.Drifter => time < 20f ? 1 : time < 60f ? 2 : 3,
			_ => 1
		};
		int count = Mathf.Min(group, capacity - alive);
		for (int i = 0; i < count; i++)
			SpawnOne(kind, origin.Value + Jitter(90));
	}

	/// <summary>A pack from one direction. It may briefly overfill the cap; that is the point.</summary>
	private void SpawnRush(float time, int room)
	{
		int size = Mathf.Min(Mathf.RoundToInt(Mathf.Lerp(4f, 10f, Mathf.Clamp((time - FirstRushAt) / 900f, 0f, 1f))), room);
		Vector2? origin = FindOrigin();
		if (size <= 0 || origin == null)
			return;

		bool swarm = RunState.Rng.Randf() < 0.5f;
		for (int i = 0; i < size; i++)
			SpawnOne(swarm ? BodyKind.Shard : BodyKind.Drifter, origin.Value + Jitter(150));
	}

	private Vector2? FindOrigin()
	{
		var player = GameManager.Of(this)?.GetNodeOrNull<Node2D>("player");
		Vector2 from = player?.GlobalPosition ?? Arena.Centre;
		return Arena.TryFindSpawnPoint(from, out Vector2 point) ? point : null;
	}

	private static Vector2 Jitter(int range) =>
		new(RunState.Rng.RandiRange(-range, range), RunState.Rng.RandiRange(-range, range));

	/// <summary>
	/// Kinds join the mix over time. The first of each arrives alone, so it can
	/// be noticed. After that, weights are cumulative bands over one roll, and
	/// anything not claimed falls through to a Drifter.
	///
	/// Once the roster is complete the dangerous bands keep widening up to
	/// <see cref="Balance.CompositionPeakAt"/>: a late run is not the same run
	/// with more in it, it is a run made of worse things.
	/// </summary>
	private BodyKind PickKind(float time)
	{
		foreach ((BodyKind kind, float at) in Roster)
		{
			if (time >= at && introduced.Add(kind))
				return kind;
		}

		if (time < Balance.ShardAt)
			return BodyKind.Drifter;

		float bias = Mathf.Clamp((time - Balance.FlareAt) / Mathf.Max(Balance.CompositionPeakAt - Balance.FlareAt, 1f), 0f, 1f);
		float dangerous = Mathf.Lerp(BaseDangerousShare, PeakDangerousShare, bias);
		float shardShare = Mathf.Lerp(BaseShardShare, PeakShardShare, bias);
		float widen = dangerous / BaseDangerousShare;

		float roll = RunState.Rng.Randf();
		float band = 0f;

		if (time >= Balance.FlareAt && roll < (band += 0.10f * widen))
			return BodyKind.Flare;
		if (time >= Balance.SatelliteAt && roll < (band += 0.10f * widen))
			return BodyKind.Satellite;
		if (time >= Balance.BulwarkAt && roll < (band += 0.12f * widen))
			return BodyKind.Bulwark;
		if (time >= Balance.FractureAt && roll < (band += 0.13f * widen))
			return BodyKind.Fracture;
		if (time >= Balance.PlanetoidAt && roll < (band += 0.13f * widen))
			return BodyKind.Planetoid;
		if (roll < band + shardShare)
			return BodyKind.Shard;

		return BodyKind.Drifter;
	}

	private static readonly (BodyKind, float)[] Roster =
	{
		(BodyKind.Shard, Balance.ShardAt),
		(BodyKind.Planetoid, Balance.PlanetoidAt),
		(BodyKind.Fracture, Balance.FractureAt),
		(BodyKind.Satellite, Balance.SatelliteAt),
		(BodyKind.Bulwark, Balance.BulwarkAt),
		(BodyKind.Flare, Balance.FlareAt)
	};

	// The five dangerous bands sum to this before any escalation, and to the
	// peak share once composition has shifted as far as it goes. Peak plus the
	// peak shard share comes to exactly 1.0, so drifters run out precisely when
	// the mix is meant to have stopped being forgiving.
	private const float BaseDangerousShare = 0.58f;
	private const float PeakDangerousShare = 0.80f;
	private const float BaseShardShare = 0.26f;
	private const float PeakShardShare = 0.20f;

	private void SpawnOne(BodyKind kind, Vector2 position)
	{
		if (GetTree().GetNodeCountInGroup("bodies") >= Body.HardCap)
			return;
		if (BodyScene.Instantiate() is not Body body)
			return;

		body.Configure(kind);
		body.GlobalPosition = Arena.ClampToPlayable(position, 30f);
		GameManager.Spawn(this, body);
	}
}
