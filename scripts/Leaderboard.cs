using System.Collections.Generic;
using Godot;

/// <summary>
/// The local top-10, per the roadmap: score, weapon, world and date for each
/// entry. Separate from <see cref="ScoreManager"/>'s single best, because a
/// leaderboard needs to keep the ninth-best run even after a tenth arrives to
/// bump someone off it — one scalar can't do that.
/// </summary>
public static class Leaderboard
{
	public const int Capacity = 10;
	private const string SavePath = "user://leaderboard.cfg";
	private const string Section = "leaderboard";
	private const int SaveVersion = 1;

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

	private static readonly List<Entry> entries = new();
	private static bool isLoaded;

	public static IReadOnlyList<Entry> Entries
	{
		get
		{
			EnsureLoaded();
			return entries;
		}
	}

	/// <summary>
	/// Records a finished orbit. Returns the 1-based rank it landed at, or -1 if
	/// it did not place in the top <see cref="Capacity"/>.
	/// </summary>
	public static int Submit(int score, float survivalTime, int kills, WeaponId weapon, int world)
	{
		EnsureLoaded();

		var entry = new Entry(score, survivalTime, kills, weapon, world, Time.GetDateStringFromSystem());

		int insertAt = entries.FindIndex(e => score > e.Score);
		if (insertAt < 0)
		{
			if (entries.Count >= Capacity)
				return -1;
			insertAt = entries.Count;
		}

		entries.Insert(insertAt, entry);
		if (entries.Count > Capacity)
			entries.RemoveRange(Capacity, entries.Count - Capacity);

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

		int count = Mathf.Clamp(config.GetValue(Section, "count", 0).AsInt32(), 0, Capacity);
		for (int i = 0; i < count; i++)
		{
			int score = config.GetValue(Section, $"score_{i}", 0).AsInt32();
			float time = config.GetValue(Section, $"time_{i}", 0.0f).AsSingle();
			int kills = config.GetValue(Section, $"kills_{i}", 0).AsInt32();
			int weaponRaw = config.GetValue(Section, $"weapon_{i}", 0).AsInt32();
			int world = Mathf.Clamp(config.GetValue(Section, $"world_{i}", 1).AsInt32(), 1, 12);
			string date = config.GetValue(Section, $"date_{i}", "").AsString();

			WeaponId weapon = System.Enum.IsDefined(typeof(WeaponId), weaponRaw) ? (WeaponId)weaponRaw : WeaponId.Comet;
			entries.Add(new Entry(score, time, kills, weapon, world, date));
		}
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "count", entries.Count);

		for (int i = 0; i < entries.Count; i++)
		{
			Entry entry = entries[i];
			config.SetValue(Section, $"score_{i}", entry.Score);
			config.SetValue(Section, $"time_{i}", entry.SurvivalTime);
			config.SetValue(Section, $"kills_{i}", entry.Kills);
			config.SetValue(Section, $"weapon_{i}", (int)entry.Weapon);
			config.SetValue(Section, $"world_{i}", entry.World);
			config.SetValue(Section, $"date_{i}", entry.Date);
		}

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"Leaderboard: could not write '{SavePath}' ({error}).");
	}
}
