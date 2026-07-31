using Godot;

public partial class Bullet : Area2D
{
	[Export] public float Speed { get; set; } = 700.0f;
	[Export] public PackedScene ExplosionScene { get; set; }

	public Vector2 Direction { get; set; }

	private bool hasHit = false;

	public override void _Ready()
	{
		ExplosionScene ??= GD.Load<PackedScene>("res://scenes/explosion.tscn");
		BodyEntered += OnBodyEntered;

		var lifetime = GetNodeOrNull<Timer>("Timer");
		if (lifetime != null)
			lifetime.Timeout += QueueFree;
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
	}

	private void OnBodyEntered(Node2D body)
	{
		// Two enemies can overlap the bullet in the same frame; only the first counts.
		if (hasHit || !body.IsInGroup("enemies"))
			return;

		hasHit = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);

		var gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.PlayKillSound();

		SpawnExplosion();
		body.QueueFree();
		QueueFree();
	}

	private void SpawnExplosion()
	{
		if (ExplosionScene == null)
			return;

		var explosion = ExplosionScene.Instantiate<CpuParticles2D>();
		explosion.GlobalPosition = GlobalPosition;
		explosion.Lifetime = (float)GD.RandRange(0.5, 0.7);
		explosion.Emitting = true;
		// explosion.tscn is one_shot, so Finished fires once the burst is done.
		explosion.Finished += explosion.QueueFree;
		GameManager.Spawn(this, explosion);
	}
}
