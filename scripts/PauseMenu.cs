// PauseMenu.cs - Fixed version
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
		ProcessMode = Node.ProcessModeEnum.Always;
		
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
	
	private async void OnResumeButtonPressed()
	{
		GetNode<GameManager>("/root/game").PlayButtonSound();
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout"); 
		EmitSignal(SignalName.ResumeGame);
	}
	
	private async void OnGiveUpButtonPressed()
	{
		GetNode<GameManager>("/root/game").PlayButtonSound();
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout"); 
		EmitSignal(SignalName.GiveUpGame);
	}
	
	private void OnResumeButtonHover()
	{
		GetNode<GameManager>("/root/game").PlayHoverSound();
	}
	
	private void OnGiveUpButtonHover()
	{
		GetNode<GameManager>("/root/game").PlayHoverSound();
	}
}
