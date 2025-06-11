using Godot;

public partial class Menu : Node
{
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private Button playButton;
	private Button quitButton;
	
	public override void _Ready()
	{
		GetNodes();
		SetupButtons();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}
	
	private void GetNodes()
	{
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("HoverSound");
		playButton = GetNodeOrNull<Button>("PlayButton");
		quitButton = GetNodeOrNull<Button>("QuitButton");
	}
	
	private void SetupButtons()
	{
		if (playButton != null)
			playButton.Pressed += OnPlayButtonPressed;
		
		if (quitButton != null)
			quitButton.Pressed += OnQuitButtonPressed;
	}
	
	private async void OnPlayButtonPressed()
	{
		if (buttonSound != null)
			buttonSound.Play();
		
		await ToSignal(GetTree().CreateTimer(0.25f), "timeout");
		GetTree().ChangeSceneToFile("res://scenes/game.tscn");
	}
	
	private async void OnQuitButtonPressed()
	{
		if (buttonSound != null)
			buttonSound.Play();
		
		await ToSignal(GetTree().CreateTimer(0.25f), "timeout");
		GetTree().Quit();
	}
}
