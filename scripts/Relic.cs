using Godot;

/// <summary>
/// One passive, rolled at the start of every orbit.
///
/// Roguelike spice without a meta tree: the player does not choose it and cannot
/// build toward it, so it colours an orbit without turning the game into a
/// progression screen. Explicitly no "None" — every orbit gets one, or the roll
/// becomes a thing to be unlucky about.
/// </summary>
public enum RelicId
{
	/// <summary>Shots pass through two extra bodies.</summary>
	Piercing,
	/// <summary>Dashing drags every mote on the field straight to you.</summary>
	VampiricDash,
	/// <summary>Bodies close to the world are slowed.</summary>
	SlowAura,
	/// <summary>Bodies shed twice the debris.</summary>
	DoubleDebris
}

public static class Relics
{
	public sealed class Profile
	{
		public required RelicId Id { get; init; }
		public required string Name { get; init; }
		/// <summary>One line, shown when the orbit starts. What it does, plainly.</summary>
		public required string Effect { get; init; }
		public required Color Colour { get; init; }
	}

	public static readonly Profile Piercing = new()
	{
		Id = RelicId.Piercing,
		Name = "LONG SHOT",
		Effect = "Your shots pass through two more bodies.",
		Colour = new Color(0.55f, 0.9f, 1.0f)
	};

	public static readonly Profile VampiricDash = new()
	{
		Id = RelicId.VampiricDash,
		Name = "GREEDY DASH",
		Effect = "Dashing drags every mote on the field to you.",
		Colour = new Color(1.0f, 0.6f, 0.75f)
	};

	public static readonly Profile SlowAura = new()
	{
		Id = RelicId.SlowAura,
		Name = "DEEP WELL",
		Effect = "Bodies that get close move sluggishly.",
		Colour = new Color(0.78f, 0.66f, 1.0f)
	};

	public static readonly Profile DoubleDebris = new()
	{
		Id = RelicId.DoubleDebris,
		Name = "RICH SEAM",
		Effect = "Everything you kill sheds twice the debris.",
		Colour = new Color(1.0f, 0.82f, 0.42f)
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All = { Piercing, VampiricDash, SlowAura, DoubleDebris };

	public static Profile Get(RelicId id) => All[(int)id];

	public static RelicId Roll() => (RelicId)RunState.Rng.RandiRange(0, All.Length - 1);
}
