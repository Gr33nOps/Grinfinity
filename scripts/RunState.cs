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

	public float SurvivalTime { get; private set; }
	public int Kills { get; private set; }
	public int Streak { get; private set; }
	public int BestStreak { get; private set; }
	public float Mass { get; private set; }
	public int Moons { get; private set; }
	public int RingTier { get; private set; }

	/// <summary>This orbit's passive. Rolled once, in _Ready, and never changes.</summary>
	public RelicId Relic { get; private set; }

	public bool Has(RelicId relic) => Relic == relic;

	/// <summary>Mass as 0..1. This is what every other system should read.</summary>
	public float MassNormalised => MaxMass <= 0f ? 0f : Mathf.Clamp(Mass / MaxMass, 0f, 1f);

	/// <summary>Score multiplier earned by carrying mass.</summary>
	public float ScoreMultiplier => Mathf.Lerp(1.0f, HeavyScoreMultiplier, MassNormalised);

	/// <summary>Mean normalised mass over the orbit — how heavy the run was played.</summary>
	public float AverageMassNormalised => SurvivalTime <= 0f ? 0f : massTimeIntegral / SurvivalTime;

	/// <summary>
	/// Accumulated as it is earned, at whatever the multiplier was at that
	/// moment. A total computed at the end from average mass would make the
	/// multiplier on the HUD a lie — the player has to be able to watch heavy
	/// seconds pay more than light ones.
	/// </summary>
	public int Score => Mathf.RoundToInt(score);

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
		Mass = Mathf.Clamp(StartMass, 0f, MaxMass);
		Relic = Relics.Roll();
	}

	public override void _Process(double delta)
	{
		SurvivalTime += (float)delta;
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
