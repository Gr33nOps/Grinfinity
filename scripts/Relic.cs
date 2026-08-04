using Godot;

/// <summary>
/// A passive effect a run can be carrying.
///
/// These used to be rolled once at the top of every orbit, which made them a
/// pre-run-adjacent decision the player had no hand in. The roll is gone and
/// so is the table of names and colours that went with it — a passive is now
/// bought at a wave break, and <see cref="RunUpgrades"/> already describes it
/// on the card that sells it. What is left is the identity of the effect.
///
/// <see cref="None"/> is the zero value on purpose. Without it, a run that has
/// earned nothing would default into the first entry and quietly hand out a
/// permanent passive nobody asked for.
/// </summary>
public enum RelicId
{
	/// <summary>Nothing. The state every orbit now starts in.</summary>
	None,
	/// <summary>Shots pass through two extra bodies.</summary>
	Piercing,
	/// <summary>Dashing drags every mote on the field straight to you.</summary>
	VampiricDash,
	/// <summary>Bodies close to the world are slowed.</summary>
	SlowAura,
	/// <summary>Bodies shed twice the debris.</summary>
	DoubleDebris
}
