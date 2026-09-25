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
	[Export] public float WaveSizeGrowth { get; set; } = 1.5f;
	/// <summary>
	/// Ceiling on pack size. Deliberately low enough to keep a wave punchy: past
	/// this the run escalates by what is in the pack, not by how long it takes
	/// to grind through it. A wave nobody can finish is not a harder wave, it is
	/// a longer one.
	/// </summary>
	[Export] public int MaxWaveSize { get; set; } = 28;
	/// <summary>Seconds of quiet between a pack being cleared and the next arriving.</summary>
	[Export] public float CalmDuration { get; set; } = 1.8f;

	// Progression follows completed waves, never time spent struggling.
	[Export] public float StartSpeed { get; set; } = 95.0f;
	[Export] public float MaxSpeed { get; set; } = 220.0f;
	[Export] public float StartSpawnInterval { get; set; } = 1.35f;
	[Export] public int MaxBodyCount { get; set; } = 18;
	[Export] public float SpawnMargin { get; set; } = 100.0f;
	[Export] public PackedScene BodyScene { get; set; }

	[ExportGroup("Bestiary")]
	// One new kind every wave or two. Gated on waves rather than the clock so
	// the escalation reads as progress the player made rather than time that
	// passed — clearing fast now earns the next threat sooner, and a wave the
	// player struggled with does not stack a new kind on top of it.
	[Export] public int ShardUnlockWave { get; set; } = 3;
	[Export] public int PlanetoidUnlockWave { get; set; } = 4;
	[Export] public int FractureUnlockWave { get; set; } = 6;
	[Export] public int BulwarkUnlockWave { get; set; } = 10;
	[Export] public int SatelliteUnlockWave { get; set; } = 8;
	[Export] public int FlareUnlockWave { get; set; } = 12;
	[Export] public int ShardPackSize { get; set; } = 3;

	/// <summary>
	/// Wave by which the mix has shifted as far toward the dangerous kinds as it
	/// ever will. Past the last unlock the roster stops growing, so composition
	/// is the only thing left that can keep escalating without the run turning
	/// into a pure stat check.
	/// </summary>
	[Export] public int CompositionPeakWave { get; set; } = 26;

	/// <summary>Current ramped base speed, read by living bodies each frame.</summary>
	public static float CurrentSpeed { get; private set; } = 100.0f;

	/// <summary>The ramp as a multiplier on its starting value, for scaling pull.</summary>
	public static float SpeedScale { get; private set; } = 1.0f;

	private Timer spawnTimer;
	private float enemySpeed;

	// --- Waves --------------------------------------------------------------
	/// <summary>1-based. The pack currently arriving, or the one just cleared.</summary>
	public int WaveNumber { get; private set; } = 1;
	/// <summary>True during the quiet window between packs.</summary>
	public bool InCalm { get; private set; }
	public float CalmTimeLeft => Mathf.Max(calmTimer, 0f);
	public int Remaining => Mathf.Max(0, waveBudget - spawnedThisWave) + GetTree().GetNodeCountInGroup("bodies");
	public void FinishCalm() { if (InCalm) calmTimer = Mathf.Min(calmTimer, 0.8f); }

	private int waveBudget;
	private int spawnedThisWave;
	private float calmTimer;

	public override void _Ready()
	{
		// The two-stage ramp below is this spawner's own escalation curve, so
		// there is nothing to scale here — Difficulties owns contact radius and
		// deliberately not speed or cadence, or both would escalate twice.

		// Assist Mode is an accessibility preference, not a difficulty tier: a
		// flat speed cut a player turns on for themselves, independent of the
		// curve everyone else plays. It never touches spawn rate or contact
		// radius.
		if (GameSettings.Instance?.AssistMode == true)
		{
			StartSpeed *= 0.8f;
			MaxSpeed *= 0.8f;
		}


		enemySpeed = StartSpeed;
		CurrentSpeed = StartSpeed;
		SpeedScale = 1.0f;
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
		// A boss is its own encounter, and wave accounting stands down for it the
		// same way trash spawning does. If one arrives mid-break the break ends
		// with it: leaving the calm frozen left the upgrade prompt sitting over
		// the boss fight for its whole duration, since the timer that closes the
		// window had stopped running.
		if (GameManager.Of(this)?.BossActive == true)
		{
			if (InCalm)
				BeginNextWave();
			return;
		}

		if (InCalm)
		{
			calmTimer -= delta;
			if (calmTimer <= 0f)
				BeginNextWave();
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

	private void BeginNextWave()
	{
		InCalm = false;
		WaveNumber++;
		waveBudget = BudgetFor(WaveNumber);
		spawnedThisWave = 0;
		EmitSignal(SignalName.WaveStarted, WaveNumber);
	}

    public override void _Process(double delta)
    {
        UpdateWaves((float)delta);
        // Progress, not time spent struggling, drives pressure. No mass penalty.
        float early=Mathf.Clamp((WaveNumber-1)/11f,0,1);
        float late=Mathf.Clamp((WaveNumber-12)/18f,0,1);
        enemySpeed=Mathf.Lerp(StartSpeed,MaxSpeed*(160f/220f),early)+(MaxSpeed-MaxSpeed*(160f/220f))*late;
        CurrentSpeed=enemySpeed;SpeedScale=enemySpeed/Mathf.Max(StartSpeed,1);
        spawnTimer.WaitTime=Mathf.Lerp(StartSpawnInterval,.72f,early)-.22f*late;
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
		if (GameManager.Of(this)?.BossActive == true || GameManager.Of(this)?.BossDue == true)
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
			int count = Mathf.Min(WaveNumber<=4?2:ShardPackSize, Mathf.Min(waveBudget-spawnedThisWave,MaxBodyCount-GetTree().GetNodeCountInGroup("bodies")));
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
	/// Introduces kinds wave by wave so the opening stays readable and each new
	/// threat is noticeable when it shows up. Weights are cumulative bands over
	/// a single roll; anything not claimed by a band falls through to a Drifter.
	///
	/// Once the roster stops growing the bands keep widening instead, up to
	/// <see cref="CompositionPeakWave"/> — a late wave is not the same wave with
	/// more bodies in it, it is a wave made of worse ones.
	/// </summary>
	private BodyKind PickKind()
	{
		if(spawnedThisWave==0)
        {
            if(WaveNumber==ShardUnlockWave)return BodyKind.Shard;
            if(WaveNumber==PlanetoidUnlockWave)return BodyKind.Planetoid;
            if(WaveNumber==FractureUnlockWave)return BodyKind.Fracture;
            if(WaveNumber==SatelliteUnlockWave)return BodyKind.Satellite;
            if(WaveNumber==BulwarkUnlockWave)return BodyKind.Bulwark;
            if(WaveNumber==FlareUnlockWave)return BodyKind.Flare;
        }
        if (WaveNumber < ShardUnlockWave)
			return BodyKind.Drifter;

		// 0 at the last unlock, 1 by the peak. The dangerous share grows, the
		// shard share shrinks, and drifters are what is left — which by the peak
		// is nothing. Scaled to a target total rather than multiplied freely:
		// bands that sum past 1.0 would mean the last few kinds could never be
		// rolled at all, quietly deleting Planetoids and Shards from a long run
		// instead of escalating it.
		float bias = Mathf.Clamp(
			(WaveNumber - FlareUnlockWave) / (float)Mathf.Max(CompositionPeakWave - FlareUnlockWave, 1),
			0f, 1f);

		float dangerous = Mathf.Lerp(BaseDangerousShare, PeakDangerousShare, bias);
		float shardShare = Mathf.Lerp(BaseShardShare, PeakShardShare, bias);
		float widen = dangerous / BaseDangerousShare;

		float roll = RunState.Rng.Randf();
		float band = 0f;

		if (Unlocked(FlareUnlockWave) && roll < (band += 0.10f * widen))
			return BodyKind.Flare;

		if (Unlocked(SatelliteUnlockWave) && roll < (band += 0.10f * widen))
			return BodyKind.Satellite;

		if (Unlocked(BulwarkUnlockWave) && roll < (band += 0.12f * widen))
			return BodyKind.Bulwark;

		if (Unlocked(FractureUnlockWave) && roll < (band += 0.13f * widen))
			return BodyKind.Fracture;

		if (Unlocked(PlanetoidUnlockWave) && roll < (band += 0.13f * widen))
			return BodyKind.Planetoid;

		if (roll < band + shardShare)
			return BodyKind.Shard;

		return BodyKind.Drifter;
	}

	// The five dangerous bands sum to this before any escalation, and to the
	// peak share once composition has shifted as far as it goes. Peak plus the
	// peak shard share comes to exactly 1.0, so drifters run out precisely when
	// the mix is meant to have stopped being forgiving — never sooner.
	private const float BaseDangerousShare = 0.58f;
	private const float PeakDangerousShare = 0.80f;
	private const float BaseShardShare = 0.26f;
	private const float PeakShardShare = 0.20f;

	private bool Unlocked(int wave) => WaveNumber >= wave;

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
