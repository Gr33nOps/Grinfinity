using Godot;

/// <summary>Which weapon the world is carrying. There is one: the Comet.</summary>
public enum WeaponId
{
	Comet
}

/// <summary>
/// Everything one weapon does, as data.
///
/// Keeping weapons as data rather than subclasses means adding the Mass Driver
/// and the Solar Flare later is a table entry plus whatever new field they
/// genuinely need — and it keeps every number visible in one place for tuning.
/// </summary>
public sealed class WeaponProfile
{
	public required WeaponId Id { get; init; }
	public required string Name { get; init; }
	/// <summary>The fantasy, one line, for the select screen.</summary>
	public required string Fantasy { get; init; }
	/// <summary>The cost, one line. Every weapon must have one.</summary>
	public required string Tradeoff { get; init; }

	/// <summary>Seconds between shots.</summary>
	public required float FireInterval { get; init; }
	/// <summary>Projectiles per shot.</summary>
	public int Pellets { get; init; } = 1;
	/// <summary>Total cone the pellets are spread across, in radians.</summary>
	public float Spread { get; init; } = 0f;
	public required float Speed { get; init; }
	/// <summary>Random speed variation per pellet, as a fraction of Speed.</summary>
	public float SpeedJitter { get; init; } = 0f;
	public int Damage { get; init; } = 1;
	/// <summary>Extra bodies a shot passes through after the first.</summary>
	public int Pierce { get; init; } = 0;
	/// <summary>Seconds before a shot expires. This is what sets a weapon's range.</summary>
	public required float Range { get; init; }
	public float ShotScale { get; init; } = 1.0f;
	public required Color Tint { get; init; }

	/// <summary>Rapid fire multiplies the interval by this, whatever the weapon.</summary>
	public float RapidFireScale { get; init; } = 0.32f;

	public static WeaponProfile Get(WeaponId id) => Comet;

	public static readonly WeaponProfile Comet = new()
	{
		Id = WeaponId.Comet,
		Name = TranslationServer.Translate("WEAPON_Comet_NAME"),
		Fantasy = TranslationServer.Translate("WEAPON_Comet_FANTASY"),
		Tradeoff = TranslationServer.Translate("WEAPON_Comet_TRADEOFF"),
		FireInterval = 0.18f,
		Speed = 950f,
		Range = 3.0f,
		Tint = new Color(0.95f, 0.62f, 0.35f)
	};

	/// <summary>Every weapon. Declared last: static field initialisers run in source order.</summary>
	public static readonly WeaponProfile[] All = { Comet };
}
