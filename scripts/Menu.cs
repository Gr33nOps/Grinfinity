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
		playButton.Pressed += OnPlayButtonPressed;
		quitButton.Pressed += OnQuitButtonPressed;
	}
	
	private async void OnPlayButtonPressed()
	{
		buttonSound.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
	}
	
	private async void OnQuitButtonPressed()
	{
		buttonSound.Play();
		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
		GetTree().Quit();
	}
	
	private void OnPlayButtonHover()
	{
		hoverSound.Play();
	}
	
	private void OnQuitButtonHover()
	{
		hoverSound.Play();
	}
}
