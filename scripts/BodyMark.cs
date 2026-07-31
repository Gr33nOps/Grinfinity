using Godot;

/// <summary>
/// Per-kind markings drawn over a body.
///
/// Every kind currently shares one of nine face sprites, so tint and scale alone
/// do not make a Bulwark's armour or a Satellite's facing legible — and standing
/// rule 3 says readability beats spectacle. These are the in-engine stand-in
/// until the distinct silhouettes in ASSETS.md exist; the shapes are deliberately
/// the ones that art will need to carry anyway.
///
/// The node is a child of the body, which rotates to face its travel direction,
/// so local +X is always "forward" here.
/// </summary>
public partial class BodyMark : Node2D
{
	/// <summary>Radius of the body sprite in local units. Marks are drawn around it.</summary>
	[Export] public float Radius { get; set; } = 26.0f;

	private BodyKind kind;
	private Color tint = Colors.White;
	private float pulse;
	private bool animated;

	public override void _Ready()
	{
		if (GetParent() is Body body)
			Configure(body.Kind, body.BaseTint);
	}

	private void Configure(BodyKind bodyKind, Color bodyTint)
	{
		kind = bodyKind;
		tint = bodyTint;
		animated = kind == BodyKind.Flare;
		Visible = kind is BodyKind.Bulwark or BodyKind.Satellite or BodyKind.Flare
			or BodyKind.Fracture or BodyKind.Splinter;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		if (!animated || !Visible)
			return;

		// A Flare is about to hurt you, so it is the one mark that moves.
		pulse += (float)delta * 4.5f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		switch (kind)
		{
			case BodyKind.Bulwark:
				DrawArmour();
				break;
			case BodyKind.Satellite:
				DrawFins();
				break;
			case BodyKind.Flare:
				DrawFuse();
				break;
			case BodyKind.Fracture:
				DrawCracks(1.0f);
				break;
			case BodyKind.Splinter:
				DrawCracks(0.55f);
				break;
		}
	}

	/// <summary>A plate across the leading face. What is not covered is the way in.</summary>
	private void DrawArmour()
	{
		var plate = new Color(0.93f, 0.96f, 1.0f);
		DrawArc(Vector2.Zero, Radius * 1.12f, -Mathf.Pi / 3.0f, Mathf.Pi / 3.0f, 24, plate, 7.0f, true);
		DrawArc(Vector2.Zero, Radius * 1.32f, -Mathf.Pi / 4.0f, Mathf.Pi / 4.0f, 18, plate, 4.0f, true);
	}

	/// <summary>Fins across the body, so which way it is pointing can be read at a glance.</summary>
	private void DrawFins()
	{
		var panel = new Color(tint.R * 0.75f, tint.G * 0.75f, tint.B * 0.85f);

		foreach (float side in new[] { -1.0f, 1.0f })
		{
			var rect = new Rect2(-Radius * 0.25f, side * Radius * 0.95f - Radius * 0.16f,
				Radius * 0.5f, Radius * 0.32f);
			DrawRect(rect, panel);
		}

		DrawLine(new Vector2(0, -Radius), new Vector2(0, Radius), panel, 3.0f, true);
		// A stub in front, marking the barrel it fires inward from.
		DrawLine(Vector2.Zero, new Vector2(Radius * 1.3f, 0), panel, 4.0f, true);
	}

	/// <summary>A lit ring that swells — the only telegraph a Flare gets.</summary>
	private void DrawFuse()
	{
		float swell = 0.5f + 0.5f * Mathf.Sin(pulse);
		var danger = new Color(1.0f, 0.30f, 0.30f, 0.45f + 0.45f * swell);
		DrawArc(Vector2.Zero, Radius * (1.15f + 0.18f * swell), 0f, Mathf.Tau, 32, danger, 5.0f, true);
	}

	/// <summary>Seams, so a body that is going to come apart looks like it will.</summary>
	private void DrawCracks(float scale)
	{
		var seam = new Color(0.32f, 0.16f, 0.42f, 0.9f);
		float r = Radius * scale;

		DrawLine(new Vector2(-r * 0.9f, -r * 0.2f), new Vector2(0, r * 0.15f), seam, 3.0f, true);
		DrawLine(new Vector2(0, r * 0.15f), new Vector2(r * 0.55f, -r * 0.6f), seam, 3.0f, true);
		DrawLine(new Vector2(0, r * 0.15f), new Vector2(r * 0.35f, r * 0.85f), seam, 3.0f, true);
	}
}
