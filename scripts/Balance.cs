using Godot;

/// <summary>
/// Every number worth tuning, in one place, grouped by system.
///
/// Times are seconds of survival. Where a value ramps over a run it is written as
/// a small table of (time, value) keys read by <see cref="Ramp"/>, so a curve can
/// be reshaped by editing a row rather than a formula.
/// </summary>
public static class Balance
{
	// --- Arena --------------------------------------------------------------
	// Roughly three screens each way at the 1920x1080 base: room to kite a pack
	// and to dash, small enough that the edge is never far away.

	/// <summary>Full size of the world, and the camera's limits.</summary>
	public static readonly Vector2 ArenaSize = new(5860f, 3860f);
	/// <summary>Dark empty space around the playable area. The camera can see into it; nothing can enter it.</summary>
	public const float ArenaBorder = 320f;

	// --- Camera ---------------------------------------------------------------
	/// <summary>How quickly the camera catches the planet. Higher is tighter.</summary>
	public const float CameraFollowRate = 8.5f;
	/// <summary>The planet can move this far from centre before the camera starts to follow.</summary>
	public const float CameraDeadZone = 28f;
	/// <summary>How far the camera leans toward where the player is aiming.</summary>
	public const float CameraAimLead = 90f;
	/// <summary>During a boss fight the camera shifts this share of the way toward the boss.</summary>
	public const float CameraBossLean = 0.25f;
	public const float CameraBossLeanMax = 190f;

	// --- Spawning -------------------------------------------------------------
	/// <summary>Enemies appear this far outside the visible screen.</summary>
	public const float SpawnBeyondView = 120f;
	/// <summary>
	/// Never closer than this to the planet, whatever the camera is doing. Just
	/// over the distance to the top and bottom of the screen, so enemies can come
	/// from every side rather than only the left and right.
	/// </summary>
	public const float SpawnMinDistance = 760f;
	/// <summary>An enemy this far from the planet is brought back just outside the view.</summary>
	public const float LeashDistance = 2500f;

	/// <summary>Seconds between spawns.</summary>
	public static readonly Vector2[] SpawnInterval =
	{
		new(0f, 0.95f), new(45f, 0.8f), new(120f, 0.66f), new(240f, 0.54f), new(420f, 0.44f),
		new(660f, 0.36f), new(900f, 0.29f), new(1500f, 0.2f), new(2400f, 0.13f)
	};

	/// <summary>Most enemies alive at once.</summary>
	public static readonly Vector2[] MaxAlive =
	{
		new(0f, 10f), new(60f, 15f), new(180f, 22f), new(360f, 30f),
		new(600f, 38f), new(900f, 46f), new(1500f, 58f), new(2400f, 70f)
	};

	/// <summary>Base enemy speed. Keeps creeping after the last key, up to <see cref="EnemySpeedCeiling"/>.</summary>
	public static readonly Vector2[] EnemySpeed =
	{
		new(0f, 92f), new(120f, 110f), new(360f, 140f), new(600f, 165f),
		new(900f, 190f), new(1500f, 220f)
	};
	public const float EnemySpeedCreepPerMinute = 3f;
	public const float EnemySpeedCeiling = 285f;

	// When each kind first joins the mix. The first of a kind arrives on its own
	// so it can be noticed.
	public const float ShardAt = 30f;
	public const float PlanetoidAt = 75f;
	public const float FractureAt = 120f;
	public const float SatelliteAt = 225f;
	public const float BulwarkAt = 300f;
	public const float FlareAt = 420f;
	/// <summary>By this time the mix has shifted as far toward the dangerous kinds as it goes.</summary>
	public const float CompositionPeakAt = 1080f;

	// --- Abilities --------------------------------------------------------------
	// Each boss is met with the newest ability in hand: Dash for learning the
	// arena, Overdrive for the Coil, Nova for the Brood.
	public const float DashUnlockAt = 40f;
	public const float OverdriveUnlockAt = 140f;
	public const float NovaUnlockAt = 310f;

	public const float DashCooldown = 2.2f;
	/// <summary>Distance covered by one dash, before upgrades.</summary>
	public const float DashDistance = 340f;
	public const float DashDuration = 0.15f;
	/// <summary>Blinking safety window after the dash stops moving.</summary>
	public const float DashGrace = 0.4f;
	/// <summary>Each Dash upgrade level adds this fraction of the base distance.</summary>
	public const float DashDistancePerLevel = 0.14f;
	/// <summary>Each Dash upgrade level adds this much grace, in seconds.</summary>
	public const float DashGracePerLevel = 0.08f;
	/// <summary>Share of a boss's full health one dash takes. Counted once per dash.</summary>
	public const float DashBossDamage = 0.035f;

	public const float OverdriveCooldown = 30f;
	public const float OverdriveDuration = 6f;
	/// <summary>Fire interval multiplier while Overdrive is up. Lower is faster.</summary>
	public const float OverdriveFireScale = 0.4f;
	public const float OverdriveFireScalePerLevel = 0.85f;
	public const float OverdriveDurationPerLevel = 1.5f;
	public const int OverdriveDamageMultiplier = 2;
	public const int OverdriveExtraPierce = 1;

	public const float NovaCooldown = 45f;
	public const float NovaRadius = 720f;
	public const float NovaRadiusPerLevel = 0.18f;
	/// <summary>How long the blast takes to reach its full radius. Enemies pop as it passes.</summary>
	public const float NovaExpandTime = 0.4f;
	/// <summary>Share of a boss's full health one Nova takes.</summary>
	public const float NovaBossDamage = 0.1f;

	/// <summary>Invulnerable blinking after a Shield breaks.</summary>
	public const float ShieldBreakGrace = 1.0f;

	// --- Upgrade drops ------------------------------------------------------
	// A drop comes due after a gap rolled in a range, and then falls from the
	// next kill — but only once the player has also been fighting since the last
	// one. Time sets the cadence, so two players in comparable runs see a
	// comparable number of drops; the kill requirement means hiding earns none.
	// Kill counts alone were tried first: kill rates climb so steeply with enemy
	// density that the whole build was maxed by six minutes.

	/// <summary>Seconds between enemy drops, rolled between these each time.</summary>
	public const float DropGapMin = 38f;
	public const float DropGapMax = 58f;
	/// <summary>The gap grows by this share per minute survived.</summary>
	public const float DropGapGrowthPerMinute = 0.1f;
	public const float DropGapGrowthCap = 2.4f;
	/// <summary>Kill value needed since the last drop (a Drifter is 1).</summary>
	public const float DropMinFighting = 10f;
	/// <summary>Seconds an enemy drop stays on the field. It blinks for the last quarter.</summary>
	public const float DropLifetime = 16f;
	/// <summary>Seconds a boss reward stays.</summary>
	public const float BossRewardLifetime = 40f;
	/// <summary>Pickups drift to the planet from this close.</summary>
	public const float PickupMagnetRadius = 320f;
	public const int BossRewardCount = 3;
	/// <summary>Weapon swaps cannot drop before this.</summary>
	public const float WeaponSwapAt = 150f;
	/// <summary>
	/// Seconds after one shield drops before an enemy can drop another. Without
	/// it, a finished build turns every drop into a spare life.
	/// </summary>
	public const float ShieldDropCooldown = 75f;

	// --- Bosses -------------------------------------------------------------
	/// <summary>When the first boss arrives.</summary>
	public const float FirstBossAt = 170f;
	/// <summary>Scheduled gap between boss arrivals in the first cycle.</summary>
	public const float BossGapFirstCycle = 180f;
	/// <summary>Scheduled gap once the bosses start coming round again.</summary>
	public const float BossGapLaterCycles = 150f;
	/// <summary>Never less than this between one boss dying and the next arriving.</summary>
	public const float BossBreather = 60f;
	/// <summary>Warning shown before a boss arrives.</summary>
	public const float BossWarning = 3.2f;
	/// <summary>Bosses arrive this far from the planet.</summary>
	public const float BossArrivalDistance = 720f;
	/// <summary>Boss health grows by this share with each full cycle.</summary>
	public const float BossHealthPerCycle = 0.3f;
	/// <summary>
	/// Share of the normal spawn rate that keeps running during a boss fight,
	/// per cycle. The first time round a boss is fought alone.
	/// </summary>
	public static float BossSupport(int cycle) => cycle <= 1 ? 0f : Mathf.Min(0.35f + 0.2f * (cycle - 2), 0.95f);

	public const int BossScoreBonus = 5000;

	// --- Hazards and events -----------------------------------------------------
	public const float FirstCometAt = 250f;
	public const float CometGapMin = 16f;
	public const float CometGapMax = 28f;
	/// <summary>Seconds a comet's path is shown before it flies.</summary>
	public const float CometWarning = 1.35f;
	public const float FirstWellAt = 470f;
	public const float WellGapMin = 45f;
	public const float WellGapMax = 75f;
	public const float FirstEventAt = 520f;

	// --- Score ----------------------------------------------------------------
	public const float PointsPerSecond = 10f;
	public const float PointsPerKill = 25f;

	/// <summary>
	/// Reads a (time, value) table. Linear between keys, flat before the first,
	/// flat after the last.
	/// </summary>
	public static float Ramp(Vector2[] keys, float time)
	{
		if (time <= keys[0].X)
			return keys[0].Y;

		for (int i = 1; i < keys.Length; i++)
		{
			if (time <= keys[i].X)
				return Mathf.Lerp(keys[i - 1].Y, keys[i].Y, (time - keys[i - 1].X) / (keys[i].X - keys[i - 1].X));
		}

		return keys[^1].Y;
	}
}
