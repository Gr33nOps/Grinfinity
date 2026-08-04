using System.Collections.Generic;
using Godot;

/// <summary>
/// Everything the HUD draws, read from <see cref="RunState"/>. The run itself
/// owns no labels — it raises signals and this decides how loud they look.
/// </summary>
public partial class UIManager : Node
{
	/// <summary>Resting streak colour — the UI accent from the style guide.</summary>
	private static readonly Color StreakIdle = new Color(1.0f, 0.72f, 0.32f);
	private static readonly Color StreakFlash = Colors.White;

	// Reused rather than reallocated; the effects readout runs every frame.
	private readonly List<string> effects = new();

	private Sprite2D crosshair;
	private Sprite2D dashIcon;
	private Sprite2D rapidFireIcon;
	private Label dashKeyLabel;
	private Label rapidFireKeyLabel;
	private Label timeLabel;
	private Label bestLabel;
	private Label killsLabel;
	private Label streakLabel;
	private Label scoreLabel;
	private Label effectsLabel;
	private Player player;
	private RunState run;
	private Tween streakPop;

	public override void _Ready()
	{
		// Resolved relative to the game root rather than by absolute path, so
		// renaming or reparenting the scene cannot silently break the HUD.
		var gameRoot = GetParent();
		crosshair = gameRoot?.GetNodeOrNull<Sprite2D>("CrosshairLayer/Crosshair");
		dashIcon = gameRoot?.GetNodeOrNull<Sprite2D>("UI/dash");
		rapidFireIcon = gameRoot?.GetNodeOrNull<Sprite2D>("UI/rapid_fire");
		dashKeyLabel = gameRoot?.GetNodeOrNull<Label>("UI/DashKey");
		rapidFireKeyLabel = gameRoot?.GetNodeOrNull<Label>("UI/RapidFireKey");
		timeLabel = gameRoot?.GetNodeOrNull<Label>("UI/ScoreLabel");
		bestLabel = gameRoot?.GetNodeOrNull<Label>("UI/HighScoreLabel");
		killsLabel = gameRoot?.GetNodeOrNull<Label>("UI/KillsLabel");
		streakLabel = gameRoot?.GetNodeOrNull<Label>("UI/ComboLabel");
		scoreLabel = gameRoot?.GetNodeOrNull<Label>("UI/RunScoreLabel");
		effectsLabel = gameRoot?.GetNodeOrNull<Label>("UI/EffectsLabel");
		player = gameRoot?.GetNodeOrNull<Player>("player");
		run = GameManager.Of(this)?.Run;

		// Scales the HUD alone — gameplay lives on Entities, a sibling of this
		// CanvasLayer, so nothing about the arena itself changes size.
		var uiLayer = gameRoot?.GetNodeOrNull<CanvasLayer>("UI");
		if (uiLayer != null && GameSettings.Instance != null)
			uiLayer.Scale = Vector2.One * GameSettings.Instance.UiScale;

		// The best score cannot change mid-orbit, so it only needs writing once.
		if (bestLabel != null)
			bestLabel.Text = ScoreManager.GetFormattedHighScore();

		// The banner is left-aligned in the HUD's left column, so it has to grow
		// rightward from its own left edge. Scaling from the box centre would
		// swing the text sideways, because the glyphs start at the left edge
		// while the box runs the full width of the column.
		if (streakLabel != null)
			streakLabel.PivotOffset = new Vector2(0f, streakLabel.Size.Y * 0.5f);

		if (run != null)
		{
			run.KillsChanged += OnKillsChanged;
			run.StreakChanged += OnStreakChanged;
			OnKillsChanged(run.Kills);
			OnStreakChanged(run.Streak, false);
		}

		RefreshKeyLabels();
		HideCursor();
	}

	public override void _ExitTree()
	{
		if (run == null || !IsInstanceValid(run))
			return;

		run.KillsChanged -= OnKillsChanged;
		run.StreakChanged -= OnStreakChanged;
	}

	/// <summary>
	/// The icon art bakes a key name into its top third, which goes stale the
	/// moment anything is rebound. The sprites are cropped to the icon itself and
	/// the key is drawn here instead, read from the live input map.
	/// </summary>
	private void RefreshKeyLabels()
	{
		SetKeyLabel(dashKeyLabel, "dash");
		SetKeyLabel(rapidFireKeyLabel, "rapid_fire");
	}

	private static void SetKeyLabel(Label label, string action)
	{
		if (label == null)
			return;

		Key key = GameSettings.GetActionKey(action);
		label.Text = key == Key.None ? "—" : OS.GetKeycodeString(key).ToUpperInvariant();
	}

	// This node is pausable, so _Process stops while the pause menu is open and
	// the icon states set by ShowCursor() are left alone.
	public override void _Process(double delta)
	{
		// The crosshair lives on a CanvasLayer, which ignores the camera. Aim is a
		// world position, so it has to be pushed through the canvas transform or
		// the reticle would drift with every screen shake.
		if (crosshair != null && crosshair.Visible && player != null)
			crosshair.GlobalPosition = GetViewport().CanvasTransform * player.AimPosition;

		UpdateAbilityIcons();
		UpdateRunLabels();
	}

	private void UpdateRunLabels()
	{
		if (run == null)
			return;

		if (timeLabel != null)
			timeLabel.Text = ScoreManager.FormatTime(run.SurvivalTime);

		// The live multiplier sits next to the score, because it is the only
		// place the player can see what carrying mass is actually buying them.
		if (scoreLabel != null)
			scoreLabel.Text = $"{run.Score:N0}   x{run.ScoreMultiplier:0.0}";

		RefreshEffects();
	}

	/// <summary>
	/// What is currently up, and for how long. Counting down beats a static icon:
	/// the decision a pickup creates is "how long have I got", not "do I have it".
	/// </summary>
	private void RefreshEffects()
	{
		if (effectsLabel == null)
			return;

		effects.Clear();

		// The event first and in its own colour: it is a rule change, not a buff,
		// and the player needs to know how long they are living under it.
		if (run.Event != ArenaEventId.Calm)
			effects.Add($"{ArenaEvents.Get(run.Event).Name} {Mathf.CeilToInt(run.EventTimeLeft)}");

		if (run.HasShield)
			effects.Add(PowerUps.Shield.Name);

		AppendTimed(PowerUpKind.Freeze);
		AppendTimed(PowerUpKind.Magnet);
		AppendTimed(PowerUpKind.Damage);

		effectsLabel.Visible = effects.Count > 0;
		effectsLabel.Text = string.Join("   ", effects);
	}

	private void AppendTimed(PowerUpKind kind)
	{
		float left = run.TimeLeft(kind);
		if (left > 0f)
			effects.Add($"{PowerUps.Get(kind).Name} {Mathf.CeilToInt(left)}");
	}

	private void OnKillsChanged(int kills)
	{
		if (killsLabel != null)
			killsLabel.Text = $"KILLS: {kills}";
	}

	private void OnStreakChanged(int streak, bool milestone)
	{
		if (streakLabel == null)
			return;

		// A streak of one is just a kill; only shout about actual chains.
		bool wasVisible = streakLabel.Visible;
		streakLabel.Visible = streak >= 2;
		streakLabel.Text = $"x{streak} STREAK";

		if (!streakLabel.Visible)
		{
			if (wasVisible)
				ResetStreakBanner();
			return;
		}

		// A milestone gets a much bigger punch so it reads without being counted.
		PopStreak(milestone ? 1.85f : 1.26f);
	}

	private void ResetStreakBanner()
	{
		streakPop?.Kill();
		streakLabel.Scale = Vector2.One;
		streakLabel.AddThemeColorOverride("font_color", StreakIdle);
	}

	/// <summary>Scale-and-flash punch on the streak banner, restarted on every kill.</summary>
	private void PopStreak(float scale)
	{
		streakPop?.Kill();
		streakLabel.Scale = new Vector2(scale, scale);
		streakLabel.AddThemeColorOverride("font_color", StreakFlash);

		streakPop = CreateTween().SetParallel();
		streakPop.TweenProperty(streakLabel, "scale", Vector2.One, 0.24f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		streakPop.TweenProperty(streakLabel, "theme_override_colors/font_color", StreakIdle, 0.3f);
	}

	private void UpdateAbilityIcons()
	{
		if (player == null)
			return;

		bool dashReady = player.GetDashCooldownPercent() >= 1.0f;
		bool rapidReady = player.GetRapidFireCooldownPercent() >= 1.0f && !player.IsRapidFiring();

		SetAbilityVisible(dashIcon, dashKeyLabel, dashReady);
		SetAbilityVisible(rapidFireIcon, rapidFireKeyLabel, rapidReady);
	}

	private static void SetAbilityVisible(Sprite2D icon, Label key, bool visible)
	{
		if (icon != null)
			icon.Visible = visible;

		if (key != null)
			key.Visible = visible;
	}

	public void ShowCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		SetHudVisible(false);
	}

	public void HideCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		SetHudVisible(true);
	}

	private void SetHudVisible(bool visible)
	{
		if (crosshair != null)
			crosshair.Visible = visible;

		SetAbilityVisible(dashIcon, dashKeyLabel, visible);
		SetAbilityVisible(rapidFireIcon, rapidFireKeyLabel, visible);
	}
}
