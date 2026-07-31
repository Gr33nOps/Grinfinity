using Godot;

/// <summary>The five pickups. Short, loud, and frequent enough to plan around.</summary>
public enum PowerUpKind
{
	/// <summary>Absorbs exactly one hit. The only one that is not on a timer.</summary>
	Shield,
	/// <summary>Every body stops dead.</summary>
	Freeze,
	/// <summary>Debris comes to you from anywhere on the arena.</summary>
	Magnet,
	/// <summary>Clears the screen on pickup. Instant, no duration.</summary>
	Nuke,
	/// <summary>Shots hit harder.</summary>
	Damage
}

/// <summary>Look and timing for each pickup, kept next to the enum it describes.</summary>
public static class PowerUps
{
	public sealed class Profile
	{
		public required PowerUpKind Kind { get; init; }
		public required string Name { get; init; }
		public required Color Colour { get; init; }
		/// <summary>Seconds it lasts. Zero means instant or until spent.</summary>
		public required float Duration { get; init; }
	}

	public static readonly Profile Shield = new()
	{
		Kind = PowerUpKind.Shield,
		Name = "SHIELD",
		Colour = new Color(0.55f, 0.85f, 1.0f),
		Duration = 0f
	};

	public static readonly Profile Freeze = new()
	{
		Kind = PowerUpKind.Freeze,
		Name = "FREEZE",
		Colour = new Color(0.7f, 0.95f, 1.0f),
		Duration = 3.5f
	};

	public static readonly Profile Magnet = new()
	{
		Kind = PowerUpKind.Magnet,
		Name = "MAGNET",
		Colour = new Color(1.0f, 0.72f, 0.35f),
		Duration = 7.0f
	};

	public static readonly Profile Nuke = new()
	{
		Kind = PowerUpKind.Nuke,
		Name = "NUKE",
		Colour = new Color(1.0f, 0.45f, 0.35f),
		Duration = 0f
	};

	public static readonly Profile Damage = new()
	{
		Kind = PowerUpKind.Damage,
		Name = "OVERCHARGE",
		Colour = new Color(1.0f, 0.9f, 0.4f),
		Duration = 8.0f
	};

	// Declared after the profiles: static field initialisers run in source order,
	// and an array up top would capture five nulls.
	public static readonly Profile[] All = { Shield, Freeze, Magnet, Nuke, Damage };

	public static Profile Get(PowerUpKind kind) => All[(int)kind];

	/// <summary>Uniform for now; weighting is a tuning job for after a playtest.</summary>
	public static PowerUpKind Roll() => (PowerUpKind)GD.RandRange(0, All.Length - 1);
}
