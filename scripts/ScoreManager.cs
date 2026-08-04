using System.Collections.Generic;
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
	// v4 adds a per-mode table alongside the existing global fields — a v3
	// save's numbers become the global figures unchanged, and every mode
	// simply starts with nothing on record until it is actually played.
	private const int SaveVersion = 4;
	private const float MaxPlausibleTime = 36000.0f;

	private readonly struct ModeRecord
	{
		public ModeRecord(float time, int kills, int streak, int score)
		{
			Time = time;
			Kills = kills;
			Streak = streak;
			Score = score;
		}

		public float Time { get; }
		public int Kills { get; }
		public int Streak { get; }
		public int Score { get; }
	}

	private static float bestTime;
	private static int bestKills;
	private static int bestStreak;
	private static int bestScore;
	private static readonly Dictionary<GameMode, ModeRecord> modeRecords = new();
	private static bool isLoaded;

	/// <summary>Best of any mode — what the main menu shows. One number to beat, regardless of what was last played.</summary>
	public static float BestTime
	{
		get { EnsureLoaded(); return bestTime; }
	}

	/// <summary>Best of any mode. See <see cref="BestTime"/>.</summary>
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

	/// <summary>This mode's own best score, or 0 if it has never been played.</summary>
	public static int BestScoreFor(GameMode mode)
	{
		EnsureLoaded();
		return modeRecords.TryGetValue(mode, out ModeRecord record) ? record.Score : 0;
	}

	/// <summary>This mode's own best survival time, or 0 if it has never been played.</summary>
	public static float BestTimeFor(GameMode mode)
	{
		EnsureLoaded();
		return modeRecords.TryGetValue(mode, out ModeRecord record) ? record.Time : 0f;
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

	/// <summary>
	/// Records a finished orbit and reports which records it broke — against its
	/// own mode's table, not the global one. A 60-second Flyby should not have
	/// to out-score a ten-minute Endless Orbit run to earn a "NEW BEST!".
	/// </summary>
	public static Result SaveRun(GameMode mode, float time, int kills, int streak, int score)
	{
		EnsureLoaded();

		if (time > bestTime) bestTime = time;
		if (score > bestScore) bestScore = score;
		if (kills > bestKills) bestKills = kills;
		if (streak > bestStreak) bestStreak = streak;

		ModeRecord previous = modeRecords.GetValueOrDefault(mode, default);
		bool newBestTime = time > previous.Time;
		bool newBestScore = score > previous.Score;
		bool improved = newBestTime || newBestScore || kills > previous.Kills || streak > previous.Streak;

		modeRecords[mode] = new ModeRecord(
			Mathf.Max(time, previous.Time),
			Mathf.Max(kills, previous.Kills),
			Mathf.Max(streak, previous.Streak),
			Mathf.Max(score, previous.Score));

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

			foreach (GameMode mode in System.Enum.GetValues<GameMode>())
			{
				string prefix = $"mode_{mode}_";
				float time = config.GetValue(Section, prefix + "time", 0.0f).AsSingle();
				int kills = config.GetValue(Section, prefix + "kills", 0).AsInt32();
				int streak = config.GetValue(Section, prefix + "streak", 0).AsInt32();
				int score = config.GetValue(Section, prefix + "score", 0).AsInt32();

				if (time > 0f || kills > 0 || streak > 0 || score > 0)
					modeRecords[mode] = new ModeRecord(time, kills, streak, score);
			}

			// A pre-M6 save has a global best but no mode table yet. That best
			// was always Endless Orbit — it was the only mode that existed —
			// so it seeds that mode's record instead of starting it at zero.
			if (!modeRecords.ContainsKey(GameMode.EndlessOrbit) && (bestScore > 0 || bestTime > 0f))
				modeRecords[GameMode.EndlessOrbit] = new ModeRecord(bestTime, bestKills, bestStreak, bestScore);
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

		foreach ((GameMode mode, ModeRecord record) in modeRecords)
		{
			string prefix = $"mode_{mode}_";
			config.SetValue(Section, prefix + "time", record.Time);
			config.SetValue(Section, prefix + "kills", record.Kills);
			config.SetValue(Section, prefix + "streak", record.Streak);
			config.SetValue(Section, prefix + "score", record.Score);
		}

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"ScoreManager: could not write '{SavePath}' ({error}).");
	}
}
