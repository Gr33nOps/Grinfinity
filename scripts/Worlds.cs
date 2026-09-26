using Godot;

/// <summary>
/// Eight illustrated cosmetic planets, earned through lifetime play milestones.
/// Each wears one positive emotion, gentlest first: Stillwater (calmness),
/// Easewind (relief), Hearthglow (contentment), Wishfall (hope), Boldcrest
/// (confidence), Laurelcrown (pride), Sparkrush (excitement) and Heartsong
/// (love). Cosmetics never change power.
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
	/// There used to be twelve planets. Saves from then store their numbers, so
	/// this maps one of those to today's planet, or 0 for the four since removed
	/// (Hushmere, Anchorlight, Gracebloom and Sunburst).
	/// </summary>
	public static int FromTwelve(int oldId) => oldId switch
	{
		1 => 1, 2 => 2, 3 => 3, 7 => 4, 8 => 5, 9 => 6, 11 => 7, 12 => 8,
		_ => 0
	};

	/// <summary>Saves written since the cut to eight mark themselves with this.</summary>
	public const int Layout = 8;

	/// <summary>
	/// Each planet's own happy face, and the same face mid-blink. Each planet
	/// wears one positive emotion, from calmness on the first to love on the last
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
