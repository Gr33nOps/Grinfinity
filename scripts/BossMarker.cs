using Godot;

/// <summary>
/// Marks where a boss is about to appear: rings closing in on the spot, faster
/// as the moment arrives. It is harmless; it exists so a boss never simply
/// appears somewhere the player was not looking.
/// </summary>
public partial class BossMarker : Node2D
{
	public Color Colour { get; set; } = new(0.86f, 0.72f, 1.0f);
	public float Duration { get; set; } = 3f;

	private float age;

	public override void _Ready()
	{
		ZIndex = -5;
	}

	public override void _Process(double delta)
	{
		age += (float)delta;
		if (age >= Duration + 0.2f)
		{
			QueueFree();
			return;
		}

		QueueRedraw();
	}

	public override void _Draw()
	{
		float progress = Mathf.Clamp(age / Duration, 0f, 1f);
		float fadeIn = Mathf.Clamp(age / 0.3f, 0f, 1f);

		DrawCircle(Vector2.Zero, 130f, new Color(Colour, 0.08f * fadeIn));
		DrawArc(Vector2.Zero, 130f, 0f, Mathf.Tau, 64, new Color(Colour, 0.55f * fadeIn), 4f, true);

		// Rings sweep inward on a beat that quickens toward arrival.
		float beat = age * Mathf.Lerp(1.2f, 3.5f, progress);
		for (int i = 0; i < 3; i++)
		{
			float t = Mathf.PosMod(beat + i / 3f, 1f);
			float radius = Mathf.Lerp(320f, 130f, t);
			DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 64, new Color(Colour, 0.4f * t * fadeIn), 6f, true);
		}

		// Four markers point at the spot from outside, like a lock-on.
		for (int i = 0; i < 4; i++)
		{
			Vector2 dir = Vector2.FromAngle(Mathf.Tau * i / 4f + age * 0.8f);
			Vector2 tip = dir * 150f;
			Vector2 side = dir.Orthogonal() * 16f;
			DrawColoredPolygon(new[] { tip, tip + dir * 34f + side, tip + dir * 34f - side }, new Color(Colour, 0.8f * fadeIn));
		}
	}
}
