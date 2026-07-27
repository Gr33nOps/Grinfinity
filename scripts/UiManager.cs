using Godot;

public partial class UIManager : Node
{
	private Sprite2D crosshair;
	private Player player;
	private Sprite2D dashIcon;
	private Sprite2D rapidFireIcon;
	
	public override void _Ready()
	{
		SetupCrosshair();
		SetupAbilityIcons();
		player = GetNode<Player>("/root/game/player");
		HideCursor();
	}
	
	public override void _Process(double delta)
	{
		if (crosshair?.Visible == true)
			crosshair.GlobalPosition = crosshair.GetGlobalMousePosition();
		
		UpdateAbilityIcons();
	}
	
	private void SetupCrosshair()
	{
		crosshair = new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://sprites/crosshair.png"),
			ZIndex = 1000
		};
		GetParent().AddChild(crosshair);
	}
	
	private void SetupAbilityIcons()
	{
		dashIcon = GetNode<Sprite2D>("/root/game/UI/dash");
		rapidFireIcon = GetNode<Sprite2D>("/root/game/UI/rapid_fire");
	}
	
	private void UpdateAbilityIcons()
	{
		if (player == null) return;
		
		dashIcon.Visible = player.GetDashCooldownPercent() >= 1.0f;
		rapidFireIcon.Visible = player.GetRapidFireCooldownPercent() >= 1.0f && !player.IsRapidFiring();
	}
	
	public void ShowCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		crosshair.Visible = false;
		dashIcon.Visible = false;
		rapidFireIcon.Visible = false;
	}
	
	public void HideCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		crosshair.Visible = true;
	}
}
