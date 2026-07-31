using Godot;

/// <summary>
/// Planetary rings drawn straight onto the world, one band per mass tier.
///
/// This is the mass meter. Putting it on the player's own silhouette rather than
/// in a corner means risk is read where the player is already looking, and it
/// states "I am a planet" more plainly than any HUD element could.
///
/// Two of these sit on the player — one behind the sprite drawing the far half
/// of each ring, one in front drawing the near half — which is what makes the
/// bands read as rings around a body instead of circles painted on it.
/// </summary>
public partial class WorldRings : Node2D
{
	/// <summary>False draws the far half (put it behind the sprite), true the near half.</summary>
	[Export] public bool NearHalf { get; set; } = false;

	// Local units: the node inherits the world's scale, which is 2 at rest and
	// grows with mass, so these read roughly 2.6x larger on screen.
	[Export] public float InnerRadius { get; set; } = 44.0f;
	[Export] public float RadiusStep { get; set; } = 12.0f;
	[Export] public float Width { get; set; } = 3.4f;
	/// <summary>Vertical squash. Lower reads as a shallower viewing angle.</summary>
	[Export] public float Flatten { get; set; } = 0.30f;
	[Export] public float Tilt { get; set; } = -0.30f;
	[Export] public int Segments { get; set; } = 48;
	/// <summary>Speed of the brightness sweep that makes a solid ring look like it spins.</summary>
	[Export] public float SweepSpeed { get; set; } = 1.4f;
	[Export] public Color RingColor { get; set; } = new Color(1.0f, 0.78f, 0.45f);

	private RunState run;
	private Node2D world;
	private float[] appear = System.Array.Empty<float>();
	private float sweep;

	public override void _Ready()
	{
		run = GameManager.Of(this)?.Run;
		appear = new float[run?.RingThresholds?.Length ?? 3];

		// The world rotates to face the aim, and rings that spun with it would
		// read as a hula hoop. Detaching the transform keeps their tilt fixed;
		// the scale below still tracks mass.
		world = GetParent<Node2D>();
		TopLevel = true;
	}

	public override void _Process(double delta)
	{
		if (run == null || world == null || !IsInstanceValid(world))
			return;

		GlobalPosition = world.GlobalPosition;
		Scale = Vector2.One * world.Scale.X;

		sweep += (float)delta * SweepSpeed;

		// Each band eases in and out rather than snapping, so crossing a
		// threshold reads as the world growing rather than a sprite swapping.
		bool changing = false;
		for (int i = 0; i < appear.Length; i++)
		{
			float target = i < run.RingTier ? 1.0f : 0.0f;
			float next = Mathf.MoveToward(appear[i], target, (float)delta * 2.2f);
			if (!Mathf.IsEqualApprox(next, appear[i]))
				changing = true;
			appear[i] = next;
		}

		// Redraw only when something is actually moving.
		if (changing || run.RingTier > 0)
			QueueRedraw();
	}

	public override void _Draw()
	{
		for (int i = 0; i < appear.Length; i++)
		{
			if (appear[i] <= 0.01f)
				continue;

			DrawBand(i);
		}
	}

	private void DrawBand(int index)
	{
		float ease = appear[index];
		float radius = (InnerRadius + RadiusStep * index) * Mathf.Lerp(0.82f, 1.0f, ease);

		var points = new Vector2[Segments + 1];
		var colors = new Color[Segments + 1];

		// The far half is the top of the ellipse before tilt, the near half the
		// bottom, so each node walks its own semicircle of the same curve.
		float start = NearHalf ? 0.0f : Mathf.Pi;
		float cosTilt = Mathf.Cos(Tilt);
		float sinTilt = Mathf.Sin(Tilt);

		for (int s = 0; s <= Segments; s++)
		{
			float t = start + Mathf.Pi * s / Segments;
			float x = Mathf.Cos(t) * radius;
			float y = Mathf.Sin(t) * radius * Flatten;

			points[s] = new Vector2(x * cosTilt - y * sinTilt, x * sinTilt + y * cosTilt);

			// A brightness wave travelling round the band; a uniform ring has
			// nothing to show for rotating.
			float wave = 0.62f + 0.38f * Mathf.Sin(t * 3.0f - sweep);
			colors[s] = new Color(RingColor.R, RingColor.G, RingColor.B, RingColor.A * wave * ease);
		}

		DrawPolylineColors(points, colors, Width * Mathf.Lerp(0.5f, 1.0f, ease), true);
	}
}
