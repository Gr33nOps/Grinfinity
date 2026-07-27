using Godot;

public partial class Bullet : Area2D
{
	public Vector2 Direction { get; set; }
	private const float Speed = 700.0f;
	private PackedScene explosionScene;

	public override void _Ready()
	{
		explosionScene = GD.Load<PackedScene>("res://scenes/explosion.tscn");
		AddToGroup("bullets");
		CollisionLayer = 4;
		CollisionMask = 2;
		BodyEntered += OnBodyEntered;
		GetNode<Timer>("Timer").Timeout += OnTimerTimeout;
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
	}

	private void OnTimerTimeout()
	{
		QueueFree();
	}

	private void OnBodyEntered(Node body)
	{
		if (!body.IsInGroup("enemies"))
			return;

		var gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.PlayKillSound();

		body.QueueFree();
		QueueFree();

		if (explosionScene == null)
			return;

		var explosion = explosionScene.Instantiate<CpuParticles2D>();
		explosion.GlobalPosition = GlobalPosition;
		explosion.Emitting = true;
		explosion.Lifetime = (float)GD.RandRange(0.5, 0.7);

		var parent = GetTree().GetFirstNodeInGroup("game_manager") ?? GetTree().CurrentScene;
		parent?.AddChild(explosion);

		var cleanupTimer = new Timer
		{
			WaitTime = explosion.Lifetime + 0.1f,
			OneShot = true,
			Autostart = true
		};
		explosion.AddChild(cleanupTimer);
		cleanupTimer.Timeout += () => explosion.QueueFree();
	}
}
