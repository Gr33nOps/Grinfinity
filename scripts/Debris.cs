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
	/// <summary>How much harder the Magnet pickup pulls.</summary>
	[Export] public float MagnetFactor { get; set; } = 5.0f;
	/// <summary>Speed a mote is thrown at the world by the Greedy Dash relic.</summary>
	[Export] public float YankSpeed { get; set; } = 1500.0f;
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

		float angle = RunState.Rng.Randf() * Mathf.Tau;
		float speed = RunState.Rng.RandfRange(LaunchSpeedMin, LaunchSpeedMax);
		velocity = Vector2.FromAngle(angle) * speed;

		Rotation = angle;
		// Scaled relative to the scene, so the size set there stays authoritative.
		Scale *= RunState.Rng.RandfRange(0.75f, 1.25f);
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

		// A run that has bought into the pull catches motes from further out and
		// reels them harder — the mass economy paying to feed itself faster.
		float pull = run?.PullScale ?? 1.0f;

		if (distance <= AbsorbRadius * pull)
		{
			Absorb();
			return;
		}

		// The pull ramps with age rather than with distance alone: the mote gets
		// its orbit, then the world reels it in on a deadline.
		float grab = Mathf.Lerp(1.0f, FinalAccelerationFactor, Mathf.Clamp(age / GrabTime, 0f, 1f)) * pull;

		// Magnet skips the orbit entirely — that is the whole point of it.
		if (run != null && run.Magnetised)
			grab *= MagnetFactor;

		velocity += (toWorld / distance) * BaseAcceleration * grab * step;
		velocity *= Mathf.Max(1.0f - Drag * step, 0f);

		GlobalPosition += velocity * step;
		Rotation += step * 6.0f;
	}

	/// <summary>An external push, for anything that wants to nudge a mote off its path — a well, later a magnet field.</summary>
	public void Nudge(Vector2 acceleration)
	{
		velocity += acceleration;
	}

	/// <summary>
	/// Called by Greedy Dash. Skips the mote past its orbit phase and throws it
	/// at the world, rather than teleporting it — the pull has to stay visible.
	/// </summary>
	public void Yank()
	{
		if (absorbed || world == null || !IsInstanceValid(world))
			return;

		age = Mathf.Max(age, GrabTime);
		velocity = (world.GlobalPosition - GlobalPosition).Normalized() * YankSpeed;
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
