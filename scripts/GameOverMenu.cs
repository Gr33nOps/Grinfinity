using Godot;

public partial class GameOverMenu : Control
{
	[Signal] public delegate void RestartGameEventHandler();
	[Signal] public delegate void GoToMainMenuEventHandler(); // NEW SIGNAL

	private Label survivalTimeLabel;
	private Button playAgainButton;
	private Button mainMenuButton;

	public override void _Ready()
	{
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;

		survivalTimeLabel = GetNode<Label>("SurvivalTimeLabel");

		playAgainButton = GetNode<Button>("PlayAgainButton");
		mainMenuButton = GetNode<Button>("MainMenuButton");

		playAgainButton.Pressed += () => EmitSignal(SignalName.RestartGame);
		mainMenuButton.Pressed += () => EmitSignal(SignalName.GoToMainMenu); // NEW
	}

	public void ShowGameOver(float survivalTime = 0.0f)
	{
		Visible = true;

		if (survivalTimeLabel != null)
		{
			int minutes = (int)(survivalTime / 60);
			int seconds = (int)(survivalTime % 60);
			survivalTimeLabel.Text = $"You survived: {minutes:D2}:{seconds:D2}";
		}
	}

	public void HideGameOver()
	{
		Visible = false;
	}
}
