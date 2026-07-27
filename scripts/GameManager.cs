using Godot;

public partial class GameManager : Node2D
{
	private PauseMenu pauseMenu;
	private ScoreManager scoreManager;
	private EnemySpawner enemySpawner;
	private UIManager uiManager;
	private PlayerManager playerManager;
	private AudioStreamPlayer killSound;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private bool isPaused = false;

	public override void _Ready()
	{
		SetupComponents();
		ConnectSignals();
		ProcessMode = ProcessModeEnum.Always;
		AddToGroup("game_manager");
	}

	private void SetupComponents()
	{
		pauseMenu = GetNode<PauseMenu>("PauseLayer/PauseMenu");
		killSound = GetNode<AudioStreamPlayer>("KillSound");
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("HoverSound");

		scoreManager = new ScoreManager();
		AddChild(scoreManager);

		enemySpawner = new EnemySpawner();
		AddChild(enemySpawner);

		uiManager = new UIManager();
		AddChild(uiManager);

		playerManager = new PlayerManager();
		AddChild(playerManager);
	}

	private void ConnectSignals()
	{
		pauseMenu.ResumeGame += OnResumeGame;
		pauseMenu.GiveUpGame += OnGiveUpGame;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("esc"))
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
		GetTree().Paused = false;
		isPaused = false;

		var score = scoreManager.GetSurvivalTime();
		scoreManager.SaveHighScore(score);
		GameOver.SurvivalTimeToShow = score;
		SceneTransition.Instance.ChangeScene("res://scenes/gameOver.tscn");
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
		if (isPaused)
		{
			isPaused = false;
			GetTree().Paused = false;
			pauseMenu.HidePauseMenu();
			uiManager.HideCursor();
		}
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
