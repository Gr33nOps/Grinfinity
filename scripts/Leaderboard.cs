using System.Collections.Generic;
using Godot;

/// <summary>
/// The local top-10. Separate from <see cref="ScoreManager"/>'s single best,
/// because a leaderboard needs to keep the ninth-best run even after a tenth
/// arrives to bump someone off it — one scalar can't do that.
///
/// One mode means one table. The per-entry weapon, world and date that v2
/// carried existed to answer "how did this run happen" across a roster of
/// different run shapes; with one shape there is nothing to disambiguate, so
/// an entry is a name and a score, plus the time and kills that earned it.
/// </summary>
public static class Leaderboard
{
	public const int Capacity = 10;
	public const int MaxNameLength = 12;

	private const string SavePath = "user://leaderboard.cfg";
	private const string Section = "leaderboard";
	// v2 split the table per mode. v3 collapses it back to one and adds the
	// player's name — the one thing the board was always missing. Entries from
	// either older layout are read forward under a placeholder name.
	private const int SaveVersion = 3;

	public readonly struct Entry
	{
		public Entry(string name, int score, float survivalTime, int kills)
		{
			Name = name;
			Score = score;
			SurvivalTime = survivalTime;
			Kills = kills;
		}

		public string Name { get; }
		public int Score { get; }
		public float SurvivalTime { get; }
		public int Kills { get; }
	}

	private static readonly List<Entry> entries = new();
	private static bool isLoaded;

	/// <summary>The top entries, best first. Empty until something has been played.</summary>
	public static IReadOnlyList<Entry> Entries
	{
		get { EnsureLoaded(); return entries; }
	}

	/// <summary>
	/// Would this score place? Asked before the name prompt, so a run that
	/// missed the board is never interrupted to ask who played it.
	/// </summary>
	public static bool WouldPlace(int score)
	{
		EnsureLoaded();
		return entries.Count < Capacity || score > entries[^1].Score;
	}

	/// <summary>
	/// Records a finished orbit. Returns the 1-based rank it landed at, or -1
	/// if it did not place in the top <see cref="Capacity"/>.
	/// </summary>
	public static int Submit(string name, int score, float survivalTime, int kills)
	{
		EnsureLoaded();

		var entry = new Entry(Sanitise(name), score, survivalTime, kills);

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

	/// <summary>
	/// Keeps a name to one short, printable line. A leaderboard row has a fixed
	/// column and no say in what gets typed into it.
	/// </summary>
	public static string Sanitise(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return "PLAYER";

		string trimmed = name.Trim().Replace("\n", "").Replace("\r", "");
		if (trimmed.Length > MaxNameLength)
			trimmed = trimmed[..MaxNameLength];

		return trimmed.ToUpperInvariant();
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return;

		int version = config.GetValue(Section, "version", 1).AsInt32();

		if (version >= 3)
		{
			LoadSection(Section);
			return;
		}

		// v1 kept everything in the base section; v2 moved it into per-mode
		// sections. Either way, only the Endless Orbit runs are still the same
		// game, so they are the only ones carried across.
		LoadSection(version < 2 ? Section : $"{Section}_EndlessOrbit");
	}

	private static void LoadSection(string section)
	{
		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return;

		int count = Mathf.Clamp(config.GetValue(section, "count", 0).AsInt32(), 0, Capacity);
		if (count <= 0)
			return;

		for (int i = 0; i < count; i++)
		{
			int score = config.GetValue(section, $"score_{i}", 0).AsInt32();
			float time = config.GetValue(section, $"time_{i}", 0.0f).AsSingle();
			int kills = config.GetValue(section, $"kills_{i}", 0).AsInt32();
			// Older entries predate the board asking who was playing.
			string name = config.GetValue(section, $"name_{i}", "PLAYER").AsString();

			entries.Add(new Entry(Sanitise(name), score, time, kills));
		}

		entries.Sort((a, b) => b.Score.CompareTo(a.Score));
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "count", entries.Count);

		for (int i = 0; i < entries.Count; i++)
		{
			Entry entry = entries[i];
			config.SetValue(Section, $"name_{i}", entry.Name);
			config.SetValue(Section, $"score_{i}", entry.Score);
			config.SetValue(Section, $"time_{i}", entry.SurvivalTime);
			config.SetValue(Section, $"kills_{i}", entry.Kills);
		}

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"Leaderboard: could not write '{SavePath}' ({error}).");
	}
}
