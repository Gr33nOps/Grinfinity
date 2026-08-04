/// <summary>
/// How hard the arena is pushing right now.
///
/// This used to be a player-facing Easy/Normal/Hard choice. There is one curve
/// now, tuned once, that everyone plays — so what is left is the shape of the
/// knobs, not a menu of presets. The escalation pass replaces <see cref="Baseline"/>
/// with a function of elapsed run time; the hooks that read these numbers
/// (body contact radius, spawn cadence, body speed) already sit in the right
/// places and do not move when that happens.
///
/// Nothing here has ever touched player damage — only how much the arena
/// throws and how much room it leaves to dodge it.
/// </summary>
public static class Difficulties
{
	public sealed class Profile
	{
		/// <summary>Multiplies every spawn interval. Below 1.0 spawns faster.</summary>
		public float SpawnIntervalMultiplier { get; init; } = 1.0f;
		/// <summary>Multiplies body speed, start and ramp ceiling alike.</summary>
		public float SpeedMultiplier { get; init; } = 1.0f;
		/// <summary>Multiplies each body's collision radius. Never the player's.</summary>
		public float ContactRadiusMultiplier { get; init; } = 1.0f;
	}

	/// <summary>
	/// Neutral: what the old Normal preset was. The curve starts gentler than
	/// this and passes through it, rather than treating it as a floor.
	/// </summary>
	public static readonly Profile Baseline = new();
}
