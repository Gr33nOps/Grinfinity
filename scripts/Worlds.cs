using Godot;

/// <summary>
/// Eight illustrated cosmetic planets, earned through lifetime play milestones.
/// Each wears one positive emotion, gentlest first: Stillwater (calmness),
/// Easewind (relief), Hearthglow (contentment), Wishfall (hope), Laurelcrown
/// (pride), Sparkrush (excitement), Heartsong (love) and Boldcrest
/// (confidence). Cosmetics never change power.
/// </summary>
public static class Worlds
{
	public sealed class Profile
	{
		public required int Id { get; init; }
		public required string Name { get; init; }
		/// <summary>One line of flavour, shown once a world is unlocked.</summary>
		public required string Flavour { get; init; }
		/// <summary>Shown on a locked world card — what it actually takes.</summary>
		public required string UnlockHint { get; init; }
		/// <summary>True once <see cref="PlayerProfile"/>'s current stats satisfy it.</summary>
		public required System.Func<bool> IsEarned { get; init; }
	}

	private static Profile Make(int id, System.Func<bool> earned, string hint = null) => new()
	{
		Id = id,
		Name = TranslationServer.Translate($"WORLD_{id}_NAME"),
		Flavour = TranslationServer.Translate($"WORLD_{id}_FLAVOUR"),
		UnlockHint = hint ?? TranslationServer.Translate($"WORLD_{id}_HINT"),
		IsEarned = earned
	};

	public static readonly Profile[] All =
	{
		Make(1, () => true),
		Make(2, () => PlayerProfile.TotalOrbits >= 3),
		Make(3, () => PlayerProfile.TotalKills >= 300),
		Make(4, () => ScoreManager.BestTime >= 180f),
		Make(5, () => ScoreManager.BestTime >= 300f),
		Make(6, () => PlayerProfile.HeaviestMassEver >= 0.999f),
		Make(7, () => PlayerProfile.TotalTimePlayed >= 3600f),
		Make(8, () => PlayerProfile.TotalOrbits >= 20 && ScoreManager.BestTime >= 600f,
			string.Format(TranslationServer.Translate("WORLD_8_HINT"), 20)),
	};

	public static Profile Get(int id) => All[Mathf.Clamp(id, 1, All.Length) - 1];

	/// <summary>
	/// Saves remember which planet list they were written with, so a planet keeps
	/// being the same planet when the list changes. Layout 12 was the old twelve;
	/// 8 the first eight, with Boldcrest fifth; 9 is today's order.
	/// </summary>
	public const int Layout = 9;

	/// <summary>A planet number from a save written with <paramref name="layout"/>, as today's number, or 0 if it is gone.</summary>
	public static int Migrate(int layout, int id) => layout switch
	{
		Layout => id,
		// The first eight: Boldcrest was fifth, then Laurelcrown, Sparkrush, Heartsong.
		8 => id switch { 5 => 8, 6 => 5, 7 => 6, 8 => 7, _ => id },
		// The old twelve. Hushmere, Anchorlight, Gracebloom and Sunburst are gone.
		_ => id switch { 1 => 1, 2 => 2, 3 => 3, 7 => 4, 8 => 8, 9 => 5, 11 => 6, 12 => 7, _ => 0 }
	};

	/// <summary>
	/// Each planet's own happy face, and the same face mid-blink. Each planet
	/// wears one positive emotion, from calmness on the first to confidence on the last
	/// (see tools/make_moods.py).
	/// </summary>
	public static Texture2D Face(int id, bool blinking = false)
	{
		id = Mathf.Clamp(id, 1, All.Length);
		string name = id == 1 ? "face" : $"face_{id}";
		return GD.Load<Texture2D>($"res://art/cosmic/{name}{(blinking ? "_blink" : "")}.svg");
	}

	/// <summary>
	/// Checks every locked world against the profile's current stats and
	/// unlocks any that are now earned. Called once an orbit's stats are final,
	/// so a world can be earned by the very orbit that satisfies it.
	/// </summary>
	/// <returns>Newly unlocked worlds, in id order — empty if none.</returns>
	public static System.Collections.Generic.List<Profile> RefreshUnlocks()
	{
		var newlyUnlocked = new System.Collections.Generic.List<Profile>();

		foreach (Profile world in All)
		{
			if (PlayerProfile.IsWorldUnlocked(world.Id))
				continue;

			if (world.IsEarned() && PlayerProfile.UnlockWorld(world.Id))
				newlyUnlocked.Add(world);
		}

		return newlyUnlocked;
	}
}
