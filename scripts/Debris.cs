using Godot;

/// <summary>
/// A chunk thrown off a dying enemy. It tumbles outward, slows and fades — pure
/// feedback, so a kill reads as something breaking apart rather than a sprite
/// vanishing. It does nothing and is worth nothing.
/// </summary>
public partial class Debris : Node2D
{
	[Export] public float LaunchSpeedMin { get; set; } = 160.0f;
	[Export] public float LaunchSpeedMax { get; set; } = 380.0f;
	[Export] public float Drag { get; set; } = 3.2f;
	[Export] public float Lifetime { get; set; } = 0.8f;

	private Vector2 velocity;
	private float spin;
	private float age;
	private float life;

	public override void _Ready()
	{
		foreach (Node child in GetChildren()) if (child is Polygon2D polygon) polygon.Hide();
		AddChild(new Sprite2D { Texture = GD.Load<Texture2D>("res://art/cosmic/debris.svg"), Scale = Vector2.One * .55f });

		float angle = RunState.Rng.Randf() * Mathf.Tau;
		velocity = Vector2.FromAngle(angle) * RunState.Rng.RandfRange(LaunchSpeedMin, LaunchSpeedMax);
		spin = RunState.Rng.RandfRange(-9f, 9f);
		life = Lifetime * RunState.Rng.RandfRange(0.75f, 1.25f);
		Rotation = angle;
		Scale *= RunState.Rng.RandfRange(0.75f, 1.25f);
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		age += step;
		if (age >= life)
		{
			QueueFree();
			return;
		}

		velocity *= Mathf.Max(1f - Drag * step, 0f);
		GlobalPosition += velocity * step;
		Rotation += spin * step;

		float fade = 1f - age / life;
		Modulate = new Color(Modulate, fade * fade);
	}

	/// <summary>An external push — a gravity well or the Black Hole tugging the wreckage.</summary>
	public void Nudge(Vector2 acceleration)
	{
		velocity += acceleration;
	}
}
