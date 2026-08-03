using Godot;
using System.Collections.Generic;

/// <summary>
/// Spends stardust on the three permanent upgrades. Soft-capped by design —
/// see <see cref="Upgrades"/> — so this screen is never the place an orbit gets
/// trivialised, only nudged.
/// </summary>
public partial class UpgradesMenu : Control
{
	private static readonly Color Idle = new Color(0.85f, 0.85f, 0.9f);
	private static readonly Color Maxed = new Color(0.6f, 0.85f, 0.65f);
	private static readonly Color CantAfford = new Color(0.55f, 0.55f, 0.6f);

	private Label stardustLabel;
	private VBoxContainer rows;
	private Button backButton;
	private Font font;

	private readonly Dictionary<UpgradeId, Label> levelLabels = new();
	private readonly Dictionary<UpgradeId, Button> buyButtons = new();

	public override void _Ready()
	{
		stardustLabel = GetNode<Label>("Layout/StardustLabel");
		rows = GetNode<VBoxContainer>("Layout/Rows");
		backButton = GetNode<Button>("Layout/BackButton");
		font = GetNode<Label>("Layout/Title").GetThemeFont("font");

		BuildRows();
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

	private void BuildRows()
	{
		foreach (Upgrades.Profile upgrade in Upgrades.All)
		{
			var card = new HBoxContainer();
			card.AddThemeConstantOverride("separation", 20);

			var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			text.AddChild(Label(upgrade.Name, 46, Idle, HorizontalAlignment.Left));
			text.AddChild(Label(upgrade.Description, 26, new Color(0.7f, 0.7f, 0.78f), HorizontalAlignment.Left));
			card.AddChild(text);

			var level = Label($"0/{upgrade.MaxLevel}", 34, Idle, HorizontalAlignment.Right);
			level.CustomMinimumSize = new Vector2(110, 0);
			levelLabels[upgrade.Id] = level;
			card.AddChild(level);

			var buy = new Button { CustomMinimumSize = new Vector2(220, 64), Flat = true };
			Style(buy);
			buy.Pressed += () => Buy(upgrade);
			buyButtons[upgrade.Id] = buy;
			card.AddChild(buy);

			rows.AddChild(card);
		}

		RefreshAll();
	}

	private Label Label(string text, int size, Color colour, HorizontalAlignment alignment)
	{
		var label = new Label { Text = text, HorizontalAlignment = alignment };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		return label;
	}

	private void Style(Button button)
	{
		button.AddThemeFontOverride("font", font);
		button.AddThemeFontSizeOverride("font_size",32);
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.72f, 0.35f));
		button.AddThemeColorOverride("font_focus_color", new Color(1f, 0.72f, 0.35f));
	}

	private void Buy(Upgrades.Profile upgrade)
	{
		int level = PlayerProfile.UpgradeLevel(upgrade.Id);
		if (level >= upgrade.MaxLevel)
			return;

		if (PlayerProfile.TryBuyUpgrade(upgrade.Id, upgrade.CostForLevel(level), upgrade.MaxLevel))
			RefreshAll();
	}

	private void RefreshAll()
	{
		stardustLabel.Text = $"{PlayerProfile.Stardust:N0} STARDUST";

		foreach (Upgrades.Profile upgrade in Upgrades.All)
		{
			int level = PlayerProfile.UpgradeLevel(upgrade.Id);
			bool maxed = level >= upgrade.MaxLevel;

			levelLabels[upgrade.Id].Text = $"{level}/{upgrade.MaxLevel}";
			levelLabels[upgrade.Id].AddThemeColorOverride("font_color", maxed ? Maxed : Idle);

			Button buy = buyButtons[upgrade.Id];
			if (maxed)
			{
				buy.Text = "MAXED";
				buy.Disabled = true;
				buy.AddThemeColorOverride("font_color", Maxed);
				buy.AddThemeColorOverride("font_disabled_color", Maxed);
				continue;
			}

			int cost = upgrade.CostForLevel(level);
			bool affordable = PlayerProfile.Stardust >= cost;

			buy.Text = $"{cost:N0}";
			buy.Disabled = !affordable;
			buy.AddThemeColorOverride("font_color", affordable ? Idle : CantAfford);
			buy.AddThemeColorOverride("font_disabled_color", CantAfford);
		}
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
