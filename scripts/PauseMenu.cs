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
		resumeButton.MouseEntered += OnResumeButtonHover;
		giveUpButton.MouseEntered += OnGiveUpButtonHover;
	}

	public void ShowPauseMenu()
	{
		Visible = true;
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

	private void OnResumeButtonHover()
	{
		PlayHoverSound();
	}

	private void OnGiveUpButtonHover()
	{
		PlayHoverSound();
	}

	private void PlayButtonSound()
	{
		var gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.PlayButtonSound();
	}

	private void PlayHoverSound()
	{
		var gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.PlayHoverSound();
	}
}
