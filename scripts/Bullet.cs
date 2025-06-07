using Godot;
using System;

public partial class Bullet : Area2D
{
	public Vector2 Direction { get; set; }
	private const float SPEED = 10.0f;
	
	public override void _Ready(){
		BodyEntered += OnBodyEntered;
		GetNode<Timer>("Timer").Timeout += OnTimerTimeout;
	}
	
	public override void _PhysicsProcess(double delta){
		GlobalPosition += Direction * SPEED;
	}
	
	private void OnTimerTimeout(){
		QueueFree();
	}
	
	private void OnBodyEntered(Node body){
		if (body.IsInGroup("enemies")){
			body.QueueFree();
			QueueFree();
		}
	}
}
