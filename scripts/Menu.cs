using Godot;

public partial class Menu : Node
{
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private ConfirmationDialog quitConfirm;

	public override void _Ready()
	{
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("HoverSound");
		quitConfirm = GetNodeOrNull<ConfirmationDialog>("QuitConfirm");

		var playButton = GetNode<TextureButton>("UI/Buttons/PlayButton");
		var quitButton = GetNode<TextureButton>("UI/Buttons/QuitButton");
		var settingsButton = GetNode<Button>("UI/SideMenu/SettingsButton");
		var creditsButton = GetNode<Button>("UI/SideMenu/CreditsButton");

		// Play goes through the weapon choice; only a restart skips it.
		playButton.Pressed += () => GoTo("res://scenes/weapon_select.tscn");
		settingsButton.Pressed += () => GoTo("res://scenes/settings.tscn");
		creditsButton.Pressed += () => GoTo("res://scenes/credits.tscn");
		quitButton.Pressed += OnQuitButtonPressed;

		foreach (var button in new BaseButton[] { playButton, settingsButton, creditsButton, quitButton })
		{
			button.MouseEntered += PlayHoverSound;
		}

		if (quitConfirm != null)
			quitConfirm.Confirmed += OnQuitConfirmed;

		playButton.GrabFocus();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void GoTo(string scenePath)
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene(scenePath);
	}

	private void OnQuitButtonPressed()
	{
		buttonSound?.Play();

		if (quitConfirm == null)
		{
			OnQuitConfirmed();
			return;
		}

		quitConfirm.PopupCentered();
	}

	private async void OnQuitConfirmed()
	{
		buttonSound?.Play();
		await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
		GetTree().Quit();
	}

	private void PlayHoverSound()
	{
		hoverSound?.Play();
	}
}
