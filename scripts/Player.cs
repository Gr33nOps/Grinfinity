using Godot;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private float speed = 200.0f;
	private bool isReloading = false;
	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private AudioStreamPlayer2D shootSound;
	
	public override void _Ready()
	{
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		shootyPart = GetNode<Node2D>("shootyPart");
		playerSprite = FindPlayerSprite();
		shootSound = GetNode<AudioStreamPlayer2D>("ShootSound");
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 mousePosition = GetGlobalMousePosition();
		
		HandleSpriteFlip(mousePosition);
		LookAt(mousePosition);
		HandleMovement();
		HandleShooting(mousePosition);
		CheckCollisions();
		
		MoveAndSlide();
		StayInViewport();
	}
	
	private Sprite2D FindPlayerSprite()
	{
		if (HasNode("Sprite2D"))
			return GetNode<Sprite2D>("Sprite2D");
		
		if (HasNode("sprite"))
			return GetNode<Sprite2D>("sprite");
		
		foreach (Node child in GetChildren())
		{
			if (child is Sprite2D sprite2D)
				return sprite2D;
		}
		
		return null;
	}
	
	private void HandleSpriteFlip(Vector2 mousePosition)
	{
		if (playerSprite != null)
		{
			playerSprite.FlipV = mousePosition.X < GlobalPosition.X;
		}
	}
	
	private void HandleMovement()
	{
		Velocity = new Vector2(
			Input.GetAxis("left", "right") * speed,
			Input.GetAxis("up", "down") * speed
		);
		
		Velocity = GetRealVelocity().Lerp(Velocity, 0.1f);
	}
	
	private void HandleShooting(Vector2 mousePosition)
	{
		if (Input.IsActionJustPressed("shoot"))
		{
			var bullet = bulletScene.Instantiate<Bullet>();
			bullet.GlobalPosition = shootyPart.GlobalPosition;
			bullet.Direction = (mousePosition - GlobalPosition).Normalized();
			GetNode("/root/game").AddChild(bullet);
			
			if (shootSound != null)
			{
				shootSound.Play();
			}
		}
	}
	
	private void CheckCollisions()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision.GetCollider() is Node colliderNode && 
				colliderNode.IsInGroup("enemies") && 
				!isReloading)
			{
				isReloading = true;
				var gameManager = GetNode<GameManager>("/root/game");
				gameManager.TriggerGameOver();
			}
		}
	}
	
	private void StayInViewport()
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
