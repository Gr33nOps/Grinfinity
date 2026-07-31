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

	private PauseMenu pauseMenu;
	private ScoreManager scoreManager;
	private EnemySpawner enemySpawner;
	private UIManager uiManager;
	private PlayerManager playerManager;
	private GameCamera gameCamera;
	private Node2D entities;
	private AudioStreamPlayer killSound;
	private AudioStreamPlayer streakSound;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private bool isPaused = false;
	private bool isGameOver = false;
	private bool isDying = false;

	private bool hitstopActive = false;
	private ulong hitstopEndMsec = 0;

	public bool IsPaused => isPaused;

	/// <summary>Seconds survived so far. Read by the music layer and the spawner.</summary>
	public float RunTime => scoreManager?.GetSurvivalTime() ?? 0f;

	public override void _Ready()
	{
		// Must be in the group before the child managers run their own _Ready,
		// since they look the manager up by group.
		AddToGroup("game_manager");
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
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("HoverSound");

		scoreManager = AddPausableChild(new ScoreManager());
		enemySpawner = AddPausableChild(new EnemySpawner());
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
		scoreManager.StreakMilestone += OnStreakMilestone;
	}

	/// <summary>Parents runtime-spawned nodes under the pausable entity container.</summary>
	public void AddEntity(Node entity)
	{
		entities.AddChild(entity);
	}

	/// <summary>
	/// Parents a runtime-spawned node (bullet, enemy, particle burst) under the
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
		if (!hitstopActive)
			return;

		if (isPaused || isGameOver || Time.GetTicksMsec() >= hitstopEndMsec)
			EndHitstop();
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
	public void Hitstop(float seconds)
	{
		if (isGameOver || seconds <= 0f || isPaused)
			return;

		ulong end = Time.GetTicksMsec() + (ulong)(seconds * 1000f);
		if (hitstopActive && end <= hitstopEndMsec)
			return;

		hitstopEndMsec = end;

		if (!hitstopActive)
		{
			hitstopActive = true;
			Engine.TimeScale = HitstopScale;
		}
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

		float score = scoreManager.GetSurvivalTime();
		int runKills = scoreManager.GetKills();
		int runCombo = scoreManager.GetBestCombo();

		GameOver.SurvivalTimeToShow = score;
		GameOver.KillsToShow = runKills;
		GameOver.BestComboToShow = runCombo;
		GameOver.IsNewBestTime = ScoreManager.SaveRun(score, runKills, runCombo);

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
	/// Called by bullets when they destroy a body. Owns the whole kill payoff:
	/// score, sound, freeze and shake, so the weighting stays in one place.
	/// </summary>
	public void RegisterKill(EnemyKind kind)
	{
		bool heavy = kind == EnemyKind.Tank;

		scoreManager?.AddKill();
		PlayKillSound(heavy);
		Hitstop(heavy ? HeavyKillHitstop : LightKillHitstop);
		Shake(heavy ? HeavyKillTrauma : LightKillTrauma);
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

	private void OnStreakMilestone(int combo)
	{
		if (streakSound == null)
			return;

		// Each milestone lands a step higher, capped so it stays musical.
		float step = combo >= 25 ? 2.0f : combo >= 10 ? 1.7f : 1.45f;
		streakSound.PitchScale = step;
		streakSound.Play();

		Shake(0.12f);
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
