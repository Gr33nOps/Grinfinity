// PauseMenu.cs
using Godot;

public partial class PauseMenu : Control
{
	public override void _Ready()
	{
		Visible = false;
		ProcessMode = Node.ProcessModeEnum.WhenPaused;
		
		var canvasLayer = GetParent<CanvasLayer>();
		if (canvasLayer != null)
		{
			canvasLayer.Layer = 100;
		}
	}
	
	public void ShowPauseMenu()
	{
		Visible = true;
	}
	
	public void HidePauseMenu()
	{
		Visible = false;
	}
}
