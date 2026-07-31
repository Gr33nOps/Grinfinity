using Godot;

public partial class GameManager : Node2D
{
	private PauseMenu pauseMenu;
	private ScoreManager scoreManager;
	private EnemySpawner enemySpawner;
	private UIManager uiManager;
	private PlayerManager playerManager;
	private Node2D entities;
	private AudioStreamPlayer killSound;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private bool isPaused = false;
	private bool isGameOver = false;

	public bool IsPaused => isPaused;

	public override void _Ready()
	{
		// Must be in the group before the child managers run their own _Ready,
		// since they look the manager up by group.
		AddToGroup("game_manager");
		SetupComponents();
		ConnectSignals();
	}

	private void SetupComponents()
	{
		pauseMenu = GetNode<PauseMenu>("PauseLayer/PauseMenu");
		entities = GetNode<Node2D>("Entities");
		killSound = GetNode<AudioStreamPlayer>("KillSound");
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
		if (isGameOver)
			return;

		if (inputEvent.IsActionPressed("pause"))
		{
			TogglePause();
		}
	}

	private void TogglePause()
	{
		isPaused = !isPaused;
		GetTree().Paused = isPaused;

		if (isPaused)
		{
			pauseMenu.ShowPauseMenu();
			uiManager.ShowCursor();
		}
		else
		{
			pauseMenu.HidePauseMenu();
			uiManager.HideCursor();
		}
	}

	public void TriggerGameOver()
	{
		if (isGameOver)
			return;

		isGameOver = true;
		isPaused = false;
		GetTree().Paused = false;
		pauseMenu.HidePauseMenu();

		float score = scoreManager.GetSurvivalTime();
		int runKills = scoreManager.GetKills();
		int runCombo = scoreManager.GetBestCombo();

		GameOver.SurvivalTimeToShow = score;
		GameOver.KillsToShow = runKills;
		GameOver.BestComboToShow = runCombo;
		GameOver.IsNewBestTime = ScoreManager.SaveRun(score, runKills, runCombo);

		SceneTransition.Instance.ChangeScene("res://scenes/gameOver.tscn");
	}

	/// <summary>Called by bullets when they destroy an enemy.</summary>
	public void RegisterKill()
	{
		scoreManager?.AddKill();
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

	public void PlayKillSound()
	{
		killSound.Play();
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
