using Godot;

/// <summary>Which weapon the world is carrying. Chosen once, at orbit start.</summary>
public enum WeaponId
{
	Comet,
	DebrisCannon,
	IonLance
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

	public static WeaponProfile Get(WeaponId id) => id switch
	{
		WeaponId.DebrisCannon => DebrisCannon,
		WeaponId.IonLance => IonLance,
		_ => Comet
	};

	public static readonly WeaponProfile Comet = new()
	{
		Id = WeaponId.Comet,
		Name = "COMET",
		Fantasy = "A steady shot with a tail.",
		Tradeoff = "Reliable. Unremarkable.",
		FireInterval = 0.22f,
		Speed = 950f,
		Range = 3.0f,
		Tint = new Color(0.95f, 0.62f, 0.35f)
	};

	public static readonly WeaponProfile DebrisCannon = new()
	{
		Id = WeaponId.DebrisCannon,
		Name = "DEBRIS CANNON",
		Fantasy = "A fistful of rock, all at once.",
		Tradeoff = "Deletes a swarm. Useless at range.",
		FireInterval = 0.52f,
		Pellets = 6,
		Spread = Mathf.DegToRad(30f),
		Speed = 820f,
		SpeedJitter = 0.22f,
		// Short life, not slow shots: the spread stays fast and simply stops
		// existing before it reaches anything far away.
		Range = 0.42f,
		ShotScale = 0.8f,
		Tint = new Color(0.88f, 0.72f, 0.5f)
	};

	public static readonly WeaponProfile IonLance = new()
	{
		Id = WeaponId.IonLance,
		Name = "ION LANCE",
		Fantasy = "One line, straight through a clump.",
		Tradeoff = "Slow. Has to be lined up.",
		FireInterval = 0.78f,
		Speed = 1650f,
		Damage = 2,
		Pierce = 8,
		Range = 1.6f,
		ShotScale = 1.35f,
		Tint = new Color(0.55f, 0.9f, 1.0f)
	};

	/// <summary>
	/// Every weapon, in the order the select screen lists them. Declared last on
	/// purpose: static field initialisers run in source order, so an array up at
	/// the top of the class would capture three nulls.
	/// </summary>
	public static readonly WeaponProfile[] All = { Comet, DebrisCannon, IonLance };
}
