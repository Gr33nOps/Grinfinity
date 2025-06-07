using Godot;
using System;

public partial class Enemies : CharacterBody2D
{
	private Node2D  player;
	private const float SPEED = 100.0f;

	public override void _Ready(){
		AddToGroup("enemies");
		player = GetNode<Node2D>("/root/game/player");
	}
	
	public override void _PhysicsProcess(double delta){
		Velocity = (player.GlobalPosition - GlobalPosition).Normalized() * SPEED;
		LookAt(player.GlobalPosition);
		MoveAndSlide();
	}
}
