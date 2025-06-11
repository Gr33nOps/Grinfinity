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
		InitializeComponents();
		ConnectSignals();
		ProcessMode = Node.ProcessModeEnum.Always;
	}
	
	private void InitializeComponents()
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
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
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
		
		enemySpawner.SetPaused(isPaused);
		playerManager.SetPaused(isPaused);
	}
	
	public void TriggerGameOver()
	{
		var score = scoreManager.GetSurvivalTime();
		GameOver.SurvivalTimeToShow = score;
		var gameOverScene = GD.Load<PackedScene>("res://scenes/GameOver.tscn");
		GetTree().ChangeSceneToPacked(gameOverScene);
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
	
	public async void PlayButtonSound()
	{
		buttonSound.Play();
	}
	
	public async void PlayHoverSound()
	{
		hoverSound.Play();
	}
}
