using Godot;
public partial class ScoreManager : Node
{
	private float survivalTime = 0.0f;
	private Label scoreLabel;
	private Label highScoreLabel;
	private bool isPaused = false; // Add pause state
	
	private const string SAVE_FILE = "user://highscore.save";
	private static float bestTime = 0.0f;
	private static ScoreManager instance;
	
	public override void _Ready()
	{
		instance = this;
		LoadHighScore();
		
		scoreLabel = GetNode<Label>("/root/game/UI/ScoreLabel");
		highScoreLabel = GetNode<Label>("/root/game/UI/HighScoreLabel");
	}
	
	public override void _Process(double delta)
	{
		// Only update survival time if not paused
		if (!isPaused)
		{
			survivalTime += (float)delta;
		}
		
		UpdateScoreDisplay();
		UpdateHighScoreDisplay();
	}
	
	// Add method to set pause state
	public void SetPaused(bool paused)
	{
		isPaused = paused;
	}
	
	private void UpdateScoreDisplay()
	{
		if (scoreLabel != null)
		{
			int minutes = (int)(survivalTime / 60);
			int seconds = (int)(survivalTime % 60);
			scoreLabel.Text = $"{minutes:D2}:{seconds:D2}";
		}
	}
	
	private void UpdateHighScoreDisplay()
	{
		if (highScoreLabel != null)
		{
			highScoreLabel.Text = GetFormattedHighScore();
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
	
	// High Score Methods
	public static ScoreManager GetInstance()
	{
		return instance;
	}
	
	public void SaveHighScore(float time)
	{
		if (time > bestTime)
		{
			bestTime = time;
			SaveToFile();
		}
	}
	
	public float GetHighScore()
	{
		return bestTime;
	}
	
	public string GetFormattedHighScore()
	{
		if (bestTime <= 0)
		{
			return "BEST: 00:00";
		}
		
		int minutes = (int)(bestTime / 60);
		int seconds = (int)(bestTime % 60);
		return $"BEST: {minutes:D2}:{seconds:D2}";
	}
	
	private void SaveToFile()
	{
		var file = FileAccess.Open(SAVE_FILE, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreFloat(bestTime);
			file.Close();
		}
	}
	
	private void LoadHighScore()
	{
		var file = FileAccess.Open(SAVE_FILE, FileAccess.ModeFlags.Read);
		if (file != null)
		{
			bestTime = file.GetFloat();
			file.Close();
			
			if (bestTime < 0 || bestTime > 36000) 
			{
				bestTime = 0;
				SaveToFile(); 
			}
		}
	}
	
	public void ResetHighScore()
	{
		bestTime = 0;
		SaveToFile();
	}
}
