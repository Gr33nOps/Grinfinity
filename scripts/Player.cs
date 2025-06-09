// Player.cs
using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private const float SPEED = 200.0f;
	private bool isReloading = false;
	private Node2D shootyPart;
	
	public override void _Ready()
	{
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		shootyPart = GetNode<Node2D>("shootyPart");
	}
	
	public override void _PhysicsProcess(double delta)
	{
		LookAt(GetGlobalMousePosition());
		
		Velocity = new Vector2(
			Input.GetAxis("left", "right") * SPEED,
			Input.GetAxis("up", "down") * SPEED
		);
		
		if (Input.IsActionJustPressed("shoot"))
		{
			var bullet = bulletScene.Instantiate<Bullet>();
			bullet.GlobalPosition = shootyPart.GlobalPosition;
			bullet.Direction = (GetGlobalMousePosition() - GlobalPosition).Normalized();
			GetNode("/root/game").AddChild(bullet);
		}
		
		Velocity = GetRealVelocity().Lerp(Velocity, 0.1f);
		MoveAndSlide();
		ClampToViewport();
		
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision.GetCollider() is Node colliderNode && colliderNode.IsInGroup("enemies") && !isReloading)
			{
				isReloading = true;
				var gameNode = GetNode<Game>("/root/game");
				gameNode.TriggerGameOver();
			}
		}
	}
	
	private void ClampToViewport()
	{
		var viewportSize = GetViewportRect().Size;
		float margin = 25f;
		GlobalPosition = GlobalPosition.Clamp(
			new Vector2(margin, margin),
			viewportSize - new Vector2(margin, margin)
		);
	}
	
	public void SetReloading(bool value)
	{
		isReloading = value;
	}
}
