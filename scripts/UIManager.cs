using System.Collections.Generic;
using Godot;

/// <summary>
/// The gameplay HUD, fixed to the screen while the world moves under it.
///
/// Time comes first because time is the record. Score sits under it. The three
/// abilities live bottom centre, a shield chip bottom left, and pickups announce
/// themselves in a short line just above the abilities — the banner at the top
/// is kept for bigger moments. Nothing else.
/// </summary>
public partial class UIManager : Node
{
	private Control hud;
	private Label time, score, streak, best, hint, toast;
	private TextureRect shieldChip;
	private CoreBar coreBar;
	private Button upgradePrompt;
	private Player player;
	private RunState run;
	private Sprite2D crosshair;
	private Tween toastTween;
	private float refresh;
	private readonly Dictionary<Ability, AbilitySlot> slots = new();
	private const float HintSeconds = 5f;

	/// <summary>"SHIFT / B" — the keyboard key as bound, and the pad button.</summary>
	/// <summary>"TAB / BACK" — how to open the upgrade screen.</summary>
	public static string UpgradeHint() =>
		$"{OS.GetKeycodeString(GameSettings.GetActionKey("upgrades")).ToUpperInvariant()} / BACK";

	public static string ControlHint(Ability ability)
	{
		string action = RunUpgrades.ActionFor(ability);
		string pad = ability switch { Ability.Dash => "B", Ability.Overdrive => "X", _ => "Y" };
		return $"{OS.GetKeycodeString(GameSettings.GetActionKey(action)).ToUpperInvariant()} / {pad}";
	}

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

		var clock = new VBoxContainer { Position = new Vector2(34, 20), MouseFilter = Control.MouseFilterEnum.Ignore };
		clock.AddThemeConstantOverride("separation", -4);
		hud.AddChild(clock);
		var caption = ArcadeSkin.Label("SURVIVED", Size(18), ArcadeSkin.Muted);
		caption.HorizontalAlignment = HorizontalAlignment.Left;
		clock.AddChild(caption);
		time = ArcadeSkin.Label("00:00", Size(64));
		time.Name = "RunInfo";
		time.HorizontalAlignment = HorizontalAlignment.Left;
		clock.AddChild(time);
		score = ArcadeSkin.Label("0", Size(26));
		score.Name = "LiveScore";
		score.HorizontalAlignment = HorizontalAlignment.Left;
		clock.AddChild(score);
		streak = ArcadeSkin.Label("", Size(22), ArcadeSkin.Orange);
		streak.HorizontalAlignment = HorizontalAlignment.Left;
		clock.AddChild(streak);

		best = ArcadeSkin.Label("", Size(22), ArcadeSkin.Muted);
		best.HorizontalAlignment = HorizontalAlignment.Right;
		best.AnchorLeft = 1; best.AnchorRight = 1; best.OffsetLeft = -360; best.OffsetRight = -34; best.OffsetTop = 30; best.OffsetBottom = 60;
		hud.AddChild(best);

		var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AnchorLeft = .5f; row.AnchorRight = .5f; row.AnchorTop = 1; row.AnchorBottom = 1;
		row.OffsetLeft = -300; row.OffsetRight = 300; row.OffsetTop = -Size(150); row.OffsetBottom = -14;
		row.AddThemeConstantOverride("separation", Size(14));
		hud.AddChild(row);
		foreach (Ability ability in System.Enum.GetValues<Ability>())
		{
			var slot = new AbilitySlot { Ability = ability, Diameter = Size(84), Name = RunUpgrades.AbilityName(ability) };
			row.AddChild(slot);
			slots[ability] = slot;
		}

		// CORE: a bar exactly as wide as the three ability rings, sitting on
		// them, with its name inside it rather than hanging off one end. The
		// upgrade prompt sits on top of it when a bar is full.
		float ringSpan = (3 * (Size(84) + 40f) + 2 * Size(14)) * 0.5f - 20f;
		coreBar = new CoreBar { Name = "CoreBar", TextSize = Size(16) };
		coreBar.AnchorLeft = .5f; coreBar.AnchorRight = .5f; coreBar.AnchorTop = 1; coreBar.AnchorBottom = 1;
		coreBar.OffsetLeft = -ringSpan; coreBar.OffsetRight = ringSpan; coreBar.OffsetTop = -Size(150) - Size(34); coreBar.OffsetBottom = -Size(150) - 10;
		hud.AddChild(coreBar);

		upgradePrompt = ArcadeSkin.Button("", () => GameManager.Of(this)?.OpenUpgradeTree(), true);
		upgradePrompt.Name = "UpgradePrompt";
		upgradePrompt.FocusMode = Control.FocusModeEnum.None;
		upgradePrompt.AddThemeFontSizeOverride("font_size", Size(22));
		upgradePrompt.CustomMinimumSize = new Vector2(0, Size(42));
		upgradePrompt.AnchorLeft = .5f; upgradePrompt.AnchorRight = .5f; upgradePrompt.AnchorTop = 1; upgradePrompt.AnchorBottom = 1;
		upgradePrompt.OffsetLeft = -ringSpan; upgradePrompt.OffsetRight = ringSpan; upgradePrompt.OffsetTop = -Size(150) - Size(34) - 12 - Size(42); upgradePrompt.OffsetBottom = -Size(150) - Size(34) - 12;
		upgradePrompt.Visible = false;
		hud.AddChild(upgradePrompt);

		shieldChip = ArcadeSkin.Icon("shield", Size(64));
		shieldChip.AnchorTop = 1; shieldChip.AnchorBottom = 1;
		shieldChip.OffsetLeft = 34; shieldChip.OffsetTop = -Size(64) - 34; shieldChip.OffsetBottom = -34; shieldChip.OffsetRight = 34 + Size(64);
		shieldChip.TooltipText = "SHIELD";
		hud.AddChild(shieldChip);

		toast = ArcadeSkin.Label("", Size(30));
		toast.Name = "Toast";
		toast.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.06f, 0.15f));
		toast.AddThemeConstantOverride("outline_size", 10);
		toast.AnchorLeft = .5f; toast.AnchorRight = .5f; toast.AnchorTop = 1; toast.AnchorBottom = 1;
		toast.OffsetLeft = -500; toast.OffsetRight = 500; toast.OffsetTop = -Size(150) - 140; toast.OffsetBottom = -Size(150) - 96;
		toast.Modulate = new Color(1, 1, 1, 0);
		hud.AddChild(toast);

		// Two lines: the basics, then the three ability buttons, which are all
		// live from the first second.
		hint = ArcadeSkin.Label("WASD / left stick to move     •     Mouse / right stick to aim     •     Click / RT to shoot\n"
			+ $"{ControlHint(Ability.Dash)} to dash     •     {ControlHint(Ability.Overdrive)} for overdrive     •     {ControlHint(Ability.Nova)} for nova", Size(23), ArcadeSkin.Muted);
		hint.Name = "HowTo";
		hint.AnchorLeft = .5f; hint.AnchorRight = .5f; hint.AnchorTop = .5f; hint.AnchorBottom = .5f;
		hint.OffsetLeft = -650; hint.OffsetRight = 650; hint.OffsetTop = 140; hint.OffsetBottom = 220;
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
		coreBar.Refresh(run.CoreFraction, run.CoreReady, run.BuildComplete);
		bool ready = run.CoreReady;
		if (ready && !upgradePrompt.Visible)
		{
			upgradePrompt.Text = $"UPGRADE READY  •  {UpgradeHint()}";
			upgradePrompt.PivotOffset = upgradePrompt.Size * 0.5f;
			upgradePrompt.Scale = Vector2.One * 1.2f;
			upgradePrompt.CreateTween().TweenProperty(upgradePrompt, "scale", Vector2.One, 0.25f)
				.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}
		upgradePrompt.Visible = ready;

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

	private void UpdateLabels()
	{
		time.Text = ScoreManager.FormatTime(run.SurvivalTime);
		score.Text = $"SCORE  {run.Score:N0}";
		streak.Text = run.Streak >= 2 ? $"{run.Streak} COMBO" : "";
		best.Text = ScoreManager.BestTime > 0f ? $"BEST  {ScoreManager.FormatTime(ScoreManager.BestTime)}" : "";
		shieldChip.Visible = run.HasShield;
	}

	/// <summary>A short line above the abilities. A new one replaces the old at once.</summary>
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
