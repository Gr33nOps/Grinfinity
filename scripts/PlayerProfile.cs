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
	private const string WorldSection = "worlds";
	private const string AchievementSection = "achievements";
	private const string WeaponTallySection = "weapon_tally";
	/// <summary>
	/// v2 dropped the permanent upgrade shop and turned stardust from a balance
	/// into a lifetime tally. Old files keep their "upgrades" section on disk —
	/// nothing reads it, and rewriting the file to drop it would only risk
	/// losing the parts still worth keeping.
	/// </summary>
	private const int SaveVersion = 2;

	private static bool isLoaded;

	// --- Lifetime stats ---------------------------------------------------
	/// <summary>
	/// Every point of stardust ever earned. Not a balance: there is nothing to
	/// spend it on outside a run, and the stardust a run spends on upgrades is
	/// the run's own, starting from zero each time. This is here to be looked
	/// at on the Stats screen and nothing else.
	/// </summary>
	public static int StardustEarned { get; private set; }

	public static int TotalOrbits { get; private set; }
	public static int TotalKills { get; private set; }
	public static float TotalTimePlayed { get; private set; }
	/// <summary>Highest normalised mass (0..1) ever carried, across every orbit.</summary>
	public static float HeaviestMassEver { get; private set; }

	/// <summary>
	/// The name that goes on the board. Defaulted rather than demanded: a first
	/// run should never be gated behind a text field, so an unnamed player still
	/// places and can put a name to it afterwards.
	/// </summary>
	public static string PlayerName { get; private set; } = "PLAYER";

	public static void SetPlayerName(string name)
	{
		EnsureLoaded();
		PlayerName = Leaderboard.Sanitise(name);
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

	/// <summary>
	/// Folds one finished orbit into the profile: stardust earned, lifetime
	/// totals, and which weapon it was played with. Called once, from
	/// GameManager.TriggerGameOver.
	/// </summary>
	public static void RecordOrbit(int stardustEarned, int kills, float survivalTime, float massNormalised, WeaponId weapon)
	{
		EnsureLoaded();

		StardustEarned += Mathf.Max(stardustEarned, 0);
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

		// A v1 file's "stardust" was a spendable balance. Reading it forward as
		// a lifetime tally understates anyone who spent some in the old shop,
		// which is the honest direction to be wrong in: it never claims they
		// earned more than they did.
		StardustEarned = Mathf.Max(config.GetValue(Section, "stardust", 0).AsInt32(), 0);
		TotalOrbits = Mathf.Max(config.GetValue(Section, "total_orbits", 0).AsInt32(), 0);
		TotalKills = Mathf.Max(config.GetValue(Section, "total_kills", 0).AsInt32(), 0);
		TotalTimePlayed = Mathf.Max(config.GetValue(Section, "total_time", 0.0f).AsSingle(), 0f);
		HeaviestMassEver = Mathf.Clamp(config.GetValue(Section, "heaviest_mass", 0.0f).AsSingle(), 0f, 1f);
		PlayerName = Leaderboard.Sanitise(config.GetValue(Section, "name", "PLAYER").AsString());

		foreach (int worldId in config.GetValue(WorldSection, "unlocked", new int[] { 1 }).AsInt32Array())
			unlockedWorlds.Add(worldId);

		foreach (string name in config.GetValue(AchievementSection, "unlocked", System.Array.Empty<string>()).AsStringArray())
		{
			if (System.Enum.TryParse(name, out AchievementId id))
				unlockedAchievements.Add(id);
		}

		foreach (WeaponId id in System.Enum.GetValues<WeaponId>())
			weaponTally[id] = Mathf.Max(config.GetValue(WeaponTallySection, id.ToString(), 0).AsInt32(), 0);
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "stardust", StardustEarned);
		config.SetValue(Section, "total_orbits", TotalOrbits);
		config.SetValue(Section, "total_kills", TotalKills);
		config.SetValue(Section, "total_time", TotalTimePlayed);
		config.SetValue(Section, "heaviest_mass", HeaviestMassEver);
		config.SetValue(Section, "name", PlayerName);

		var worlds = new int[unlockedWorlds.Count];
		unlockedWorlds.CopyTo(worlds);
		config.SetValue(WorldSection, "unlocked", worlds);

		var achievements = new string[unlockedAchievements.Count];
		int ai = 0;
		foreach (AchievementId id in unlockedAchievements)
			achievements[ai++] = id.ToString();
		config.SetValue(AchievementSection, "unlocked", achievements);

		foreach ((WeaponId id, int count) in weaponTally)
			config.SetValue(WeaponTallySection, id.ToString(), count);

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"PlayerProfile: could not write '{SavePath}' ({error}).");
	}
}
