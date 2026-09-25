using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
/// The skill tree. The CORE sits at the root; four branches grow up out of it,
/// one for the gun and one for each ability. Each node needs one rank in the
/// node below it, and the gun's branch ends in a fork: Debris Cannon or Ion
/// Lance, never both.
///
/// Opened whenever the player likes; buying needs a full CORE bar and takes one
/// rank. The whole game is paused behind it (see
/// <see cref="GameManager.OpenUpgradeTree"/>). A node's words are shown in the
/// readout at the top right when it is pointed at, so the tree itself stays
/// just icons, names and rank pips.
/// </summary>
public partial class UpgradeTree : Control
{
	private static readonly Vector2 CanvasSize = new(1480, 700);
	private static readonly Vector2 RootAt = new(740, 640);
	private static readonly float[] Tiers = { 500, 360, 220, 80 };

	private static float ColumnOf(Branch branch) => branch switch
	{
		Branch.Gun => 300,
		Branch.Dash => 660,
		Branch.Overdrive => 950,
		_ => 1240
	};

	private VBoxContainer rows;
	private Label status;
	private SkillBranches canvas;
	private TextureRect infoIcon;
	private Label infoName, infoLine, infoState;
	private Button close;
	private readonly List<SkillNode> nodes = new();
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
		nodes.Clear();

		RunState run = GameManager.Of(this)?.Run;
		if (run == null)
			return;

		rows = ArcadeSkin.Modal(this, "UPGRADES", 1560);
		status = ArcadeSkin.Label("", 24, ArcadeSkin.Muted);
		rows.AddChild(status);

		canvas = new SkillBranches { CustomMinimumSize = CanvasSize, Root = RootAt };
		rows.AddChild(canvas);

		foreach (Branch branch in System.Enum.GetValues<Branch>())
		{
			SkillNode below = null;
			int tier = 0;
			foreach (RunUpgrades.Profile profile in RunUpgrades.All)
			{
				if (profile.Branch != branch || profile.Equips != null)
					continue;
				SkillNode node = Place(profile, new Vector2(ColumnOf(branch), Tiers[tier++]), false);
				canvas.Link(below, node);
				below = node;
			}

			// The gun's fork: the two weapons sit either side above Piercing.
			if (branch == Branch.Gun)
			{
				float x = ColumnOf(branch);
				canvas.Link(below, Place(RunUpgrades.DebrisCannon, new Vector2(x - 90, Tiers[tier]), true));
				canvas.Link(below, Place(RunUpgrades.IonLance, new Vector2(x + 90, Tiers[tier]), false));
			}
		}

		BuildReadout();

		var footer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		footer.AddThemeConstantOverride("separation", 24);
		rows.AddChild(footer);
		footer.AddChild(ArcadeSkin.Label($"{UIManager.UpgradeHint()} or ESC to close", 20, ArcadeSkin.Muted));
		close = ArcadeSkin.Button("BACK TO THE FIGHT", () => GameManager.Of(this)?.CloseUpgradeTree());
		close.CustomMinimumSize = new Vector2(300, 52);
		footer.AddChild(close);

		Refresh(run);
	}

	private SkillNode Place(RunUpgrades.Profile profile, Vector2 centre, bool labelOnLeft)
	{
		var node = new SkillNode { Profile = profile, LabelOnLeft = labelOnLeft };
		canvas.AddChild(node);
		node.Position = centre - node.Centre;
		node.Pressed += () => Buy(node);
		node.FocusEntered += () => ShowInfo(node);
		// Pointing with the mouse moves the focus too, so only one node is ever ringed.
		node.MouseEntered += node.GrabFocus;
		nodes.Add(node);
		return node;
	}

	/// <summary>Top right, over the short branches: what the pointed-at node does.</summary>
	private void BuildReadout()
	{
		var readout = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(720, 24), Size = new Vector2(740, 200) };
		readout.AddThemeConstantOverride("separation", 20);
		canvas.AddChild(readout);

		infoIcon = ArcadeSkin.Icon("firerate", 88);
		infoIcon.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		readout.AddChild(infoIcon);

		var words = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		words.AddThemeConstantOverride("separation", 4);
		readout.AddChild(words);
		infoName = ArcadeSkin.Label("", 34);
		infoLine = ArcadeSkin.Label("", 24);
		infoState = ArcadeSkin.Label("", 20, ArcadeSkin.Muted);
		foreach (Label label in new[] { infoName, infoLine, infoState })
		{
			label.HorizontalAlignment = HorizontalAlignment.Left;
			label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			words.AddChild(label);
		}
	}

	private void Refresh(RunState run)
	{
		foreach (SkillNode node in nodes)
			node.Set(StateOf(run, node.Profile), run.LevelOf(node.Profile.Id));

		canvas.Fraction = run.CoreFraction;
		canvas.IsFull = run.CoreReady;
		canvas.Complete = run.BuildComplete;
		canvas.QueueRedraw();

		status.Text = run.BuildComplete ? "Every upgrade is maxed. Nice!"
			: run.CoreReady ? "Pick one upgrade"
			: "Fill the CORE bar to pick an upgrade";
		status.AddThemeColorOverride("font_color", run.CoreReady ? ArcadeSkin.Orange : ArcadeSkin.Muted);
	}

	private static SkillNode.State StateOf(RunState run, RunUpgrades.Profile profile)
	{
		if (RunUpgrades.AbilityFor(profile.Branch) is Ability ability && !run.IsUnlocked(ability))
			return SkillNode.State.Locked;
		if (run.IsMaxed(profile.Id))
			return SkillNode.State.Maxed;
		if (!run.CanTake(profile.Id))
			return SkillNode.State.Closed;
		return run.CoreReady ? SkillNode.State.Buyable : SkillNode.State.Open;
	}

	private void ShowInfo(SkillNode node)
	{
		RunState run = GameManager.Of(this)?.Run;
		if (run == null)
			return;

		RunUpgrades.Profile profile = node.Profile;
		infoIcon.Texture = GD.Load<Texture2D>($"res://art/cosmic/icon_{profile.Icon}.svg");
		infoName.Text = profile.Name;
		infoName.AddThemeColorOverride("font_color", profile.Colour);
		infoLine.Text = profile.Equips != null ? $"{profile.Short}. Replaces your gun." : profile.Short;

		string rankOnly = profile.MaxLevel > 1 ? $"Rank {node.Level} of {profile.MaxLevel}" : "Pick one of the two weapons";
		string rank = profile.MaxLevel > 1 ? $"{rankOnly}  •  " : "";
		(string words, Color colour) = node.Current switch
		{
			SkillNode.State.Locked => ($"Unlocks at {ScoreManager.FormatTime(RunState.UnlockTime(RunUpgrades.AbilityFor(profile.Branch).Value))}", ArcadeSkin.Muted),
			SkillNode.State.Maxed => (profile.Equips != null ? "Equipped" : $"{rank}Maxed", profile.Colour),
			SkillNode.State.Closed when profile.Equips != null && run.Weapon != WeaponId.Comet => ("You picked the other weapon", ArcadeSkin.Muted),
			SkillNode.State.Closed => ($"Needs {Title(RunUpgrades.Get(profile.Requires.Value).Name)} first", ArcadeSkin.Muted),
			SkillNode.State.Buyable => ($"{rank}Press to buy", ArcadeSkin.Orange),
			_ => (rankOnly, ArcadeSkin.Muted)
		};
		infoState.Text = words;
		infoState.AddThemeColorOverride("font_color", colour);
	}

	/// <summary>"FASTER SHOTS" → "Faster Shots", for use inside a sentence.</summary>
	private static string Title(string name) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());

	private void Buy(SkillNode node)
	{
		ShowInfo(node);
		var manager = GameManager.Of(this);
		if (closing || manager == null || node.Current != SkillNode.State.Buyable || !manager.BuyUpgrade(node.Profile.Id))
			return;

		// Let the branch light up and the node swell, then straight back to the fight.
		closing = true;
		Refresh(manager.Run);
		node.Pop();
		ShowInfo(node);
		status.Text = "Upgraded!";
		status.AddThemeColorOverride("font_color", ArcadeSkin.Orange);
		ReturnSoon();
	}

	private async void ReturnSoon()
	{
		await ToSignal(GetTree().CreateTimer(0.7, processAlways: true, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
		if (IsInstanceValid(this) && Visible)
			GameManager.Of(this)?.CloseUpgradeTree();
	}

	/// <summary>Something to buy if there is one, else the first node still open, else the way out.</summary>
	private void FocusFirst()
	{
		foreach (SkillNode.State wanted in new[] { SkillNode.State.Buyable, SkillNode.State.Open })
		{
			foreach (SkillNode node in nodes)
			{
				if (node.Current == wanted)
				{
					node.GrabFocus();
					return;
				}
			}
		}
		close.GrabFocus();
	}
}
