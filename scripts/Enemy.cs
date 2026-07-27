using Godot;

public partial class Enemy : CharacterBody2D
{
	private Node2D player;
	private float speed = 100.0f;

	public override void _Ready()
	{
		AddToGroup("enemies");
		CollisionLayer = 2;
		CollisionMask = 1;

		var gameManager = GetTree().GetFirstNodeInGroup("game_manager");
		if (gameManager != null)
		{
			player = gameManager.GetNodeOrNull<Node2D>("player");
		}
	}

	public void SetSpeed(float newSpeed)
	{
		speed = newSpeed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (player == null || !IsInstanceValid(player))
			return;

		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * speed;
		LookAt(player.GlobalPosition);
		MoveAndSlide();
	}
}
