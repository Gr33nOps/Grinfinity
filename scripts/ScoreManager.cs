using Godot;

public partial class ScoreManager : Node
{
	private float survivalTime = 0.0f;
	private Label scoreLabel;
	
	public override void _Ready()
	{
		scoreLabel = GetNode<Label>("/root/game/UI/ScoreLabel");
	}
	
	public override void _Process(double delta)
	{
		survivalTime += (float)delta;
		UpdateScoreDisplay();
	}
	
	private void UpdateScoreDisplay()
	{
		if (scoreLabel != null)
		{
			int minutes = (int)(survivalTime / 60);
			int seconds = (int)(survivalTime % 60);
			scoreLabel.Text = $"Survival Time: {minutes:D2}:{seconds:D2}";
		}
	}
	
	public float GetSurvivalTime()
	{
		return survivalTime;
	}
	
	public void ResetScore()
	{
		survivalTime = 0.0f;
	}
}
