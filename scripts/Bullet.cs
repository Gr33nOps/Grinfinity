using Godot;

public partial class Bullet : Area2D
{
	[Export] public float Speed { get; set; } = 950.0f;
	[Export] public int Damage { get; set; } = 1;
	[Export] public PackedScene ExplosionScene { get; set; }

	/// <summary>Points kept in the motion trail. More is longer and softer.</summary>
	[Export] public int TrailLength { get; set; } = 9;

	public Vector2 Direction { get; set; }

	private Line2D trail;
	private bool hasHit = false;

	public override void _Ready()
	{
		ExplosionScene ??= GD.Load<PackedScene>("res://scenes/explosion.tscn");
		BodyEntered += OnBodyEntered;

		trail = GetNodeOrNull<Line2D>("Trail");
		trail?.ClearPoints();

		var lifetime = GetNodeOrNull<Timer>("Timer");
		if (lifetime != null)
			lifetime.Timeout += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
		UpdateTrail();
	}

	// The trail is top_level, so its points live in global space and do not have
	// to be un-transformed out of the bullet every frame.
	private void UpdateTrail()
	{
		if (trail == null)
			return;

		trail.AddPoint(GlobalPosition);
		while (trail.GetPointCount() > TrailLength)
			trail.RemovePoint(0);
	}

	private void OnBodyEntered(Node2D body)
	{
		// Two enemies can overlap the bullet in the same frame; only the first counts.
		if (hasHit || body is not Enemy enemy)
			return;

		hasHit = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);

		// Captured before the hit, because a lethal one queues the body for free.
		Enemy.Remains remains = enemy.GetRemains();

		// Tanks survive several hits, so the kill only scores when it lands.
		if (enemy.TakeDamage(Damage, Direction))
		{
			GameManager.Of(this)?.RegisterKill(remains, GlobalPosition);
			SpawnBurst(remains.BurstAmount, remains.BurstScale, remains.BurstColor, 1.0f);
		}
		else
		{
			// A chip hit gets a small pale spark instead of a full death burst.
			SpawnBurst(14, 0.45f, new Color(1.0f, 0.95f, 0.8f), 0.55f);
		}

		QueueFree();
	}

	private void SpawnBurst(int amount, float scale, Color color, float lifetimeScale)
	{
		if (ExplosionScene == null)
			return;

		var burst = ExplosionScene.Instantiate<CpuParticles2D>();
		burst.GlobalPosition = GlobalPosition;
		burst.Amount = Mathf.Max(amount, 1);
		// Node scale carries the particle velocities with it, so one burst scene
		// covers a swarmer pop and a tank detonation.
		burst.Scale = new Vector2(scale, scale);
		burst.Color = color;
		burst.Lifetime = (float)GD.RandRange(0.5, 0.7) * lifetimeScale;
		burst.Emitting = true;
		// explosion.tscn is one_shot, so Finished fires once the burst is done.
		burst.Finished += burst.QueueFree;
		GameManager.Spawn(this, burst);
	}
}
