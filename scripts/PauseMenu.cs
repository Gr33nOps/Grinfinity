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
		ProcessMode = Node.ProcessModeEnum.Always; // Changed back to Always
		
		var canvasLayer = GetParent<CanvasLayer>();
		if (canvasLayer != null)
		{
			canvasLayer.Layer = 100;
		}
		
		// Find the resume button (it's inside the Panel)
		resumeButton = GetNode<TextureButton>("Panel/ResumeButton");
		if (resumeButton != null)
		{
			resumeButton.Pressed += OnResumeButtonPressed;
		}
		
		// Find the give up button (it's inside the Panel)
		giveUpButton = GetNode<TextureButton>("Panel/GiveUpButton");
		if (giveUpButton != null)
		{
			giveUpButton.Pressed += OnGiveUpButtonPressed;
		}
	}
	
	public void ShowPauseMenu()
	{
		Visible = true;
		// Removed GetTree().Paused = true; to use your original system
	}
	
	public void HidePauseMenu()
	{
		Visible = false;
		// Removed GetTree().Paused = false; to use your original system
	}
	
	private void OnResumeButtonPressed()
	{
		EmitSignal(SignalName.ResumeGame);
	}
	
	private void OnGiveUpButtonPressed()
	{
		EmitSignal(SignalName.GiveUpGame);
	}
}
