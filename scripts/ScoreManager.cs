using Godot;

/// <summary>
/// Persistent records only: the best time, plus the most kills and longest
/// streak as quiet extras. There is no score; time is the only result. The live numbers for an orbit in progress belong to
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
	private static bool isLoaded;

	/// <summary>The longest orbit yet. One mode means one number to beat.</summary>
	public static float BestTime
	{
		get { EnsureLoaded(); return bestTime; }
	}

	public static string FormatTime(float seconds)
	{
		int minutes = (int)(seconds / 60);
		int remainder = (int)(seconds % 60);
		return $"{minutes:D2}:{remainder:D2}";
	}

	/// <summary>Records a finished orbit. True if it was the longest yet.</summary>
	public static bool SaveRun(float time, int kills, int streak)
	{
		EnsureLoaded();
		if (!float.IsFinite(time) || time < 0 || time > MaxPlausibleTime || kills < 0 || streak < 0)
			return false;

		bool newBestTime = time > bestTime;
		bool improved = newBestTime || kills > bestKills || streak > bestStreak;

		bestTime = Mathf.Max(time, bestTime);
		bestKills = Mathf.Max(kills, bestKills);
		bestStreak = Mathf.Max(streak, bestStreak);

		if (improved)
			SaveToFile();

		return newBestTime;
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (SaveStore.Load(config, SavePath) == Error.Ok)
		{
			bestTime = SaveStore.Value(config, Section, "best_time", 0.0f).AsSingle();
			bestKills = SaveStore.Value(config, Section, "best_kills", 0).AsInt32();
			bestStreak = SaveStore.Value(config, Section, "best_combo", 0).AsInt32();

			// v4 kept a table per mode, and the globals above were the best of
			// all of them — so a Flyby record could be sitting in a number that
			// now claims to be an Endless Orbit record. Endless Orbit's own row
			// is the only honest one to carry forward, and it wins wherever the
			// two disagree. The other four modes' rows are left unread.
			float endlessTime = SaveStore.Value(config, Section, "mode_EndlessOrbit_time", 0.0f).AsSingle();
			int endlessKills = SaveStore.Value(config, Section, "mode_EndlessOrbit_kills", 0).AsInt32();
			int endlessStreak = SaveStore.Value(config, Section, "mode_EndlessOrbit_streak", 0).AsInt32();

			if (endlessTime > 0f || endlessKills > 0 || endlessStreak > 0)
			{
				bestTime = endlessTime;
				bestKills = endlessKills;
				bestStreak = endlessStreak;
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

		Error error = SaveStore.Save(config, SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"ScoreManager: could not write '{SavePath}' ({error}).");
	}
}
