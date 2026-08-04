using Godot;

/// <summary>How forgiving an orbit is. Never touches player damage — only how much the game throws at you and how much room it gives you to dodge it.</summary>
public enum Difficulty
{
	Easy,
	Normal,
	Hard
}

public static class Difficulties
{
	public sealed class Profile
	{
		public required Difficulty Id { get; init; }
		public required string Name { get; init; }
		public required string Flavour { get; init; }

		/// <summary>Multiplies every spawn interval. Below 1.0 spawns faster.</summary>
		public float SpawnIntervalMultiplier { get; init; } = 1.0f;
		/// <summary>Multiplies body speed, start and ramp ceiling alike.</summary>
		public float SpeedMultiplier { get; init; } = 1.0f;
		/// <summary>Multiplies each body's collision radius. Never the player's.</summary>
		public float ContactRadiusMultiplier { get; init; } = 1.0f;
	}

	public static readonly Profile Easy = new()
	{
		Id = Difficulty.Easy,
		Name = TranslationServer.Translate("DIFFICULTY_Easy_NAME"),
		Flavour = TranslationServer.Translate("DIFFICULTY_Easy_FLAVOUR"),
		SpawnIntervalMultiplier = 1.3f,
		SpeedMultiplier = 0.82f,
		ContactRadiusMultiplier = 0.85f
	};

	public static readonly Profile Normal = new()
	{
		Id = Difficulty.Normal,
		Name = TranslationServer.Translate("DIFFICULTY_Normal_NAME"),
		Flavour = TranslationServer.Translate("DIFFICULTY_Normal_FLAVOUR")
	};

	public static readonly Profile Hard = new()
	{
		Id = Difficulty.Hard,
		Name = TranslationServer.Translate("DIFFICULTY_Hard_NAME"),
		Flavour = TranslationServer.Translate("DIFFICULTY_Hard_FLAVOUR"),
		SpawnIntervalMultiplier = 0.78f,
		SpeedMultiplier = 1.22f,
		ContactRadiusMultiplier = 1.15f
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All = { Easy, Normal, Hard };

	public static Profile Get(Difficulty id) => All[(int)id];
}
