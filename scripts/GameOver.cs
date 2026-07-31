using Godot;

public partial class GameOver : Control
{
	public static float SurvivalTimeToShow = 0f;
	public static int KillsToShow = 0;
	public static int BestComboToShow = 0;
	public static bool IsNewBestTime = false;

	private TextureButton restartButton;
	private TextureButton menuButton;
	private Label survivalTimeLabel;
	private Label statsLabel;
	private Label highScoreLabel;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;

	public override void _Ready()
	{
		survivalTimeLabel = GetNode<Label>("Recap/SurvivalTimeLabel");
		statsLabel = GetNodeOrNull<Label>("Recap/StatsLabel");
		highScoreLabel = GetNodeOrNull<Label>("Recap/HighScoreLabel");
		restartButton = GetNode<TextureButton>("GameOverMenu/RestartButton");
		menuButton = GetNode<TextureButton>("GameOverMenu/MenuButton");
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/HoverSound");
		gameOverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/GameOverSound");

		gameOverSound?.Play();
		ShowRecap();

		restartButton.Pressed += OnRestartButtonPressed;
		menuButton.Pressed += OnMenuButtonPressed;
		restartButton.MouseEntered += PlayHoverSound;
		menuButton.MouseEntered += PlayHoverSound;

		restartButton.GrabFocus();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void ShowRecap()
	{
		survivalTimeLabel.Text = $"You Survived: {ScoreManager.FormatTime(SurvivalTimeToShow)}";

		if (statsLabel != null)
			statsLabel.Text = $"KILLS: {KillsToShow}     BEST STREAK: x{BestComboToShow}";

		if (highScoreLabel == null)
			return;

		if (IsNewBestTime)
		{
			highScoreLabel.Text = "NEW BEST TIME!";
			highScoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.32f));
		}
		else
		{
			highScoreLabel.Text = ScoreManager.GetFormattedHighScore();
		}
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
