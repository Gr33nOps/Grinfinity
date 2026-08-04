using System.Collections.Generic;
using Godot;

/// <summary>
/// The local top-10, per the roadmap: score, weapon, world and date for each
/// entry. Separate from <see cref="ScoreManager"/>'s single best, because a
/// leaderboard needs to keep the ninth-best run even after a tenth arrives to
/// bump someone off it — one scalar can't do that. Kept per <see cref="GameMode"/>
/// since M6 — a Flyby's 60-second sprint and an Endless Orbit survival run are
/// not the same currency, so they get their own top-10 each.
/// </summary>
public static class Leaderboard
{
	public const int Capacity = 10;
	private const string SavePath = "user://leaderboard.cfg";
	private const string LegacySection = "leaderboard";
	// v2 splits the single table into one per GameMode. A v1 file's entries
	// were all played before modes existed, so they migrate straight into
	// Endless Orbit's bucket rather than being lost.
	private const int SaveVersion = 2;

	public readonly struct Entry
	{
		public Entry(int score, float survivalTime, int kills, WeaponId weapon, int world, string date)
		{
			Score = score;
			SurvivalTime = survivalTime;
			Kills = kills;
			Weapon = weapon;
			World = world;
			Date = date;
		}

		public int Score { get; }
		public float SurvivalTime { get; }
		public int Kills { get; }
		public WeaponId Weapon { get; }
		public int World { get; }
		/// <summary>ISO date only (no time) — a leaderboard does not need finer than a day.</summary>
		public string Date { get; }
	}

	private static readonly Dictionary<GameMode, List<Entry>> entriesByMode = new();
	private static bool isLoaded;

	/// <summary>This mode's top entries, best first. Empty if none have been played yet.</summary>
	public static IReadOnlyList<Entry> EntriesFor(GameMode mode)
	{
		EnsureLoaded();
		return entriesByMode.TryGetValue(mode, out List<Entry> list) ? list : System.Array.Empty<Entry>();
	}

	/// <summary>
	/// Records a finished orbit against its own mode's table. Returns the
	/// 1-based rank it landed at within that mode, or -1 if it did not place
	/// in the top <see cref="Capacity"/>.
	/// </summary>
	public static int Submit(GameMode mode, int score, float survivalTime, int kills, WeaponId weapon, int world)
	{
		EnsureLoaded();

		if (!entriesByMode.TryGetValue(mode, out List<Entry> list))
			entriesByMode[mode] = list = new List<Entry>();

		var entry = new Entry(score, survivalTime, kills, weapon, world, Time.GetDateStringFromSystem());

		int insertAt = list.FindIndex(e => score > e.Score);
		if (insertAt < 0)
		{
			if (list.Count >= Capacity)
				return -1;
			insertAt = list.Count;
		}

		list.Insert(insertAt, entry);
		if (list.Count > Capacity)
			list.RemoveRange(Capacity, list.Count - Capacity);

		SaveToFile();
		return insertAt + 1;
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return;

		int version = config.GetValue(LegacySection, "version", 1).AsInt32();

		if (version < 2)
		{
			LoadSection(LegacySection, GameMode.EndlessOrbit, config);
			return;
		}

		foreach (GameMode mode in System.Enum.GetValues<GameMode>())
			LoadSection($"{LegacySection}_{mode}", mode, config);
	}

	private static void LoadSection(string section, GameMode mode, ConfigFile config)
	{
		int count = Mathf.Clamp(config.GetValue(section, "count", 0).AsInt32(), 0, Capacity);
		if (count <= 0)
			return;

		var list = new List<Entry>(count);
		for (int i = 0; i < count; i++)
		{
			int score = config.GetValue(section, $"score_{i}", 0).AsInt32();
			float time = config.GetValue(section, $"time_{i}", 0.0f).AsSingle();
			int kills = config.GetValue(section, $"kills_{i}", 0).AsInt32();
			int weaponRaw = config.GetValue(section, $"weapon_{i}", 0).AsInt32();
			int world = Mathf.Clamp(config.GetValue(section, $"world_{i}", 1).AsInt32(), 1, 12);
			string date = config.GetValue(section, $"date_{i}", "").AsString();

			WeaponId weapon = System.Enum.IsDefined(typeof(WeaponId), weaponRaw) ? (WeaponId)weaponRaw : WeaponId.Comet;
			list.Add(new Entry(score, time, kills, weapon, world, date));
		}

		entriesByMode[mode] = list;
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(LegacySection, "version", SaveVersion);

		foreach ((GameMode mode, List<Entry> list) in entriesByMode)
		{
			string section = $"{LegacySection}_{mode}";
			config.SetValue(section, "count", list.Count);

			for (int i = 0; i < list.Count; i++)
			{
				Entry entry = list[i];
				config.SetValue(section, $"score_{i}", entry.Score);
				config.SetValue(section, $"time_{i}", entry.SurvivalTime);
				config.SetValue(section, $"kills_{i}", entry.Kills);
				config.SetValue(section, $"weapon_{i}", (int)entry.Weapon);
				config.SetValue(section, $"world_{i}", entry.World);
				config.SetValue(section, $"date_{i}", entry.Date);
			}
		}

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"Leaderboard: could not write '{SavePath}' ({error}).");
	}
}
