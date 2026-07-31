using Godot;

public partial class ScoreManager : Node
{
	private const string SavePath = "user://highscore.cfg";
	private const string LegacySavePath = "user://highscore.save";
	private const string Section = "score";
	private const int SaveVersion = 1;
	private const float MaxPlausibleTime = 36000.0f;

	private static float bestTime = 0.0f;
	private static bool isLoaded = false;

	private float survivalTime = 0.0f;
	private Label scoreLabel;
	private Label highScoreLabel;

	public override void _Ready()
	{
		EnsureLoaded();

		var gameRoot = GetParent();
		scoreLabel = gameRoot?.GetNodeOrNull<Label>("UI/ScoreLabel");
		highScoreLabel = gameRoot?.GetNodeOrNull<Label>("UI/HighScoreLabel");

		// The best time cannot change mid-run, so it only needs writing once.
		if (highScoreLabel != null)
			highScoreLabel.Text = GetFormattedHighScore();

	}

	public override void _Process(double delta)
	{
		survivalTime += (float)delta;

		if (scoreLabel != null)
			scoreLabel.Text = FormatTime(survivalTime);
	}

	public float GetSurvivalTime()
	{
		return survivalTime;
	}

	public static string FormatTime(float seconds)
	{
		int minutes = (int)(seconds / 60);
		int remainder = (int)(seconds % 60);
		return $"{minutes:D2}:{remainder:D2}";
	}

	public static string GetFormattedHighScore()
	{
		EnsureLoaded();
		return $"BEST: {FormatTime(bestTime)}";
	}

	public static void SaveHighScore(float time)
	{
		EnsureLoaded();

		if (time <= bestTime)
			return;

		bestTime = time;
		SaveToFile();
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (config.Load(SavePath) == Error.Ok)
		{
			bestTime = config.GetValue(Section, "best_time", 0.0f).AsSingle();
		}
		else if (FileAccess.FileExists(LegacySavePath))
		{
			// Carry a pre-existing best time over from the old raw-float format.
			bestTime = ReadLegacyBestTime();
			if (bestTime > 0)
				SaveToFile();
		}

		if (!float.IsFinite(bestTime) || bestTime < 0 || bestTime > MaxPlausibleTime)
		{
			bestTime = 0;
		}
	}

	private static float ReadLegacyBestTime()
	{
		using var file = FileAccess.Open(LegacySavePath, FileAccess.ModeFlags.Read);
		if (file == null || file.GetLength() < sizeof(float))
			return 0f;

		return file.GetFloat();
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "best_time", bestTime);

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"ScoreManager: could not write '{SavePath}' ({error}).");
	}
}
