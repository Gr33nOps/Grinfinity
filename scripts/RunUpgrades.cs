using Godot;

/// <summary>Everything the skill tree sells. Each is permanent until the run ends.</summary>
public enum RunUpgradeId
{
	FireRate,
	SpreadShot,
	Piercing,
	DebrisCannon,
	IonLance,
	DashReach,
	DashBlink,
	OverdrivePower,
	OverdriveDuration,
	BiggerNova,
	NovaPower
}

/// <summary>The three abilities. They unlock by survival time, always in this order.</summary>
public enum Ability
{
	Dash,
	Overdrive,
	Nova
}

/// <summary>The four branches of the skill tree, left to right.</summary>
public enum Branch
{
	Gun,
	Dash,
	Overdrive,
	Nova
}

public static class RunUpgrades
{
	public sealed class Profile
	{
		public required RunUpgradeId Id { get; init; }
		public required Branch Branch { get; init; }
		public required string Name { get; init; }
		/// <summary>A few words, for the tree. What it does, not how much.</summary>
		public required string Short { get; init; }
		/// <summary>Icon under art/cosmic/icon_*.svg.</summary>
		public required string Icon { get; init; }
		/// <summary>Rim colour on a pickup and the tree, so upgrades sharing an icon still differ.</summary>
		public required Color Colour { get; init; }
		public int MaxLevel { get; init; } = 1;
		/// <summary>Replaces the starting Comet. Only one swap per run.</summary>
		public WeaponId? Equips { get; init; }
		/// <summary>Relative chance in a boss reward. Zero keeps it out of boss rewards.</summary>
		public float BossWeight { get; init; } = 1f;
		/// <summary>The node below this one in the tree. Needs one rank before this opens.</summary>
		public RunUpgradeId? Requires { get; init; }
	}

	private static readonly Color Gun = new("f5a451");
	private static readonly Color Move = new("7fd6c2");
	private static readonly Color Power = new("ff7b8e");
	private static readonly Color Blast = new("ffd66b");

	public static readonly Profile FireRate = new()
	{
		Id = RunUpgradeId.FireRate, Branch = Branch.Gun, Name = TranslationServer.Translate("UPG_FireRate_NAME"),
		Short = "Shoot faster", Icon = "firerate", Colour = Gun, MaxLevel = 3, BossWeight = 1.2f
	};

	public static readonly Profile SpreadShot = new()
	{
		Id = RunUpgradeId.SpreadShot, Branch = Branch.Gun, Name = "SPREAD SHOT",
		Short = "Extra angled shots", Icon = "spread", Colour = Gun, MaxLevel = 2, BossWeight = 1.2f, Requires = RunUpgradeId.FireRate
	};

	public static readonly Profile Piercing = new()
	{
		Id = RunUpgradeId.Piercing, Branch = Branch.Gun, Name = TranslationServer.Translate("UPG_Piercing_NAME"),
		Short = "Shots go through foes", Icon = "pierce", Colour = Gun, MaxLevel = 2, BossWeight = 1.1f, Requires = RunUpgradeId.SpreadShot
	};

	// The two weapons are a choice, not a pair: taking one closes the other, and
	// a boss never picks one for you.
	public static readonly Profile DebrisCannon = new()
	{
		Id = RunUpgradeId.DebrisCannon, Branch = Branch.Gun, Name = "DEBRIS CANNON",
		Short = "Six pellets, close range", Icon = "cannon", Colour = new Color("c9a0ff"), Equips = WeaponId.DebrisCannon, BossWeight = 0f, Requires = RunUpgradeId.Piercing
	};

	public static readonly Profile IonLance = new()
	{
		Id = RunUpgradeId.IonLance, Branch = Branch.Gun, Name = "ION LANCE",
		Short = "Slow, heavy, pierces lines", Icon = "lance", Colour = new Color("8ce6ff"), Equips = WeaponId.IonLance, BossWeight = 0f, Requires = RunUpgradeId.Piercing
	};

	public static readonly Profile DashReach = new()
	{
		Id = RunUpgradeId.DashReach, Branch = Branch.Dash, Name = "DASH REACH",
		Short = "Dash further", Icon = "dash", Colour = Move, MaxLevel = 3
	};

	public static readonly Profile DashBlink = new()
	{
		Id = RunUpgradeId.DashBlink, Branch = Branch.Dash, Name = "DASH BLINK",
		Short = "Longer safe blink", Icon = "dash", Colour = Move, MaxLevel = 2, Requires = RunUpgradeId.DashReach
	};

	public static readonly Profile OverdrivePower = new()
	{
		Id = RunUpgradeId.OverdrivePower, Branch = Branch.Overdrive, Name = "OVERDRIVE POWER",
		Short = "Even faster firing", Icon = "rapid", Colour = Power, MaxLevel = 3, BossWeight = 1.2f
	};

	public static readonly Profile OverdriveDuration = new()
	{
		Id = RunUpgradeId.OverdriveDuration, Branch = Branch.Overdrive, Name = "OVERDRIVE TIME",
		Short = "Overdrive lasts longer", Icon = "rapid", Colour = Power, MaxLevel = 2, Requires = RunUpgradeId.OverdrivePower
	};

	public static readonly Profile BiggerNova = new()
	{
		Id = RunUpgradeId.BiggerNova, Branch = Branch.Nova, Name = TranslationServer.Translate("UPG_BiggerNova_NAME"),
		Short = "Wider blast", Icon = "nova", Colour = Blast, MaxLevel = 3, BossWeight = 1.1f
	};

	public static readonly Profile NovaPower = new()
	{
		Id = RunUpgradeId.NovaPower, Branch = Branch.Nova, Name = "NOVA POWER",
		Short = "Stronger, recharges faster", Icon = "nova", Colour = Blast, MaxLevel = 2, Requires = RunUpgradeId.BiggerNova
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		FireRate, SpreadShot, Piercing, DebrisCannon, IonLance,
		DashReach, DashBlink, OverdrivePower, OverdriveDuration, BiggerNova, NovaPower
	};

	/// <summary>Every rank the tree holds. A weapon counts once — only one can be taken.</summary>
	public static readonly int MaxTotalLevels = CountMaxLevels();

	private static int CountMaxLevels()
	{
		int total = 1;
		foreach (Profile profile in All)
		{
			if (profile.Equips == null)
				total += profile.MaxLevel;
		}
		return total;
	}

	public static Profile Get(RunUpgradeId id)
	{
		foreach (Profile profile in All)
		{
			if (profile.Id == id)
				return profile;
		}

		return null;
	}

	/// <summary>The ability a branch belongs to, or null for the gun, which is always open.</summary>
	public static Ability? AbilityFor(Branch branch) => branch switch
	{
		Branch.Dash => Ability.Dash,
		Branch.Overdrive => Ability.Overdrive,
		Branch.Nova => Ability.Nova,
		_ => null
	};

	public static string BranchName(Branch branch) => branch switch
	{
		Branch.Gun => "GUN",
		Branch.Dash => "DASH",
		Branch.Overdrive => "OVERDRIVE",
		_ => "NOVA"
	};

	public static string BranchIcon(Branch branch) => branch switch
	{
		Branch.Gun => "firerate",
		Branch.Dash => "dash",
		Branch.Overdrive => "rapid",
		_ => "nova"
	};

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
