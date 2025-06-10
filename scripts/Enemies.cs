using Godot;

public partial class Enemies : CharacterBody2D
{
	private Node2D player;
	private float speed; 
	
	public override void _Ready()
	{
		AddToGroup("enemies");
		player = GetNode<Node2D>("/root/game/player");
	}
	
	// Method to set speed from spawner
	public void SetSpeed(float newSpeed)
	{
		speed = newSpeed;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (player == null) return;
		
		Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
		Velocity = direction * speed; // Use variable speed instead of const
		LookAt(player.GlobalPosition);
		MoveAndSlide();
	}
}
