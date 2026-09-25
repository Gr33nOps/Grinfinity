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
		new(0f, 0.78f), new(40f, 0.66f), new(100f, 0.56f), new(200f, 0.5f), new(330f, 0.43f),
		new(520f, 0.35f), new(760f, 0.28f), new(1200f, 0.19f), new(2000f, 0.13f)
	};

	/// <summary>Most enemies alive at once.</summary>
	public static readonly Vector2[] MaxAlive =
	{
		new(0f, 13f), new(45f, 18f), new(140f, 23f), new(280f, 28f),
		new(470f, 35f), new(720f, 44f), new(1200f, 58f), new(2000f, 72f)
	};

	/// <summary>Base enemy speed. Keeps creeping after the last key, up to <see cref="EnemySpeedCeiling"/>.</summary>
	public static readonly Vector2[] EnemySpeed =
	{
		new(0f, 100f), new(100f, 118f), new(280f, 138f), new(470f, 160f),
		new(720f, 195f), new(1200f, 225f)
	};
	public const float EnemySpeedCreepPerMinute = 3f;
	public const float EnemySpeedCeiling = 285f;

	// When each kind first joins the mix. The first of a kind arrives on its own
	// so it can be noticed.
	public const float ShardAt = 15f;
	public const float PlanetoidAt = 45f;
	public const float FractureAt = 80f;
	public const float SatelliteAt = 150f;
	public const float BulwarkAt = 240f;
	public const float FlareAt = 330f;
	/// <summary>By this time the mix has shifted as far toward the dangerous kinds as it goes.</summary>
	public const float CompositionPeakAt = 780f;

	// --- Abilities --------------------------------------------------------------
	// All three are ready from the first second. Unlocking them over five minutes
	// made the opening a long stretch of plain shooting; the growth now comes
	// from upgrades instead. Set these above zero to stagger them again.
	public const float DashUnlockAt = 0f;
	public const float OverdriveUnlockAt = 0f;
	public const float NovaUnlockAt = 0f;

	public const float DashCooldown = 2.0f;
	/// <summary>Distance covered by one dash, before upgrades.</summary>
	public const float DashDistance = 340f;
	public const float DashDuration = 0.15f;
	/// <summary>Blinking safety window after the dash stops moving.</summary>
	public const float DashGrace = 0.4f;
	/// <summary>Each Dash upgrade level adds this fraction of the base distance.</summary>
	public const float DashDistancePerLevel = 0.14f;
	/// <summary>Each Dash upgrade level adds this much grace, in seconds.</summary>
	public const float DashGracePerLevel = 0.12f;
	/// <summary>Share of a boss's full health one dash takes. Counted once per dash.</summary>
	public const float DashBossDamage = 0.035f;

	public const float OverdriveCooldown = 26f;
	public const float OverdriveDuration = 6f;
	/// <summary>Fire interval multiplier while Overdrive is up. Lower is faster.</summary>
	public const float OverdriveFireScale = 0.4f;
	public const float OverdriveFireScalePerLevel = 0.88f;
	public const float OverdriveDurationPerLevel = 1.5f;
	public const int OverdriveDamageMultiplier = 2;
	public const int OverdriveExtraPierce = 1;

	public const float NovaCooldown = 40f;
	public const float NovaRadius = 720f;
	public const float NovaRadiusPerLevel = 0.15f;
	/// <summary>How long the blast takes to reach its full radius. Enemies pop as it passes.</summary>
	public const float NovaExpandTime = 0.4f;
	/// <summary>Share of a boss's full health one Nova takes.</summary>
	public const float NovaBossDamage = 0.1f;
	/// <summary>Nova Power adds this much boss damage per level.</summary>
	public const float NovaBossDamagePerLevel = 0.03f;
	/// <summary>Nova Power multiplies the cooldown by this per level.</summary>
	public const float NovaCooldownPerLevel = 0.85f;

	/// <summary>Invulnerable blinking after a Shield breaks.</summary>
	public const float ShieldBreakGrace = 1.0f;

	// --- CORE and the skill tree ------------------------------------------------
	// Every kill gives CORE (tougher enemies give more, see Pickups.CoreFor).
	// A full bar buys one upgrade from the tree, whenever the player chooses to
	// open it. Each bar needs a little more than the last, so upgrades come
	// thick and fast early and slow down once the build is strong.

	/// <summary>CORE needed for the first upgrade.</summary>
	public const float CoreFirstBar = 35f;
	/// <summary>Each later bar needs this much more than the one before.</summary>
	public const float CoreBarGrowth = 1.32f;
	/// <summary>Blinking safety after buying an upgrade, so returning to the fight is never an instant death.</summary>
	public const float UpgradeGrace = 1.4f;

	// --- Random pickups ----------------------------------------------------------
	// Only three things drop from enemies: a Shield, a CORE Burst (fills the bar)
	// and a Power Cell (every ability ready at once). A drop comes due after a
	// gap rolled in a range and falls from the next kill, but only once the
	// player has also been fighting since the last one, so hiding earns none.

	/// <summary>Seconds between pickups, rolled between these each time.</summary>
	public const float DropGapMin = 24f;
	public const float DropGapMax = 36f;
	/// <summary>The first pickup comes a little sooner.</summary>
	public const float FirstDropGapMin = 18f;
	public const float FirstDropGapMax = 26f;
	/// <summary>A pickup from a kill off screen lands this far from the planet, toward the kill.</summary>
	public const float DropMaxDistance = 520f;
	/// <summary>The gap grows by this share per minute survived.</summary>
	public const float DropGapGrowthPerMinute = 0.05f;
	public const float DropGapGrowthCap = 1.5f;
	/// <summary>Kill value needed since the last pickup (a Drifter is 1).</summary>
	public const float DropMinFighting = 10f;
	/// <summary>Seconds an enemy drop stays on the field. It blinks for the last quarter.</summary>
	public const float DropLifetime = 22f;
	/// <summary>Seconds a boss reward stays.</summary>
	public const float BossRewardLifetime = 40f;
	/// <summary>Pickups drift to the planet from this close.</summary>
	public const float PickupMagnetRadius = 440f;
	public const int BossRewardCount = 3;
	/// <summary>
	/// Seconds after one shield drops before an enemy can drop another. Without
	/// it, a finished build turns every drop into a spare life.
	/// </summary>
	public const float ShieldDropCooldown = 60f;

	// --- Bosses -------------------------------------------------------------
	/// <summary>When the first boss arrives.</summary>
	public const float FirstBossAt = 125f;
	/// <summary>Scheduled gap between boss arrivals in the first cycle.</summary>
	public const float BossGapFirstCycle = 140f;
	/// <summary>Scheduled gap once the bosses start coming round again.</summary>
	public const float BossGapLaterCycles = 130f;
	/// <summary>Never less than this between one boss dying and the next arriving.</summary>
	public const float BossBreather = 45f;
	/// <summary>Warning shown before a boss arrives.</summary>
	public const float BossWarning = 3.2f;
	/// <summary>Bosses arrive this far from the planet.</summary>
	public const float BossArrivalDistance = 720f;
	/// <summary>Boss health grows by this share with each full cycle.</summary>
	public const float BossHealthPerCycle = 0.7f;
	/// <summary>
	/// Share of the normal spawn rate that keeps running during a boss fight,
	/// per cycle. The first time round a boss is fought alone.
	/// </summary>
	public static float BossSupport(int cycle) => cycle <= 1 ? 0f : Mathf.Min(0.35f + 0.2f * (cycle - 2), 0.95f);


	// --- Hazards and events -----------------------------------------------------
	public const float FirstCometAt = 170f;
	public const float CometGapMin = 16f;
	public const float CometGapMax = 28f;
	/// <summary>Seconds a comet's path is shown before it flies.</summary>
	public const float CometWarning = 1.35f;
	public const float FirstWellAt = 330f;
	public const float WellGapMin = 45f;
	public const float WellGapMax = 75f;
	public const float FirstEventAt = 380f;


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
