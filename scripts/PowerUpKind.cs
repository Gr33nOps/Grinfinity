using Godot;

/// <summary>Pickup identifiers. Freeze and Magnet are retained for legacy compatibility, excluded from live drops.</summary>
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
		/// <summary>One line, shown when the pickup is taken. What it does, plainly.</summary>
		public required string Effect { get; init; }
		public required Color Colour { get; init; }
		/// <summary>Seconds it lasts. Zero means instant or until spent.</summary>
		public required float Duration { get; init; }
	}

	public static readonly Profile Shield = new()
	{
		Kind = PowerUpKind.Shield,
		Name = TranslationServer.Translate("POWERUP_Shield_NAME"),
		Effect = TranslationServer.Translate("POWERUP_Shield_EFFECT"),
		Colour = new Color(0.55f, 0.85f, 1.0f),
		Duration = 0f
	};

	public static readonly Profile Freeze = new()
	{
		Kind = PowerUpKind.Freeze,
		Name = TranslationServer.Translate("POWERUP_Freeze_NAME"),
		Effect = TranslationServer.Translate("POWERUP_Freeze_EFFECT"),
		Colour = new Color(0.7f, 0.95f, 1.0f),
		Duration = 3.5f
	};

	public static readonly Profile Magnet = new()
	{
		Kind = PowerUpKind.Magnet,
		Name = TranslationServer.Translate("POWERUP_Magnet_NAME"),
		Effect = TranslationServer.Translate("POWERUP_Magnet_EFFECT"),
		Colour = new Color(1.0f, 0.72f, 0.35f),
		Duration = 7.0f
	};

	public static readonly Profile Nuke = new()
	{
		Kind = PowerUpKind.Nuke,
		Name = TranslationServer.Translate("POWERUP_Nuke_NAME"),
		Effect = TranslationServer.Translate("POWERUP_Nuke_EFFECT"),
		Colour = new Color(1.0f, 0.45f, 0.35f),
		Duration = 0f
	};

	public static readonly Profile Damage = new()
	{
		Kind = PowerUpKind.Damage,
		Name = TranslationServer.Translate("POWERUP_Damage_NAME"),
		Effect = TranslationServer.Translate("POWERUP_Damage_EFFECT"),
		Colour = new Color(1.0f, 0.9f, 0.4f),
		Duration = 8.0f
	};

	// Declared after the profiles: static field initialisers run in source order,
	// and an array up top would capture five nulls.
	public static readonly Profile[] All = { Shield, Nuke, Damage };

	public static Profile Get(PowerUpKind kind) => kind switch {PowerUpKind.Shield=>Shield,PowerUpKind.Nuke=>Nuke,PowerUpKind.Damage=>Damage,PowerUpKind.Freeze=>Freeze,_=>Magnet};

	/// <summary>Base weights: shield 50%, overcharge 35%, nuke 15%; the manager applies wave/ownership restrictions.</summary>
	public static PowerUpKind Roll() {float roll=RunState.Rng.Randf();return roll<.5f?PowerUpKind.Shield:roll<.85f?PowerUpKind.Damage:PowerUpKind.Nuke;}
}
