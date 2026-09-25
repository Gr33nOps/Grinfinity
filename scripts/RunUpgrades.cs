using Godot;

/// <summary>Everything a run can buy at a wave break.</summary>
public enum RunUpgradeId
{
	// Abilities you do not start with. An orbit opens with move and shoot and
	// nothing else; everything past that is earned at a wave break.
	UnlockDash,
	UnlockRapidFire,
	UnlockNova,
	// Weapon
	FireRate,
	Piercing,
	// Ability
	QuickerDash,
	BiggerNova,
	HungryDash,
	// Mass economy
	WiderPull,
	RichDebris,
	SlowField,
	DebrisCannon,
	IonLance,
	FanShot
}

/// <summary>
/// The gameplay system improved by a boost.
/// </summary>
public enum UpgradeFamily
{
	Weapon,
	Ability,
	Mass
}

public static class RunUpgrades
{
	public sealed class Profile
	{
		public required RunUpgradeId Id { get; init; }
		public required UpgradeFamily Family { get; init; }
		public required string Name { get; init; }
		/// <summary>One line, plainly. What it does, not what it is called.</summary>
		public required string Effect { get; init; }
		/// <summary>How many times it can be taken. One-offs are switches, not dials.</summary>
		public int MaxLevel { get; init; } = 5;
		/// <summary>A passive this unlocks, if any. <see cref="RelicId.None"/> for the numeric ones.</summary>
		public RelicId Grants { get; init; } = RelicId.None;
		/// <summary>
		/// An ability granted automatically at its wave milestone.
		/// </summary>
		public bool IsUnlock { get; init; }
		/// <summary>
		/// An ability this improves, which the run has to own first. Offering
		/// "dash again sooner" to someone with no dash is a card that cannot
		/// mean anything to them yet.
		/// </summary>
		public RunUpgradeId? Requires { get; init; }
		public WeaponId? Equips { get; init; }
        public int MinWave { get; init; } = 1;
        public int WaveStep { get; init; } = 0;
        public int RequiredWave(int currentLevel) => MinWave + WaveStep * currentLevel;

	}

	public static readonly Profile UnlockDash = new()
	{
		Id = RunUpgradeId.UnlockDash,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_UnlockDash_NAME"),
		Effect = TranslationServer.Translate("UPG_UnlockDash_EFFECT"),
		MaxLevel = 1,
		IsUnlock = true
	};

	public static readonly Profile UnlockRapidFire = new()
	{
		Id = RunUpgradeId.UnlockRapidFire,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_UnlockRapidFire_NAME"),
		Effect = TranslationServer.Translate("UPG_UnlockRapidFire_EFFECT"),
		MaxLevel = 1,
		IsUnlock = true
	};

	public static readonly Profile UnlockNova = new()
	{
		Id = RunUpgradeId.UnlockNova,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_UnlockNova_NAME"),
		Effect = TranslationServer.Translate("UPG_UnlockNova_EFFECT"),
		MaxLevel = 1,
		IsUnlock = true
	};

	public static readonly Profile FireRate = new()
	{
		Id = RunUpgradeId.FireRate,
		Family = UpgradeFamily.Weapon,
		Name = TranslationServer.Translate("UPG_FireRate_NAME"),
		Effect = "18% less time between shots per level.", MaxLevel = 3, WaveStep = 3,
	};

	public static readonly Profile Piercing = new()
	{
		Id = RunUpgradeId.Piercing,
		Family = UpgradeFamily.Weapon,
		Name = TranslationServer.Translate("UPG_Piercing_NAME"),
		Effect = TranslationServer.Translate("UPG_Piercing_EFFECT"),
		MaxLevel = 1,
		Grants = RelicId.Piercing
	};

	public static readonly Profile QuickerDash = new()
	{
		Id = RunUpgradeId.QuickerDash,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_QuickerDash_NAME"),
		Effect = TranslationServer.Translate("UPG_QuickerDash_EFFECT"),
		MinWave = 1, WaveStep = 3, MaxLevel = 3, Requires = RunUpgradeId.UnlockDash
	};

	public static readonly Profile BiggerNova = new()
	{
		Id = RunUpgradeId.BiggerNova,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_BiggerNova_NAME"),
		Effect = TranslationServer.Translate("UPG_BiggerNova_EFFECT"),
		MaxLevel = 3,
		MinWave = 6, WaveStep = 3, Requires = RunUpgradeId.UnlockNova
	};





	public static readonly Profile DebrisCannon = new()
	{
		Id = RunUpgradeId.DebrisCannon, Family = UpgradeFamily.Weapon,
		Name = "DEBRIS CANNON", Effect = "This run: six close-range pellets. Slower, wide spread.",
		MaxLevel = 1, MinWave = 4, Equips = WeaponId.DebrisCannon
	};
	public static readonly Profile IonLance = new()
	{
		Id = RunUpgradeId.IonLance, Family = UpgradeFamily.Weapon,
		Name = "ION LANCE", Effect = "This run: powerful piercing shots. Aim carefully.",
		MaxLevel = 1, MinWave = 6, Equips = WeaponId.IonLance
	};

	public static readonly Profile FanShot = new()
	{
		Id = RunUpgradeId.FanShot, Family = UpgradeFamily.Weapon,
		Name = "SPREAD SHOT", Effect = "Add one angled bullet per volley per level. Your aimed shot stays straight.",
		MaxLevel = 2, MinWave = 5, WaveStep = 5
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		UnlockDash, UnlockRapidFire, UnlockNova,
		FireRate, Piercing, QuickerDash, BiggerNova,
		DebrisCannon, IonLance, FanShot
	};

	public static Profile Get(RunUpgradeId id)
	{
		foreach (Profile profile in All)
		{
			if (profile.Id == id)
				return profile;
		}

		return null;
	}
}
