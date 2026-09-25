using System.Collections.Generic;
using Godot;

/// <summary>What a pickup is.</summary>
public enum RewardKind
{
	/// <summary>One rank of an upgrade. Only bosses drop these; the tree sells the rest.</summary>
	Upgrade,
	/// <summary>Blocks one lethal hit.</summary>
	Shield,
	/// <summary>Fills the CORE bar at once.</summary>
	CoreBurst,
	/// <summary>Every unlocked ability ready again.</summary>
	PowerCell
}

/// <summary>What a pickup gives.</summary>
public readonly struct Reward
{
	public Reward(RunUpgradeId upgrade) { Kind = RewardKind.Upgrade; Upgrade = upgrade; }
	private Reward(RewardKind kind) { Kind = kind; Upgrade = default; }

	public static readonly Reward Shield = new(RewardKind.Shield);
	public static readonly Reward CoreBurst = new(RewardKind.CoreBurst);
	public static readonly Reward PowerCell = new(RewardKind.PowerCell);

	public RewardKind Kind { get; }
	public RunUpgradeId Upgrade { get; }
	public bool IsShield => Kind == RewardKind.Shield;

	public string Name => Kind switch
	{
		RewardKind.Shield => "MOON",
		RewardKind.CoreBurst => "CORE BURST",
		RewardKind.PowerCell => "POWER CELL",
		_ => RunUpgrades.Get(Upgrade).Name
	};

	public string Icon => Kind switch
	{
		RewardKind.Shield => "moon",
		RewardKind.CoreBurst => "core",
		RewardKind.PowerCell => "powercell",
		_ => RunUpgrades.Get(Upgrade).Icon
	};

	public Color Colour => Kind switch
	{
		RewardKind.Shield => Pickups.ShieldColour,
		RewardKind.CoreBurst => Pickups.CoreColour,
		RewardKind.PowerCell => Pickups.PowerCellColour,
		_ => RunUpgrades.Get(Upgrade).Colour
	};
}

/// <summary>
/// Decides what drops. Enemies drop only three things, and only ones that would
/// do something right now: no moon when the orbit is full, no CORE Burst into a
/// full bar, no Power Cell when every ability is already ready. Bosses drop
/// strong upgrades instead, chosen from what the run can still take. Pickups
/// already lying on the field count as taken, so two of the same thing never
/// drop into the same situation.
/// </summary>
public static class Pickups
{
	/// <summary>The moons' pale lilac, used wherever a moon shield shows or breaks.</summary>
	public static readonly Color ShieldColour = new(0.9f, 0.86f, 0.97f);
	public static readonly Color CoreColour = new("f5a451");
	public static readonly Color PowerCellColour = new("b58cff");

	/// <summary>How much CORE a kill gives, and how much it feeds the pickup timer. Tougher enemies give more.</summary>
	public static float CoreFor(BodyKind kind) => kind switch
	{
		BodyKind.Shard => 0.7f,
		BodyKind.Splinter => 0.4f,
		BodyKind.Planetoid => 3.0f,
		BodyKind.Fracture => 1.5f,
		BodyKind.Satellite => 2.0f,
		BodyKind.Bulwark => 2.5f,
		BodyKind.Flare => 2.0f,
		_ => 1.0f
	};

	/// <summary>One of the three enemy pickups, or false if none would help right now.</summary>
	/// <param name="abilitiesCharging">True if at least one unlocked ability is on cooldown.</param>
	public static bool TryRollEnemyDrop(RunState run, List<Reward> pending, bool abilitiesCharging, out Reward reward)
	{
		var options = new List<Reward>();
		if (ShieldEligible(run, pending))
			options.Add(Reward.Shield);
		if (run.Banked < Balance.MaxBankedUpgrades && !run.BuildComplete && !pending.Exists(r => r.Kind == RewardKind.CoreBurst))
			options.Add(Reward.CoreBurst);
		if (abilitiesCharging && !pending.Exists(r => r.Kind == RewardKind.PowerCell))
			options.Add(Reward.PowerCell);

		if (options.Count == 0)
		{
			reward = default;
			return false;
		}

		reward = options[RunState.Rng.RandiRange(0, options.Count - 1)];
		return true;
	}

	/// <summary>A strong upgrade for a boss to drop, or false if the tree has nothing left a boss may give.</summary>
	public static bool TryRollBossReward(RunState run, List<Reward> pending, out Reward reward)
	{
		var options = new List<(Reward reward, float weight)>();
		foreach (RunUpgrades.Profile profile in RunUpgrades.All)
		{
			int waiting = pending.FindAll(r => r.Kind == RewardKind.Upgrade && r.Upgrade == profile.Id).Count;
			if (profile.BossWeight > 0f && run.CanTake(profile.Id) && run.LevelOf(profile.Id) + waiting < profile.MaxLevel)
				options.Add((new Reward(profile.Id), profile.BossWeight));
		}

		float total = 0f;
		foreach (var option in options)
			total += option.weight;

		if (total <= 0f)
		{
			reward = default;
			return false;
		}

		float roll = RunState.Rng.Randf() * total;
		foreach (var option in options)
		{
			roll -= option.weight;
			if (roll <= 0f)
			{
				reward = option.reward;
				return true;
			}
		}

		reward = options[^1].reward;
		return true;
	}

	/// <summary>A moon can drop while there is room in orbit and none is already lying in the arena.</summary>
	public static bool ShieldEligible(RunState run, List<Reward> pending)
	{
		if (run.Moons >= Balance.MaxMoons || pending.Exists(r => r.IsShield))
			return false;

		return run.SurvivalTime - run.LastShieldDropAt >= Balance.ShieldDropCooldown;
	}
}
