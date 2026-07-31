using Godot;

public partial class GameManager : Node2D
{
	[ExportGroup("Feel")]
	/// <summary>Time scale held during a hitstop. Low enough to read as a freeze.</summary>
	[Export] public float HitstopScale { get; set; } = 0.04f;
	[Export] public float LightKillHitstop { get; set; } = 0.045f;
	[Export] public float HeavyKillHitstop { get; set; } = 0.085f;
	[Export] public float DeathHitstop { get; set; } = 0.14f;
	[Export] public float LightKillTrauma { get; set; } = 0.17f;
	[Export] public float HeavyKillTrauma { get; set; } = 0.42f;
	[Export] public float DeathTrauma { get; set; } = 1.0f;

	[ExportGroup("Gravity")]
	[Export] public PackedScene DebrisScene { get; set; }
	[Export] public PackedScene BurstScene { get; set; }
	/// <summary>Ceiling on live motes, so a wipe cannot flood the arena.</summary>
	[Export] public int MaxDebris { get; set; } = 220;

	[ExportGroup("Pickups")]
	[Export] public PackedScene PowerUpScene { get; set; }
	/// <summary>Chance a killed body leaves a pickup. "Short, loud, frequent."</summary>
	[Export] public float PowerUpDropChance { get; set; } = 0.05f;
	/// <summary>Tougher bodies are likelier to drop, so a hard kill pays twice.</summary>
	[Export] public float HeavyDropBonus { get; set; } = 0.14f;
	/// <summary>Radius a Nuke clears. Generous — it is meant to feel like relief.</summary>
	[Export] public float NukeRadius { get; set; } = 1400.0f;

	[ExportGroup("Bosses")]
	[Export] public PackedScene BossScene { get; set; }
	/// <summary>Seconds into an orbit before The Coil arrives.</summary>
	[Export] public float BossTime { get; set; } = 180.0f;
	[Export] public int BossScoreBonus { get; set; } = 5000;
	/// <summary>Time scale held while a boss dies. Slow motion, not a freeze.</summary>
	[Export] public float BossKillSlowMo { get; set; } = 0.22f;
	[Export] public float BossKillSlowMoTime { get; set; } = 1.1f;

	private PauseMenu pauseMenu;
	private RunState run;
	private BodySpawner bodySpawner;
	private UIManager uiManager;
	private PlayerManager playerManager;
	private GameCamera gameCamera;
	private Node2D entities;
	private AudioStreamPlayer killSound;
	private AudioStreamPlayer streakSound;
	private AudioStreamPlayer absorbSound;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private bool isPaused = false;
	private bool isGameOver = false;
	private bool isDying = false;

	private bool hitstopActive = false;
	private ulong hitstopEndMsec = 0;

	private BossCoil boss;
	private bool bossSpawned;
	private Control bossBar;
	private ProgressBar bossHealth;

	public bool IsPaused => isPaused;

	/// <summary>True while a boss is on the field. The spawner stands down.</summary>
	public bool BossActive => boss != null && IsInstanceValid(boss);

	/// <summary>The live orbit: time, kills, streak, mass, moons and score.</summary>
	public RunState Run => run;

	/// <summary>Seconds survived so far. Read by the music layer and the spawner.</summary>
	public float RunTime => run?.SurvivalTime ?? 0f;

	// _EnterTree runs top-down, _Ready bottom-up. The player and everything under
	// it read mass during their own _Ready, which is *before* this node's, so the
	// group registration and RunState have to be in place by here.
	public override void _EnterTree()
	{
		AddToGroup("game_manager");
		run = AddPausableChild(new RunState());
	}

	public override void _Ready()
	{
		SetupComponents();
		ConnectSignals();
	}

	/// <summary>The game manager for the current scene, or null outside gameplay.</summary>
	public static GameManager Of(Node context)
	{
		return context.GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
	}

	private void SetupComponents()
	{
		pauseMenu = GetNode<PauseMenu>("PauseLayer/PauseMenu");
		entities = GetNode<Node2D>("Entities");
		gameCamera = GetNodeOrNull<GameCamera>("GameCamera");
		killSound = GetNode<AudioStreamPlayer>("KillSound");
		streakSound = GetNodeOrNull<AudioStreamPlayer>("StreakSound");
		absorbSound = GetNodeOrNull<AudioStreamPlayer>("AbsorbSound");
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("HoverSound");

		DebrisScene ??= GD.Load<PackedScene>("res://scenes/debris.tscn");
		BurstScene ??= GD.Load<PackedScene>("res://scenes/explosion.tscn");
		BossScene ??= GD.Load<PackedScene>("res://scenes/boss_coil.tscn");
		PowerUpScene ??= GD.Load<PackedScene>("res://scenes/power_up.tscn");

		bossBar = GetNodeOrNull<Control>("UI/BossBar");
		bossHealth = GetNodeOrNull<ProgressBar>("UI/BossBar/Health");
		if (bossBar != null)
			bossBar.Visible = false;

		bodySpawner = AddPausableChild(new BodySpawner());
		uiManager = AddPausableChild(new UIManager());
		playerManager = AddPausableChild(new PlayerManager());
	}

	// The game root runs with ProcessMode.Always so it can still read the pause
	// key while the tree is paused. Everything it owns must opt back out.
	private T AddPausableChild<T>(T node) where T : Node
	{
		node.ProcessMode = ProcessModeEnum.Pausable;
		AddChild(node);
		return node;
	}

	private void ConnectSignals()
	{
		pauseMenu.ResumeGame += OnResumeGame;
		pauseMenu.GiveUpGame += OnGiveUpGame;
		run.StreakChanged += OnStreakChanged;
	}

	/// <summary>Parents runtime-spawned nodes under the pausable entity container.</summary>
	public void AddEntity(Node entity)
	{
		entities.AddChild(entity);
	}

	/// <summary>
	/// Parents a runtime-spawned node (bullet, body, particle burst) under the
	/// pausable entity container so it pauses along with the rest of the game.
	/// Falls back to the current scene if no game manager is present.
	/// </summary>
	public static void Spawn(Node context, Node node)
	{
		if (context.GetTree().GetFirstNodeInGroup("game_manager") is GameManager manager)
		{
			manager.AddEntity(node);
			return;
		}

		Node fallback = context.GetTree().CurrentScene;
		if (fallback != null)
			fallback.AddChild(node);
		else
			node.QueueFree();
	}

	public override void _Input(InputEvent inputEvent)
	{
		// isDying covers the death freeze, before the recap has been handed over.
		if (isGameOver || isDying)
			return;

		if (inputEvent.IsActionPressed("pause"))
		{
			TogglePause();
		}
	}

	// Runs with ProcessMode.Always, so hitstop is measured against wall-clock
	// time: delta is scaled by Engine.TimeScale and would stretch with it.
	public override void _Process(double delta)
	{
		if (!isPaused && !isGameOver && !bossSpawned && RunTime >= BossTime)
			SpawnBoss();

		if (!hitstopActive)
			return;

		if (isPaused || isGameOver || Time.GetTicksMsec() >= hitstopEndMsec)
			EndHitstop();
	}

	private void SpawnBoss()
	{
		if (BossScene == null)
			return;

		bossSpawned = true;
		boss = BossScene.Instantiate<BossCoil>();

		// Arrives off to one side rather than on top of the player.
		Vector2 bounds = GetViewportRect().Size;
		boss.GlobalPosition = new Vector2(bounds.X * 0.5f, bounds.Y * 0.22f);

		boss.HealthChanged += OnBossHealthChanged;
		boss.Defeated += OnBossDefeated;
		AddEntity(boss);

		if (bossBar != null)
			bossBar.Visible = true;
		OnBossHealthChanged(1.0f);

		Shake(0.5f);
		PlayStreakSting(0.6f);
	}

	private void OnBossHealthChanged(float fraction)
	{
		if (bossHealth != null)
			bossHealth.Value = fraction * 100.0;
	}

	private void OnBossDefeated()
	{
		if (bossBar != null)
			bossBar.Visible = false;

		Vector2 at = IsInstanceValid(boss) ? boss.GlobalPosition : Vector2.Zero;
		boss = null;

		// Slow motion rather than a freeze: the payoff is watching it come apart.
		Hitstop(BossKillSlowMoTime, BossKillSlowMo);
		Shake(0.9f);
		SpawnBlast(at, 420.0f, new Color(0.86f, 0.72f, 1.0f));
		run?.AddBonus(BossScoreBonus);
		PlayStreakSting(0.45f);
	}

	public override void _ExitTree()
	{
		// Engine.TimeScale is global; a scene change mid-freeze must not leak it.
		if (hitstopActive)
			EndHitstop();
	}

	private void TogglePause()
	{
		isPaused = !isPaused;
		GetTree().Paused = isPaused;

		if (isPaused)
		{
			EndHitstop();
			pauseMenu.ShowPauseMenu();
			uiManager.ShowCursor();
		}
		else
		{
			pauseMenu.HidePauseMenu();
			uiManager.HideCursor();
		}
	}

	/// <summary>
	/// Briefly drops the engine time scale so an impact lands. Overlapping calls
	/// extend the freeze rather than cutting it short.
	/// </summary>
	/// <param name="scale">
	/// Time scale to hold. Defaults to a near-freeze; a boss kill passes a
	/// higher value to get slow motion out of the same machinery.
	/// </param>
	public void Hitstop(float seconds, float scale = -1f)
	{
		if (isGameOver || seconds <= 0f || isPaused)
			return;

		ulong end = Time.GetTicksMsec() + (ulong)(seconds * 1000f);
		if (hitstopActive && end <= hitstopEndMsec)
			return;

		hitstopEndMsec = end;
		hitstopActive = true;
		Engine.TimeScale = scale > 0f ? scale : HitstopScale;
	}

	private void EndHitstop()
	{
		if (!hitstopActive)
			return;

		hitstopActive = false;
		Engine.TimeScale = 1.0;
	}

	/// <summary>Adds screen shake. 0.2 is a light kill, 1.0 is death.</summary>
	public void Shake(float trauma)
	{
		gameCamera?.AddTrauma(trauma);
	}

	public void TriggerGameOver()
	{
		if (isGameOver)
			return;

		isGameOver = true;
		isPaused = false;
		GetTree().Paused = false;
		pauseMenu.HidePauseMenu();

		// The transition runs on an AnimationPlayer, which obeys Engine.TimeScale.
		EndHitstop();

		// Snapshot first: clearing the tiers below would zero the moon count the
		// recap is meant to report.
		GameOver.SurvivalTimeToShow = run.SurvivalTime;
		GameOver.KillsToShow = run.Kills;
		GameOver.BestComboToShow = run.BestStreak;
		GameOver.ScoreToShow = run.Score;
		GameOver.MassAtDeath = run.MassNormalised;
		GameOver.MoonsAtDeath = run.Moons;

		// Every moon breaks away with the world that held them.
		run.ClearTiers();

		var records = ScoreManager.SaveRun(run.SurvivalTime, run.Kills, run.BestStreak, run.Score);
		GameOver.IsNewBestScore = records.NewBestScore;
		GameOver.IsNewBestTime = records.NewBestTime;

		SceneTransition.Instance.ChangeScene("res://scenes/gameOver.tscn");
	}

	/// <summary>
	/// Death juice. Freezes and shakes first, then hands over to the recap, so the
	/// hit registers before the screen starts fading.
	/// </summary>
	public async void OnPlayerKilled()
	{
		if (isDying || isGameOver)
			return;

		isDying = true;
		Shake(DeathTrauma);
		Hitstop(DeathHitstop);

		// ignoreTimeScale, or the wait would stretch by the freeze it is timing.
		await ToSignal(
			GetTree().CreateTimer(DeathHitstop, processAlways: true, processInPhysics: false, ignoreTimeScale: true),
			SceneTreeTimer.SignalName.Timeout);

		if (IsInstanceValid(this))
			TriggerGameOver();
	}

	/// <summary>
	/// Called by bullets when they destroy a body. Owns the whole kill payoff —
	/// score, debris, sound, freeze and shake — so the weighting stays in one place.
	/// </summary>
	/// <param name="shedDebris">
	/// False for a nova, whose kills are already paid for in mass. Refunding that
	/// mass as debris would make the ability free.
	/// </param>
	public void RegisterKill(in Body.Remains remains, Vector2 at, bool shedDebris = true)
	{
		bool heavy = remains.Kind is BodyKind.Planetoid or BodyKind.Bulwark or BodyKind.Flare;

		run?.AddKill();

		if (shedDebris)
		{
			ShedDebris(remains, at);
			MaybeDropPowerUp(remains, at, heavy);
		}

		PlayKillSound(heavy);
		Hitstop(heavy ? HeavyKillHitstop : LightKillHitstop);
		Shake(heavy ? HeavyKillTrauma : LightKillTrauma);
	}

	/// <summary>
	/// Draws a shockwave ring and its burst. The ring is not decoration: it is
	/// the only thing that shows how far the blast actually reached, which the
	/// player needs in order to learn the spacing.
	/// </summary>
	public void SpawnBlast(Vector2 at, float radius, Color colour)
	{
		var wave = new NovaWave { MaxRadius = radius, WaveColor = colour };
		wave.GlobalPosition = at;
		AddEntity(wave);

		if (BurstScene == null)
			return;

		var burst = BurstScene.Instantiate<CpuParticles2D>();
		burst.GlobalPosition = at;
		burst.Amount = 220;
		burst.Scale = new Vector2(radius / 165.0f, radius / 165.0f);
		burst.Color = colour;
		burst.Lifetime = 0.8f;
		burst.Emitting = true;
		burst.Finished += burst.QueueFree;
		AddEntity(burst);
	}

	/// <summary>
	/// Scatters the motes a dead body leaves behind. Gravity does the rest — the
	/// player's own pull is what turns them into mass.
	/// </summary>
	private void ShedDebris(in Body.Remains remains, Vector2 at)
	{
		if (DebrisScene == null || remains.DebrisCount <= 0)
			return;

		int budget = MaxDebris - GetTree().GetNodeCountInGroup("debris");
		int count = Mathf.Min(remains.DebrisCount, budget);

		for (int i = 0; i < count; i++)
		{
			var mote = DebrisScene.Instantiate<Debris>();
			mote.GlobalPosition = at;
			mote.Modulate = remains.BurstColor;
			mote.AddToGroup("debris");
			AddEntity(mote);
		}
	}

	private void MaybeDropPowerUp(in Body.Remains remains, Vector2 at, bool heavy)
	{
		if (PowerUpScene == null)
			return;

		float chance = PowerUpDropChance + (heavy ? HeavyDropBonus : 0f);
		if (GD.Randf() > chance)
			return;

		var pickup = PowerUpScene.Instantiate<PowerUp>();
		pickup.Configure(PowerUps.Roll());
		pickup.GlobalPosition = at;

		// Kills happen inside collision callbacks, and inserting an Area2D while
		// the physics server is flushing queries is an error.
		Callable.From(() =>
		{
			if (IsInstanceValid(this) && IsInstanceValid(pickup))
				AddEntity(pickup);
			else
				pickup.QueueFree();
		}).CallDeferred();
	}

	/// <summary>Applies a taken pickup. Nuke is the only one that acts immediately.</summary>
	public void CollectPowerUp(PowerUpKind kind, Vector2 at)
	{
		PowerUps.Profile profile = PowerUps.Get(kind);

		run?.GrantPowerUp(kind);
		SpawnBlast(at, 170f, profile.Colour);
		Shake(0.18f);
		PlayStreakSting(1.75f);

		if (kind == PowerUpKind.Nuke)
			DetonateNuke(at);
	}

	/// <summary>
	/// Clears the arena. These kills score — the pickup was earned — but shed no
	/// debris, or a Nuke would hand back more mass than a nova costs.
	/// </summary>
	private void DetonateNuke(Vector2 at)
	{
		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body body || !IsInstanceValid(body))
				continue;

			if (at.DistanceTo(body.GlobalPosition) > NukeRadius)
				continue;

			Body.Remains remains = body.GetRemains();
			if (body.TakeDamage(9999, (body.GlobalPosition - at).Normalized()))
				RegisterKill(remains, body.GlobalPosition, shedDebris: false);
		}

		SpawnBlast(at, NukeRadius * 0.5f, PowerUps.Nuke.Colour);
		Hitstop(0.16f);
		Shake(1.0f);
	}

	/// <summary>A mote reaching the world. Tiny by design: this fires constantly.</summary>
	public void PlayAbsorbTick()
	{
		if (absorbSound == null)
			return;

		absorbSound.PitchScale = (float)GD.RandRange(1.6, 2.1);
		absorbSound.Play();
	}

	/// <summary>A moon took a hit meant for the world, and breaks away with the mass that earned it.</summary>
	public void OnMoonBlocked(Vector2 at)
	{
		run?.BreakMoon();
		Hitstop(HeavyKillHitstop);
		Shake(HeavyKillTrauma);
		PlayKillSound(true);
	}

	private void OnResumeGame()
	{
		if (isPaused)
		{
			TogglePause();
		}
	}

	private void OnGiveUpGame()
	{
		TriggerGameOver();
	}

	/// <summary>
	/// One sample stands in for the light/heavy kill sounds until the real ones
	/// exist: heavy kills drop the pitch and gain volume, and every shot gets a
	/// little jitter so a long streak does not fatigue.
	/// </summary>
	private void PlayKillSound(bool heavy)
	{
		float jitter = (float)GD.RandRange(-0.06, 0.06);
		killSound.PitchScale = (heavy ? 0.72f : 1.14f) + jitter;
		killSound.VolumeDb = heavy ? -6.0f : -10.0f;
		killSound.Play();
	}

	private void OnStreakChanged(int streak, bool milestone)
	{
		if (!milestone || streakSound == null)
			return;

		// Each milestone lands a step higher, capped so it stays musical.
		PlayStreakSting(streak >= 25 ? 2.0f : streak >= 10 ? 1.7f : 1.45f);
		Shake(0.12f);
	}

	/// <summary>
	/// One sample doing several jobs: streak milestones ring up, boss arrival and
	/// defeat ring down. Placeholder until the real stings exist.
	/// </summary>
	private void PlayStreakSting(float pitch)
	{
		if (streakSound == null)
			return;

		streakSound.PitchScale = pitch;
		streakSound.Play();
	}

	public void PlayButtonSound()
	{
		buttonSound.Play();
	}

	public void PlayHoverSound()
	{
		hoverSound.Play();
	}
}
