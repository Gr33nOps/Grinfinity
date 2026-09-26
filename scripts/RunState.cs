using System.Collections.Generic;
using Godot;

/// <summary>
/// The single owner of one run: time, kills, streak, the build and the
/// moons. Nothing in this class touches the UI; it raises signals and
/// <see cref="UIManager"/> decides how to draw them.
///
/// Everything here is run-only. A new run is a new RunState, so dying resets the
/// abilities, the upgrades, and the moons in one go.
/// </summary>
public partial class RunState : Node
{
	[Signal] public delegate void KillsChangedEventHandler(int kills);
	[Signal] public delegate void StreakChangedEventHandler(int streak, bool milestone);
	/// <summary>Raised when the moons, the build or an arena event changes.</summary>
	[Signal] public delegate void EffectsChangedEventHandler();
	/// <summary>Raised once per ability, the moment survival time reaches it.</summary>
	[Signal] public delegate void AbilityUnlockedEventHandler(int ability);

	/// <summary>Streak lengths worth shouting about, ascending.</summary>
	public static readonly int[] StreakMilestones = { 5, 10, 25, 50, 100 };

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

	[ExportGroup("Streak")]
	/// <summary>How long a streak survives without a kill before it resets.</summary>
	[Export] public float StreakWindow { get; set; } = 2.5f;


	public float SurvivalTime { get; private set; }
	public int Kills { get; private set; }
	public int Streak { get; private set; }
	public int BestStreak { get; private set; }

	/// <summary>
	/// Moons in orbit, up to <see cref="Balance.MaxMoons"/>. Each one is a
	/// shield: it blocks one lethal hit and is gone. Runs start with none.
	/// </summary>
	public int Moons { get; private set; }
	public bool HasShield => Moons > 0;

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
	/// <returns>False if it was maxed or locked.</returns>
	public bool TryGrant(RunUpgradeId id)
	{
		RunUpgrades.Profile profile = RunUpgrades.Get(id);
		if (profile == null || IsMaxed(id))
			return false;
		if (RunUpgrades.AbilityFor(profile.Branch) is Ability required && !IsUnlocked(required))
			return false;
		if (profile.Requires is RunUpgradeId below && LevelOf(below) == 0)
			return false;

		upgradeLevels[id] = LevelOf(id) + 1;

		PeakBuildFraction = Mathf.Max(PeakBuildFraction, BuildFraction);
		// A boss's gift can use up ranks a saved bar was waiting for.
		Banked = Mathf.Min(Banked, RanksLeft);
		EmitSignal(SignalName.EffectsChanged);
		return true;
	}

	/// <summary>Puts one more moon in orbit, if there is room.</summary>
	public void GrantShield()
	{
		if (Moons >= Balance.MaxMoons)
			return;

		Moons++;
		EmitSignal(SignalName.EffectsChanged);
	}

	/// <summary>Spends one moon, if there is one.</summary>
	/// <returns>True if a hit was absorbed and the planet survives.</returns>
	public bool ConsumeShield()
	{
		if (Moons <= 0)
			return false;

		Moons--;
		EmitSignal(SignalName.EffectsChanged);
		return true;
	}

	// --- Derived numbers --------------------------------------------------------
	// Read every time they are used, so a pickup takes effect on the next shot.

	/// <summary>Shots come this much closer together. Below 1 is faster.</summary>
	public float FireIntervalScale => Mathf.Pow(Balance.FireRatePerLevel, LevelOf(RunUpgradeId.FireRate));

	/// <summary>Extra enemies each shot passes through.</summary>
	public int ExtraPierce => LevelOf(RunUpgradeId.Piercing) switch { 0 => 0, 1 => 1, _ => 3 };

	public int SpreadLevel => LevelOf(RunUpgradeId.SpreadShot);

	public float DashDistance => Balance.DashDistance * (1f + Balance.DashDistancePerLevel * LevelOf(RunUpgradeId.DashReach));

	public float DashGrace => Balance.DashGrace + Balance.DashGracePerLevel * LevelOf(RunUpgradeId.DashBlink);

	public float NovaRadius => Balance.NovaRadius * (1f + Balance.NovaRadiusPerLevel * LevelOf(RunUpgradeId.BiggerNova));

	public float OverdriveDuration => Balance.OverdriveDuration + Balance.OverdriveDurationPerLevel * LevelOf(RunUpgradeId.OverdriveDuration);

	public float OverdriveFireScale => Balance.OverdriveFireScale * Mathf.Pow(Balance.OverdriveFireScalePerLevel, LevelOf(RunUpgradeId.OverdrivePower));

	/// <summary>Share of a boss's health one Nova takes.</summary>
	public float NovaBossDamage => Balance.NovaBossDamage + Balance.NovaBossDamagePerLevel * LevelOf(RunUpgradeId.NovaPower);

	/// <summary>Nova's cooldown after Nova Power has shortened it.</summary>
	public float NovaCooldown => Balance.NovaCooldown * Mathf.Pow(Balance.NovaCooldownPerLevel, LevelOf(RunUpgradeId.NovaPower));

	// --- CORE ---------------------------------------------------------------------
	// Every kill fills the bar; a full bar buys one upgrade from the tree. Full
	// bars are saved, up to three, so nothing is wasted by fighting on instead
	// of stopping to spend — and three saved bars buy three upgrades in one go.

	[Signal] public delegate void CoreChangedEventHandler(float fraction, bool ready);
	/// <summary>The build is finished and a full bar of CORE has come in: every ability recharges.</summary>
	[Signal] public delegate void OverchargedEventHandler();

	/// <summary>CORE toward the bar being filled now.</summary>
	public float Core { get; private set; }
	/// <summary>Upgrades bought with CORE so far. Each makes the next bar a little longer.</summary>
	public int UpgradesBought { get; private set; }
	/// <summary>Full bars saved and not yet spent, up to <see cref="Balance.MaxBankedUpgrades"/>.</summary>
	public int Banked { get; private set; }
	/// <summary>The size of the bar being filled now: every bar bought or saved makes the next one longer.</summary>
	public float CoreNeeded => Balance.CoreFirstBar * Mathf.Pow(Balance.CoreBarGrowth, UpgradesBought + Banked);
	public bool CoreReady => Banked > 0 && !BuildComplete;
	/// <summary>Ranks still in the tree. No point saving more bars than this.</summary>
	private int RanksLeft => Mathf.Max(RunUpgrades.MaxTotalLevels - TotalLevels, 0);
	private int BankCap => Mathf.Min(Balance.MaxBankedUpgrades, RanksLeft);
	/// <summary>
	/// How full the bar on the HUD is: toward the next saved upgrade (full while
	/// the savings are at their cap), or Overcharge once there is nothing left.
	/// </summary>
	public float CoreFraction => BuildComplete ? Mathf.Clamp(Overcharge / Balance.OverchargeBar, 0f, 1f)
		: Banked >= BankCap ? 1f : Mathf.Clamp(Core / CoreNeeded, 0f, 1f);
	/// <summary>CORE gathered after the build is complete, toward the next ability recharge.</summary>
	public float Overcharge { get; private set; }

	/// <summary>Nothing left in the tree to buy.</summary>
	public bool BuildComplete => TotalLevels >= RunUpgrades.MaxTotalLevels;

	public void AddCore(float amount)
	{
		if (!float.IsFinite(amount) || amount <= 0f)
			return;

		// Nothing left to buy: kills still count, toward a full recharge.
		if (BuildComplete)
		{
			Overcharge += amount;
			if (Overcharge >= Balance.OverchargeBar)
			{
				Overcharge = 0f;
				EmitSignal(SignalName.Overcharged);
			}
			EmitSignal(SignalName.CoreChanged, CoreFraction, false);
			return;
		}

		if (Banked >= BankCap)
			return;

		Core += amount;
		while (Banked < BankCap && Core >= CoreNeeded)
		{
			Core -= CoreNeeded;
			Banked++;
		}
		if (Banked >= BankCap)
			Core = 0f;
		EmitSignal(SignalName.CoreChanged, CoreFraction, CoreReady);
	}

	/// <summary>
	/// A CORE Burst: one whole upgrade saved at once, however full the bar was,
	/// and the progress toward the next one kept. A top-up of whatever was
	/// missing would make the pickup worth almost nothing to a nearly full bar.
	/// </summary>
	public void FillCore()
	{
		if (BuildComplete)
		{
			AddCore(Balance.OverchargeBar);
			return;
		}
		if (Banked >= BankCap)
			return;

		float progress = Mathf.Clamp(Core / CoreNeeded, 0f, 1f);
		Banked++;
		// The next bar is longer; keep the same share of it filled.
		Core = Banked >= BankCap ? 0f : progress * CoreNeeded;
		EmitSignal(SignalName.CoreChanged, CoreFraction, CoreReady);
	}

	/// <summary>Spends one saved bar on one rank of an upgrade.</summary>
	/// <returns>False if no bar is saved or the upgrade cannot be taken.</returns>
	public bool BuyWithCore(RunUpgradeId id)
	{
		if (!CoreReady || !CanTake(id))
			return false;

		// Moved from saved to bought first: the bar being filled keeps its size.
		Banked--;
		UpgradesBought++;
		TryGrant(id);
		EmitSignal(SignalName.CoreChanged, CoreFraction, CoreReady);
		return true;
	}

	/// <summary>Whether one more rank of this upgrade could be taken right now, CORE aside.</summary>
	public bool CanTake(RunUpgradeId id)
	{
		RunUpgrades.Profile profile = RunUpgrades.Get(id);
		if (profile == null || IsMaxed(id))
			return false;
		if (RunUpgrades.AbilityFor(profile.Branch) is Ability required && !IsUnlocked(required))
			return false;
		if (profile.Requires is RunUpgradeId below && LevelOf(below) == 0)
			return false;
		return true;
	}

	// --- Pickup timing ----------------------------------------------------------
	// A drop is due once a rolled gap has passed and the player has done some
	// fighting since the last one. Hidden on purpose: drops should feel random,
	// not like a bar filling.

	private float fighting;
	private float nextDropAt;
	private bool rolledFirstDrop;

	/// <summary>Seconds of survival when a moon last dropped, so moons cannot chain.</summary>
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

	// --- Clock and streak ---------------------------------------------------------

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

		bool milestone = nextMilestone < StreakMilestones.Length
			&& Streak >= StreakMilestones[nextMilestone];

		if (milestone)
			nextMilestone++;

		EmitSignal(SignalName.KillsChanged, Kills);
		EmitSignal(SignalName.StreakChanged, Streak, milestone);
	}

}
