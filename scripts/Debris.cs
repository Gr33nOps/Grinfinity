using Godot;

/// <summary>
/// A mote shed by a dying body.
///
/// It is flung outward, caught by the world's gravity, swings around it once or
/// twice, and is absorbed. That short orbit is deliberate: it is the game
/// explaining its own central mechanic every single time something dies, and it
/// costs nothing to watch.
/// </summary>
public partial class Debris : Node2D
{
	[Export] public float MassValue { get; set; } = 2.0f;

	[ExportGroup("Motion")]
	[Export] public float LaunchSpeedMin { get; set; } = 210.0f;
	[Export] public float LaunchSpeedMax { get; set; } = 420.0f;
	/// <summary>Pull toward the world at birth.</summary>
	[Export] public float BaseAcceleration { get; set; } = 900.0f;
	/// <summary>Pull is multiplied up to this over <see cref="GrabTime"/>, so nothing lingers.</summary>
	[Export] public float FinalAccelerationFactor { get; set; } = 4.5f;
	[Export] public float GrabTime { get; set; } = 1.1f;
	[Export] public float Drag { get; set; } = 1.5f;
	[Export] public float AbsorbRadius { get; set; } = 46.0f;
	/// <summary>Hard stop, so a mote can never outlive the body that shed it.</summary>
	[Export] public float Lifetime { get; set; } = 6.0f;

	private Node2D world;
	private RunState run;
	private GameManager manager;
	private Vector2 velocity;
	private float age;
	private bool absorbed;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		world = manager?.GetNodeOrNull<Node2D>("player");
		run = manager?.Run;

		float angle = GD.Randf() * Mathf.Tau;
		float speed = (float)GD.RandRange(LaunchSpeedMin, LaunchSpeedMax);
		velocity = Vector2.FromAngle(angle) * speed;

		Rotation = angle;
		// Scaled relative to the scene, so the size set there stays authoritative.
		Scale *= (float)GD.RandRange(0.75, 1.25);
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		age += step;

		if (world == null || !IsInstanceValid(world) || age > Lifetime)
		{
			QueueFree();
			return;
		}

		Vector2 toWorld = world.GlobalPosition - GlobalPosition;
		float distance = toWorld.Length();

		if (distance <= AbsorbRadius)
		{
			Absorb();
			return;
		}

		// The pull ramps with age rather than with distance alone: the mote gets
		// its orbit, then the world reels it in on a deadline.
		float grab = Mathf.Lerp(1.0f, FinalAccelerationFactor, Mathf.Clamp(age / GrabTime, 0f, 1f));
		velocity += (toWorld / distance) * BaseAcceleration * grab * step;
		velocity *= Mathf.Max(1.0f - Drag * step, 0f);

		GlobalPosition += velocity * step;
		Rotation += step * 6.0f;
	}

	private void Absorb()
	{
		if (absorbed)
			return;

		absorbed = true;
		run?.AddMass(MassValue);
		manager?.PlayAbsorbTick();
		QueueFree();
	}
}
