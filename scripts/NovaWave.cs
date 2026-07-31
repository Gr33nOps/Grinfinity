using Godot;

/// <summary>
/// The nova shockwave: a ring that races out to the ability's radius and fades.
///
/// It exists for readability as much as spectacle — it is the only thing that
/// tells the player how far the blast actually reached, which they need in order
/// to learn when spending mass is worth it.
/// </summary>
public partial class NovaWave : Node2D
{
	[Export] public float MaxRadius { get; set; } = 520.0f;
	[Export] public float Duration { get; set; } = 0.42f;
	[Export] public float StartWidth { get; set; } = 26.0f;
	[Export] public Color WaveColor { get; set; } = new Color(1.0f, 0.86f, 0.5f);

	private float age;

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
		float t = Mathf.Clamp(age / Duration, 0f, 1f);

		// Fast out, slow to settle: the ring should read as an impulse, not a
		// steadily expanding circle.
		float eased = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
		float radius = MaxRadius * eased;
		float width = StartWidth * (1.0f - t);
		var color = new Color(WaveColor.R, WaveColor.G, WaveColor.B, WaveColor.A * (1.0f - t));

		DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 72, color, Mathf.Max(width, 1.0f), true);
	}
}
