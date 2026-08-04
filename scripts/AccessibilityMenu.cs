using Godot;

/// <summary>
/// Every accessibility toggle in one place, off Settings. Screen shake stays on
/// its own slider back there — it already shipped with M1's "every toggle
/// ships with its options entry" rule, no reason to move it.
/// </summary>
public partial class AccessibilityMenu : Control
{
	private Button colourblindCheck;
	private Button outlinesCheck;
	private Button rapidFireCheck;
	private Button assistCheck;
	private Button damageNumbersCheck;
	private Button aimAssistCheck;
	private Button backButton;

	public override void _Ready()
	{
		colourblindCheck = GetNode<Button>("Layout/ColourblindRow/Check");
		outlinesCheck = GetNode<Button>("Layout/OutlinesRow/Check");
		rapidFireCheck = GetNode<Button>("Layout/RapidFireRow/Check");
		assistCheck = GetNode<Button>("Layout/AssistRow/Check");
		damageNumbersCheck = GetNode<Button>("Layout/DamageNumbersRow/Check");
		aimAssistCheck = GetNode<Button>("Layout/AimAssistRow/Check");
		backButton = GetNode<Button>("Layout/BackButton");

		var settings = GameSettings.Instance;
		if (settings != null)
		{
			colourblindCheck.ButtonPressed = settings.ColourblindMode;
			outlinesCheck.ButtonPressed = settings.HighContrastOutlines;
			rapidFireCheck.ButtonPressed = settings.RapidFireHoldMode;
			assistCheck.ButtonPressed = settings.AssistMode;
			damageNumbersCheck.ButtonPressed = settings.ShowDamageNumbers;
			aimAssistCheck.ButtonPressed = settings.GamepadAimAssist;
		}

		foreach (Button check in new[]
			{ colourblindCheck, outlinesCheck, rapidFireCheck, assistCheck, damageNumbersCheck, aimAssistCheck })
		{
			check.Text = check.ButtonPressed ? "ON" : "OFF";
		}

		colourblindCheck.Toggled += pressed =>
		{
			colourblindCheck.Text = pressed ? "ON" : "OFF";
			GameSettings.Instance?.SetColourblindMode(pressed);
		};
		outlinesCheck.Toggled += pressed =>
		{
			outlinesCheck.Text = pressed ? "ON" : "OFF";
			GameSettings.Instance?.SetHighContrastOutlines(pressed);
		};
		rapidFireCheck.Toggled += pressed =>
		{
			rapidFireCheck.Text = pressed ? "ON" : "OFF";
			GameSettings.Instance?.SetRapidFireHoldMode(pressed);
		};
		assistCheck.Toggled += pressed =>
		{
			assistCheck.Text = pressed ? "ON" : "OFF";
			GameSettings.Instance?.SetAssistMode(pressed);
		};
		damageNumbersCheck.Toggled += pressed =>
		{
			damageNumbersCheck.Text = pressed ? "ON" : "OFF";
			GameSettings.Instance?.SetShowDamageNumbers(pressed);
		};
		aimAssistCheck.Toggled += pressed =>
		{
			aimAssistCheck.Text = pressed ? "ON" : "OFF";
			GameSettings.Instance?.SetGamepadAimAssist(pressed);
		};
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

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/settings.tscn");
	}
}
