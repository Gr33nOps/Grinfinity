using Godot;

/// <summary>The three permanent, stardust-bought upgrades.</summary>
public enum UpgradeId
{
	MoveSpeed,
	DashCooldown,
	StartMass
}

/// <summary>
/// Tuning for one upgrade track. Soft-capped by design — standing rule from the
/// roadmap's M5 entry: "must never trivialise an orbit". Each level is a small
/// percentage, and the track tops out well short of removing the tradeoff it
/// touches.
/// </summary>
public static class Upgrades
{
	public sealed class Profile
	{
		public required UpgradeId Id { get; init; }
		public required string Name { get; init; }
		public required string Description { get; init; }
		public required int MaxLevel { get; init; }
		/// <summary>Stardust cost of the next level, given the level currently held.</summary>
		public required System.Func<int, int> CostForLevel { get; init; }
		/// <summary>
		/// Effect per level. A fraction (0.03 = 3%) for MoveSpeed and DashCooldown;
		/// a flat amount for StartMass, since mass starts at zero and a percentage
		/// of zero is nothing.
		/// </summary>
		public required float PerLevel { get; init; }
	}

	public static readonly Profile MoveSpeed = new()
	{
		Id = UpgradeId.MoveSpeed,
		Name = "THRUST",
		Description = "Move speed, before mass slows you down.",
		MaxLevel = 5,
		// 40, 60, 90, 130, 180 — rises so the last level is a real decision, not a formality.
		CostForLevel = level => 40 + level * level * 20,
		PerLevel = 0.03f // 5 levels = +15% at the cap
	};

	public static readonly Profile DashCooldown = new()
	{
		Id = UpgradeId.DashCooldown,
		Name = "COOLANT",
		Description = "Shorter dash cooldown.",
		MaxLevel = 5,
		CostForLevel = level => 40 + level * level * 20,
		PerLevel = 0.035f // 5 levels = -17.5% at the cap
	};

	public static readonly Profile StartMass = new()
	{
		Id = UpgradeId.StartMass,
		Name = "BALLAST",
		Description = "Start every orbit already carrying a little mass.",
		MaxLevel = 5,
		CostForLevel = level => 50 + level * level * 25,
		// A flat amount rather than a fraction: mass starts at zero, so a
		// percentage of it would be a percentage of nothing.
		PerLevel = 6.0f
	};

	public static readonly Profile[] All = { MoveSpeed, DashCooldown, StartMass };

	public static Profile Get(UpgradeId id) => All[(int)id];
}
