using Godot;

/// <summary>
/// How hard the arena is pushing right now, as a function of how long the orbit
/// has lasted. One curve, tuned once, that everyone plays.
///
/// This owns exactly one knob: how forgiving a body is to graze. Speed and
/// spawn cadence are *not* here, deliberately — <see cref="BodySpawner"/>
/// already ramps both across two stages and has its own late-game ceiling, and
/// layering a second multiplier on top of that would escalate them twice as
/// fast as either curve claims to.
///
/// It has never touched player damage, and still doesn't. Only how much room
/// the arena leaves to dodge.
/// </summary>
public static class Difficulties
{
	public sealed class Profile
	{
		/// <summary>Multiplies each body's collision radius. Never the player's.</summary>
		public float ContactRadiusMultiplier { get; init; } = 1.0f;
	}

	/// <summary>Neutral — what the old Normal preset was. The curve passes through it.</summary>
	public static readonly Profile Baseline = new();

	/// <summary>
	/// Bodies open more forgiving than Normal ever was, so the first minute has
	/// room to teach, and end up meaningfully less so — but bounded. An
	/// unbounded squeeze is just a slower way of becoming unfair.
	/// </summary>
	private const float OpeningRadius = 0.88f;
	private const float BaselineRadius = 1.0f;
	private const float FinalRadius = 1.16f;

	/// <summary>Seconds at which the curve reaches the old Normal values.</summary>
	private const float BaselineAt = 120.0f;
	/// <summary>Seconds at which it reaches its ceiling and holds.</summary>
	private const float CeilingAt = 420.0f;

	/// <summary>
	/// The curve at a given point in an orbit. Read once per body, at spawn, so
	/// a body's forgiveness is fixed for its whole life — a hitbox that grew
	/// while you were already committed to a graze would be exactly the kind of
	/// ambush this curve is supposed to avoid.
	/// </summary>
	public static Profile At(float seconds)
	{
		return new Profile { ContactRadiusMultiplier = RadiusAt(seconds) };
	}

	private static float RadiusAt(float seconds)
	{
		if (seconds <= 0f)
			return OpeningRadius;

		if (seconds < BaselineAt)
		{
			// Eased rather than linear: the opening minute should feel like it is
			// being handed to you, and tighten most in its back half.
			float t = seconds / BaselineAt;
			return Mathf.Lerp(OpeningRadius, BaselineRadius, t * t);
		}

		float late = Mathf.Clamp((seconds - BaselineAt) / (CeilingAt - BaselineAt), 0f, 1f);
		return Mathf.Lerp(BaselineRadius, FinalRadius, late);
	}
}
