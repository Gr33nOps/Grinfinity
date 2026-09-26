using System.Collections.Generic;
using Godot;

/// <summary>
/// Keeps enemies coming for as long as the planet survives. There are no waves
/// and nothing to clear: pressure is a function of survival time, read from the
/// tables in <see cref="Balance"/> — how often something arrives, how many can
/// be alive at once, how fast they fall, and which kinds are in the mix.
///
/// Every twenty seconds or so a rush arrives in one of five shapes (pack,
/// pincer, ring, wall, escort), so the arena has peaks and lulls and the
/// same situation rarely repeats.
/// </summary>
public partial class BodySpawner : Node
{
	[Export] public PackedScene BodyScene { get; set; }
	[Export] public int ShardPackSize { get; set; } = 3;

	[ExportGroup("Rushes")]
	[Export] public float FirstRushAt { get; set; } = 45.0f;
	[Export] public float RushGap { get; set; } = 20.0f;

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
			BodyKind.Shard => time < 90f ? 2 : ShardPackSize,
			BodyKind.Drifter => time < 8f ? 1 : time < 40f ? 2 : 3,
			_ => 1
		};
		int count = Mathf.Min(group, capacity - alive);
		for (int i = 0; i < count; i++)
			SpawnOne(kind, origin.Value + Jitter(90));
	}

	/// <summary>The shapes a rush can take. Rotated so the same one never comes twice running.</summary>
	private enum Formation { Pack, Pincer, Ring, Wall, Escort }

	private Formation lastFormation = Formation.Escort;

	/// <summary>
	/// A burst of enemies in a recognisable shape. It may briefly overfill the
	/// cap; that is the point. The shapes ask different things of the player:
	/// a pack is shot down, a pincer has to be split, a ring has to be broken
	/// out of, a wall has to be dodged or dashed through, and an escort has a
	/// tough one in the middle that the small ones protect.
	/// </summary>
	private void SpawnRush(float time, int room)
	{
		int size = Mathf.Min(Mathf.RoundToInt(Mathf.Lerp(5f, 12f, Mathf.Clamp((time - FirstRushAt) / 840f, 0f, 1f))), room);
		var player = GameManager.Of(this)?.GetNodeOrNull<Node2D>("player");
		if (size <= 0 || player == null)
			return;

		Formation formation = PickFormation(time);
		lastFormation = formation;
		Vector2 here = player.GlobalPosition;

		switch (formation)
		{
			case Formation.Pack:
			{
				if (FindOrigin() is not Vector2 origin)
					return;
				BodyKind kind = RunState.Rng.Randf() < 0.5f ? BodyKind.Shard : BodyKind.Drifter;
				for (int i = 0; i < size; i++)
					SpawnOne(kind, origin + Jitter(150));
				break;
			}

			case Formation.Pincer:
			{
				// Two halves from opposite sides of the screen, arriving together.
				Vector2 axis = RunState.Rng.Randf() < 0.6f ? Vector2.Right : Vector2.Down;
				float reach = (axis == Vector2.Right ? Arena.View.Size.X : Arena.View.Size.Y) * 0.5f + Balance.SpawnBeyondView + 120f;
				foreach (float side in new[] { -1f, 1f })
				{
					Vector2 at = here + axis * side * reach;
					if (!Arena.Playable.Grow(-40f).HasPoint(at))
						continue;
					for (int i = 0; i < size / 2 + 1; i++)
						SpawnOne(i % 3 == 0 ? BodyKind.Shard : BodyKind.Drifter, at + Jitter(120));
				}
				break;
			}

			case Formation.Ring:
			{
				// All the way round, just off screen, closing in together.
				float radius = Arena.View.Size.Length() * 0.5f + 160f;
				int count = size + 3;
				float turn = RunState.Rng.Randf() * Mathf.Tau;
				for (int i = 0; i < count; i++)
				{
					Vector2 at = here + Vector2.FromAngle(turn + Mathf.Tau * i / count) * radius;
					if (!Arena.Playable.Grow(-40f).HasPoint(at))
						continue;
					bool tough = time >= Balance.FractureAt && i % 4 == 0;
					SpawnOne(tough ? BodyKind.Fracture : BodyKind.Drifter, at);
				}
				break;
			}

			case Formation.Wall:
			{
				// A line of Shards across one side of the screen, sweeping in.
				Rect2 edge = Arena.View.Grow(Balance.SpawnBeyondView + 100f);
				int count = size + 2;
				for (int attempt = 0; attempt < 4; attempt++)
				{
					int sideIndex = RunState.Rng.RandiRange(0, 3);
					bool horizontal = sideIndex < 2;
					Vector2 start = sideIndex switch
					{
						0 => edge.Position,
						1 => new Vector2(edge.Position.X, edge.End.Y),
						2 => edge.Position,
						_ => new Vector2(edge.End.X, edge.Position.Y)
					};
					Vector2 step = horizontal ? new Vector2(edge.Size.X / (count - 1), 0f) : new Vector2(0f, edge.Size.Y / (count - 1));
					Vector2 middle = start + step * (count - 1) * 0.5f;
					if (!Arena.Playable.Grow(-40f).HasPoint(middle))
						continue;
					for (int i = 0; i < count; i++)
					{
						Vector2 at = start + step * i;
						if (Arena.Playable.Grow(-40f).HasPoint(at))
							SpawnOne(BodyKind.Shard, at);
					}
					break;
				}
				break;
			}

			case Formation.Escort:
			{
				// One tough enemy with a guard of Shards around it.
				if (FindOrigin() is not Vector2 origin)
					return;
				BodyKind heavy = time >= Balance.BulwarkAt && RunState.Rng.Randf() < 0.5f ? BodyKind.Bulwark : BodyKind.Planetoid;
				SpawnOne(heavy, origin);
				for (int i = 0; i < size - 1; i++)
					SpawnOne(BodyKind.Shard, origin + Vector2.FromAngle(Mathf.Tau * i / (size - 1)) * 130f);
				break;
			}
		}
	}

	/// <summary>A random shape that is unlocked by now and was not the last one.</summary>
	private Formation PickFormation(float time)
	{
		var options = new List<Formation> { Formation.Pack };
		if (time >= 60f) options.Add(Formation.Pincer);
		if (time >= 90f) options.Add(Formation.Ring);
		if (time >= Balance.ShardAt) options.Add(Formation.Wall);
		if (time >= Balance.PlanetoidAt + 45f) options.Add(Formation.Escort);
		if (options.Count > 1)
			options.Remove(lastFormation);
		return options[RunState.Rng.RandiRange(0, options.Count - 1)];
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

		if (time >= Balance.FlareAt && roll < (band += 0.12f * widen))
			return BodyKind.Flare;
		if (time >= Balance.BulwarkAt && roll < (band += 0.14f * widen))
			return BodyKind.Bulwark;
		if (time >= Balance.FractureAt && roll < (band += 0.16f * widen))
			return BodyKind.Fracture;
		if (time >= Balance.PlanetoidAt && roll < (band += 0.16f * widen))
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
		(BodyKind.Bulwark, Balance.BulwarkAt),
		(BodyKind.Flare, Balance.FlareAt)
	};

	// The four dangerous bands sum to this before any escalation, and to the
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
