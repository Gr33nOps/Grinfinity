using System.Collections.Generic;
using Godot;

/// <summary>
/// Progression that outlives a single orbit: stardust, lifetime stats, unlocked
/// worlds and achievements, and upgrade levels. Where <see cref="ScoreManager"/>
/// answers "what is the best I have ever done", this answers "what have I built
/// up over every orbit put together".
/// </summary>
public static class PlayerProfile
{
	private const string SavePath = "user://profile.cfg";
	private const string Section = "profile";
	private const string UpgradeSection = "upgrades";
	private const string WorldSection = "worlds";
	private const string AchievementSection = "achievements";
	private const string WeaponTallySection = "weapon_tally";
	private const int SaveVersion = 1;

	private static bool isLoaded;

	public static int Stardust { get; private set; }

	// --- Lifetime stats ---------------------------------------------------
	public static int TotalOrbits { get; private set; }
	public static int TotalKills { get; private set; }
	public static float TotalTimePlayed { get; private set; }
	/// <summary>Highest normalised mass (0..1) ever carried, across every orbit.</summary>
	public static float HeaviestMassEver { get; private set; }

	/// <summary><see cref="Modes.TodayKey"/> on the last day Daily Alignment was played, or 0 if never.</summary>
	public static int LastDailyAlignmentDay { get; private set; }
	/// <summary>The score that attempt earned, for the mode-select card to show while today's is locked.</summary>
	public static int LastDailyAlignmentScore { get; private set; }

	/// <summary>True once today's one Daily Alignment attempt has already been spent.</summary>
	public static bool PlayedDailyAlignmentToday
	{
		get { EnsureLoaded(); return LastDailyAlignmentDay == Modes.TodayKey(); }
	}

	/// <summary>Spends today's Daily Alignment attempt. Called once, from GameManager.TriggerGameOver.</summary>
	public static void RecordDailyAlignment(int score)
	{
		EnsureLoaded();
		LastDailyAlignmentDay = Modes.TodayKey();
		LastDailyAlignmentScore = Mathf.Max(score, 0);
		SaveToFile();
	}

	private static readonly Dictionary<WeaponId, int> weaponTally = new();

	/// <summary>The weapon most orbits have been launched with, or Comet if none yet.</summary>
	public static WeaponId FavouriteWeapon
	{
		get
		{
			EnsureLoaded();
			WeaponId best = WeaponId.Comet;
			int bestCount = -1;
			foreach (WeaponProfile weapon in WeaponProfile.All)
			{
				int count = weaponTally.GetValueOrDefault(weapon.Id, 0);
				if (count > bestCount)
				{
					bestCount = count;
					best = weapon.Id;
				}
			}
			return best;
		}
	}

	// --- Unlocks ------------------------------------------------------------
	private static readonly HashSet<int> unlockedWorlds = new() { 1 };
	private static readonly HashSet<AchievementId> unlockedAchievements = new();

	// --- Upgrades -------------------------------------------------------
	private static readonly Dictionary<UpgradeId, int> upgradeLevels = new();

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
		EnsureLoaded();
		if (!unlockedAchievements.Add(id))
			return false;

		SaveToFile();
		return true;
	}

	public static int UpgradeLevel(UpgradeId id)
	{
		EnsureLoaded();
		return upgradeLevels.GetValueOrDefault(id, 0);
	}

	/// <summary>Spends stardust to raise an upgrade one level, if both are available.</summary>
	public static bool TryBuyUpgrade(UpgradeId id, int cost, int maxLevel)
	{
		EnsureLoaded();
		int level = UpgradeLevel(id);
		if (level >= maxLevel || Stardust < cost)
			return false;

		Stardust -= cost;
		upgradeLevels[id] = level + 1;
		SaveToFile();
		return true;
	}

	/// <summary>
	/// Folds one finished orbit into the profile: stardust earned, lifetime
	/// totals, and which weapon it was played with. Called once, from
	/// GameManager.TriggerGameOver.
	/// </summary>
	public static void RecordOrbit(int stardustEarned, int kills, float survivalTime, float massNormalised, WeaponId weapon)
	{
		EnsureLoaded();

		Stardust += Mathf.Max(stardustEarned, 0);
		TotalOrbits++;
		TotalKills += Mathf.Max(kills, 0);
		TotalTimePlayed += Mathf.Max(survivalTime, 0f);
		HeaviestMassEver = Mathf.Max(HeaviestMassEver, massNormalised);
		weaponTally[weapon] = weaponTally.GetValueOrDefault(weapon, 0) + 1;

		SaveToFile();
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return;

		Stardust = Mathf.Max(config.GetValue(Section, "stardust", 0).AsInt32(), 0);
		TotalOrbits = Mathf.Max(config.GetValue(Section, "total_orbits", 0).AsInt32(), 0);
		TotalKills = Mathf.Max(config.GetValue(Section, "total_kills", 0).AsInt32(), 0);
		TotalTimePlayed = Mathf.Max(config.GetValue(Section, "total_time", 0.0f).AsSingle(), 0f);
		HeaviestMassEver = Mathf.Clamp(config.GetValue(Section, "heaviest_mass", 0.0f).AsSingle(), 0f, 1f);
		LastDailyAlignmentDay = config.GetValue(Section, "daily_day", 0).AsInt32();
		LastDailyAlignmentScore = Mathf.Max(config.GetValue(Section, "daily_score", 0).AsInt32(), 0);

		foreach (int worldId in config.GetValue(WorldSection, "unlocked", new int[] { 1 }).AsInt32Array())
			unlockedWorlds.Add(worldId);

		foreach (string name in config.GetValue(AchievementSection, "unlocked", System.Array.Empty<string>()).AsStringArray())
		{
			if (System.Enum.TryParse(name, out AchievementId id))
				unlockedAchievements.Add(id);
		}

		foreach (UpgradeId id in System.Enum.GetValues<UpgradeId>())
			upgradeLevels[id] = Mathf.Max(config.GetValue(UpgradeSection, id.ToString(), 0).AsInt32(), 0);

		foreach (WeaponId id in System.Enum.GetValues<WeaponId>())
			weaponTally[id] = Mathf.Max(config.GetValue(WeaponTallySection, id.ToString(), 0).AsInt32(), 0);
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "stardust", Stardust);
		config.SetValue(Section, "total_orbits", TotalOrbits);
		config.SetValue(Section, "total_kills", TotalKills);
		config.SetValue(Section, "total_time", TotalTimePlayed);
		config.SetValue(Section, "heaviest_mass", HeaviestMassEver);
		config.SetValue(Section, "daily_day", LastDailyAlignmentDay);
		config.SetValue(Section, "daily_score", LastDailyAlignmentScore);

		var worlds = new int[unlockedWorlds.Count];
		unlockedWorlds.CopyTo(worlds);
		config.SetValue(WorldSection, "unlocked", worlds);

		var achievements = new string[unlockedAchievements.Count];
		int ai = 0;
		foreach (AchievementId id in unlockedAchievements)
			achievements[ai++] = id.ToString();
		config.SetValue(AchievementSection, "unlocked", achievements);

		foreach ((UpgradeId id, int level) in upgradeLevels)
			config.SetValue(UpgradeSection, id.ToString(), level);

		foreach ((WeaponId id, int count) in weaponTally)
			config.SetValue(WeaponTallySection, id.ToString(), count);

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"PlayerProfile: could not write '{SavePath}' ({error}).");
	}
}
