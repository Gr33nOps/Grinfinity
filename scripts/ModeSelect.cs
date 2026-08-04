using Godot;
using System.Collections.Generic;

/// <summary>
/// Picks the mode and difficulty for the next orbit. Sits between the menu and
/// the weapon choice — mirrors <see cref="WeaponSelect"/>'s pick-then-pick-again
/// pattern for the mode itself, since that is the step this screen owns.
/// </summary>
public partial class ModeSelect : Control
{
	private static readonly Color Idle = new Color(0.85f, 0.85f, 0.9f);
	private static readonly Color Chosen = new Color(1.0f, 0.72f, 0.35f);
	private static readonly Color Locked = new Color(0.5f, 0.5f, 0.56f);

	private VBoxContainer rows;
	private HBoxContainer difficultyRow;
	private Button backButton;
	private Font font;

	private readonly Dictionary<GameMode, Button> modeButtons = new();
	private readonly Dictionary<Difficulty, Button> difficultyButtons = new();

	public override void _Ready()
	{
		rows = GetNode<VBoxContainer>("Layout/Rows");
		difficultyRow = GetNode<HBoxContainer>("Layout/DifficultyRow");
		backButton = GetNode<Button>("Layout/BackButton");
		font = GetNode<Label>("Layout/Title").GetThemeFont("font");

		// Daily Alignment cannot be re-picked once today's attempt is spent —
		// if that is what was left selected from an earlier visit, fall back
		// to the default rather than opening on a card that can't be chosen.
		if (Loadout.Mode == GameMode.DailyAlignment && PlayerProfile.PlayedDailyAlignmentToday)
			Loadout.Mode = GameMode.EndlessOrbit;

		BuildModeRows();
		BuildDifficultyRow();
		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (modeButtons.TryGetValue(Loadout.Mode, out Button focused))
			focused.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("pause"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildModeRows()
	{
		foreach (Modes.Profile mode in Modes.All)
		{
			bool lockedToday = mode.Id == GameMode.DailyAlignment && PlayerProfile.PlayedDailyAlignmentToday;

			var card = new VBoxContainer();
			card.AddThemeConstantOverride("separation", 0);

			var pick = new Button
			{
				Text = mode.Name,
				Flat = true,
				CustomMinimumSize = new Vector2(0, 64),
				Disabled = lockedToday
			};
			Style(pick, 46, Idle);
			pick.Pressed += () => Choose(mode.Id);
			card.AddChild(pick);

			string caption = lockedToday
				? string.Format(TranslationServer.Translate("UI_DAILY_LOCKED"), PlayerProfile.LastDailyAlignmentScore.ToString("N0"))
				: mode.Flavour;
			card.AddChild(Caption(caption, 26, lockedToday ? Locked : new Color(0.78f, 0.78f, 0.85f)));

			modeButtons[mode.Id] = pick;
			rows.AddChild(card);
		}

		RefreshChosen();
	}

	private void BuildDifficultyRow()
	{
		foreach (Difficulties.Profile difficulty in Difficulties.All)
		{
			var pick = new Button
			{
				Text = difficulty.Name,
				Flat = true,
				CustomMinimumSize = new Vector2(160, 60)
			};
			Style(pick, 38, Idle);
			pick.Pressed += () => ChooseDifficulty(difficulty.Id);
			difficultyButtons[difficulty.Id] = pick;
			difficultyRow.AddChild(pick);
		}

		RefreshDifficulty();
	}

	private Label Caption(string text, int size, Color colour)
	{
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		return label;
	}

	private void Style(Button button, int size, Color colour)
	{
		button.AddThemeFontOverride("font", font);
		button.AddThemeFontSizeOverride("font_size", size);
		button.AddThemeColorOverride("font_color", colour);
		button.AddThemeColorOverride("font_hover_color", Chosen);
		button.AddThemeColorOverride("font_focus_color", Chosen);
		button.AddThemeColorOverride("font_pressed_color", Chosen);
		button.AddThemeColorOverride("font_disabled_color", Locked);
	}

	private void RefreshChosen()
	{
		// Colour alone marks the choice: Bubblegum is a display face with no
		// bracket glyphs, so any "> like this <" decoration renders as nothing.
		foreach ((GameMode id, Button button) in modeButtons)
			button.AddThemeColorOverride("font_color", id == Loadout.Mode ? Chosen : (button.Disabled ? Locked : Idle));
	}

	private void RefreshDifficulty()
	{
		foreach ((Difficulty id, Button button) in difficultyButtons)
			button.AddThemeColorOverride("font_color", id == Loadout.Difficulty ? Chosen : Idle);
	}

	private void Choose(GameMode id)
	{
		// Second press on the already-chosen mode carries on to the weapon
		// pick, the same shortcut WeaponSelect gives an already-decided player.
		if (Loadout.Mode == id)
		{
			SceneTransition.Instance.ChangeScene("res://scenes/weapon_select.tscn");
			return;
		}

		Loadout.Mode = id;
		RefreshChosen();
	}

	private void ChooseDifficulty(Difficulty id)
	{
		Loadout.Difficulty = id;
		RefreshDifficulty();
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
