using System.Collections.Generic;
using Godot;

/// <summary>What a pickup gives: one level of an upgrade, or a shield.</summary>
public readonly struct Reward
{
	public Reward(RunUpgradeId upgrade) { Upgrade = upgrade; IsShield = false; }
	private Reward(bool shield) { Upgrade = default; IsShield = shield; }

	public static readonly Reward Shield = new(true);

	public bool IsShield { get; }
	public RunUpgradeId Upgrade { get; }

	public string Name => IsShield ? UpgradeDrops.ShieldName : RunUpgrades.Get(Upgrade).Name;
	public string Icon => IsShield ? "shield" : RunUpgrades.Get(Upgrade).Icon;
	public Color Colour => IsShield ? UpgradeDrops.ShieldColour : RunUpgrades.Get(Upgrade).Colour;

	public bool Matches(Reward other) => IsShield == other.IsShield && (IsShield || Upgrade == other.Upgrade);
}

/// <summary>
/// Decides what drops. The roll is random, but only ever between things that
/// would actually do something right now: nothing maxed, nothing for an ability
/// the run does not have yet, never a second weapon swap, never a shield on top
/// of a shield. Pickups already lying on the field count as taken, so two of the
/// last level of something cannot both drop.
/// </summary>
public static class UpgradeDrops
{
	public const string ShieldName = "SHIELD";
	public static readonly Color ShieldColour = new(0.55f, 0.85f, 1.0f);

	/// <summary>Enemy drops weight the shield at this, against roughly 1 for each upgrade.</summary>
	private const float ShieldWeight = 1.1f;
	private const float BossShieldWeight = 0.6f;

	/// <summary>How much a kill feeds the hidden drop meter. Tougher enemies are worth more.</summary>
	public static float PointsFor(BodyKind kind) => kind switch
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

	/// <summary>Picks one eligible reward, or returns false if nothing would be useful.</summary>
	/// <param name="pending">Rewards already on the field, or already rolled in this batch.</param>
	public static bool TryRoll(RunState run, List<Reward> pending, bool boss, out Reward reward)
	{
		var options = new List<(Reward reward, float weight)>();

		foreach (RunUpgrades.Profile profile in RunUpgrades.All)
		{
			if (IsEligible(run, profile, pending, boss))
				options.Add((new Reward(profile.Id), boss ? profile.BossWeight : profile.Weight));
		}

		if (ShieldEligible(run, pending, boss))
			options.Add((Reward.Shield, boss ? BossShieldWeight : ShieldWeight));

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

	public static bool IsEligible(RunState run, RunUpgrades.Profile profile, List<Reward> pending, bool boss)
	{
		int waiting = pending.FindAll(r => !r.IsShield && r.Upgrade == profile.Id).Count;
		if (run.LevelOf(profile.Id) + waiting >= profile.MaxLevel)
			return false;

		if (profile.Requires is Ability required && !run.IsUnlocked(required))
			return false;

		if (profile.Equips != null)
		{
			if (run.Weapon != WeaponId.Comet)
				return false;
			if (pending.Exists(r => !r.IsShield && RunUpgrades.Get(r.Upgrade).Equips != null))
				return false;
			if (!boss && run.SurvivalTime < Balance.WeaponSwapAt)
				return false;
		}

		return true;
	}

	public static bool ShieldEligible(RunState run, List<Reward> pending, bool boss)
	{
		if (run.HasShield || pending.Exists(r => r.IsShield))
			return false;

		return boss || run.SurvivalTime - run.LastShieldDropAt >= Balance.ShieldDropCooldown;
	}
}
