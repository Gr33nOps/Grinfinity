using Godot;

public partial class SettingsMenu : Control
{
	private HSlider masterSlider;
	private HSlider musicSlider;
	private HSlider sfxSlider;
	private Label masterValue;
	private Label musicValue;
	private Label sfxValue;
	private Button fullscreenCheck;
	private Button controlsButton;
	private Button backButton;

	public override void _Ready()
	{
		masterSlider = GetNode<HSlider>("Layout/MasterRow/Slider");
		musicSlider = GetNode<HSlider>("Layout/MusicRow/Slider");
		sfxSlider = GetNode<HSlider>("Layout/SfxRow/Slider");
		masterValue = GetNode<Label>("Layout/MasterRow/Value");
		musicValue = GetNode<Label>("Layout/MusicRow/Value");
		sfxValue = GetNode<Label>("Layout/SfxRow/Value");
		fullscreenCheck = GetNode<Button>("Layout/FullscreenRow/Check");
		controlsButton = GetNode<Button>("Layout/ControlsButton");
		backButton = GetNode<Button>("Layout/BackButton");

		var settings = GameSettings.Instance;
		if (settings != null)
		{
			masterSlider.Value = settings.MasterVolume;
			musicSlider.Value = settings.MusicVolume;
			sfxSlider.Value = settings.SfxVolume;
			fullscreenCheck.ButtonPressed = settings.Fullscreen;
		}

		fullscreenCheck.Text = fullscreenCheck.ButtonPressed ? "ON" : "OFF";
		RefreshLabels();

		masterSlider.ValueChanged += OnMasterChanged;
		musicSlider.ValueChanged += OnMusicChanged;
		sfxSlider.ValueChanged += OnSfxChanged;
		fullscreenCheck.Toggled += OnFullscreenToggled;
		controlsButton.Pressed += OnControlsPressed;
		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("pause"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void OnMasterChanged(double value)
	{
		GameSettings.Instance?.SetMasterVolume((float)value);
		RefreshLabels();
	}

	private void OnMusicChanged(double value)
	{
		GameSettings.Instance?.SetMusicVolume((float)value);
		RefreshLabels();
	}

	private void OnSfxChanged(double value)
	{
		GameSettings.Instance?.SetSfxVolume((float)value);
		RefreshLabels();
	}

	private void OnFullscreenToggled(bool pressed)
	{
		GameSettings.Instance?.SetFullscreen(pressed);
		fullscreenCheck.Text = pressed ? "ON" : "OFF";
	}

	private void RefreshLabels()
	{
		masterValue.Text = $"{Mathf.RoundToInt(masterSlider.Value * 100)}%";
		musicValue.Text = $"{Mathf.RoundToInt(musicSlider.Value * 100)}%";
		sfxValue.Text = $"{Mathf.RoundToInt(sfxSlider.Value * 100)}%";
	}

	private void OnControlsPressed()
	{
		GameSettings.Instance?.SaveSettings();
		SceneTransition.Instance.ChangeScene("res://scenes/controls.tscn");
	}

	private void OnBackPressed()
	{
		GameSettings.Instance?.SaveSettings();
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
