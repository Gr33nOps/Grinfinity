// Enhanced UIManager.cs with Ability Cooldown Display
using Godot;

public partial class UIManager : Node
{
	private Sprite2D crosshair;
	private Player player;
	
	// Ability UI elements
	private Control abilityUI;
	private ProgressBar dashCooldownBar;
	private ProgressBar rapidFireCooldownBar;
	private Label dashLabel;
	private Label rapidFireLabel;
	private Label rapidFireStatusLabel;
	
	public override void _Ready()
	{
		SetupCrosshair();
		SetupAbilityUI();
		GetPlayerReference();
		HideCursor();
	}
	
	public override void _Process(double delta)
	{
		if (crosshair != null && crosshair.Visible)
		{
			crosshair.GlobalPosition = crosshair.GetGlobalMousePosition();
		}
		
		UpdateAbilityUI();
	}
	
	private void GetPlayerReference()
	{
		player = GetNode<Player>("/root/game/player");
	}
	
	private void SetupCrosshair()
	{
		crosshair = new Sprite2D();
		crosshair.Texture = GD.Load<Texture2D>("res://sprites/crosshair.png");
		crosshair.ZIndex = 1000;
		GetParent().AddChild(crosshair);
	}
	
	private void SetupAbilityUI()
	{
		// Get the game scene to add UI to
		var gameScene = GetTree().CurrentScene;
		
		// Create ability UI container - simple positioning
		abilityUI = new Control();
		abilityUI.Position = new Vector2(50, 50); // Top-left for now, easier to see
		abilityUI.Size = new Vector2(300, 100);
		gameScene.AddChild(abilityUI);
		
		// Add a background so we can see the UI area
		var background = new ColorRect();
		background.Size = new Vector2(300, 100);
		background.Color = new Color(0, 0, 0, 0.5f); // Semi-transparent black
		abilityUI.AddChild(background);
		
		// Dash ability UI
		dashLabel = new Label();
		dashLabel.Text = "DASH (Space) - READY";
		dashLabel.Position = new Vector2(10, 10);
		dashLabel.AddThemeColorOverride("font_color", Colors.White);
		abilityUI.AddChild(dashLabel);
		
		dashCooldownBar = new ProgressBar();
		dashCooldownBar.Position = new Vector2(10, 30);
		dashCooldownBar.Size = new Vector2(120, 20);
		dashCooldownBar.Value = 100;
		abilityUI.AddChild(dashCooldownBar);
		
		// Rapid Fire ability UI
		rapidFireLabel = new Label();
		rapidFireLabel.Text = "RAPID FIRE (Q) - READY";
		rapidFireLabel.Position = new Vector2(150, 10);
		rapidFireLabel.AddThemeColorOverride("font_color", Colors.White);
		abilityUI.AddChild(rapidFireLabel);
		
		rapidFireCooldownBar = new ProgressBar();
		rapidFireCooldownBar.Position = new Vector2(150, 30);
		rapidFireCooldownBar.Size = new Vector2(120, 20);
		rapidFireCooldownBar.Value = 100;
		abilityUI.AddChild(rapidFireCooldownBar);
		
		rapidFireStatusLabel = new Label();
		rapidFireStatusLabel.Text = "";
		rapidFireStatusLabel.Position = new Vector2(150, 55);
		rapidFireStatusLabel.AddThemeColorOverride("font_color", Colors.Orange);
		abilityUI.AddChild(rapidFireStatusLabel);
		
		GD.Print("Ability UI created and added to scene");
	}
	
	private StyleBoxFlat CreateStyleBox(Color color)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = color;
		styleBox.CornerRadiusBottomLeft = 3;
		styleBox.CornerRadiusBottomRight = 3;
		styleBox.CornerRadiusTopLeft = 3;
		styleBox.CornerRadiusTopRight = 3;
		return styleBox;
	}
	
	private void UpdateAbilityUI()
	{
		if (player == null) return;
		
		// Update dash cooldown
		float dashPercent = player.GetDashCooldownPercent();
		dashCooldownBar.Value = dashPercent * 100;
		
		if (dashPercent >= 1.0f)
		{
			dashLabel.Text = "DASH READY!";
			dashLabel.AddThemeColorOverride("font_color", Colors.Cyan);
		}
		else
		{
			dashLabel.Text = "DASH (Space)";
			dashLabel.AddThemeColorOverride("font_color", Colors.White);
		}
		
		// Update rapid fire cooldown
		float rapidFirePercent = player.GetRapidFireCooldownPercent();
		rapidFireCooldownBar.Value = rapidFirePercent * 100;
		
		if (player.IsRapidFiring())
		{
			rapidFireLabel.Text = "RAPID FIRE!";
			rapidFireLabel.AddThemeColorOverride("font_color", Colors.Orange);
			float timeLeft = player.GetRapidFireTimeLeft();
			rapidFireStatusLabel.Text = $"{timeLeft:F1}s";
		}
		else if (rapidFirePercent >= 1.0f)
		{
			rapidFireLabel.Text = "RAPID FIRE READY!";
			rapidFireLabel.AddThemeColorOverride("font_color", Colors.Orange);
			rapidFireStatusLabel.Text = "";
		}
		else
		{
			rapidFireLabel.Text = "RAPID FIRE (Q)";
			rapidFireLabel.AddThemeColorOverride("font_color", Colors.White);
			rapidFireStatusLabel.Text = "";
		}
	}
	
	public void ShowCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (crosshair != null) crosshair.Visible = false;
		if (abilityUI != null) abilityUI.Visible = false;
	}
	
	public void HideCursor()
	{
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		if (crosshair != null) crosshair.Visible = true;
		if (abilityUI != null) abilityUI.Visible = true;
	}
}
