using Godot;

/// <summary>
/// A cartoon pop, the game's one burst effect. A bright core swells and is
/// gone, a ring in the popped thing's own colour races outward, and a handful
/// of little cream and gold stars (the menu's sparkle shape) spin away and
/// shrink. Snappy and clean, so a kill reads as a satisfying pop rather than a
/// cloud of sparks, and it never hangs around to clutter the fight.
///
/// Drawn directly rather than simulated: a few shapes per pop, nothing to pool.
/// </summary>
public partial class PopEffect : Node2D
{
	private static readonly Color Core = new("fff0ce");
	private static readonly Color[] StarColours = { new("fff0ce"), new("ffd66b"), new("f5a451") };

	/// <summary>Roughly the radius of what popped. Everything scales from it.</summary>
	public float Size { get; set; } = 50f;
	/// <summary>The popped thing's own colour, for the ring.</summary>
	public Color Tint { get; set; } = Core;
	public int Stars { get; set; } = 6;
	public float Duration { get; set; } = 0.42f;

	private float age;
	private float[] angles, speeds, spins, sizes;
	private Color[] colours;

	/// <summary>Adds a pop at a point. Skipped far off screen, where nobody would see it.</summary>
	public static void Spawn(Node context, Vector2 at, float size, Color tint, int stars = 6, float duration = 0.42f)
	{
		if (!Arena.IsNearView(at, size * 2f + 100f))
			return;

		var pop = new PopEffect { Size = size, Tint = tint, Stars = stars, Duration = duration };
		pop.GlobalPosition = at;
		GameManager.Spawn(context, pop);
	}

	public override void _Ready()
	{
		ZIndex = 6;
		// Looks only, never an outcome, so it stays off the run's seeded RNG.
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		angles = new float[Stars];
		speeds = new float[Stars];
		spins = new float[Stars];
		sizes = new float[Stars];
		colours = new Color[Stars];
		float offset = rng.Randf() * Mathf.Tau;
		for (int i = 0; i < Stars; i++)
		{
			// Evenly spread with a little wobble, so the burst looks full but not stamped.
			angles[i] = offset + Mathf.Tau * i / Stars + rng.RandfRange(-0.3f, 0.3f);
			speeds[i] = rng.RandfRange(1.1f, 1.8f);
			spins[i] = rng.RandfRange(-7f, 7f);
			sizes[i] = rng.RandfRange(0.16f, 0.26f);
			colours[i] = StarColours[rng.RandiRange(0, StarColours.Length - 1)];
		}
	}

	public override void _Process(double delta)
	{
		age += (float)delta;
		if (age >= Duration)
		{
			QueueFree();
			return;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = age / Duration;
		float eased = 1f - Mathf.Pow(1f - t, 3f);

		// The core: a solid bright disc that swells, then snaps shut. Solid all
		// the way, never see-through: a fading disc read as a grey smudge.
		float coreT = Mathf.Clamp(t / 0.3f, 0f, 1f);
		if (coreT < 1f)
		{
			float grow = coreT < 0.35f ? coreT / 0.35f : 1f - (coreT - 0.35f) / 0.65f;
			float coreRadius = Size * 0.8f * Mathf.Sin(grow * Mathf.Pi * 0.5f);
			DrawCircle(Vector2.Zero, coreRadius, Core);
			DrawCircle(Vector2.Zero, coreRadius * 0.55f, Colors.White);
		}

		// The ring, in the popped thing's colour, lightened so it reads as light.
		// Thick while it is fast, and gone quickly once it slows.
		float ringT = Mathf.Clamp(t / 0.6f, 0f, 1f);
		float ringOut = 1f - Mathf.Pow(1f - ringT, 3f);
		float ringRadius = Size * Mathf.Lerp(0.45f, 1.45f, ringOut);
		float ringWidth = Size * 0.24f * (1f - ringT) + 1f;
		if (ringT < 1f)
			DrawArc(Vector2.Zero, ringRadius, 0f, Mathf.Tau, 40, new Color(Tint.Lightened(0.5f), 1f - ringT), ringWidth, true);

		// The stars: out fast, spinning, shrinking away.
		float fade = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
		for (int i = 0; i < Stars; i++)
		{
			Vector2 at = Vector2.FromAngle(angles[i]) * Size * (0.35f + speeds[i] * eased);
			float starSize = Size * sizes[i] * Mathf.Pow(1f - t, 0.6f) + 1.5f;
			DrawStar(at, starSize, spins[i] * t, new Color(colours[i], fade));
		}
	}

	private void DrawStar(Vector2 centre, float size, float rotation, Color colour)
	{
		var points = new Vector2[8];
		for (int i = 0; i < 8; i++)
		{
			float radius = i % 2 == 0 ? size : size * 0.3f;
			points[i] = centre + Vector2.FromAngle(rotation + Mathf.Tau * i / 8f) * radius;
		}
		DrawColoredPolygon(points, colour);
	}
}
