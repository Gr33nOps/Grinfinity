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
		ProcessMode = Node.ProcessModeEnum.Always;
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
		if (inputEvent.IsActionPressed("ui_cancel"))
		{
			TogglePause();
		}
	}
	
	private void TogglePause()
	{
		isPaused = !isPaused;
		
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
		
		// Pause all game systems
		enemySpawner.SetPaused(isPaused);
		playerManager.SetPaused(isPaused);
		scoreManager.SetPaused(isPaused); // Add this line to pause the score manager
	}
	
	public void TriggerGameOver()
	{
		var score = scoreManager.GetSurvivalTime();
		scoreManager.SaveHighScore(score);
		GameOver.SurvivalTimeToShow = score;
		SceneTransition.Instance.ChangeScene("res://scenes/gameOver.tscn");
	}
	
	private void OnResumeGame()
	{
		TogglePause();
	}
	
	private void OnGiveUpGame()
	{
		if (isPaused)
		{
			isPaused = false;
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
