using Godot;

/// <summary>
/// The twelve existing player skins, turned into named, unlockable worlds. The
/// art already exists — ASSETS.md calls this the cheapest content in the whole
/// plan — so the only job here is naming them and deciding what earns each one.
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

	public static readonly Profile World1 = new()
	{
		Id = 1,
		Name = TranslationServer.Translate("WORLD_1_NAME"),
		Flavour = TranslationServer.Translate("WORLD_1_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_1_HINT"),
		IsEarned = () => true
	};

	public static readonly Profile World2 = new()
	{
		Id = 2,
		Name = TranslationServer.Translate("WORLD_2_NAME"),
		Flavour = TranslationServer.Translate("WORLD_2_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_2_HINT"),
		IsEarned = () => PlayerProfile.TotalOrbits >= 3
	};

	public static readonly Profile World3 = new()
	{
		Id = 3,
		Name = TranslationServer.Translate("WORLD_3_NAME"),
		Flavour = TranslationServer.Translate("WORLD_3_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_3_HINT"),
		IsEarned = () => PlayerProfile.TotalOrbits >= 6
	};

	public static readonly Profile World4 = new()
	{
		Id = 4,
		Name = TranslationServer.Translate("WORLD_4_NAME"),
		Flavour = TranslationServer.Translate("WORLD_4_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_4_HINT"),
		IsEarned = () => PlayerProfile.TotalKills >= 100
	};

	public static readonly Profile World5 = new()
	{
		Id = 5,
		Name = TranslationServer.Translate("WORLD_5_NAME"),
		Flavour = TranslationServer.Translate("WORLD_5_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_5_HINT"),
		IsEarned = () => PlayerProfile.TotalKills >= 300
	};

	public static readonly Profile World6 = new()
	{
		Id = 6,
		Name = TranslationServer.Translate("WORLD_6_NAME"),
		Flavour = TranslationServer.Translate("WORLD_6_FLAVOUR"),
		UnlockHint = string.Format(TranslationServer.Translate("WORLD_6_HINT"), 5000.ToString("N0")),
		IsEarned = () => ScoreManager.BestScore >= 5000
	};

	public static readonly Profile World7 = new()
	{
		Id = 7,
		Name = TranslationServer.Translate("WORLD_7_NAME"),
		Flavour = TranslationServer.Translate("WORLD_7_FLAVOUR"),
		UnlockHint = string.Format(TranslationServer.Translate("WORLD_7_HINT"), 15000.ToString("N0")),
		IsEarned = () => ScoreManager.BestScore >= 15000
	};

	public static readonly Profile World8 = new()
	{
		Id = 8,
		Name = TranslationServer.Translate("WORLD_8_NAME"),
		Flavour = TranslationServer.Translate("WORLD_8_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_8_HINT"),
		IsEarned = () => ScoreManager.BestTime >= 180f
	};

	public static readonly Profile World9 = new()
	{
		Id = 9,
		Name = TranslationServer.Translate("WORLD_9_NAME"),
		Flavour = TranslationServer.Translate("WORLD_9_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_9_HINT"),
		IsEarned = () => ScoreManager.BestTime >= 300f
	};

	public static readonly Profile World10 = new()
	{
		Id = 10,
		Name = TranslationServer.Translate("WORLD_10_NAME"),
		Flavour = TranslationServer.Translate("WORLD_10_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_10_HINT"),
		IsEarned = () => PlayerProfile.HeaviestMassEver >= 0.999f
	};

	public static readonly Profile World11 = new()
	{
		Id = 11,
		Name = TranslationServer.Translate("WORLD_11_NAME"),
		Flavour = TranslationServer.Translate("WORLD_11_FLAVOUR"),
		UnlockHint = TranslationServer.Translate("WORLD_11_HINT"),
		IsEarned = () => PlayerProfile.TotalTimePlayed >= 3600f
	};

	public static readonly Profile World12 = new()
	{
		Id = 12,
		Name = TranslationServer.Translate("WORLD_12_NAME"),
		Flavour = TranslationServer.Translate("WORLD_12_FLAVOUR"),
		UnlockHint = string.Format(TranslationServer.Translate("WORLD_12_HINT"), 20, 25000.ToString("N0")),
		IsEarned = () => PlayerProfile.TotalOrbits >= 20 && ScoreManager.BestScore >= 25000
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		World1, World2, World3, World4, World5, World6,
		World7, World8, World9, World10, World11, World12
	};

	public static Profile Get(int id) => All[Mathf.Clamp(id, 1, All.Length) - 1];

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
