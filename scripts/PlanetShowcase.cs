using Godot;

/// <summary>
/// One planet shown large, the way it looks in play: its face, and the ring
/// badge when the player has earned it. A locked planet is only a dark shape
/// with a padlock, so the roster can be browsed without giving the art away.
/// </summary>
public partial class PlanetShowcase : Control
{
	private const float PlanetSide = 190f;
	/// <summary>The ring art is drawn 1.56 times the planet's width, as on the menu.</summary>
	private const float RingSide = PlanetSide * 1.5625f;
	private static readonly Color Silhouette = new(0.16f, 0.1f, 0.2f);
	private static readonly Color Glow = new(0.96f, 0.64f, 0.32f, 0.05f);

	private Texture2D planet, face, blink, ringBack, ringFront;
	private bool locked, ringed;
	private float time;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		ringBack = GD.Load<Texture2D>("res://art/cosmic/ring_2_back.svg");
		ringFront = GD.Load<Texture2D>("res://art/cosmic/ring_2_front.svg");
		ringed = WorldRings.Earned;
	}

	/// <summary>Shows planet <paramref name="id"/>, unlocked or not.</summary>
	public void Present(int id, bool isLocked)
	{
		planet = GD.Load<Texture2D>($"res://art/cosmic/planet_{id}.svg");
		face = Worlds.Face(id);
		blink = Worlds.Face(id, true);
		locked = isLocked;
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (planet == null)
			return;
		var centre = Size * 0.5f + new Vector2(0f, Mathf.Sin(time * 1.6f) * 4f);
		DrawCircle(centre, PlanetSide * 0.72f, Glow);

		bool ring = ringed && !locked;
		if (ring)
			DrawTextureRect(ringBack, Square(centre, RingSide), false);
		DrawTextureRect(planet, Square(centre, PlanetSide), false, locked ? Silhouette : Colors.White);
		if (locked)
		{
			Padlock(centre);
			return;
		}
		DrawTextureRect(time % 4.5f > 4.35f ? blink : face, Square(centre, PlanetSide), false);
		if (ring)
			DrawTextureRect(ringFront, Square(centre, RingSide), false);
	}

	private void Padlock(Vector2 at)
	{
		var shackle = at + new Vector2(0f, -8f);
		DrawArc(shackle, 17f, Mathf.Pi, Mathf.Tau, 20, ArcadeSkin.Muted, 7f, true);
		DrawLine(shackle + new Vector2(-17f, 0f), shackle + new Vector2(-17f, 10f), ArcadeSkin.Muted, 7f);
		DrawLine(shackle + new Vector2(17f, 0f), shackle + new Vector2(17f, 10f), ArcadeSkin.Muted, 7f);
		var body = new Rect2(at + new Vector2(-27f, 0f), new Vector2(54f, 42f));
		DrawStyleBox(new StyleBoxFlat
		{
			BgColor = ArcadeSkin.Muted,
			CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8
		}, body);
		DrawCircle(body.GetCenter() + new Vector2(0f, -3f), 6f, Silhouette);
		DrawLine(body.GetCenter() + new Vector2(0f, -1f), body.GetCenter() + new Vector2(0f, 10f), Silhouette, 5f);
	}

	private static Rect2 Square(Vector2 centre, float side) => new(centre - Vector2.One * side * 0.5f, Vector2.One * side);
}
