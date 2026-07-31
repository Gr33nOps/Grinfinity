using System.Collections.Generic;
using Godot;

public partial class ControlsMenu : Control
{
	private const int LabelWidth = 420;
	private const int KeyWidth = 300;

	private VBoxContainer rows;
	private Label hint;
	private Button resetButton;
	private Button backButton;
	private Font font;

	private readonly Dictionary<string, Button> keyButtons = new();
	private string listeningAction;
	private Button listeningButton;

	public override void _Ready()
	{
		rows = GetNode<VBoxContainer>("Layout/Rows");
		hint = GetNode<Label>("Layout/Hint");
		resetButton = GetNode<Button>("Layout/ResetButton");
		backButton = GetNode<Button>("Layout/BackButton");
		font = GetNode<Label>("Layout/Title").GetThemeFont("font");

		BuildRows();

		resetButton.Pressed += OnResetPressed;
		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	private void BuildRows()
	{
		foreach (var (action, label) in GameSettings.RebindableActions)
		{
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 20);

			var name = new Label
			{
				Text = label,
				CustomMinimumSize = new Vector2(LabelWidth, 0)
			};
			StyleLabel(name, 40);
			row.AddChild(name);

			var keyButton = new Button
			{
				CustomMinimumSize = new Vector2(KeyWidth, 62),
				ClipText = true
			};
			StyleButton(keyButton, 38);
			// Captured so each button knows which action it edits.
			keyButton.Pressed += () => StartListening(action, keyButton);
			row.AddChild(keyButton);

			keyButtons[action] = keyButton;
			rows.AddChild(row);
		}

		RefreshKeyLabels();
	}

	private void StyleLabel(Label label, int size)
	{
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
	}

	private void StyleButton(Button button, int size)
	{
		button.AddThemeFontOverride("font", font);
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.9f));
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.72f, 0.35f));
		button.AddThemeColorOverride("font_focus_color", new Color(1f, 0.72f, 0.35f));
	}

	private void RefreshKeyLabels()
	{
		foreach (var (action, _) in GameSettings.RebindableActions)
		{
			Key key = GameSettings.GetActionKey(action);
			keyButtons[action].Text = key == Key.None ? "—" : OS.GetKeycodeString(key);
		}
	}

	private void StartListening(string action, Button button)
	{
		if (listeningAction != null)
			StopListening();

		listeningAction = action;
		listeningButton = button;
		button.Text = "PRESS A KEY";
		hint.Text = "Press a key to bind, or Esc to cancel.";
	}

	private void StopListening()
	{
		listeningAction = null;
		listeningButton = null;
		hint.Text = "Click a key to rebind it. Esc cancels.";
		RefreshKeyLabels();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (listeningAction == null || inputEvent is not InputEventKey keyEvent)
			return;

		if (!keyEvent.Pressed || keyEvent.Echo)
			return;

		// Swallow the keypress so it cannot also fire whatever it is bound to.
		GetViewport().SetInputAsHandled();

		Key pressed = keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;

		if (pressed == Key.Escape)
		{
			StopListening();
			return;
		}

		var settings = GameSettings.Instance;
		if (settings == null)
		{
			StopListening();
			return;
		}

		// Free the key from whatever held it, so two actions cannot share one bind.
		string conflict = GameSettings.FindConflict(listeningAction, pressed);
		if (conflict != null)
			settings.SetActionKey(conflict, Key.None);

		settings.SetActionKey(listeningAction, pressed);
		settings.SaveSettings();
		StopListening();
	}

	private void OnResetPressed()
	{
		GameSettings.Instance?.ResetBindings();
		RefreshKeyLabels();
	}

	private void OnBackPressed()
	{
		if (listeningAction != null)
		{
			StopListening();
			return;
		}

		GameSettings.Instance?.SaveSettings();
		SceneTransition.Instance.ChangeScene("res://scenes/settings.tscn");
	}
}
