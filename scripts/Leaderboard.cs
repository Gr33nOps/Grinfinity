using System.Collections.Generic;
using Godot;

/// <summary>
/// The local top-10. Separate from <see cref="ScoreManager"/>'s single best,
/// because a leaderboard needs to keep the ninth-best run even after a tenth
/// arrives to bump someone off it — one scalar can't do that.
///
/// One mode means one table, ranked by how long the run survived. Score is
/// kept alongside as the tie-breaker and a second number to be proud of.
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
	/// Would a run this long place? Survival time is the record; score only
	/// breaks ties between runs that lasted exactly as long.
	/// </summary>
	public static bool WouldPlace(float survivalTime, int score = 0)
	{
		EnsureLoaded();
		if (survivalTime < 0f || !float.IsFinite(survivalTime)) return false;
		return entries.Count < Capacity || Beats(survivalTime, score, entries[^1]);
	}

	private static bool Beats(float time, int score, Entry other) =>
		time > other.SurvivalTime || (Mathf.IsEqualApprox(time, other.SurvivalTime) && score > other.Score);

	/// <summary>
	/// Records a finished orbit. Returns the 1-based rank it landed at, or -1
	/// if it did not place in the top <see cref="Capacity"/>.
	/// </summary>
	public static int Submit(string name, int score, float survivalTime, int kills)
	{
		EnsureLoaded();
		if (!Valid(score, survivalTime, kills)) return -1;

		var entry = new Entry(Sanitise(name), score, survivalTime, kills);

		int insertAt = entries.FindIndex(e => Beats(survivalTime, score, e));
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
	/// Renames an entry already on the board, by the 1-based rank
	/// <see cref="Submit"/> returned. The recap asks who was playing *after* the
	/// run has been recorded, so the row exists before the answer does.
	/// </summary>
	public static void RenameAt(int rank, string name)
	{
		EnsureLoaded();

		int index = rank - 1;
		if (index < 0 || index >= entries.Count)
			return;

		Entry old = entries[index];
		entries[index] = new Entry(Sanitise(name), old.Score, old.SurvivalTime, old.Kills);
		SaveToFile();
	}

	/// <summary>
	/// Keeps a name to one short, printable line. A leaderboard row has a fixed
	/// column and no say in what gets typed into it.
	/// </summary>
	public static string Sanitise(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return "PLAYER";

		var clean = new System.Text.StringBuilder();
		foreach (var rune in name.EnumerateRunes())
		{
			var category = System.Text.Rune.GetUnicodeCategory(rune);
			if (category is System.Globalization.UnicodeCategory.Control or System.Globalization.UnicodeCategory.Format
				or System.Globalization.UnicodeCategory.LineSeparator or System.Globalization.UnicodeCategory.ParagraphSeparator) continue;
			string printable = rune.ToString().ToUpperInvariant();
			if (clean.Length + printable.Length > MaxNameLength) break;
			clean.Append(printable);
		}
		string trimmed = clean.ToString().Trim();
		return string.IsNullOrWhiteSpace(trimmed) ? "PLAYER" : trimmed;
	}

	private static bool Valid(int score, float time, int kills) =>
		score >= 0 && kills >= 0 && float.IsFinite(time) && time >= 0f && time <= 36000f;

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (SaveStore.Load(config, SavePath) != Error.Ok)
			return;

		int version = SaveStore.Value(config, Section, "version", 1).AsInt32();

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
		if (SaveStore.Load(config, SavePath) != Error.Ok)
			return;

		int count = Mathf.Clamp(SaveStore.Value(config, section, "count", 0).AsInt32(), 0, Capacity);
		if (count <= 0)
			return;

		for (int i = 0; i < count; i++)
		{
			int score = SaveStore.Value(config, section, $"score_{i}", 0).AsInt32();
			float time = SaveStore.Value(config, section, $"time_{i}", 0.0f).AsSingle();
			int kills = SaveStore.Value(config, section, $"kills_{i}", 0).AsInt32();
			// Older entries predate the board asking who was playing.
			string name = SaveStore.Value(config, section, $"name_{i}", "PLAYER").AsString();

			if (Valid(score, time, kills)) entries.Add(new Entry(Sanitise(name), score, time, kills));
		}

		// Stable insertion sort, longest run first, preserving arrival order for
		// ties after reload. Boards saved when score led are re-ranked by time.
		for (int i = 1; i < entries.Count; i++)
		{
			Entry entry = entries[i]; int j = i - 1;
			while (j >= 0 && Beats(entry.SurvivalTime, entry.Score, entries[j])) { entries[j + 1] = entries[j]; j--; }
			entries[j + 1] = entry;
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
			config.SetValue(Section, $"name_{i}", entry.Name);
			config.SetValue(Section, $"score_{i}", entry.Score);
			config.SetValue(Section, $"time_{i}", entry.SurvivalTime);
			config.SetValue(Section, $"kills_{i}", entry.Kills);
		}

		Error error = SaveStore.Save(config, SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"Leaderboard: could not write '{SavePath}' ({error}).");
	}
}
