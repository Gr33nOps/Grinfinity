using System.Collections.Generic;
using Godot;

/// <summary>
/// The skill tree: four short branches side by side. Opened whenever the player
/// likes; buying needs a full CORE bar and takes one rank. The whole game is
/// paused behind it (see <see cref="GameManager.OpenUpgradeTree"/>).
///
/// Deliberately small: an icon, a name, a few words and rank dots per upgrade.
/// A branch for an ability that has not come online yet is shown, dimmed, so
/// the player can see what is coming.
/// </summary>
public partial class UpgradeTree : Control
{
	private VBoxContainer rows;
	private Label status;
	private readonly List<Button> buttons = new();
	private bool closing;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Stop;
		Hide();
	}

	public void Open()
	{
		closing = false;
		Build();
		Show();
		ArcadeSkin.Pop(rows.GetParent<Control>());
		FocusFirst();
	}

	private void Build()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.QueueFree();
		}
		buttons.Clear();

		RunState run = GameManager.Of(this)?.Run;
		if (run == null)
			return;

		rows = ArcadeSkin.Modal(this, "UPGRADES", 1480);
		status = ArcadeSkin.Label("", 24, run.CoreReady ? ArcadeSkin.Orange : ArcadeSkin.Muted);
		status.Text = run.BuildComplete ? "Everything is maxed. Nice!"
			: run.CoreReady ? "Pick one upgrade"
			: $"Fill the CORE bar to upgrade  •  {Mathf.RoundToInt(run.CoreFraction * 100)}%";
		rows.AddChild(status);

		var branches = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		branches.AddThemeConstantOverride("separation", 22);
		rows.AddChild(branches);
		foreach (Branch branch in System.Enum.GetValues<Branch>())
			branches.AddChild(BuildBranch(run, branch));

		var footer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		footer.AddThemeConstantOverride("separation", 24);
		rows.AddChild(footer);
		footer.AddChild(ArcadeSkin.Label($"{UIManager.UpgradeHint()} or ESC to close", 20, ArcadeSkin.Muted));
		var close = ArcadeSkin.Button("BACK TO THE FIGHT", () => GameManager.Of(this)?.CloseUpgradeTree());
		close.CustomMinimumSize = new Vector2(300, 52);
		footer.AddChild(close);
		buttons.Add(close);
	}

	private Control BuildBranch(RunState run, Branch branch)
	{
		bool gun = branch == Branch.Gun;
		Ability? ability = RunUpgrades.AbilityFor(branch);
		bool open = ability == null || run.IsUnlocked(ability.Value);

		var column = new VBoxContainer { CustomMinimumSize = new Vector2(gun ? 380 : 300, 0) };
		column.AddThemeConstantOverride("separation", 10);
		column.Modulate = open ? Colors.White : new Color(1, 1, 1, 0.45f);

		var header = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		header.AddThemeConstantOverride("separation", 10);
		header.AddChild(ArcadeSkin.Icon(RunUpgrades.BranchIcon(branch), 40));
		header.AddChild(ArcadeSkin.Label(RunUpgrades.BranchName(branch), 30, ArcadeSkin.Orange));
		column.AddChild(header);

		if (!open)
		{
			float at = RunState.UnlockTime(ability.Value);
			column.AddChild(ArcadeSkin.Label($"Unlocks at {ScoreManager.FormatTime(at)}", 20, ArcadeSkin.Muted));
		}

		foreach (RunUpgrades.Profile profile in RunUpgrades.All)
		{
			if (profile.Branch != branch || profile.Equips != null)
				continue;
			column.AddChild(BuildNode(run, profile, open));
		}

		if (gun)
		{
			column.AddChild(ArcadeSkin.Label("PICK ONE WEAPON", 18, ArcadeSkin.Muted));
			var pair = new HBoxContainer();
			pair.AddThemeConstantOverride("separation", 10);
			pair.AddChild(BuildWeapon(run, RunUpgrades.DebrisCannon));
			pair.AddChild(BuildWeapon(run, RunUpgrades.IonLance));
			column.AddChild(pair);
		}

		return column;
	}

	private Control BuildNode(RunState run, RunUpgrades.Profile profile, bool branchOpen)
	{
		int level = run.LevelOf(profile.Id);
		bool maxed = level >= profile.MaxLevel;
		bool canBuy = branchOpen && run.CoreReady && run.CanTake(profile.Id);

		var card = ArcadeSkin.Button("", () => Buy(profile.Id));
		card.CustomMinimumSize = new Vector2(0, 96);
		card.Disabled = !canBuy;
		if (maxed)
			card.AddThemeStyleboxOverride("disabled", ArcadeSkin.Box(new Color("3d2440"), ArcadeSkin.Orange, 18, 3));

		var layout = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1, OffsetLeft = 14, OffsetRight = -14, OffsetTop = 10, OffsetBottom = -10 };
		layout.AddThemeConstantOverride("separation", 12);
		card.AddChild(layout);

		var icon = ArcadeSkin.Icon(profile.Icon, 60);
		icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		layout.AddChild(icon);

		var text = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		text.AddThemeConstantOverride("separation", 2);
		layout.AddChild(text);
		var name = ArcadeSkin.Label(profile.Name, 22);
		name.HorizontalAlignment = HorizontalAlignment.Left;
		text.AddChild(name);
		var line = ArcadeSkin.Label(profile.Short, 17, ArcadeSkin.Muted);
		line.HorizontalAlignment = HorizontalAlignment.Left;
		line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		text.AddChild(line);
		text.AddChild(new RankDots { Filled = level, Total = profile.MaxLevel, Tint = profile.Colour });

		if (canBuy)
			buttons.Add(card);
		return card;
	}

	private Control BuildWeapon(RunState run, RunUpgrades.Profile profile)
	{
		bool taken = run.LevelOf(profile.Id) > 0;
		bool canBuy = run.CoreReady && run.CanTake(profile.Id);

		var card = ArcadeSkin.Button("", () => Buy(profile.Id));
		card.CustomMinimumSize = new Vector2(185, 112);
		card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		card.Disabled = !canBuy;
		if (taken)
			card.AddThemeStyleboxOverride("disabled", ArcadeSkin.Box(new Color("3d2440"), ArcadeSkin.Orange, 18, 3));
		else if (run.Weapon != WeaponId.Comet)
			card.Modulate = new Color(1, 1, 1, 0.45f);

		var stack = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1, OffsetTop = 8, OffsetBottom = -8, OffsetLeft = 8, OffsetRight = -8, Alignment = BoxContainer.AlignmentMode.Center };
		stack.AddThemeConstantOverride("separation", 2);
		card.AddChild(stack);
		var icon = ArcadeSkin.Icon(profile.Icon, 48);
		icon.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		stack.AddChild(icon);
		stack.AddChild(ArcadeSkin.Label(profile.Name, 18));
		stack.AddChild(ArcadeSkin.Label(taken ? "EQUIPPED" : profile.Short, 14, taken ? ArcadeSkin.Orange : ArcadeSkin.Muted));

		if (canBuy)
			buttons.Add(card);
		return card;
	}

	private void Buy(RunUpgradeId id)
	{
		var manager = GameManager.Of(this);
		if (closing || manager == null || !manager.BuyUpgrade(id))
			return;

		// Show the new rank for a moment, then straight back to the fight.
		closing = true;
		Build();
		status.Text = "Upgraded!";
		status.AddThemeColorOverride("font_color", ArcadeSkin.Orange);
		ReturnSoon();
	}

	private async void ReturnSoon()
	{
		await ToSignal(GetTree().CreateTimer(0.5, processAlways: true, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
		if (IsInstanceValid(this) && Visible)
			GameManager.Of(this)?.CloseUpgradeTree();
	}

	private void FocusFirst()
	{
		foreach (Button button in buttons)
		{
			if (!button.Disabled)
			{
				button.GrabFocus();
				return;
			}
		}
	}
}
