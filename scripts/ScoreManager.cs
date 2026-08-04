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
	// v4 added a per-mode table. v5 removes it again: there is one mode, so
	// the global fields are the whole record. Endless Orbit's v4 row is read
	// once on the way through and the rest is left behind.
	private const int SaveVersion = 5;
	private const float MaxPlausibleTime = 36000.0f;

	private static float bestTime;
	private static int bestKills;
	private static int bestStreak;
	private static int bestScore;
	private static bool isLoaded;

	/// <summary>The longest orbit yet. One mode means one number to beat.</summary>
	public static float BestTime
	{
		get { EnsureLoaded(); return bestTime; }
	}

	/// <summary>The highest score yet. See <see cref="BestTime"/>.</summary>
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
		return string.Format(TranslationServer.Translate("UI_BEST_SCORE_LABEL"), bestScore);
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

		bestTime = Mathf.Max(time, bestTime);
		bestScore = Mathf.Max(score, bestScore);
		bestKills = Mathf.Max(kills, bestKills);
		bestStreak = Mathf.Max(streak, bestStreak);

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

			// v4 kept a table per mode, and the globals above were the best of
			// all of them — so a Flyby score could be sitting in a number that
			// now claims to be an Endless Orbit record. Endless Orbit's own row
			// is the only honest one to carry forward, and it wins wherever the
			// two disagree. The other four modes' rows are left unread.
			float endlessTime = config.GetValue(Section, "mode_EndlessOrbit_time", 0.0f).AsSingle();
			int endlessKills = config.GetValue(Section, "mode_EndlessOrbit_kills", 0).AsInt32();
			int endlessStreak = config.GetValue(Section, "mode_EndlessOrbit_streak", 0).AsInt32();
			int endlessScore = config.GetValue(Section, "mode_EndlessOrbit_score", 0).AsInt32();

			if (endlessTime > 0f || endlessScore > 0 || endlessKills > 0 || endlessStreak > 0)
			{
				bestTime = endlessTime;
				bestKills = endlessKills;
				bestStreak = endlessStreak;
				bestScore = endlessScore;
			}
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
