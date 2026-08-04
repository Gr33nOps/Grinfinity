using System.Collections.Generic;
using Godot;

/// <summary>
/// The single owner of one orbit: time, kills, streak, mass, moons and score.
///
/// Mass is the spine of the whole design — it is the risk dial and the score
/// multiplier at once, and everything else reads it from here rather than
/// keeping a copy. Nothing in this class touches the UI; it raises signals and
/// <see cref="UIManager"/> decides how to draw them.
/// </summary>
public partial class RunState : Node
{
	[Signal] public delegate void MassChangedEventHandler(float mass, float normalised);
	[Signal] public delegate void MoonCountChangedEventHandler(int moons);
	[Signal] public delegate void KillsChangedEventHandler(int kills);
	[Signal] public delegate void StreakChangedEventHandler(int streak, bool milestone);
	[Signal] public delegate void RingTierChangedEventHandler(int tier);
	/// <summary>Raised when a pickup is taken or one of its timers runs out.</summary>
	[Signal] public delegate void EffectsChangedEventHandler();

	/// <summary>Streak lengths worth shouting about, ascending.</summary>
	public static readonly int[] StreakMilestones = { 5, 10, 25, 50, 100 };

	/// <summary>
	/// The RNG driving this orbit. Every gameplay roll should draw from this
	/// instead of <c>GD.Rand*</c>, or Daily Alignment's fixed seed would not
	/// actually fix the orbit. Static so static roll tables (<see cref="Relics"/>,
	/// <see cref="ArenaEvent"/>, <see cref="PowerUpKind"/>) and classes with no
	/// <see cref="RunState"/> reference can reach it without one threaded through
	/// every call site — the same shortcut <see cref="BodySpawner.CurrentSpeed"/>
	/// already takes for "the current orbit's" ramped speed. <see cref="Modes"/>
	/// reseeds it in <see cref="GameManager"/>._EnterTree, before this instance
	/// (or anything that rolls against it) exists.
	/// </summary>
	public static RandomNumberGenerator Rng { get; } = new RandomNumberGenerator();

	/// <summary>
	/// How long the current orbit has lasted, reachable without a reference to
	/// the run itself — the same shortcut <see cref="Rng"/> and
	/// <see cref="BodySpawner.CurrentSpeed"/> already take. The escalation curve
	/// is read by bodies at spawn, and a body has no route to its RunState at
	/// that point.
	/// </summary>
	public static float ElapsedSeconds { get; private set; }

	[ExportGroup("Mass")]
	// Sized so a full dial is roughly 100 absorbed motes — about a hundred
	// bodies. Reaching maximum should be an achievement of the back half of an
	// orbit, not something that happens in the first twenty seconds.
	[Export] public float MaxMass { get; set; } = 200.0f;
	[Export] public float StartMass { get; set; } = 0.0f;

	/// <summary>Mass at which each ring tier appears. Ascending.</summary>
	[Export] public float[] RingThresholds { get; set; } = { 45.0f, 105.0f, 165.0f };

	/// <summary>Mass at which each moon is gained. Ascending.</summary>
	[Export] public float[] MoonThresholds { get; set; } = { 75.0f, 135.0f, 185.0f };

	/// <summary>
	/// Mass a tier must fall below before it is lost, so a body hovering on a
	/// threshold does not flicker its rings and moons on and off.
	/// </summary>
	[Export] public float TierHysteresis { get; set; } = 10.0f;

	[ExportGroup("Scoring")]
	[Export] public float PointsPerSecond { get; set; } = 10.0f;
	[Export] public float PointsPerKill { get; set; } = 25.0f;
	/// <summary>Extra points per kill for each link already in the streak.</summary>
	[Export] public float PointsPerStreakLink { get; set; } = 3.0f;
	/// <summary>
	/// Links past this stop paying. Without a ceiling a streak that never breaks
	/// makes every later kill worth more than the entire early run.
	/// </summary>
	[Export] public int StreakBonusCap { get; set; } = 25;
	/// <summary>Score multiplier at full mass. 1.0 at zero mass.</summary>
	[Export] public float HeavyScoreMultiplier { get; set; } = 3.0f;

	/// <summary>How long a streak survives without a kill before it resets.</summary>
	[Export] public float StreakWindow { get; set; } = 2.5f;

	[ExportGroup("Stardust")]
	[Export] public float StardustPerSecond { get; set; } = 0.4f;
	[Export] public int StardustPerKill { get; set; } = 2;
	[Export] public int StardustPerStreakBest { get; set; } = 3;

	public float SurvivalTime { get; private set; }
	public int Kills { get; private set; }
	public int Streak { get; private set; }
	public int BestStreak { get; private set; }
	public float Mass { get; private set; }
	public int Moons { get; private set; }
	public int RingTier { get; private set; }

	/// <summary>
	/// Whether this orbit has earned a given passive. A run can hold several at
	/// once now — they are bought at wave breaks rather than rolled once at the
	/// top, so there is no reason to limit a long run to one.
	/// </summary>
	public bool Has(RelicId relic) => passives.Contains(relic);

	/// <summary>The arena event currently running, or Calm.</summary>
	public ArenaEventId Event { get; private set; } = ArenaEventId.Calm;
	public float EventTimeLeft { get; private set; }

	public bool During(ArenaEventId id) => Event == id;

	/// <summary>Direction the Solar Wind is blowing this orbit. Fixed, so it can be learned.</summary>
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

	/// <summary>Mass as 0..1. This is what every other system should read.</summary>
	public float MassNormalised => MaxMass <= 0f ? 0f : Mathf.Clamp(Mass / MaxMass, 0f, 1f);

	/// <summary>Score multiplier earned by carrying mass.</summary>
	public float ScoreMultiplier => Mathf.Lerp(1.0f, HeavyScoreMultiplier, MassNormalised);

	/// <summary>Mean normalised mass over the orbit — how heavy the run was played.</summary>
	public float AverageMassNormalised => SurvivalTime <= 0f ? 0f : massTimeIntegral / SurvivalTime;

	/// <summary>Highest normalised mass reached this orbit — a high-water mark, not the mass at any one moment.</summary>
	public float PeakMassNormalised { get; private set; }

	/// <summary>
	/// Accumulated as it is earned, at whatever the multiplier was at that
	/// moment. A total computed at the end from average mass would make the
	/// multiplier on the HUD a lie — the player has to be able to watch heavy
	/// seconds pay more than light ones.
	/// </summary>
	public int Score => Mathf.RoundToInt(score);

	/// <summary>
	/// Stardust this orbit has earned so far — from time, kills and streaks.
	/// Never goes down: this is what the run produced, and what the recap and
	/// the lifetime tally are both interested in.
	/// </summary>
	public int StardustEarned => Mathf.RoundToInt(SurvivalTime * StardustPerSecond)
		+ Kills * StardustPerKill + BestStreak * StardustPerStreakBest;

	/// <summary>Stardust already spent on upgrades this orbit.</summary>
	public int StardustSpent { get; private set; }

	/// <summary>
	/// What is actually available to spend right now. Derived rather than
	/// accumulated, so the balance can never drift out of step with what the
	/// run earned.
	/// </summary>
	public int Stardust => Mathf.Max(StardustEarned - StardustSpent, 0);

	private readonly HashSet<RelicId> passives = new();
	private readonly Dictionary<RunUpgradeId, int> upgradeLevels = new();

	/// <summary>How many times an upgrade has been taken this orbit.</summary>
	public int LevelOf(RunUpgradeId id) => upgradeLevels.GetValueOrDefault(id, 0);

	/// <summary>True once the run can no longer take this one any further.</summary>
	public bool IsMaxed(RunUpgradeId id) => LevelOf(id) >= RunUpgrades.Get(id).MaxLevel;

	/// <summary>
	/// Buys one level, if the run can afford it and has not maxed it. Returns
	/// false rather than throwing, because the offer UI and the purchase can
	/// disagree by a frame's worth of earned stardust.
	/// </summary>
	public bool TryBuy(RunUpgradeId id)
	{
		RunUpgrades.Profile profile = RunUpgrades.Get(id);
		if (profile == null || IsMaxed(id))
			return false;

		int cost = profile.CostAt(LevelOf(id));
		if (cost > Stardust)
			return false;

		StardustSpent += cost;
		upgradeLevels[id] = LevelOf(id) + 1;

		if (profile.Grants != RelicId.None)
			passives.Add(profile.Grants);

		EmitSignal(SignalName.EffectsChanged);
		return true;
	}

	// --- Derived multipliers ------------------------------------------------
	// Read every frame by the things they modify, so a purchase takes effect on
	// the next shot rather than at the next orbit.

	// --- Abilities the run has earned ---------------------------------------
	// An orbit opens able to move and shoot and nothing else. Each of these is
	// a verb the player buys back at a wave break, which gives the opening a
	// teaching order it never had and gives the first few breaks something
	// unmistakably worth stopping for.

	/// <summary>Dash is available. False until it has been bought this orbit.</summary>
	public bool HasDash => LevelOf(RunUpgradeId.UnlockDash) > 0;
	/// <summary>Rapid fire is available.</summary>
	public bool HasRapidFire => LevelOf(RunUpgradeId.UnlockRapidFire) > 0;
	/// <summary>Nova is available.</summary>
	public bool HasNova => LevelOf(RunUpgradeId.UnlockNova) > 0;

	/// <summary>Shots come this much closer together. Below 1 is faster.</summary>
	public float FireIntervalScale => Mathf.Pow(0.86f, LevelOf(RunUpgradeId.FireRate));

	/// <summary>Dash comes back this much sooner. Below 1 is quicker.</summary>
	public float DashCooldownScale => Mathf.Pow(0.87f, LevelOf(RunUpgradeId.QuickerDash));

	/// <summary>Nova reaches this much further.</summary>
	public float NovaRadiusScale => 1.0f + 0.22f * LevelOf(RunUpgradeId.BiggerNova);

	/// <summary>Debris is caught from this much further out.</summary>
	public float PullScale => 1.0f + 0.25f * LevelOf(RunUpgradeId.WiderPull);

	private float score;
	private float streakTimer;
	private int nextMilestone;
	private float massTimeIntegral;

	// --- Pickups ---------------------------------------------------------
	// Every timed effect is one float of remaining seconds. Systems that care
	// ask this class rather than keeping their own copy, for the same reason
	// mass lives here: one owner, no drift.

	private float freezeLeft;
	private float magnetLeft;
	private float damageLeft;

	/// <summary>A shield absorbs one hit. It has no timer — it waits.</summary>
	public bool HasShield { get; private set; }

	public bool Frozen => freezeLeft > 0f;
	public bool Magnetised => magnetLeft > 0f;
	public bool Overcharged => damageLeft > 0f;

	/// <summary>Extra damage on every shot while Overcharge is up.</summary>
	[Export] public int OverchargeBonus { get; set; } = 1;

	/// <summary>Seconds left on a pickup, for the HUD. Zero when it is not up.</summary>
	public float TimeLeft(PowerUpKind kind) => kind switch
	{
		PowerUpKind.Freeze => freezeLeft,
		PowerUpKind.Magnet => magnetLeft,
		PowerUpKind.Damage => damageLeft,
		_ => 0f
	};

	/// <summary>Starts a pickup's effect. Taking one already up refreshes it.</summary>
	public void GrantPowerUp(PowerUpKind kind)
	{
		float duration = PowerUps.Get(kind).Duration;

		switch (kind)
		{
			case PowerUpKind.Shield: HasShield = true; break;
			case PowerUpKind.Freeze: freezeLeft = Mathf.Max(freezeLeft, duration); break;
			case PowerUpKind.Magnet: magnetLeft = Mathf.Max(magnetLeft, duration); break;
			case PowerUpKind.Damage: damageLeft = Mathf.Max(damageLeft, duration); break;
			// Nuke is instant; GameManager does the clearing.
		}

		EmitSignal(SignalName.EffectsChanged);
	}

	/// <summary>Spends the shield, if there is one.</summary>
	/// <returns>True if a hit was absorbed and the world survives.</returns>
	public bool ConsumeShield()
	{
		if (!HasShield)
			return false;

		HasShield = false;
		EmitSignal(SignalName.EffectsChanged);
		return true;
	}

	private void TickEffects(float delta)
	{
		if (EventTimeLeft > 0f)
			EventTimeLeft -= delta;

		bool changed = false;
		changed |= Countdown(ref freezeLeft, delta);
		changed |= Countdown(ref magnetLeft, delta);
		changed |= Countdown(ref damageLeft, delta);

		if (changed)
			EmitSignal(SignalName.EffectsChanged);
	}

	/// <returns>True on the frame the timer reaches zero.</returns>
	private static bool Countdown(ref float timer, float delta)
	{
		if (timer <= 0f)
			return false;

		timer -= delta;
		return timer <= 0f;
	}

	public override void _Ready()
	{
		// Every orbit now starts from the same place. Growth happens inside the
		// run, not in a shop before it, so there is no bought mass to fold in
		// and no relic rolled over the player's head at the top.
		ElapsedSeconds = 0f;
		Mass = Mathf.Clamp(StartMass, 0f, MaxMass);
		PeakMassNormalised = MassNormalised;
	}

	public override void _Process(double delta)
	{
		SurvivalTime += (float)delta;
		ElapsedSeconds = SurvivalTime;
		// Integrated rather than sampled, so a brief spike near the end cannot
		// pass for having played the whole orbit heavy.
		massTimeIntegral += MassNormalised * (float)delta;
		score += PointsPerSecond * ScoreMultiplier * (float)delta;
		TickEffects((float)delta);

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

		// A long chain is worth more per kill than the same kills spread out,
		// which is what makes the streak worth protecting.
		int paidLinks = Mathf.Min(Streak - 1, StreakBonusCap);
		score += (PointsPerKill + PointsPerStreakLink * paidLinks) * ScoreMultiplier;

		bool milestone = nextMilestone < StreakMilestones.Length
			&& Streak >= StreakMilestones[nextMilestone];

		if (milestone)
			nextMilestone++;

		EmitSignal(SignalName.KillsChanged, Kills);
		EmitSignal(SignalName.StreakChanged, Streak, milestone);
	}

	/// <summary>
	/// A flat award — beating a boss. Not multiplied by mass: a boss is worth the
	/// same whether it was fought light or heavy.
	/// </summary>
	public void AddBonus(int points)
	{
		score += Mathf.Max(points, 0);
	}

	/// <summary>Absorbing debris. The only way mass goes up.</summary>
	public void AddMass(float amount)
	{
		SetMass(Mass + Mathf.Max(amount, 0f));
	}

	/// <summary>
	/// Spending mass on an ability.
	/// </summary>
	/// <returns>False if there was not enough to spend, in which case nothing changed.</returns>
	public bool Vent(float amount)
	{
		if (amount <= 0f)
			return true;

		if (Mass < amount)
			return false;

		SetMass(Mass - amount);
		return true;
	}

	private void SetMass(float value)
	{
		float clamped = Mathf.Clamp(value, 0f, MaxMass);
		if (Mathf.IsEqualApprox(clamped, Mass))
			return;

		Mass = clamped;
		PeakMassNormalised = Mathf.Max(PeakMassNormalised, MassNormalised);
		EmitSignal(SignalName.MassChanged, Mass, MassNormalised);

		RefreshTier(RingThresholds, RingTier, SignalName.RingTierChanged, tier =>
		{
			RingTier = tier;
			return tier;
		});

		RefreshTier(MoonThresholds, Moons, SignalName.MoonCountChanged, tier =>
		{
			Moons = tier;
			return tier;
		});
	}

	/// <summary>
	/// Recomputes how many thresholds are met and raises <paramref name="signal"/>
	/// only when it changes. Losing a tier needs the mass to drop a further
	/// <see cref="TierHysteresis"/> below the threshold that granted it.
	/// </summary>
	private void RefreshTier(float[] thresholds, int current, StringName signal, System.Func<int, int> apply)
	{
		if (thresholds == null)
			return;

		int tier = 0;
		for (int i = 0; i < thresholds.Length; i++)
		{
			// Already holding this tier? Keep it until mass falls past the
			// hysteresis band. Not holding it? It costs the full threshold.
			float needed = i < current ? thresholds[i] - TierHysteresis : thresholds[i];
			if (Mass >= needed)
				tier = i + 1;
		}

		if (tier == current)
			return;

		apply(tier);
		EmitSignal(signal, tier);
	}

	/// <summary>
	/// A moon spent itself blocking a hit. Mass falls below the threshold that
	/// granted it, which is what actually removes the moon — so the block costs
	/// the risk the player took to earn it, and it cannot immediately come back.
	/// </summary>
	public void BreakMoon()
	{
		if (Moons <= 0 || MoonThresholds == null || MoonThresholds.Length == 0)
			return;

		float threshold = MoonThresholds[Mathf.Min(Moons, MoonThresholds.Length) - 1];
		SetMass(Mathf.Min(Mass, threshold - TierHysteresis - 0.01f));
	}

	/// <summary>Breaks every moon and drops every ring — used when the orbit ends.</summary>
	public void ClearTiers()
	{
		if (Moons != 0)
		{
			Moons = 0;
			EmitSignal(SignalName.MoonCountChanged, 0);
		}

		if (RingTier != 0)
		{
			RingTier = 0;
			EmitSignal(SignalName.RingTierChanged, 0);
		}
	}
}
