using Godot;

/// <summary>
/// What an enemy shot looks like: a round hot-pink orb with an ink rim and a
/// cream core, inside a soft glow. Round so it reads the same from any angle,
/// ringed in ink so it stands out against bright enemies and dark space alike,
/// and drawn the size of its hitbox, so what you see is exactly what hits you.
/// It swells in when fired, breathes gently while it flies, and shrinks away at
/// the end of its range instead of vanishing.
/// </summary>
public partial class HostileOrb : Node2D
{
	public static readonly Color Hot = new("ff4a6e");
	private static readonly Color Glow = new(1f, 0.3f, 0.45f);

	/// <summary>Radius in the parent's space. The bullet scene is scaled up 2x, so 8 is a 16 px orb, matching its hitbox.</summary>
	[Export] public float Radius { get; set; } = 8f;

	private float time;
	private float grow;
	private float fade = 1f;
	private bool fading;

	public override void _Ready() => time = RunState.Rng.Randf() * 10f;

	/// <summary>Shrinks the orb away over a moment; the owner frees itself when <see cref="Gone"/>.</summary>
	public void FadeOut() => fading = true;

	public bool Gone => fading && fade <= 0f;

	public override void _Process(double delta)
	{
		float step = (float)delta;
		time += step;
		grow = Mathf.Min(1f, grow + step / 0.12f);
		if (fading)
			fade = Mathf.Max(0f, fade - step / 0.15f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		float swell = Mathf.Sin(grow * Mathf.Pi * 0.5f) * (1f + 0.25f * Mathf.Sin(grow * Mathf.Pi));
		Draw(this, Vector2.Zero, Radius * swell * fade, time, fade);
	}

	/// <summary>Draws an orb at <paramref name="at"/>.</summary>
	public static void Draw(CanvasItem canvas, Vector2 at, float radius, float time, float alpha)
	{
		if (radius <= 0.1f)
			return;
		float breathe = 1f + 0.06f * Mathf.Sin(time * 9f);
		float r = radius * breathe;
		canvas.DrawCircle(at, r * 2.0f, new Color(Glow, 0.10f * alpha));
		canvas.DrawCircle(at, r * 1.5f, new Color(Glow, 0.18f * alpha));
		canvas.DrawCircle(at, r + 1.6f, new Color(ArcadeSkin.Ink, alpha));
		canvas.DrawCircle(at, r, new Color(Hot, alpha));
		canvas.DrawCircle(at, r * 0.5f, new Color(ArcadeSkin.Cream, alpha));
		canvas.DrawCircle(at + new Vector2(-r * 0.35f, -r * 0.4f), r * 0.2f, new Color(1f, 1f, 1f, 0.8f * alpha));
	}
}
