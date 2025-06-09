using Godot;
using System;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private const float SPEED = 200.0f;
	private bool isReloading = false;
	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private Vector2 lastMousePosition;
	
	public override void _Ready()
	{
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		shootyPart = GetNode<Node2D>("shootyPart");
		
		// Get the player sprite - adjust the path as needed based on your scene structure
		if (HasNode("Sprite2D"))
		{
			playerSprite = GetNode<Sprite2D>("Sprite2D");
		}
		else if (HasNode("sprite"))
		{
			playerSprite = GetNode<Sprite2D>("sprite");
		}
		else
		{
			// Search through children to find the Sprite2D
			foreach (Node child in GetChildren())
			{
				if (child is Sprite2D sprite2D)
				{
					playerSprite = sprite2D;
					break;
				}
			}
		}
		
		lastMousePosition = GetGlobalMousePosition();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 currentMousePosition = GetGlobalMousePosition();
		
		// Flip the player sprite based on mouse direction
		if (playerSprite != null)
		{
			// If mouse moved to the left of the player, flip horizontally
			if (currentMousePosition.X < GlobalPosition.X)
			{
				playerSprite.FlipV = true;
			}
			// If mouse moved to the right of the player, don't flip
			else if (currentMousePosition.X > GlobalPosition.X)
			{
				playerSprite.FlipV = false;
			}
		}
		
		LookAt(currentMousePosition);
		
		Velocity = new Vector2(
			Input.GetAxis("left", "right") * SPEED,
			Input.GetAxis("up", "down") * SPEED
		);
		
		if (Input.IsActionJustPressed("shoot"))
		{
			var bullet = bulletScene.Instantiate<Bullet>();
			bullet.GlobalPosition = shootyPart.GlobalPosition;
			bullet.Direction = (currentMousePosition - GlobalPosition).Normalized();
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
		
		lastMousePosition = currentMousePosition;
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
