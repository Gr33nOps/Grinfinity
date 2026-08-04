using Godot;

/// <summary>
/// A passive effect a run can be carrying.
///
/// These used to be rolled once at the top of every orbit, which made them a
/// pre-run-adjacent decision the player had no hand in. The roll is gone; the
/// effects are not, because they are tested, they work, and they are the right
/// shape for things a run can earn part-way through.
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
		Name = TranslationServer.Translate("RELIC_Piercing_NAME"),
		Effect = TranslationServer.Translate("RELIC_Piercing_EFFECT"),
		Colour = new Color(0.55f, 0.9f, 1.0f)
	};

	public static readonly Profile VampiricDash = new()
	{
		Id = RelicId.VampiricDash,
		Name = TranslationServer.Translate("RELIC_VampiricDash_NAME"),
		Effect = TranslationServer.Translate("RELIC_VampiricDash_EFFECT"),
		Colour = new Color(1.0f, 0.6f, 0.75f)
	};

	public static readonly Profile SlowAura = new()
	{
		Id = RelicId.SlowAura,
		Name = TranslationServer.Translate("RELIC_SlowAura_NAME"),
		Effect = TranslationServer.Translate("RELIC_SlowAura_EFFECT"),
		Colour = new Color(0.78f, 0.66f, 1.0f)
	};

	public static readonly Profile DoubleDebris = new()
	{
		Id = RelicId.DoubleDebris,
		Name = TranslationServer.Translate("RELIC_DoubleDebris_NAME"),
		Effect = TranslationServer.Translate("RELIC_DoubleDebris_EFFECT"),
		Colour = new Color(1.0f, 0.82f, 0.42f)
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All = { Piercing, VampiricDash, SlowAura, DoubleDebris };

	/// <summary>
	/// The profile for an effect, or null for <see cref="RelicId.None"/>.
	/// Indexed by search rather than by cast: None is the zero value, so a cast
	/// would land on the first real entry and describe an effect that is not
	/// actually running.
	/// </summary>
	public static Profile Get(RelicId id)
	{
		foreach (Profile profile in All)
		{
			if (profile.Id == id)
				return profile;
		}

		return null;
	}
}
