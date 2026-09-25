using System.Collections.Generic;
using Godot;

/// <summary>
/// The gameplay HUD, fixed to the screen while the world moves under it.
///
/// Top right holds the run's numbers: time first because time is the only
/// result, then the best time. Top left holds the three abilities in a column.
/// The CORE bar runs along the bottom centre and says "UPGRADE READY" inside
/// itself when it is full. Moons orbit the planet itself, and pickups announce
/// themselves in a short line just above the bar; the banner at the top is
/// kept for bigger moments. Nothing else.
/// </summary>
public partial class UIManager : Node
{
	private Control hud;
	private Label time, streak, best, toast;
	private ControlsCard hint;
	private CoreBar coreBar;
	private Player player;
	private RunState run;
	private Sprite2D crosshair;
	private Tween toastTween;
	private float refresh;
	private readonly Dictionary<Ability, AbilitySlot> slots = new();
	private bool padHints;
	private const float HintSeconds = 5f;

	/// <summary>How to open the upgrade screen on whatever the player is holding: "TAB" or "BACK".</summary>
	public static string UpgradeHint() =>
		InputDevice.Pad ? "BACK" : KeyName("upgrades");

	/// <summary>The button for an ability on whatever the player is holding: the bound key, or the pad button.</summary>
	public static string ControlHint(Ability ability) =>
		InputDevice.Pad
			? ability switch { Ability.Dash => "B", Ability.Overdrive => "X", _ => "Y" }
			: KeyName(RunUpgrades.ActionFor(ability));

	private static string KeyName(string action) =>
		OS.GetKeycodeString(GameSettings.GetActionKey(action)).ToUpperInvariant();


	public override void _Ready()
	{
		var root = GetParent();
		var layer = root.GetNode<CanvasLayer>("UI");
		foreach (Node child in layer.GetChildren())
			if (child is CanvasItem item && child.Name != "Announcer" && child.Name != "BossBar" && child.Name != "Flash") item.Hide();

		player = root.GetNode<Player>("player");
		run = GameManager.Of(this).Run;
		crosshair = root.GetNode<Sprite2D>("CrosshairLayer/Crosshair");
		crosshair.Texture = GD.Load<Texture2D>("res://art/cosmic/crosshair.svg");

		float scale = GameSettings.Instance?.UiScale ?? 1f;
		int Size(int size) => Mathf.RoundToInt(size * scale);

		hud = new Control { Name = "Hud", MouseFilter = Control.MouseFilterEnum.Ignore, Theme = ArcadeSkin.Theme() };
		layer.AddChild(hud);
		ArcadeSkin.Fill(hud);

		// Top right: the run's numbers. Time first and biggest, because time is
		// the only result; the best time under it; the combo last, so it can
		// come and go without shifting anything.
		var numbers = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		numbers.AnchorLeft = 1; numbers.AnchorRight = 1; numbers.OffsetLeft = -Size(420); numbers.OffsetRight = -34; numbers.OffsetTop = 20;
		numbers.AddThemeConstantOverride("separation", -4);
		hud.AddChild(numbers);
		var caption = ArcadeSkin.Label("SURVIVED", Size(18), ArcadeSkin.Muted);
		time = ArcadeSkin.Label("00:00", Size(64));
		time.Name = "RunInfo";
		best = ArcadeSkin.Label("", Size(22), ArcadeSkin.Muted);
		streak = ArcadeSkin.Label("", Size(22), ArcadeSkin.Orange);
		foreach (Label label in new[] { caption, time, best, streak })
		{
			label.HorizontalAlignment = HorizontalAlignment.Right;
			numbers.AddChild(label);
		}

		// Top left: the three abilities in a column. A slot pads its ring by 20
		// each side, so the rings themselves line up with the screen margin.
		int diameter = Size(76), gap = Size(4);
		var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore, Position = new Vector2(34 - 20, 24) };
		column.AddThemeConstantOverride("separation", gap);
		hud.AddChild(column);
		foreach (Ability ability in System.Enum.GetValues<Ability>())
		{
			var slot = new AbilitySlot { Ability = ability, Diameter = diameter, Name = RunUpgrades.AbilityName(ability) };
			column.AddChild(slot);
			slots[ability] = slot;
		}

		// Bottom centre: the CORE bar. When it is full, the notice to go and
		// upgrade is written inside it, and it can be clicked.
		coreBar = new CoreBar { Name = "CoreBar", TextSize = Size(17), PressText = PressText(), Pressed = () => GameManager.Of(this)?.OpenUpgradeTree() };
		coreBar.AnchorLeft = .5f; coreBar.AnchorRight = .5f; coreBar.AnchorTop = 1; coreBar.AnchorBottom = 1;
		coreBar.OffsetLeft = -Size(290); coreBar.OffsetRight = Size(290);
		coreBar.OffsetTop = -34 - Size(32); coreBar.OffsetBottom = -34;
		hud.AddChild(coreBar);


		toast = ArcadeSkin.Label("", Size(30));
		toast.Name = "Toast";
		toast.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.06f, 0.15f));
		toast.AddThemeConstantOverride("outline_size", 10);
		toast.AnchorLeft = .5f; toast.AnchorRight = .5f; toast.AnchorTop = 1; toast.AnchorBottom = 1;
		toast.OffsetLeft = -500; toast.OffsetRight = 500; toast.OffsetTop = -34 - Size(32) - 16 - Size(44); toast.OffsetBottom = -34 - Size(32) - 16;
		toast.Modulate = new Color(1, 1, 1, 0);
		hud.AddChild(toast);

		// The how-to: pictures of the keys or buttons, just under the planet
		// where the player is already looking, for the first few seconds.
		hint = new ControlsCard { Name = "HowTo", UiScale = scale };
		hint.AnchorLeft = .5f; hint.AnchorRight = .5f; hint.AnchorTop = .5f; hint.AnchorBottom = .5f;
		hint.OffsetTop = 150;
		hint.GrowHorizontal = Control.GrowDirection.Both;
		hud.AddChild(hint);

		var bossBar = root.GetNode<Control>("UI/BossBar");
		bossBar.AnchorLeft = .5f; bossBar.AnchorRight = .5f; bossBar.AnchorTop = 0; bossBar.AnchorBottom = 0;
		bossBar.OffsetLeft = -330; bossBar.OffsetRight = 330; bossBar.OffsetTop = 140; bossBar.OffsetBottom = 218;
		if (bossBar.FindChild("Name", true, false) is Label bossName) bossName.AddThemeFontSizeOverride("font_size", Size(24));
		if (bossBar.FindChild("Health", true, false) is ProgressBar health)
		{
			health.AddThemeStyleboxOverride("background", ArcadeSkin.Box(new Color("392339"), ArcadeSkin.Ink, 8, 2));
			health.AddThemeStyleboxOverride("fill", ArcadeSkin.Box(ArcadeSkin.Berry, ArcadeSkin.Orange, 8, 1));
		}

		HideCursor();
		UpdateLabels();
	}

	public override void _Process(double delta)
	{
		if (crosshair.Visible)
			crosshair.GlobalPosition = GetViewport().CanvasTransform * player.AimPosition;

		// Abilities every frame, so the cooldown sweep is smooth; text less often.
		coreBar.Refresh(run.CoreFraction, run.CoreReady, run.BuildComplete, run.Banked);
		if (padHints != InputDevice.Pad)
		{
			padHints = InputDevice.Pad;
			hint.Build();
			coreBar.PressText = PressText();
		}

		// The how-to lines are for the first few seconds only, then fade away.
		hint.Visible = run.SurvivalTime < HintSeconds;
		hint.Modulate = new Color(1, 1, 1, Mathf.Clamp(HintSeconds - run.SurvivalTime, 0f, 1f));

		foreach (var (ability, slot) in slots)
		{
			bool owned = run.IsUnlocked(ability);
			float active = ability == Ability.Overdrive ? player.Abilities.OverdriveRemaining : 0f;
			float readiness = player.CanUse(ability) || !owned ? player.Abilities.Readiness(ability) : 0f;
			slot.Refresh(owned, readiness, player.Abilities.CooldownLeft(ability), active, ControlHint(ability));
		}

		refresh -= (float)delta;
		if (refresh <= 0f)
		{
			refresh = 0.1f;
			UpdateLabels();
		}
	}

	private static string PressText() => $"PRESS {UpgradeHint()}";

	private void UpdateLabels()
	{
		time.Text = ScoreManager.FormatTime(run.SurvivalTime);
		streak.Text = run.Streak >= 2 ? $"{run.Streak} COMBO" : "";
		best.Text = ScoreManager.BestTime > 0f ? $"BEST  {ScoreManager.FormatTime(ScoreManager.BestTime)}" : "";
	}

	/// <summary>A short line just above the CORE bar. A new one replaces the old at once.</summary>
	public void Toast(string text, Color colour)
	{
		toastTween?.Kill();
		toast.Text = text;
		toast.AddThemeColorOverride("font_color", colour);
		toast.Modulate = new Color(1, 1, 1, 1);
		toast.PivotOffset = toast.Size * 0.5f;
		toast.Scale = Vector2.One * 1.25f;

		toastTween = toast.CreateTween();
		toastTween.TweenProperty(toast, "scale", Vector2.One, 0.18f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		toastTween.TweenInterval(1.1f);
		toastTween.TweenProperty(toast, "modulate:a", 0f, 0.35f);
	}

	public void PulseAbility(Ability ability)
	{
		if (slots.TryGetValue(ability, out AbilitySlot slot))
			slot.Pulse();
	}

	public void SetGameplayVisible(bool visible) { hud.Visible = visible; if (!visible) GetParent().GetNode<Control>("UI/Announcer").Hide(); }
	public void ShowCursor() { Input.MouseMode = Input.MouseModeEnum.Visible; crosshair.Hide(); }
	public void HideCursor() { Input.MouseMode = Input.MouseModeEnum.Hidden; crosshair.Show(); }
}
