using Godot;

public partial class Enemy : CharacterBody2D
{
	// How far outside the viewport an enemy may drift before it is culled.
	private const float CullMargin = 600.0f;

	private Node2D player;
	private float speed = 100.0f;

	public override void _Ready()
	{
		var gameManager = GetTree().GetFirstNodeInGroup("game_manager");
		player = gameManager?.GetNodeOrNull<Node2D>("player");
	}

	public void SetSpeed(float newSpeed)
	{
		speed = newSpeed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (player == null || !IsInstanceValid(player))
			return;

		// Pick up the spawner's ramp so living enemies speed up too, not just new ones.
		speed = Mathf.Max(speed, EnemySpawner.CurrentSpeed);

		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * speed;
		LookAt(player.GlobalPosition);
		MoveAndSlide();

		CullIfLost();
	}

	/// <summary>Safety net so an enemy that somehow drifts away cannot leak forever.</summary>
	private void CullIfLost()
	{
		var bounds = GetViewportRect().Size;
		if (GlobalPosition.X < -CullMargin || GlobalPosition.X > bounds.X + CullMargin ||
			GlobalPosition.Y < -CullMargin || GlobalPosition.Y > bounds.Y + CullMargin)
		{
			QueueFree();
		}
	}
}
