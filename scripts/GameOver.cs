using Godot;

public partial class GameOver : Control
{
	public static float SurvivalTimeToShow = 0f;

	private TextureButton restartButton;
	private TextureButton menuButton;
	private Label survivalTimeLabel;
	private Label highScoreLabel;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;

	public override void _Ready()
	{
		survivalTimeLabel = GetNode<Label>("SurvivalTimeLabel");
		highScoreLabel = GetNodeOrNull<Label>("HighScoreLabel");
		restartButton = GetNode<TextureButton>("GameOverMenu/RestartButton");
		menuButton = GetNode<TextureButton>("GameOverMenu/MenuButton");
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/HoverSound");
		gameOverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/GameOverSound");

		gameOverSound?.Play();
		ShowSurvivalTime();
		ShowHighScore();

		restartButton.Pressed += OnRestartButtonPressed;
		menuButton.Pressed += OnMenuButtonPressed;
		restartButton.MouseEntered += PlayHoverSound;
		menuButton.MouseEntered += PlayHoverSound;

		restartButton.GrabFocus();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void ShowSurvivalTime()
	{
		survivalTimeLabel.Text = $"You Survived: {ScoreManager.FormatTime(SurvivalTimeToShow)}";
	}

	private void ShowHighScore()
	{
		if (highScoreLabel != null)
			highScoreLabel.Text = ScoreManager.GetFormattedHighScore();
	}

	private void OnRestartButtonPressed()
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
	}

	private void OnMenuButtonPressed()
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}

	private void PlayHoverSound()
	{
		hoverSound?.Play();
	}
}
