using Godot;

public partial class UIManager : Node
{
	private Sprite2D crosshair;
	private Sprite2D dashIcon;
	private Sprite2D rapidFireIcon;
	private Label dashKeyLabel;
	private Label rapidFireKeyLabel;
	private Player player;

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
		player = gameRoot?.GetNodeOrNull<Player>("player");

		RefreshKeyLabels();
		HideCursor();
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
