using Godot;

public partial class UIManager : Node
{
	private Sprite2D crosshair;
	
	public override void _Ready()
	{
		SetupCrosshair();
		HideCursor();
	}
	
	public override void _Process(double delta)
	{
		if (crosshair != null && crosshair.Visible)
		{
			crosshair.GlobalPosition = crosshair.GetGlobalMousePosition();
		}
	}
	
	private void SetupCrosshair()
	{
		crosshair = new Sprite2D();
		crosshair.Texture = GD.Load<Texture2D>("res://sprites/crosshair.png");
		crosshair.ZIndex = 1000;
		GetParent().AddChild(crosshair);
	}
	
	public void ShowCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (crosshair != null) crosshair.Visible = false;
	}
	
	public void HideCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		if (crosshair != null) crosshair.Visible = true;
	}
}
