// GameOverMenu.cs
using Godot;

public partial class GameOverMenu : Control
{
	[Signal]
	public delegate void RestartGameEventHandler();
	
	private Label survivalTimeLabel;
	
	public override void _Ready()
	{
		Visible = false;
		ProcessMode = Node.ProcessModeEnum.Always;
		
		if (HasNode("SurvivalTimeLabel"))
		{
			survivalTimeLabel = GetNode<Label>("SurvivalTimeLabel");
		}
		
		var canvasLayer = GetParent<CanvasLayer>();
		if (canvasLayer != null)
		{
			canvasLayer.Layer = 200;
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (!Visible) return;
		
		if (@event.IsActionPressed("ui_accept"))
		{
			EmitSignal(SignalName.RestartGame);
		}
		
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter)
			{
				EmitSignal(SignalName.RestartGame);
			}
		}
	}
	
	public void ShowGameOver(float survivalTime = 0.0f)
	{
		Visible = true;
		
		if (survivalTimeLabel != null)
		{
			int minutes = (int)(survivalTime / 60);
			int seconds = (int)(survivalTime % 60);
			int milliseconds = (int)((survivalTime % 1) * 100);
			survivalTimeLabel.Text = $"You survived: {minutes:D2}:{seconds:D2}";
		}
	}
	
	public void HideGameOver()
	{
		Visible = false;
	}
}
