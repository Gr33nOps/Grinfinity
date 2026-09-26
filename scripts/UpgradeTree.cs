using System.Collections.Generic;
using Godot;

/// <summary>
/// The skill tree, laid out like a little solar system. The CORE sits at the
/// root like a sun; four branches grow up and slightly outward from it, one for
/// the gun and one for each ability, and each rank of the tree sits on its own
/// orbit round the CORE. Each node needs one rank in the node below it.
///
/// Opened whenever the player likes; buying needs a full CORE bar and takes one
/// rank. The whole game is paused behind it (see
/// <see cref="GameManager.OpenUpgradeTree"/>). A node's words are shown in the
/// readout at the top centre when it is pointed at — a little looping picture
/// of it at work, its name and a few words — so the tree itself stays
/// just icons, names and rank pips.
/// </summary>
public partial class UpgradeTree : Control
{
	private static readonly Vector2 CanvasSize = new(1480, 580);
	private static readonly Vector2 RootAt = new(740, 520);

	/// <summary>Each tier's orbit round the CORE: half its width, then half its height.</summary>
	private static readonly Vector2[] Orbits = { new(520, 200), new(640, 330), new(760, 450) };

	/// <summary>
	/// Where a node sits: its branch's column, leaning a little further out on
	/// each tier, raised onto that tier's orbit. The outer branches sit lower,
	/// so each tier curves round the CORE.
	/// </summary>
	private static Vector2 SpotFor(Branch branch, int tier)
	{
		float x = branch switch
		{
			Branch.Gun => 340 - 40 * tier,
			Branch.Dash => 630 - 20 * tier,
			Branch.Overdrive => 850 + 20 * tier,
			_ => 1140 + 40 * tier
		};
		Vector2 orbit = Orbits[tier];
		float across = (x - RootAt.X) / orbit.X;
		return new Vector2(x, RootAt.Y - orbit.Y * Mathf.Sqrt(1f - across * across));
	}

	private VBoxContainer rows;
	private SkillBranches canvas;
	private TextureRect infoIcon;
	private UpgradePreview preview;
	private Label infoName, infoLine;
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

		canvas = new SkillBranches { CustomMinimumSize = CanvasSize, Root = RootAt, Orbits = Orbits };
		rows.AddChild(canvas);

		foreach (Branch branch in System.Enum.GetValues<Branch>())
		{
			SkillNode below = null;
			int tier = 0;
			foreach (RunUpgrades.Profile profile in RunUpgrades.All)
			{
				if (profile.Branch != branch)
					continue;
				// Mirrored about the root: the left two branches read to the left.
				bool left = branch is Branch.Gun or Branch.Dash;
				SkillNode node = Place(profile, SpotFor(branch, tier++), left);
				canvas.Link(below, node);
				below = node;
			}

		}

		BuildReadout();

		var footer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		footer.AddThemeConstantOverride("separation", 24);
		rows.AddChild(footer);
		footer.AddChild(ArcadeSkin.Label(InputDevice.Pad ? "BACK to close" : $"{UIManager.UpgradeHint()} or ESC to close", 20, ArcadeSkin.Muted));
		close = ArcadeSkin.Button("BACK TO THE FIGHT", () => GameManager.Of(this)?.CloseUpgradeTree(), false, "play");
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

	/// <summary>
	/// Top centre, above the root and between the branches: what the
	/// pointed-at node does. Centred, so the tree stays mirrored.
	/// </summary>
	private void BuildReadout()
	{
		// A little loop showing the upgrade at work, then its name and what it does.
		var readout = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center, Position = new Vector2(CanvasSize.X * 0.5f - 400, 4), Size = new Vector2(800, 136) };
		readout.AddThemeConstantOverride("separation", 24);
		canvas.AddChild(readout);
		// The orbits pass behind it rather than through its words.
		canvas.KeepClear.Add(new Rect2(readout.Position, readout.Size).Grow(8f));

		preview = new UpgradePreview { CustomMinimumSize = new Vector2(240, 132) };
		readout.AddChild(preview);

		var words = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		words.AddThemeConstantOverride("separation", 4);
		readout.AddChild(words);
		var title = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		title.AddThemeConstantOverride("separation", 10);
		words.AddChild(title);
		infoIcon = ArcadeSkin.Icon("firerate", 48);
		title.AddChild(infoIcon);
		infoName = ArcadeSkin.Label("", 32);
		infoName.VerticalAlignment = VerticalAlignment.Center;
		title.AddChild(infoName);

		infoLine = ArcadeSkin.Label("", 24);
		infoLine.HorizontalAlignment = HorizontalAlignment.Left;
		words.AddChild(infoLine);
	}

	private void Refresh(RunState run)
	{
		foreach (SkillNode node in nodes)
			node.Set(StateOf(run, node.Profile), run.LevelOf(node.Profile.Id));

		canvas.Fraction = run.CoreFraction;
		canvas.IsFull = run.CoreReady;
		canvas.Banked = run.Banked;
		canvas.Complete = run.BuildComplete;
		canvas.QueueRedraw();
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

	/// <summary>Just the upgrade's name and what it does. The node itself shows its rank and whether it can be bought.</summary>
	private void ShowInfo(SkillNode node)
	{
		RunUpgrades.Profile profile = node.Profile;
		infoIcon.Texture = GD.Load<Texture2D>($"res://art/cosmic/icon_{profile.Icon}.svg");
		infoName.Text = profile.Name;
		infoName.AddThemeColorOverride("font_color", profile.Colour);
		infoLine.Text = profile.Short;
		preview.Play(profile.Id);
	}

	private void Buy(SkillNode node)
	{
		ShowInfo(node);
		var manager = GameManager.Of(this);
		if (closing || manager == null || node.Current != SkillNode.State.Buyable || !manager.BuyUpgrade(node.Profile.Id))
			return;

		// Let the branch light up and the node swell. With more bars saved, stay
		// for the next pick; with none left, straight back to the fight.
		Refresh(manager.Run);
		node.Pop();
		if (manager.Run.CoreReady)
		{
			if (node.Current != SkillNode.State.Buyable)
				FocusFirst();
			return;
		}
		closing = true;
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
