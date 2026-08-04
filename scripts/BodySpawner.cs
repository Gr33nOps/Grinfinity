using Godot;

public partial class BodySpawner : Node
{
	/// <summary>
	/// Raised the moment a wave's pack is cleared and the arena has gone quiet.
	/// The calm window that follows is the only time upgrades are on offer.
	/// </summary>
	[Signal] public delegate void WaveClearedEventHandler(int waveNumber);

	/// <summary>Raised when the next pack starts arriving and the window shuts.</summary>
	[Signal] public delegate void WaveStartedEventHandler(int waveNumber);

	[ExportGroup("Waves")]
	/// <summary>Bodies in the opening pack. Small enough to be read, not survived.</summary>
	[Export] public int StartWaveSize { get; set; } = 6;
	/// <summary>Extra bodies added to the pack per wave cleared.</summary>
	[Export] public float WaveSizeGrowth { get; set; } = 2.2f;
	/// <summary>Ceiling on pack size, so a long run escalates by composition rather than by sheer count.</summary>
	[Export] public int MaxWaveSize { get; set; } = 46;
	/// <summary>Seconds of quiet between a pack being cleared and the next arriving.</summary>
	[Export] public float CalmDuration { get; set; } = 7.0f;

	// Tuned for a 2-6 minute orbit: the primary ramp tops out around 2:00. See
	// "Late-game escalation" below for what happens to an orbit that outlasts it.
	[Export] public float StartSpeed { get; set; } = 110.0f;
	[Export] public float MaxSpeed { get; set; } = 215.0f;
	[Export] public float SpeedIncreasePerSecond { get; set; } = 0.9f;
	[Export] public float StartSpawnInterval { get; set; } = 1.35f;
	[Export] public float MinSpawnInterval { get; set; } = 0.38f;
	[Export] public int MaxBodyCount { get; set; } = 90;
	[Export] public float SpawnMargin { get; set; } = 100.0f;
	[Export] public PackedScene BodyScene { get; set; }

	[ExportGroup("Late-game escalation")]
	// The primary ramp above used to just stop at 2:00 and hold dead flat for
	// however much longer the player survived — a bot playtest sat through
	// minutes of literally nothing changing. This second, much slower ramp
	// picks up exactly where the first one caps, so a long Endless Orbit keeps
	// quietly tightening instead of going static. It has its own ceiling too —
	// unbounded escalation would just be a different way of going nowhere,
	// this time by becoming unwinnable instead of boring.
	/// <summary>Final speed ceiling as a multiplier on MaxSpeed, reached slowly after the primary ramp caps.</summary>
	[Export] public float LateGameSpeedMultiplier { get; set; } = 1.35f;
	/// <summary>Final spawn-interval floor as a multiplier on MinSpawnInterval. Under 1 spawns faster.</summary>
	[Export] public float LateGameSpawnMultiplier { get; set; } = 0.7f;
	/// <summary>Seconds after the primary ramp caps before the late-game ceiling is fully reached.</summary>
	[Export] public float LateGameRampDuration { get; set; } = 240.0f;

	[ExportGroup("Bestiary")]
	// One new kind roughly every 20 seconds. Each arrival is meant to be
	// noticeable, and each one teaches something the previous ones did not.
	[Export] public float ShardUnlockTime { get; set; } = 18.0f;
	[Export] public float PlanetoidUnlockTime { get; set; } = 40.0f;
	[Export] public float FractureUnlockTime { get; set; } = 62.0f;
	[Export] public float BulwarkUnlockTime { get; set; } = 85.0f;
	[Export] public float SatelliteUnlockTime { get; set; } = 108.0f;
	[Export] public float FlareUnlockTime { get; set; } = 130.0f;
	[Export] public int ShardPackSize { get; set; } = 4;

	[ExportGroup("Mass response")]
	/// <summary>Spawn interval multiplier at full world mass. Under 1 means faster.</summary>
	[Export] public float HeavySpawnRate { get; set; } = 0.78f;

	/// <summary>Current ramped base speed, read by living bodies each frame.</summary>
	public static float CurrentSpeed { get; private set; } = 100.0f;

	/// <summary>The ramp as a multiplier on its starting value, for scaling pull.</summary>
	public static float SpeedScale { get; private set; } = 1.0f;

	private Timer spawnTimer;
	private RunState run;
	private float enemySpeed;
	private float elapsed;
	private float lateGameMaxSpeed;
	private float lateGameMinSpawnInterval;
	private float lateGameSpeedIncreasePerSecond;

	// --- Waves --------------------------------------------------------------
	/// <summary>1-based. The pack currently arriving, or the one just cleared.</summary>
	public int WaveNumber { get; private set; } = 1;
	/// <summary>True during the quiet window between packs.</summary>
	public bool InCalm { get; private set; }

	private int waveBudget;
	private int spawnedThisWave;
	private float calmTimer;

	public override void _Ready()
	{
		// Difficulty scales the ramp and the spawn cadence, and only those —
		// never player damage. A fresh spawner is made every orbit, so scaling
		// the exported defaults in place here cannot compound across restarts.
		Difficulties.Profile difficulty = Loadout.DifficultyProfile;
		StartSpeed *= difficulty.SpeedMultiplier;
		MaxSpeed *= difficulty.SpeedMultiplier;
		StartSpawnInterval *= difficulty.SpawnIntervalMultiplier;
		MinSpawnInterval *= difficulty.SpawnIntervalMultiplier;

		// Assist Mode is a separate, gentler cut that stacks on top of whatever
		// Difficulty already did — an accessibility preference, not a fourth
		// difficulty tier, so it never touches spawn rate or contact radius.
		if (GameSettings.Instance?.AssistMode == true)
		{
			StartSpeed *= 0.8f;
			MaxSpeed *= 0.8f;
		}

		lateGameMaxSpeed = MaxSpeed * LateGameSpeedMultiplier;
		lateGameMinSpawnInterval = MinSpawnInterval * LateGameSpawnMultiplier;
		lateGameSpeedIncreasePerSecond = Mathf.Max(lateGameMaxSpeed - MaxSpeed, 0f) / Mathf.Max(LateGameRampDuration, 1f);

		enemySpeed = StartSpeed;
		CurrentSpeed = StartSpeed;
		SpeedScale = 1.0f;
		elapsed = 0f;
		run = GameManager.Of(this)?.Run;
		BodyScene ??= GD.Load<PackedScene>("res://scenes/body.tscn");
		waveBudget = StartWaveSize;
		SetupSpawnTimer();
	}

	/// <summary>How many bodies the given wave sends, before the cap.</summary>
	private int BudgetFor(int wave) =>
		Mathf.Min(Mathf.RoundToInt(StartWaveSize + WaveSizeGrowth * (wave - 1)), MaxWaveSize);

	/// <summary>
	/// A wave is over once its whole pack has been sent and none of it is left
	/// standing. Checked against the live group rather than a kill counter, so a
	/// body that leaves the field by any means still counts as dealt with.
	/// </summary>
	private void UpdateWaves(float delta)
	{
		// A boss is its own encounter. Wave accounting stands down for it, the
		// same way trash spawning already does.
		if (GameManager.Of(this)?.BossActive == true)
			return;

		if (InCalm)
		{
			calmTimer -= delta;
			if (calmTimer > 0f)
				return;

			InCalm = false;
			WaveNumber++;
			waveBudget = BudgetFor(WaveNumber);
			spawnedThisWave = 0;
			EmitSignal(SignalName.WaveStarted, WaveNumber);
			return;
		}

		if (spawnedThisWave < waveBudget)
			return;

		if (GetTree().GetNodeCountInGroup("bodies") > 0)
			return;

		InCalm = true;
		calmTimer = CalmDuration;
		EmitSignal(SignalName.WaveCleared, WaveNumber);
	}

	public override void _Process(double delta)
	{
		elapsed += (float)delta;
		UpdateWaves((float)delta);

		// The primary ramp reaches MaxSpeed around 2:00, tuned to feel like a
		// deliberate escalation. Once there, the far slower late-game ramp
		// takes over and keeps pushing toward its own, higher ceiling — an
		// orbit that outlasts the primary ramp keeps getting harder instead of
		// holding flat for however much longer it lasts.
		bool primaryRampDone = enemySpeed >= MaxSpeed;
		float target = primaryRampDone ? lateGameMaxSpeed : MaxSpeed;
		float rate = primaryRampDone ? lateGameSpeedIncreasePerSecond : SpeedIncreasePerSecond;
		enemySpeed = Mathf.Min(enemySpeed + rate * (float)delta, target);
		CurrentSpeed = enemySpeed;
		SpeedScale = enemySpeed / Mathf.Max(StartSpeed, 1f);

		// Spawn interval rides the same overall progress, from StartSpeed all
		// the way to the late-game ceiling, so it keeps tightening in lockstep
		// with speed across both ramps rather than flooring out on its own at 2:00.
		float overallRange = Mathf.Max(lateGameMaxSpeed - StartSpeed, 0.001f);
		float overallProgress = (enemySpeed - StartSpeed) / overallRange;
		float targetInterval = Mathf.Lerp(StartSpawnInterval, lateGameMinSpawnInterval, overallProgress);

		// A heavy world pulls harder, so it also draws more attention: mass
		// tightens the spawn interval on top of the time ramp.
		float massRate = Mathf.Lerp(1.0f, HeavySpawnRate, run?.MassNormalised ?? 0f);
		spawnTimer.WaitTime = targetInterval * massRate;
	}

	private void SetupSpawnTimer()
	{
		spawnTimer = new Timer
		{
			WaitTime = StartSpawnInterval,
			Autostart = true
		};
		spawnTimer.Timeout += OnSpawnTimeout;
		AddChild(spawnTimer);
	}

	private void OnSpawnTimeout()
	{
		if (BodyScene == null)
			return;

		// A boss fight is about the boss. Trash on top of it would only make the
		// safe gaps unreadable, which is the one thing The Coil teaches.
		if (GameManager.Of(this)?.BossActive == true)
			return;

		// The quiet between packs is the whole point of the wave break. Nothing
		// arrives during it, or there is no window to decide in.
		if (InCalm || spawnedThisWave >= waveBudget)
			return;

		if (GetTree().GetNodeCountInGroup("bodies") >= MaxBodyCount)
			return;

		BodyKind kind = PickKind();

		if (kind == BodyKind.Shard)
		{
			// Shards are only threatening in numbers, so they arrive together.
			// The whole pack counts against the budget, or a wave of shards
			// would be several times the size of any other.
			Vector2 origin = GetSpawnPosition();
			int count = Mathf.Min(ShardPackSize, waveBudget - spawnedThisWave);
			for (int i = 0; i < count; i++)
			{
				Vector2 jitter = new Vector2(RunState.Rng.RandiRange(-90, 90), RunState.Rng.RandiRange(-90, 90));
				SpawnOne(kind, origin + jitter);
				spawnedThisWave++;
			}
			return;
		}

		SpawnOne(kind, GetSpawnPosition());
		spawnedThisWave++;
	}

	/// <summary>
	/// Introduces kinds over time so the opening stays readable and each new
	/// threat is noticeable when it shows up. Weights are cumulative bands over
	/// a single roll; anything not claimed by a band falls through to a Drifter.
	/// </summary>
	private BodyKind PickKind()
	{
		if (elapsed < ShardUnlockTime)
			return BodyKind.Drifter;

		float roll = RunState.Rng.Randf();
		float band = 0f;

		if (Unlocked(FlareUnlockTime) && roll < (band += 0.10f))
			return BodyKind.Flare;

		if (Unlocked(SatelliteUnlockTime) && roll < (band += 0.10f))
			return BodyKind.Satellite;

		if (Unlocked(BulwarkUnlockTime) && roll < (band += 0.12f))
			return BodyKind.Bulwark;

		if (Unlocked(FractureUnlockTime) && roll < (band += 0.13f))
			return BodyKind.Fracture;

		if (Unlocked(PlanetoidUnlockTime) && roll < (band += 0.13f))
			return BodyKind.Planetoid;

		if (roll < band + 0.26f)
			return BodyKind.Shard;

		return BodyKind.Drifter;
	}

	private bool Unlocked(float at) => elapsed >= at;

	private void SpawnOne(BodyKind kind, Vector2 position)
	{
		if (BodyScene.Instantiate() is not Body body)
			return;

		body.Configure(kind);
		body.GlobalPosition = position;
		GameManager.Spawn(this, body);
	}

	private Vector2 GetSpawnPosition()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		int side = RunState.Rng.RandiRange(0, 3);
		return side switch
		{
			0 => new Vector2(RunState.Rng.RandiRange(0, (int)viewportSize.X), -SpawnMargin),
			1 => new Vector2(viewportSize.X + SpawnMargin, RunState.Rng.RandiRange(0, (int)viewportSize.Y)),
			2 => new Vector2(RunState.Rng.RandiRange(0, (int)viewportSize.X), viewportSize.Y + SpawnMargin),
			_ => new Vector2(-SpawnMargin, RunState.Rng.RandiRange(0, (int)viewportSize.Y))
		};
	}
}
