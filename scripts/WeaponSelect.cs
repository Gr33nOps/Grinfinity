using Godot;
using System.Collections.Generic;

/// <summary>
/// Picks the weapon for the next orbit.
///
/// It sits between the menu and the game, not between a death and a restart —
/// the "one more orbit" pillar means a restart has to go straight back in with
/// the same weapon.
/// </summary>
public partial class WeaponSelect : Control
{
	private static readonly Color Idle = new Color(0.85f, 0.85f, 0.9f);
	private static readonly Color Chosen = new Color(1.0f, 0.72f, 0.35f);

	private VBoxContainer rows;
	private Button backButton;
	private Font font;

	private readonly Dictionary<WeaponId, Button> buttons = new();

	// --- World carousel ---------------------------------------------------
	// A weapon needs a full card each (fantasy, tradeoff); twelve of those would
	// not fit on one screen. Browsing one at a time keeps this to a single row
	// regardless of how many worlds the roadmap eventually adds.
	private Label worldNameLabel;
	private Label worldDetailLabel;
	private Button worldPrevButton;
	private Button worldNextButton;
	private int browsedWorldId;

	public override void _Ready()
	{
		rows = GetNode<VBoxContainer>("Layout/Rows");
		backButton = GetNode<Button>("Layout/BackButton");
		font = GetNode<Label>("Layout/Title").GetThemeFont("font");

		BuildRows();
		backButton.Pressed += OnBackPressed;

		BuildWorldPicker();

		Input.MouseMode = Input.MouseModeEnum.Visible;
		buttons[Loadout.Weapon].GrabFocus();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("pause"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildRows()
	{
		foreach (WeaponProfile weapon in WeaponProfile.All)
		{
			var card = new VBoxContainer();
			card.AddThemeConstantOverride("separation", 0);

			var pick = new Button
			{
				Text = weapon.Name,
				Flat = true,
				CustomMinimumSize = new Vector2(0, 78)
			};
			Style(pick, 54, Idle);
			// Captured so each button knows the weapon it commits to.
			pick.Pressed += () => Choose(weapon.Id);
			card.AddChild(pick);

			// Fantasy and tradeoff both shown, because a weapon the player cannot
			// see the cost of is not a choice.
			card.AddChild(Caption(weapon.Fantasy, 30, new Color(0.78f, 0.78f, 0.85f)));
			card.AddChild(Caption(weapon.Tradeoff, 28, new Color(0.62f, 0.62f, 0.7f)));

			buttons[weapon.Id] = pick;
			rows.AddChild(card);
		}

		RefreshChosen();
	}

	private Label Caption(string text, int size, Color colour)
	{
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
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
	}

	private void RefreshChosen()
	{
		// Colour alone marks the choice: Bubblegum is a display face with no
		// bracket glyphs, so any "> like this <" decoration renders as nothing.
		foreach ((WeaponId id, Button button) in buttons)
			button.AddThemeColorOverride("font_color", id == Loadout.Weapon ? Chosen : Idle);
	}

	private void Choose(WeaponId id)
	{
		// Second press on the already-chosen weapon launches, so a decided player
		// is one click from an orbit and an undecided one can browse first.
		if (Loadout.Weapon == id)
		{
			SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
			return;
		}

		Loadout.Weapon = id;
		RefreshChosen();
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/mode_select.tscn");
	}

	private void BuildWorldPicker()
	{
		worldNameLabel = GetNode<Label>("Layout/WorldRow/Center/Name");
		worldDetailLabel = GetNode<Label>("Layout/WorldRow/Center/Detail");
		worldPrevButton = GetNode<Button>("Layout/WorldRow/Prev");
		worldNextButton = GetNode<Button>("Layout/WorldRow/Next");

		Style(worldPrevButton, 44, Idle);
		Style(worldNextButton, 44, Idle);
		worldPrevButton.Pressed += () => BrowseWorld(-1);
		worldNextButton.Pressed += () => BrowseWorld(1);

		browsedWorldId = Loadout.World;
		RefreshWorldPicker();
	}

	private void BrowseWorld(int direction)
	{
		int count = Worlds.All.Length;
		browsedWorldId = ((browsedWorldId - 1 + direction) % count + count) % count + 1;
		RefreshWorldPicker();
	}

	private void RefreshWorldPicker()
	{
		Worlds.Profile world = Worlds.Get(browsedWorldId);
		bool unlocked = PlayerProfile.IsWorldUnlocked(world.Id);

		// Landing on an unlocked world selects it immediately — there is no
		// second confirming click here, unlike the weapon, because there is
		// nothing to commit to: the world is cosmetic, so browsing to it is
		// choosing it. A locked one can still be browsed to, to see what it
		// takes, without changing what is actually equipped.
		if (unlocked)
			Loadout.World = world.Id;

		worldNameLabel.Text = world.Name;
		worldNameLabel.AddThemeColorOverride("font_color", unlocked ? Chosen : Idle);
		worldDetailLabel.Text = unlocked ? world.Flavour : $"LOCKED — {world.UnlockHint}";
		worldDetailLabel.AddThemeColorOverride("font_color",
			unlocked ? new Color(0.78f, 0.78f, 0.85f) : new Color(0.55f, 0.55f, 0.6f));
	}
}
