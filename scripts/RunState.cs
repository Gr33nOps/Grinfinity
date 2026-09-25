using System.Collections.Generic;
using Godot;

/// <summary>
/// The single owner of one run: time, kills, streak, score, the build and the
/// shield. Nothing in this class touches the UI; it raises signals and
/// <see cref="UIManager"/> decides how to draw them.
///
/// Everything here is run-only. A new run is a new RunState, so dying resets the
/// abilities, the upgrades, the shield and the weapon in one go.
/// </summary>
public partial class RunState : Node
{
	[Signal] public delegate void KillsChangedEventHandler(int kills);
	[Signal] public delegate void StreakChangedEventHandler(int streak, bool milestone);
	/// <summary>Rings around the planet, one per stage of build strength.</summary>
	[Signal] public delegate void RingTierChangedEventHandler(int tier);
	/// <summary>Raised when the shield, the build or an arena event changes.</summary>
	[Signal] public delegate void EffectsChangedEventHandler();
	/// <summary>Raised once per ability, the moment survival time reaches it.</summary>
	[Signal] public delegate void AbilityUnlockedEventHandler(int ability);

	/// <summary>Streak lengths worth shouting about, ascending.</summary>
	public static readonly int[] StreakMilestones = { 5, 10, 25, 50, 100 };

	/// <summary>Total upgrade levels at which each ring appears.</summary>
	private static readonly int[] RingThresholds = { 5, 11, 18 };

	/// <summary>
	/// The RNG driving this run. Every gameplay roll should draw from this instead
	/// of <c>GD.Rand*</c>. Static so static roll tables and classes with no
	/// <see cref="RunState"/> reference can reach it. Reseeded in
	/// <see cref="GameManager"/>._EnterTree, before anything rolls against it.
	/// </summary>
	public static RandomNumberGenerator Rng { get; } = new RandomNumberGenerator();

	/// <summary>
	/// How long the current run has lasted, reachable without a reference to the
	/// run itself. Bodies read the escalation curve at spawn, before they have a
	/// route to their RunState.
	/// </summary>
	public static float ElapsedSeconds { get; private set; }

	[ExportGroup("Scoring")]
	/// <summary>Extra points per kill for each link already in the streak.</summary>
	[Export] public float PointsPerStreakLink { get; set; } = 3.0f;
	/// <summary>Links past this stop paying, or an unbroken streak would outweigh the whole early run.</summary>
	[Export] public int StreakBonusCap { get; set; } = 25;
	/// <summary>How long a streak survives without a kill before it resets.</summary>
	[Export] public float StreakWindow { get; set; } = 2.5f;

	[ExportGroup("Stardust")]
	[Export] public float StardustPerSecond { get; set; } = 0.4f;
	[Export] public int StardustPerKill { get; set; } = 2;
	[Export] public int StardustPerStreakBest { get; set; } = 3;

	public float SurvivalTime { get; private set; }
	public WeaponId Weapon { get; private set; } = WeaponId.Comet;
	public int Kills { get; private set; }
	public int Streak { get; private set; }
	public int BestStreak { get; private set; }
	public int RingTier { get; private set; }

	/// <summary>A shield blocks exactly one lethal hit. Runs start without one.</summary>
	public bool HasShield { get; private set; }

	public int Score => (int)System.Math.Clamp(System.Math.Round(score), 0, int.MaxValue);

	/// <summary>Stardust this run has earned — time, kills and the best streak. Spent on nothing in-run.</summary>
	public int StardustEarned => (int)System.Math.Clamp(System.Math.Round(SurvivalTime * (double)StardustPerSecond)
		+ Kills * (double)StardustPerKill + BestStreak * (double)StardustPerStreakBest, 0, int.MaxValue);

	/// <summary>How much of the full build this run holds, 0..1. Recorded as the run's high-water mark.</summary>
	public float BuildFraction => Mathf.Clamp(TotalLevels / (float)RunUpgrades.MaxTotalLevels, 0f, 1f);
	public float PeakBuildFraction { get; private set; }

	public int TotalLevels
	{
		get
		{
			int total = 0;
			foreach (int level in upgradeLevels.Values)
				total += level;
			return total;
		}
	}

	// --- Abilities ------------------------------------------------------------

	private readonly HashSet<Ability> unlocked = new();

	public bool IsUnlocked(Ability ability) => unlocked.Contains(ability);
	public bool HasDash => IsUnlocked(Ability.Dash);
	public bool HasOverdrive => IsUnlocked(Ability.Overdrive);
	public bool HasNova => IsUnlocked(Ability.Nova);

	/// <summary>When an ability comes online in every run.</summary>
	public static float UnlockTime(Ability ability) => ability switch
	{
		Ability.Dash => Balance.DashUnlockAt,
		Ability.Overdrive => Balance.OverdriveUnlockAt,
		_ => Balance.NovaUnlockAt
	};

	/// <summary>Brings an ability online now. Safe to call twice.</summary>
	public void Unlock(Ability ability)
	{
		if (!unlocked.Add(ability))
			return;

		EmitSignal(SignalName.AbilityUnlocked, (int)ability);
		EmitSignal(SignalName.EffectsChanged);
	}

	// --- Upgrades ---------------------------------------------------------------

	private readonly Dictionary<RunUpgradeId, int> upgradeLevels = new();

	public int LevelOf(RunUpgradeId id) => upgradeLevels.GetValueOrDefault(id, 0);

	public bool IsMaxed(RunUpgradeId id) => RunUpgrades.Get(id) is not { } profile || LevelOf(id) >= profile.MaxLevel;

	/// <summary>Adds one level if the run can take it.</summary>
	/// <returns>False if it was maxed, locked, or a second weapon swap.</returns>
	public bool TryGrant(RunUpgradeId id)
	{
		RunUpgrades.Profile profile = RunUpgrades.Get(id);
		if (profile == null || IsMaxed(id))
			return false;
		if (profile.Requires is Ability required && !IsUnlocked(required))
			return false;
		if (profile.Equips != null && Weapon != WeaponId.Comet)
			return false;

		upgradeLevels[id] = LevelOf(id) + 1;
		if (profile.Equips is WeaponId weapon)
			Weapon = weapon;

		PeakBuildFraction = Mathf.Max(PeakBuildFraction, BuildFraction);
		RefreshRings();
		EmitSignal(SignalName.EffectsChanged);
		return true;
	}

	public void GrantShield()
	{
		if (HasShield)
			return;

		HasShield = true;
		EmitSignal(SignalName.EffectsChanged);
	}

	/// <summary>Spends the shield, if there is one.</summary>
	/// <returns>True if a hit was absorbed and the planet survives.</returns>
	public bool ConsumeShield()
	{
		if (!HasShield)
			return false;

		HasShield = false;
		EmitSignal(SignalName.EffectsChanged);
		return true;
	}

	private void RefreshRings()
	{
		int tier = 0;
		int total = TotalLevels;
		foreach (int threshold in RingThresholds)
		{
			if (total >= threshold)
				tier++;
		}

		if (tier == RingTier)
			return;

		RingTier = tier;
		EmitSignal(SignalName.RingTierChanged, tier);
	}

	// --- Derived numbers --------------------------------------------------------
	// Read every time they are used, so a pickup takes effect on the next shot.

	/// <summary>Shots come this much closer together. Below 1 is faster.</summary>
	public float FireIntervalScale => Mathf.Pow(0.88f, LevelOf(RunUpgradeId.FireRate));

	/// <summary>Extra enemies each shot passes through.</summary>
	public int ExtraPierce => LevelOf(RunUpgradeId.Piercing);

	public int SpreadLevel => LevelOf(RunUpgradeId.SpreadShot);

	public float DashDistance => Balance.DashDistance * (1f + Balance.DashDistancePerLevel * LevelOf(RunUpgradeId.DashBoost));

	public float DashGrace => Balance.DashGrace + Balance.DashGracePerLevel * LevelOf(RunUpgradeId.DashBoost);

	public float NovaRadius => Balance.NovaRadius * (1f + Balance.NovaRadiusPerLevel * LevelOf(RunUpgradeId.BiggerNova));

	public float OverdriveDuration => Balance.OverdriveDuration + Balance.OverdriveDurationPerLevel * LevelOf(RunUpgradeId.OverdriveBoost);

	public float OverdriveFireScale => Balance.OverdriveFireScale * Mathf.Pow(Balance.OverdriveFireScalePerLevel, LevelOf(RunUpgradeId.OverdriveBoost));

	// --- Upgrade drops -------------------------------------------------------
	// A drop is due once a rolled gap has passed and the player has done some
	// fighting since the last one. Hidden on purpose: drops should feel random,
	// not like a bar filling.

	private float fighting;
	private float nextDropAt;
	private bool rolledFirstDrop;

	/// <summary>Seconds of survival when a shield last dropped, so shields cannot chain.</summary>
	public float LastShieldDropAt { get; set; } = -999f;

	public bool DropDue => SurvivalTime >= nextDropAt && fighting >= Balance.DropMinFighting;

	/// <summary>Kill value since the last drop. A Drifter is worth 1.</summary>
	public void AddDropProgress(float points)
	{
		if (float.IsFinite(points) && points > 0f)
			fighting += points;
	}

	/// <summary>Called when a due drop has actually been placed.</summary>
	public void SpendDrop()
	{
		fighting = 0f;
		RollDropGap();
	}

	private void RollDropGap()
	{
		if (!rolledFirstDrop)
		{
			rolledFirstDrop = true;
			nextDropAt = SurvivalTime + Rng.RandfRange(Balance.FirstDropGapMin, Balance.FirstDropGapMax);
			return;
		}

		float growth = Mathf.Min(1f + Balance.DropGapGrowthPerMinute * SurvivalTime / 60f, Balance.DropGapGrowthCap);
		nextDropAt = SurvivalTime + Rng.RandfRange(Balance.DropGapMin, Balance.DropGapMax) * growth;
	}

	// --- Arena events -------------------------------------------------------------

	/// <summary>The arena event currently running, or Calm.</summary>
	public ArenaEventId Event { get; private set; } = ArenaEventId.Calm;
	public float EventTimeLeft { get; private set; }
	public bool During(ArenaEventId id) => Event == id;

	/// <summary>Direction the Solar Wind is blowing. Fixed per event, so it can be read.</summary>
	public Vector2 WindDirection { get; private set; } = Vector2.Right;

	/// <summary>Starts an event, or ends one by passing Calm.</summary>
	public void StartEvent(ArenaEventId id, float duration)
	{
		Event = id;
		EventTimeLeft = duration;

		if (id == ArenaEventId.SolarWind)
			WindDirection = Vector2.FromAngle(Rng.Randf() * Mathf.Tau);

		EmitSignal(SignalName.EffectsChanged);
	}

	// --- Clock and score ---------------------------------------------------------

	private double score;
	private float streakTimer;
	private int nextMilestone;

	public override void _Ready()
	{
		ElapsedSeconds = 0f;
		RollDropGap();
	}

	public override void _Process(double delta)
	{
		SurvivalTime += (float)delta;
		ElapsedSeconds = SurvivalTime;
		score += Balance.PointsPerSecond * (float)delta;

		if (EventTimeLeft > 0f)
			EventTimeLeft -= (float)delta;

		foreach (Ability ability in System.Enum.GetValues<Ability>())
		{
			if (!IsUnlocked(ability) && SurvivalTime >= UnlockTime(ability))
				Unlock(ability);
		}

		if (streakTimer <= 0f)
			return;

		streakTimer -= (float)delta;
		if (streakTimer <= 0f && Streak > 0)
		{
			Streak = 0;
			nextMilestone = 0;
			EmitSignal(SignalName.StreakChanged, Streak, false);
		}
	}

	public void AddKill()
	{
		Kills++;
		Streak++;
		streakTimer = StreakWindow;

		if (Streak > BestStreak)
			BestStreak = Streak;

		// A long chain is worth more per kill than the same kills spread out.
		int paidLinks = Mathf.Min(Streak - 1, StreakBonusCap);
		score += Balance.PointsPerKill + PointsPerStreakLink * paidLinks;

		bool milestone = nextMilestone < StreakMilestones.Length
			&& Streak >= StreakMilestones[nextMilestone];

		if (milestone)
			nextMilestone++;

		EmitSignal(SignalName.KillsChanged, Kills);
		EmitSignal(SignalName.StreakChanged, Streak, milestone);
	}

	/// <summary>A flat award — beating a boss.</summary>
	public void AddBonus(int points)
	{
		score += Mathf.Max(points, 0);
	}
}
