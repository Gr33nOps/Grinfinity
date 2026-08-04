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
	SlowField
}

/// <summary>
/// Which part of the game an upgrade touches. The offer generator uses this to
/// guarantee a mass-economy option every single break: it is the family tied to
/// the gravity spine, and the easiest one for a player to walk past when it is
/// sat next to a bigger damage number.
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
		public required int BaseCost { get; init; }
		/// <summary>How many times it can be taken. One-offs are switches, not dials.</summary>
		public int MaxLevel { get; init; } = 5;
		/// <summary>A passive this unlocks, if any. <see cref="RelicId.None"/> for the numeric ones.</summary>
		public RelicId Grants { get; init; } = RelicId.None;
		/// <summary>Cost growth per level already owned. Flat for one-offs.</summary>
		public float CostGrowth { get; init; } = 1.55f;
		/// <summary>
		/// Turns on something the player cannot do at all yet, rather than
		/// improving something they can. The offer keeps one of these on the
		/// table until they are all bought — a run that never rolled a dash
		/// would be a run missing a verb, not a run that built differently.
		/// </summary>
		public bool IsUnlock { get; init; }
		/// <summary>
		/// An ability this improves, which the run has to own first. Offering
		/// "dash again sooner" to someone with no dash is a card that cannot
		/// mean anything to them yet.
		/// </summary>
		public RunUpgradeId? Requires { get; init; }

		public int CostAt(int level) => Mathf.RoundToInt(BaseCost * Mathf.Pow(CostGrowth, level));
	}

	public static readonly Profile UnlockDash = new()
	{
		Id = RunUpgradeId.UnlockDash,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_UnlockDash_NAME"),
		Effect = TranslationServer.Translate("UPG_UnlockDash_EFFECT"),
		BaseCost = 18,
		MaxLevel = 1,
		IsUnlock = true
	};

	public static readonly Profile UnlockRapidFire = new()
	{
		Id = RunUpgradeId.UnlockRapidFire,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_UnlockRapidFire_NAME"),
		Effect = TranslationServer.Translate("UPG_UnlockRapidFire_EFFECT"),
		BaseCost = 30,
		MaxLevel = 1,
		IsUnlock = true
	};

	public static readonly Profile UnlockNova = new()
	{
		Id = RunUpgradeId.UnlockNova,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_UnlockNova_NAME"),
		Effect = TranslationServer.Translate("UPG_UnlockNova_EFFECT"),
		BaseCost = 45,
		MaxLevel = 1,
		IsUnlock = true
	};

	public static readonly Profile FireRate = new()
	{
		Id = RunUpgradeId.FireRate,
		Family = UpgradeFamily.Weapon,
		Name = TranslationServer.Translate("UPG_FireRate_NAME"),
		Effect = TranslationServer.Translate("UPG_FireRate_EFFECT"),
		BaseCost = 28
	};

	public static readonly Profile Piercing = new()
	{
		Id = RunUpgradeId.Piercing,
		Family = UpgradeFamily.Weapon,
		Name = TranslationServer.Translate("UPG_Piercing_NAME"),
		Effect = TranslationServer.Translate("UPG_Piercing_EFFECT"),
		BaseCost = 90,
		MaxLevel = 1,
		Grants = RelicId.Piercing
	};

	public static readonly Profile QuickerDash = new()
	{
		Id = RunUpgradeId.QuickerDash,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_QuickerDash_NAME"),
		Effect = TranslationServer.Translate("UPG_QuickerDash_EFFECT"),
		BaseCost = 28,
		Requires = RunUpgradeId.UnlockDash
	};

	public static readonly Profile BiggerNova = new()
	{
		Id = RunUpgradeId.BiggerNova,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_BiggerNova_NAME"),
		Effect = TranslationServer.Translate("UPG_BiggerNova_EFFECT"),
		BaseCost = 45,
		MaxLevel = 3,
		Requires = RunUpgradeId.UnlockNova
	};

	public static readonly Profile HungryDash = new()
	{
		Id = RunUpgradeId.HungryDash,
		Family = UpgradeFamily.Ability,
		Name = TranslationServer.Translate("UPG_HungryDash_NAME"),
		Effect = TranslationServer.Translate("UPG_HungryDash_EFFECT"),
		BaseCost = 80,
		MaxLevel = 1,
		Grants = RelicId.VampiricDash,
		Requires = RunUpgradeId.UnlockDash
	};

	public static readonly Profile WiderPull = new()
	{
		Id = RunUpgradeId.WiderPull,
		Family = UpgradeFamily.Mass,
		Name = TranslationServer.Translate("UPG_WiderPull_NAME"),
		Effect = TranslationServer.Translate("UPG_WiderPull_EFFECT"),
		BaseCost = 26
	};

	public static readonly Profile RichDebris = new()
	{
		Id = RunUpgradeId.RichDebris,
		Family = UpgradeFamily.Mass,
		Name = TranslationServer.Translate("UPG_RichDebris_NAME"),
		Effect = TranslationServer.Translate("UPG_RichDebris_EFFECT"),
		BaseCost = 95,
		MaxLevel = 1,
		Grants = RelicId.DoubleDebris
	};

	public static readonly Profile SlowField = new()
	{
		Id = RunUpgradeId.SlowField,
		Family = UpgradeFamily.Mass,
		Name = TranslationServer.Translate("UPG_SlowField_NAME"),
		Effect = TranslationServer.Translate("UPG_SlowField_EFFECT"),
		BaseCost = 100,
		MaxLevel = 1,
		Grants = RelicId.SlowAura
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		UnlockDash, UnlockRapidFire, UnlockNova,
		FireRate, Piercing, QuickerDash, BiggerNova, HungryDash, WiderPull, RichDebris, SlowField
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
