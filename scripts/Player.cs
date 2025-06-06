using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private const float SPEED = 200.0f;
	private Node2D shootyPart;
	
	public override void _Ready(){
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		shootyPart = GetNode<Node2D>("shootyPart");
	}
	
	public override void _PhysicsProcess(double delta)
	{
		LookAt(GetGlobalMousePosition());
		
		Velocity  = new Vector2(
			Input.GetAxis("left", "right") * SPEED,
			Input.GetAxis("up", "down") * SPEED
		);
		
		if(Input.IsActionJustPressed("shoot")){
			var bullet = bulletScene.Instantiate<Bullet>();
			bullet.GlobalPosition = shootyPart.GlobalPosition;
			bullet.Direction = (GetGlobalMousePosition() - GlobalPosition).Normalized();
			GetNode("/root/game").AddChild(bullet);
		}
		
		Velocity = GetRealVelocity().Lerp(Velocity, 0.1f);
		MoveAndSlide();
	}
}
