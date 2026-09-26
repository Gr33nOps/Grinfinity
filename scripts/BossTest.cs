using Godot;

/// <summary>
/// A cheat mode for tuning the bosses: just the planet and one boss, with no
/// ordinary enemies and no arena events, and each boss beaten brings on the
/// next. Only offered when the game runs from the editor, never in a release
/// build, and a test run is never recorded: no best time, leaderboard entry,
/// lifetime stats, achievements or planet unlocks.
/// </summary>
public static class BossTest
{
	/// <summary>Offered only in debug builds (running from the editor).</summary>
	public static bool Available => OS.IsDebugBuild();

	/// <summary>True while a boss test is being played. Cleared on the main menu.</summary>
	public static bool Active { get; set; }

	/// <summary>The first boss to fight: 0 the Coil, 1 the Brood, 2 the Black Hole.</summary>
	public static int FirstBoss { get; set; }

	/// <summary>Start with every upgrade already bought.</summary>
	public static bool Upgraded { get; set; }

	/// <summary>The planet cannot be hurt, for watching a boss's patterns.</summary>
	public static bool Invincible { get; set; }

	/// <summary>Seconds after the start before the first boss is announced.</summary>
	public const float FirstBossAfter = 1.5f;

	/// <summary>Seconds after a boss falls before the next is announced.</summary>
	public const float NextBossAfter = 4f;
}
