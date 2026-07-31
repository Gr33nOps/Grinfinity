using Godot;

public partial class PauseMenu : Control
{
	[Signal]
	public delegate void ResumeGameEventHandler();
	[Signal]
	public delegate void GiveUpGameEventHandler();

	private TextureButton resumeButton;
	private TextureButton giveUpButton;

	public override void _Ready()
	{
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;

		resumeButton = GetNode<TextureButton>("Panel/ResumeButton");
		giveUpButton = GetNode<TextureButton>("Panel/GiveUpButton");
		resumeButton.Pressed += OnResumeButtonPressed;
		giveUpButton.Pressed += OnGiveUpButtonPressed;
		resumeButton.MouseEntered += PlayHoverSound;
		giveUpButton.MouseEntered += PlayHoverSound;
	}

	public void ShowPauseMenu()
	{
		Visible = true;
		// Gives the gamepad something to start from.
		resumeButton.GrabFocus();
	}

	public void HidePauseMenu()
	{
		Visible = false;
	}

	private void OnResumeButtonPressed()
	{
		PlayButtonSound();
		EmitSignal(SignalName.ResumeGame);
	}

	private void OnGiveUpButtonPressed()
	{
		PlayButtonSound();
		EmitSignal(SignalName.GiveUpGame);
	}

	private void PlayButtonSound()
	{
		GetGameManager()?.PlayButtonSound();
	}

	private void PlayHoverSound()
	{
		GetGameManager()?.PlayHoverSound();
	}

	private GameManager GetGameManager()
	{
		return GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
	}
}
