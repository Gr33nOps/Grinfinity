using Godot;

/// <summary>
/// Persistent records only. The live numbers for an orbit in progress belong to
/// <see cref="RunState"/>; this class knows nothing about a run until it ends.
/// </summary>
public static class ScoreManager
{
	private const string SavePath = "user://highscore.cfg";
	private const string LegacySavePath = "user://highscore.save";
	private const string Section = "score";

	// v3 adds best_score, which arrived with the mass-weighted scoring in M2.
	// v2 saves load unchanged and simply start with no score on record.
	private const int SaveVersion = 3;
	private const float MaxPlausibleTime = 36000.0f;

	private static float bestTime;
	private static int bestKills;
	private static int bestStreak;
	private static int bestScore;
	private static bool isLoaded;

	public static float BestTime
	{
		get { EnsureLoaded(); return bestTime; }
	}

	public static int BestScore
	{
		get { EnsureLoaded(); return bestScore; }
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
		return $"BEST: {bestScore}";
	}

	/// <summary>What a finished orbit beat, so the recap can celebrate the right thing.</summary>
	public readonly struct Result
	{
		public Result(bool newBestScore, bool newBestTime)
		{
			NewBestScore = newBestScore;
			NewBestTime = newBestTime;
		}

		public bool NewBestScore { get; }
		public bool NewBestTime { get; }
	}

	/// <summary>Records a finished orbit and reports which records it broke.</summary>
	public static Result SaveRun(float time, int kills, int streak, int score)
	{
		EnsureLoaded();

		bool newBestTime = time > bestTime;
		bool newBestScore = score > bestScore;
		bool improved = newBestTime || newBestScore || kills > bestKills || streak > bestStreak;

		if (newBestTime)
			bestTime = time;
		if (newBestScore)
			bestScore = score;
		if (kills > bestKills)
			bestKills = kills;
		if (streak > bestStreak)
			bestStreak = streak;

		if (improved)
			SaveToFile();

		return new Result(newBestScore, newBestTime);
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
			bestKills = config.GetValue(Section, "best_kills", 0).AsInt32();
			bestStreak = config.GetValue(Section, "best_combo", 0).AsInt32();
			bestScore = config.GetValue(Section, "best_score", 0).AsInt32();
		}
		else if (FileAccess.FileExists(LegacySavePath))
		{
			// Carry a pre-existing best time over from the old raw-float format.
			bestTime = ReadLegacyBestTime();
			if (bestTime > 0)
				SaveToFile();
		}

		if (!float.IsFinite(bestTime) || bestTime < 0 || bestTime > MaxPlausibleTime)
			bestTime = 0;

		bestKills = Mathf.Max(bestKills, 0);
		bestStreak = Mathf.Max(bestStreak, 0);
		bestScore = Mathf.Max(bestScore, 0);
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
		config.SetValue(Section, "best_kills", bestKills);
		// The key keeps its v1 name so old saves still load; "combo" became
		// "streak" in the fiction, not in the file format.
		config.SetValue(Section, "best_combo", bestStreak);
		config.SetValue(Section, "best_score", bestScore);

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"ScoreManager: could not write '{SavePath}' ({error}).");
	}
}
