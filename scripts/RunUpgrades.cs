using Godot;

/// <summary>Everything a run can pick up. Each is permanent until the run ends.</summary>
public enum RunUpgradeId
{
	FireRate,
	Piercing,
	SpreadShot,
	DashBoost,
	BiggerNova,
	OverdriveBoost,
	DebrisCannon,
	IonLance
}

/// <summary>The three abilities. They unlock by survival time, always in this order.</summary>
public enum Ability
{
	Dash,
	Overdrive,
	Nova
}

public static class RunUpgrades
{
	public sealed class Profile
	{
		public required RunUpgradeId Id { get; init; }
		public required string Name { get; init; }
		/// <summary>Icon under art/cosmic/icon_*.svg.</summary>
		public required string Icon { get; init; }
		/// <summary>Disc colour behind the icon, so pickups sharing an icon still differ.</summary>
		public required Color Colour { get; init; }
		public int MaxLevel { get; init; } = 1;
		/// <summary>An ability the run must have unlocked before this can mean anything.</summary>
		public Ability? Requires { get; init; }
		/// <summary>Replaces the starting Comet. Only one swap per run.</summary>
		public WeaponId? Equips { get; init; }
		/// <summary>Relative chance among eligible drops from ordinary enemies.</summary>
		public float Weight { get; init; } = 1f;
		/// <summary>Relative chance in a boss reward.</summary>
		public float BossWeight { get; init; } = 1f;
	}

	private static readonly Color Gun = new("f5a451");
	private static readonly Color Move = new("7fd6c2");
	private static readonly Color Power = new("ff7b8e");
	private static readonly Color Blast = new("ffd66b");

	public static readonly Profile FireRate = new()
	{
		Id = RunUpgradeId.FireRate, Name = TranslationServer.Translate("UPG_FireRate_NAME"),
		Icon = "firerate", Colour = Gun, MaxLevel = 6, Weight = 1.1f, BossWeight = 1.2f
	};

	public static readonly Profile Piercing = new()
	{
		Id = RunUpgradeId.Piercing, Name = TranslationServer.Translate("UPG_Piercing_NAME"),
		Icon = "pierce", Colour = Gun, MaxLevel = 4, Weight = 0.9f, BossWeight = 1.1f
	};

	public static readonly Profile SpreadShot = new()
	{
		Id = RunUpgradeId.SpreadShot, Name = "SPREAD SHOT",
		Icon = "spread", Colour = Gun, MaxLevel = 2, Weight = 0.9f, BossWeight = 1.3f
	};

	public static readonly Profile DashBoost = new()
	{
		Id = RunUpgradeId.DashBoost, Name = "LONGER DASH",
		Icon = "dash", Colour = Move, MaxLevel = 3, Requires = Ability.Dash, Weight = 0.8f
	};

	public static readonly Profile BiggerNova = new()
	{
		Id = RunUpgradeId.BiggerNova, Name = TranslationServer.Translate("UPG_BiggerNova_NAME"),
		Icon = "nova", Colour = Blast, MaxLevel = 4, Requires = Ability.Nova, Weight = 0.7f, BossWeight = 1.1f
	};

	public static readonly Profile OverdriveBoost = new()
	{
		Id = RunUpgradeId.OverdriveBoost, Name = "OVERDRIVE BOOST",
		Icon = "rapid", Colour = Power, MaxLevel = 4, Requires = Ability.Overdrive, Weight = 0.8f, BossWeight = 1.2f
	};

	public static readonly Profile DebrisCannon = new()
	{
		Id = RunUpgradeId.DebrisCannon, Name = "DEBRIS CANNON",
		Icon = "cannon", Colour = new Color("c9a0ff"), Equips = WeaponId.DebrisCannon, Weight = 0.3f, BossWeight = 0.6f
	};

	public static readonly Profile IonLance = new()
	{
		Id = RunUpgradeId.IonLance, Name = "ION LANCE",
		Icon = "lance", Colour = new Color("8ce6ff"), Equips = WeaponId.IonLance, Weight = 0.3f, BossWeight = 0.6f
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		FireRate, Piercing, SpreadShot, DashBoost, BiggerNova, OverdriveBoost, DebrisCannon, IonLance
	};

	/// <summary>Every level a single run can hold. A weapon swap counts once — only one can be taken.</summary>
	public static readonly int MaxTotalLevels =
		FireRate.MaxLevel + Piercing.MaxLevel + SpreadShot.MaxLevel + DashBoost.MaxLevel
		+ BiggerNova.MaxLevel + OverdriveBoost.MaxLevel + 1;

	public static Profile Get(RunUpgradeId id)
	{
		foreach (Profile profile in All)
		{
			if (profile.Id == id)
				return profile;
		}

		return null;
	}

	public static string AbilityName(Ability ability) => ability switch
	{
		Ability.Dash => "DASH",
		Ability.Overdrive => "OVERDRIVE",
		_ => "NOVA"
	};

	/// <summary>What the ability does, finishing "Press SHIFT / B to ...". Shown once, when it comes online.</summary>
	public static string AbilityVerb(Ability ability) => ability switch
	{
		Ability.Dash => "zoom through enemies and pop them",
		Ability.Overdrive => "make your gun go wild",
		_ => "blast everything around you"
	};

	/// <summary>The input action each ability listens on. Overdrive keeps the old rapid-fire binding.</summary>
	public static string ActionFor(Ability ability) => ability switch
	{
		Ability.Dash => "dash",
		Ability.Overdrive => "rapid_fire",
		_ => "nova"
	};

	public static string IconFor(Ability ability) => ability switch
	{
		Ability.Dash => "dash",
		Ability.Overdrive => "rapid",
		_ => "nova"
	};
}
