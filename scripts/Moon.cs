using Godot;

/// <summary>
/// A moon earned by carrying mass. It orbits the world, fires on its own
/// cadence, and body-blocks exactly one hit before it breaks away.
///
/// Moons are what make going heavy something a player *wants* rather than
/// merely something that scores. They are also the most legible possible
/// statement of "I am a planet".
/// </summary>
public partial class Moon : Area2D
{
	[Export] public float OrbitRadius { get; set; } = 132.0f;
	[Export] public float OrbitSpeed { get; set; } = 1.5f;
	[Export] public float FireInterval { get; set; } = 1.15f;
	/// <summary>Bodies further away than this are not worth a shot.</summary>
	[Export] public float TargetRange { get; set; } = 620.0f;
	[Export] public PackedScene BulletScene { get; set; }

	private Node2D world;
	private GameManager manager;
	private float angle;
	private float fireTimer;
	private bool broken;

	/// <summary>Where in the orbit this moon sits, so several space themselves out.</summary>
	public float PhaseOffset { get; set; }

	public override void _Ready()
	{
		BulletScene ??= GD.Load<PackedScene>("res://scenes/bullet.tscn");

		manager = GameManager.Of(this);
		world = manager?.GetNodeOrNull<Node2D>("player");

		angle = PhaseOffset;
		// Staggered, so a full set of moons does not volley in lockstep.
		fireTimer = FireInterval * (PhaseOffset / Mathf.Tau);

		BodyEntered += OnBodyEntered;
		SpawnPop();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (world == null || !IsInstanceValid(world))
		{
			QueueFree();
			return;
		}

		float step = (float)delta;
		angle += OrbitSpeed * step;
		GlobalPosition = world.GlobalPosition + Vector2.FromAngle(angle) * OrbitRadius;
		Rotation = angle + Mathf.Pi * 0.5f;

		fireTimer -= step;
		if (fireTimer <= 0f)
		{
			fireTimer = FireInterval;
			Fire();
		}
	}

	private void Fire()
	{
		Node2D target = FindNearestBody();
		if (target == null || BulletScene == null)
			return;

		var bullet = BulletScene.Instantiate<Bullet>();
		bullet.GlobalPosition = GlobalPosition;
		bullet.Direction = (target.GlobalPosition - GlobalPosition).Normalized();
		GameManager.Spawn(this, bullet);
	}

	private Node2D FindNearestBody()
	{
		Node2D nearest = null;
		float nearestDistance = TargetRange * TargetRange;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Node2D body)
				continue;

			float distance = GlobalPosition.DistanceSquaredTo(body.GlobalPosition);
			if (distance >= nearestDistance)
				continue;

			nearestDistance = distance;
			nearest = body;
		}

		return nearest;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (broken || body is not Enemy enemy)
			return;

		broken = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);

		// The block is the moon's whole job: the body dies, but it does not
		// score, and the mass that earned the moon goes with it.
		enemy.TakeDamage(9999, (enemy.GlobalPosition - GlobalPosition).Normalized());
		manager?.OnMoonBlocked(GlobalPosition);

		QueueFree();
	}

	/// <summary>Scales up on arrival so gaining a moon is felt, not just noticed.</summary>
	private void SpawnPop()
	{
		Vector2 target = Scale;
		Scale = target * 0.1f;
		CreateTween()
			.TweenProperty(this, "scale", target, 0.28f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
	}
}
