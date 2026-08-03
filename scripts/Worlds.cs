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
		Name = "EMBERTIDE",
		Flavour = "Warm, small, and entirely unbothered by what it is about to do.",
		UnlockHint = "Unlocked from the start.",
		IsEarned = () => true
	};

	public static readonly Profile World2 = new()
	{
		Id = 2,
		Name = "DRIFTLIGHT",
		Flavour = "Barely holds an orbit together, and grins the whole time anyway.",
		UnlockHint = "Complete 3 orbits.",
		IsEarned = () => PlayerProfile.TotalOrbits >= 3
	};

	public static readonly Profile World3 = new()
	{
		Id = 3,
		Name = "PALEFROST",
		Flavour = "Cold enough that its own rings are ice, not rock.",
		UnlockHint = "Complete 6 orbits.",
		IsEarned = () => PlayerProfile.TotalOrbits >= 6
	};

	public static readonly Profile World4 = new()
	{
		Id = 4,
		Name = "CINDERBLOOM",
		Flavour = "Every impact leaves a scorch mark it wears like a medal.",
		UnlockHint = "100 lifetime kills.",
		IsEarned = () => PlayerProfile.TotalKills >= 100
	};

	public static readonly Profile World5 = new()
	{
		Id = 5,
		Name = "HOLLOWMERE",
		Flavour = "Something is missing from its core. It has stopped asking what.",
		UnlockHint = "300 lifetime kills.",
		IsEarned = () => PlayerProfile.TotalKills >= 300
	};

	public static readonly Profile World6 = new()
	{
		Id = 6,
		Name = "DUSKWARDEN",
		Flavour = "Keeps watch over nothing in particular, very seriously.",
		UnlockHint = $"Score {5000:N0} in one orbit.",
		IsEarned = () => ScoreManager.BestScore >= 5000
	};

	public static readonly Profile World7 = new()
	{
		Id = 7,
		Name = "VERDANT HALO",
		Flavour = "A green ring of something that used to be a lot of somethings.",
		UnlockHint = $"Score {15000:N0} in one orbit.",
		IsEarned = () => ScoreManager.BestScore >= 15000
	};

	public static readonly Profile World8 = new()
	{
		Id = 8,
		Name = "ASHEN COIL",
		Flavour = "Its rings spin the way a certain boss's rings spin. It remembers.",
		UnlockHint = "Survive 3:00 in one orbit.",
		IsEarned = () => ScoreManager.BestTime >= 180f
	};

	public static readonly Profile World9 = new()
	{
		Id = 9,
		Name = "GLASSWAKE",
		Flavour = "Brittle, bright, and still here after everything that has hit it.",
		UnlockHint = "Survive 5:00 in one orbit.",
		IsEarned = () => ScoreManager.BestTime >= 300f
	};

	public static readonly Profile World10 = new()
	{
		Id = 10,
		Name = "MOLTENCROWN",
		Flavour = "Went all the way to full mass once, and never quite came back down.",
		UnlockHint = "Reach maximum mass in one orbit.",
		IsEarned = () => PlayerProfile.HeaviestMassEver >= 0.999f
	};

	public static readonly Profile World11 = new()
	{
		Id = 11,
		Name = "VOIDKIN",
		Flavour = "Has spent so long out here it no longer remembers a horizon.",
		UnlockHint = "1 hour of lifetime playtime.",
		IsEarned = () => PlayerProfile.TotalTimePlayed >= 3600f
	};

	public static readonly Profile World12 = new()
	{
		Id = 12,
		Name = "STARFORGED",
		Flavour = "Every world before this one, pressed into something that survived.",
		UnlockHint = $"20 lifetime orbits and {25000:N0} score in one.",
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
