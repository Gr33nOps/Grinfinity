using Godot;

/// <summary>
/// A temporary rule change. Announced, twenty seconds, then gone.
///
/// Events exist so an orbit has something to be described *by* — "the one where
/// gravity inverted at four minutes" beats "the one that lasted four minutes".
/// </summary>
public enum ArenaEventId
{
	/// <summary>Nothing is happening. The default between events.</summary>
	Calm,
	/// <summary>A constant drift across the arena, on everything.</summary>
	SolarWind,
	/// <summary>No dashing. The escape hatch is closed.</summary>
	NoDash,
	/// <summary>Bodies arrive huge and slow.</summary>
	GiantSlugs,
	/// <summary>You repel instead of pull. The most on-theme modifier available.</summary>
	InvertedGravity
}

public static class ArenaEvents
{
	public sealed class Profile
	{
		public required ArenaEventId Id { get; init; }
		public required string Name { get; init; }
		public required string Effect { get; init; }
		public required Color Colour { get; init; }
	}

	public static readonly Profile SolarWind = new()
	{
		Id = ArenaEventId.SolarWind,
		Name = TranslationServer.Translate("EVENT_SolarWind_NAME"),
		Effect = TranslationServer.Translate("EVENT_SolarWind_EFFECT"),
		Colour = new Color(1.0f, 0.82f, 0.45f)
	};

	public static readonly Profile NoDash = new()
	{
		Id = ArenaEventId.NoDash,
		Name = TranslationServer.Translate("EVENT_NoDash_NAME"),
		Effect = TranslationServer.Translate("EVENT_NoDash_EFFECT"),
		Colour = new Color(1.0f, 0.45f, 0.45f)
	};

	public static readonly Profile GiantSlugs = new()
	{
		Id = ArenaEventId.GiantSlugs,
		Name = TranslationServer.Translate("EVENT_GiantSlugs_NAME"),
		Effect = TranslationServer.Translate("EVENT_GiantSlugs_EFFECT"),
		Colour = new Color(0.7f, 0.8f, 1.0f)
	};

	public static readonly Profile InvertedGravity = new()
	{
		Id = ArenaEventId.InvertedGravity,
		Name = TranslationServer.Translate("EVENT_InvertedGravity_NAME"),
		Effect = TranslationServer.Translate("EVENT_InvertedGravity_EFFECT"),
		Colour = new Color(0.86f, 0.6f, 1.0f)
	};

	// Declared last: static field initialisers run in source order. Calm is not
	// in the table because it is the absence of an event, not one of them.
	public static readonly Profile[] All = { SolarWind, NoDash, GiantSlugs, InvertedGravity };

	public static Profile Get(ArenaEventId id) => All[(int)id - 1];

	public static ArenaEventId Roll() => (ArenaEventId)(RunState.Rng.RandiRange(0, All.Length - 1) + 1);
}
