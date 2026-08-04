using Godot;

public partial class SettingsMenu : Control
{
	private HSlider masterSlider;
	private HSlider musicSlider;
	private HSlider sfxSlider;
	private HSlider shakeSlider;
	private HSlider uiScaleSlider;
	private Label masterValue;
	private Label musicValue;
	private Label sfxValue;
	private Label shakeValue;
	private Label uiScaleValue;
	private Button fullscreenCheck;
	private Button vsyncCheck;
	private OptionButton resolutionOption;
	private OptionButton fpsCapOption;
	private Button controlsButton;
	private Button accessibilityButton;
	private Button backButton;

	public override void _Ready()
	{
		masterSlider = GetNode<HSlider>("Layout/MasterRow/Slider");
		musicSlider = GetNode<HSlider>("Layout/MusicRow/Slider");
		sfxSlider = GetNode<HSlider>("Layout/SfxRow/Slider");
		shakeSlider = GetNode<HSlider>("Layout/ShakeRow/Slider");
		uiScaleSlider = GetNode<HSlider>("Layout/UiScaleRow/Slider");
		masterValue = GetNode<Label>("Layout/MasterRow/Value");
		musicValue = GetNode<Label>("Layout/MusicRow/Value");
		sfxValue = GetNode<Label>("Layout/SfxRow/Value");
		shakeValue = GetNode<Label>("Layout/ShakeRow/Value");
		uiScaleValue = GetNode<Label>("Layout/UiScaleRow/Value");
		fullscreenCheck = GetNode<Button>("Layout/FullscreenRow/Check");
		vsyncCheck = GetNode<Button>("Layout/VSyncRow/Check");
		resolutionOption = GetNode<OptionButton>("Layout/ResolutionRow/Option");
		fpsCapOption = GetNode<OptionButton>("Layout/FpsCapRow/Option");
		controlsButton = GetNode<Button>("Layout/ControlsButton");
		accessibilityButton = GetNode<Button>("Layout/AccessibilityButton");
		backButton = GetNode<Button>("Layout/BackButton");

		BuildResolutionOptions();
		BuildFpsCapOptions();

		var settings = GameSettings.Instance;
		if (settings != null)
		{
			masterSlider.Value = settings.MasterVolume;
			musicSlider.Value = settings.MusicVolume;
			sfxSlider.Value = settings.SfxVolume;
			shakeSlider.Value = settings.ShakeIntensity;
			uiScaleSlider.Value = settings.UiScale;
			fullscreenCheck.ButtonPressed = settings.Fullscreen;
			vsyncCheck.ButtonPressed = settings.VSyncEnabled;
			resolutionOption.Selected = settings.ResolutionIndex;
			fpsCapOption.Selected = settings.FpsCapIndex;
		}

		fullscreenCheck.Text = fullscreenCheck.ButtonPressed ? "ON" : "OFF";
		vsyncCheck.Text = vsyncCheck.ButtonPressed ? "ON" : "OFF";
		SetResolutionEnabled(!fullscreenCheck.ButtonPressed);
		RefreshLabels();

		masterSlider.ValueChanged += OnMasterChanged;
		musicSlider.ValueChanged += OnMusicChanged;
		sfxSlider.ValueChanged += OnSfxChanged;
		shakeSlider.ValueChanged += OnShakeChanged;
		uiScaleSlider.ValueChanged += OnUiScaleChanged;
		fullscreenCheck.Toggled += OnFullscreenToggled;
		vsyncCheck.Toggled += OnVSyncToggled;
		resolutionOption.ItemSelected += OnResolutionSelected;
		fpsCapOption.ItemSelected += OnFpsCapSelected;
		controlsButton.Pressed += OnControlsPressed;
		accessibilityButton.Pressed += OnAccessibilityPressed;
		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	private void BuildResolutionOptions()
	{
		foreach ((int width, int height) in GameSettings.Resolutions)
			resolutionOption.AddItem($"{width} x {height}");
	}

	private void BuildFpsCapOptions()
	{
		foreach (int cap in GameSettings.FpsCaps)
			fpsCapOption.AddItem(cap == 0 ? "UNCAPPED" : $"{cap} FPS");
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

	private void OnShakeChanged(double value)
	{
		GameSettings.Instance?.SetShakeIntensity((float)value);
		RefreshLabels();
	}

	private void OnFullscreenToggled(bool pressed)
	{
		GameSettings.Instance?.SetFullscreen(pressed);
		fullscreenCheck.Text = pressed ? "ON" : "OFF";
		// Resolution only means anything in windowed mode.
		SetResolutionEnabled(!pressed);
	}

	/// <summary>
	/// Greys the whole resolution row together. Disabling only the dropdown
	/// left its label at full brightness, which reads as an active row whose
	/// control just happens to be unreadable, rather than an inactive one.
	/// </summary>
	private void SetResolutionEnabled(bool enabled)
	{
		resolutionOption.Disabled = !enabled;
		var label = GetNodeOrNull<Label>("Layout/ResolutionRow/Label");
		label?.AddThemeColorOverride("font_color",
			enabled ? new Color(1f, 1f, 1f) : new Color(0.45f, 0.45f, 0.5f));
	}

	private void OnVSyncToggled(bool pressed)
	{
		GameSettings.Instance?.SetVSyncEnabled(pressed);
		vsyncCheck.Text = pressed ? "ON" : "OFF";
	}

	private void OnResolutionSelected(long index)
	{
		GameSettings.Instance?.SetResolutionIndex((int)index);
	}

	private void OnFpsCapSelected(long index)
	{
		GameSettings.Instance?.SetFpsCapIndex((int)index);
	}

	private void OnUiScaleChanged(double value)
	{
		GameSettings.Instance?.SetUiScale((float)value);
		RefreshLabels();
	}

	private void RefreshLabels()
	{
		masterValue.Text = $"{Mathf.RoundToInt(masterSlider.Value * 100)}%";
		musicValue.Text = $"{Mathf.RoundToInt(musicSlider.Value * 100)}%";
		sfxValue.Text = $"{Mathf.RoundToInt(sfxSlider.Value * 100)}%";
		// Zero is a supported answer, so say so rather than showing a bare "0%".
		shakeValue.Text = shakeSlider.Value <= 0.0 ? "OFF" : $"{Mathf.RoundToInt(shakeSlider.Value * 100)}%";
		uiScaleValue.Text = $"{Mathf.RoundToInt(uiScaleSlider.Value * 100)}%";
	}

	private void OnControlsPressed()
	{
		GameSettings.Instance?.SaveSettings();
		SceneTransition.Instance.ChangeScene("res://scenes/controls.tscn");
	}

	private void OnAccessibilityPressed()
	{
		GameSettings.Instance?.SaveSettings();
		SceneTransition.Instance.ChangeScene("res://scenes/accessibility.tscn");
	}

	private void OnBackPressed()
	{
		GameSettings.Instance?.SaveSettings();
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
