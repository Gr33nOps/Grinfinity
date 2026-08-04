using Godot;

/// <summary>How an orbit is shaped. Endless Orbit is the default; everything else bends one rule of it.</summary>
public enum GameMode
{
	EndlessOrbit,
	Flyby,
	DailyAlignment,
	Convergence,
	GlassPlanet
}

public static class Modes
{
	public sealed class Profile
	{
		public required GameMode Id { get; init; }
		public required string Name { get; init; }
		/// <summary>One line, shown on the mode select card.</summary>
		public required string Flavour { get; init; }
		public required Color Colour { get; init; }

		/// <summary>Seconds before the orbit ends cleanly with whatever score was earned. 0 = no limit.</summary>
		public float TimeLimit { get; init; }
		/// <summary>True if <see cref="BodySpawner"/> should never spawn trash — bosses only.</summary>
		public bool NoTrash { get; init; }
		/// <summary>True if bosses arrive back-to-back rather than gated by survival time.</summary>
		public bool RapidBosses { get; init; }
		/// <summary>Multiplier on the player's own shot damage. 1.0 is normal.</summary>
		public float DamageMultiplier { get; init; } = 1.0f;
		/// <summary>True if only one attempt is allowed per day, against a fixed seed.</summary>
		public bool OneAttemptPerDay { get; init; }
	}

	public static readonly Profile EndlessOrbit = new()
	{
		Id = GameMode.EndlessOrbit,
		Name = "ENDLESS ORBIT",
		Flavour = "Survive as long as you can. The default.",
		Colour = new Color(0.85f, 0.85f, 0.9f)
	};

	public static readonly Profile Flyby = new()
	{
		Id = GameMode.Flyby,
		Name = "FLYBY",
		Flavour = "60 seconds. Maximum score. Perfect for one more.",
		Colour = new Color(0.55f, 0.9f, 1.0f),
		TimeLimit = 60.0f
	};

	public static readonly Profile DailyAlignment = new()
	{
		Id = GameMode.DailyAlignment,
		Name = "DAILY ALIGNMENT",
		Flavour = "The same orbit for everyone today. One attempt.",
		Colour = new Color(1.0f, 0.82f, 0.42f),
		OneAttemptPerDay = true
	};

	public static readonly Profile Convergence = new()
	{
		Id = GameMode.Convergence,
		Name = "CONVERGENCE",
		Flavour = "Three bosses, back-to-back. No trash to hide behind.",
		Colour = new Color(0.78f, 0.66f, 1.0f),
		NoTrash = true,
		RapidBosses = true
	};

	public static readonly Profile GlassPlanet = new()
	{
		Id = GameMode.GlassPlanet,
		Name = "GLASS PLANET",
		Flavour = "One hit kills you. Your shots hit five times as hard.",
		Colour = new Color(0.91f, 0.35f, 0.45f),
		DamageMultiplier = 5.0f
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		EndlessOrbit, Flyby, DailyAlignment, Convergence, GlassPlanet
	};

	public static Profile Get(GameMode id) => All[(int)id];

	/// <summary>
	/// Reseeds <see cref="RunState.Rng"/> for the mode about to start. Daily
	/// Alignment gets a seed derived from today's UTC date — the same for every
	/// player and every machine, which is the entire point of the mode. Every
	/// other mode gets fresh OS entropy, same as before seeding existed.
	/// </summary>
	public static void SeedRun(GameMode mode)
	{
		if (mode == GameMode.DailyAlignment)
			RunState.Rng.Seed = DailySeedValue();
		else
			RunState.Rng.Randomize();
	}

	/// <summary>
	/// Today's UTC date as a stable integer seed. Deliberately not
	/// <c>string.GetHashCode()</c> — .NET randomises that per process, which
	/// would make "the same orbit for everyone today" false the moment two
	/// players ran different processes.
	/// </summary>
	public static ulong DailySeedValue()
	{
		var today = System.DateTime.UtcNow;
		return (ulong)(today.Year * 10000 + today.Month * 100 + today.Day);
	}
}
