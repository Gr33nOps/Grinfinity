using Godot;

public partial class UIManager : Node
{
	private Sprite2D crosshair;
	private Sprite2D dashIcon;
	private Sprite2D rapidFireIcon;
	private Player player;

	public override void _Ready()
	{
		// Resolved relative to the game root rather than by absolute path, so
		// renaming or reparenting the scene cannot silently break the HUD.
		var gameRoot = GetParent();
		crosshair = gameRoot?.GetNodeOrNull<Sprite2D>("CrosshairLayer/Crosshair");
		dashIcon = gameRoot?.GetNodeOrNull<Sprite2D>("UI/dash");
		rapidFireIcon = gameRoot?.GetNodeOrNull<Sprite2D>("UI/rapid_fire");
		player = gameRoot?.GetNodeOrNull<Player>("player");

		HideCursor();
	}

	// This node is pausable, so _Process stops while the pause menu is open and
	// the icon states set by ShowCursor() are left alone.
	public override void _Process(double delta)
	{
		if (crosshair != null && crosshair.Visible && player != null)
			crosshair.GlobalPosition = player.AimPosition;

		UpdateAbilityIcons();
	}

	private void UpdateAbilityIcons()
	{
		if (player == null)
			return;

		if (dashIcon != null)
			dashIcon.Visible = player.GetDashCooldownPercent() >= 1.0f;

		if (rapidFireIcon != null)
			rapidFireIcon.Visible = player.GetRapidFireCooldownPercent() >= 1.0f && !player.IsRapidFiring();
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

		if (dashIcon != null)
			dashIcon.Visible = visible;

		if (rapidFireIcon != null)
			rapidFireIcon.Visible = visible;
	}
}
