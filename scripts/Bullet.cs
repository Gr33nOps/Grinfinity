using Godot;

public partial class Bullet : Area2D
{
	public Vector2 Direction { get; set; }
	private float speed = 10.0f;
	private PackedScene explosionScene;
	
	public override void _Ready()
	{
		explosionScene = GD.Load<PackedScene>("res://scenes/explosion.tscn");
		AddToGroup("bullets");
		BodyEntered += OnBodyEntered;
		GetNode<Timer>("Timer").Timeout += OnTimerTimeout;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * speed;
	}
	
	private void OnTimerTimeout()
	{
		QueueFree();
	}
	
	private void OnBodyEntered(Node body)
	{
		if (body.IsInGroup("enemies"))
		{
			GetNode<GameManager>("/root/game").PlayKillSound();
			body.QueueFree();
			QueueFree();
			
			var explosion = explosionScene.Instantiate<CpuParticles2D>();
			explosion.GlobalPosition = GlobalPosition;
			explosion.Emitting = true;
			explosion.Lifetime = GD.RandRange(0.5f, 0.7f);
			GetNode("/root/game").AddChild(explosion);
		}
	}
}
