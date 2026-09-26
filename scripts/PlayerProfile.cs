using System.Collections.Generic;
using Godot;

/// <summary>
/// Progression that outlives a single orbit: lifetime stats, unlocked
/// worlds and achievements, and upgrade levels. Where <see cref="ScoreManager"/>
/// answers "what is the best I have ever done", this answers "what have I built
/// up over every orbit put together".
/// </summary>
public static class PlayerProfile
{
	private const string SavePath = "user://profile.cfg";
	private const string Section = "profile";
	private const string WorldSection = "worlds";
	private const string AchievementSection = "achievements";
	/// <summary>
	/// v2 dropped the permanent upgrade shop. Old files keep their "upgrades"
	/// and "stardust" entries on disk —
	/// nothing reads it, and rewriting the file to drop it would only risk
	/// losing the parts still worth keeping.
	/// </summary>
	private const int SaveVersion = 2;

	private static bool isLoaded;

	// --- Lifetime stats ---------------------------------------------------
	// Every one of these loads on access. They used to be plain auto-properties,
	// which meant reading one before anything else had touched the profile
	// returned a zero — the Stats screen only ever looked right because the
	// weapon and world rows happened to be read first and pulled the file in as
	// a side effect. Reordering that screen was enough to blank four of them.
	private static int totalOrbits;
	private static int totalKills;
	private static float totalTimePlayed;
	private static float heaviestMassEver;
	private static string playerName = "PLAYER";

	public static int TotalOrbits
	{
		get { EnsureLoaded(); return totalOrbits; }
		private set => totalOrbits = value;
	}

	public static int TotalKills
	{
		get { EnsureLoaded(); return totalKills; }
		private set => totalKills = value;
	}

	public static float TotalTimePlayed
	{
		get { EnsureLoaded(); return totalTimePlayed; }
		private set => totalTimePlayed = value;
	}

	/// <summary>
	/// Fullest build ever held, 0..1, across every run. Kept under its old name
	/// and save key: it used to be peak mass, and a world still unlocks at 100%.
	/// </summary>
	public static float HeaviestMassEver
	{
		get { EnsureLoaded(); return heaviestMassEver; }
		private set => heaviestMassEver = value;
	}

	/// <summary>
	/// The name that goes on the board. Defaulted rather than demanded: a first
	/// run should never be gated behind a text field, so an unnamed player still
	/// places and can put a name to it afterwards.
	/// </summary>
	public static string PlayerName
	{
		get { EnsureLoaded(); return playerName; }
		private set => playerName = value;
	}

	public static void SetPlayerName(string name)
	{
		EnsureLoaded();
		PlayerName = Leaderboard.Sanitise(name);
		SaveToFile();
	}

	// --- Unlocks ------------------------------------------------------------
	private static readonly HashSet<int> unlockedWorlds = new() { 1 };
	private static readonly HashSet<AchievementId> unlockedAchievements = new();

	public static bool IsWorldUnlocked(int worldId)
	{
		EnsureLoaded();
		return unlockedWorlds.Contains(worldId);
	}

	/// <summary>Unlocks a world. Returns true if it was newly unlocked.</summary>
	public static bool UnlockWorld(int worldId)
	{
		EnsureLoaded();
		if (!unlockedWorlds.Add(worldId))
			return false;

		SaveToFile();
		return true;
	}

	public static bool IsAchievementUnlocked(AchievementId id)
	{
		EnsureLoaded();
		return unlockedAchievements.Contains(id);
	}

	/// <summary>Unlocks an achievement. Returns true if it was newly unlocked.</summary>
	public static bool UnlockAchievement(AchievementId id)
	{
		// Nothing earned in a boss test counts.
		if (BossTest.Active)
			return false;
		EnsureLoaded();
		if (!unlockedAchievements.Add(id))
			return false;

		SaveToFile();
		return true;
	}

	/// <summary>
	/// Folds one finished orbit into the lifetime totals. Called once, from
	/// GameManager.TriggerGameOver.
	/// </summary>
	public static void RecordOrbit(int kills, float survivalTime, float buildFraction)
	{
		EnsureLoaded();

		TotalOrbits++;
		TotalKills += Mathf.Max(kills, 0);
		TotalTimePlayed += Mathf.Max(survivalTime, 0f);
		HeaviestMassEver = Mathf.Max(HeaviestMassEver, buildFraction);

		SaveToFile();
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (SaveStore.Load(config, SavePath) != Error.Ok)
			return;

		TotalOrbits = Mathf.Max(SaveStore.Value(config, Section, "total_orbits", 0).AsInt32(), 0);
		TotalKills = Mathf.Max(SaveStore.Value(config, Section, "total_kills", 0).AsInt32(), 0);
		TotalTimePlayed = Mathf.Max(SaveStore.Value(config, Section, "total_time", 0.0f).AsSingle(), 0f);
		HeaviestMassEver = Mathf.Clamp(SaveStore.Value(config, Section, "heaviest_mass", 0.0f).AsSingle(), 0f, 1f);
		PlayerName = Leaderboard.Sanitise(SaveStore.Value(config, Section, "name", "PLAYER").AsString());

		// An older save numbers the planets its own way; carry each over to its
		// planet today, dropping any since removed.
		int layout = SaveStore.Value(config, WorldSection, "layout", 12).AsInt32();
		foreach (int worldId in SaveStore.Value(config, WorldSection, "unlocked", new int[] { 1 }).AsInt32Array())
		{
			int id = Worlds.Migrate(layout, worldId);
			if (id >= 1 && id <= Worlds.All.Length)
				unlockedWorlds.Add(id);
		}

		foreach (string name in SaveStore.Value(config, AchievementSection, "unlocked", System.Array.Empty<string>()).AsStringArray())
		{
			if (System.Enum.TryParse(name, out AchievementId id))
				unlockedAchievements.Add(id);
		}

	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "total_orbits", TotalOrbits);
		config.SetValue(Section, "total_kills", TotalKills);
		config.SetValue(Section, "total_time", TotalTimePlayed);
		config.SetValue(Section, "heaviest_mass", HeaviestMassEver);
		config.SetValue(Section, "name", PlayerName);

		var worlds = new int[unlockedWorlds.Count];
		unlockedWorlds.CopyTo(worlds);
		config.SetValue(WorldSection, "unlocked", worlds);
		config.SetValue(WorldSection, "layout", Worlds.Layout);

		var achievements = new string[unlockedAchievements.Count];
		int ai = 0;
		foreach (AchievementId id in unlockedAchievements)
			achievements[ai++] = id.ToString();
		config.SetValue(AchievementSection, "unlocked", achievements);


		Error error = SaveStore.Save(config, SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"PlayerProfile: could not write '{SavePath}' ({error}).");
	}
}
