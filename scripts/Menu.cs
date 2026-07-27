using Godot;

public partial class Menu : Node
{
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;

	public override void _Ready()
	{
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("HoverSound");

		var playButton = GetNode<TextureButton>("VBoxContainer/PlayButton");
		var quitButton = GetNode<TextureButton>("VBoxContainer/QuitButton");

		playButton.Pressed += OnPlayButtonPressed;
		quitButton.Pressed += OnQuitButtonPressed;
		playButton.MouseEntered += OnPlayButtonHover;
		quitButton.MouseEntered += OnQuitButtonHover;

		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void OnPlayButtonPressed()
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
	}

	private async void OnQuitButtonPressed()
	{
		buttonSound?.Play();
		await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
		GetTree().Quit();
	}

	private void OnPlayButtonHover()
	{
		hoverSound?.Play();
	}

	private void OnQuitButtonHover()
	{
		hoverSound?.Play();
	}
}
