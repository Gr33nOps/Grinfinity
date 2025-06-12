// Player.cs - Main player movement and basic shooting
using Godot;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private AudioStreamPlayer2D shootSound;
	
	private float moveSpeed = 200.0f;
	private bool isDead = false;
	
	// Player abilities - handled by separate script
	private PlayerAbilities abilities;
	
	public override void _Ready()
	{
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		
		shootyPart = GetNode<Node2D>("shootyPart");
		playerSprite = FindPlayerSprite();
		shootSound = GetNode<AudioStreamPlayer2D>("ShootSound");
		
		abilities = new PlayerAbilities(this);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return;
		
		Vector2 mousePos = GetGlobalMousePosition();
		
		abilities.Update(delta);
		
		FlipSpriteTowardsMouse(mousePos);
		LookAtMouse(mousePos);
		HandleMovement();
		HandleShooting(mousePos);
		CheckForEnemyHit();
		
		MoveAndSlide();
		StayOnScreen();
	}
	
	private void FlipSpriteTowardsMouse(Vector2 mousePos)
	{
		if (playerSprite != null)
		{
			// Flip sprite based on mouse position
			playerSprite.FlipV = mousePos.X < GlobalPosition.X;
		}
	}
	
	private void LookAtMouse(Vector2 mousePos)
	{
		LookAt(mousePos);
	}
	
	private void HandleMovement()
	{
		if (abilities.IsDashing())
		{
			Velocity = abilities.GetDashVelocity();
		}
		else
		{
			Vector2 moveDirection = new Vector2(
				Input.GetAxis("left", "right"),
				Input.GetAxis("up", "down")
			);
			
			Velocity = moveDirection * moveSpeed;
			
			Velocity = GetRealVelocity().Lerp(Velocity, 0.1f);
		}
	}
	
	private void HandleShooting(Vector2 mousePos)
	{
		abilities.HandleShooting(mousePos);
	}
	
	private void CheckForEnemyHit()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision.GetCollider() is Node enemy && enemy.IsInGroup("enemies"))
			{
				Die();
				break;
			}
		}
	}
	
	private void Die()
	{
		if (isDead) return;
		
		isDead = true;
		var gameManager = GetNode<GameManager>("/root/game");
		gameManager.TriggerGameOver();
	}
	
	private void StayOnScreen()
	{
		var screenSize = GetViewportRect().Size;
		float border = 25f;
		
		GlobalPosition = GlobalPosition.Clamp(
			new Vector2(border, border),
			screenSize - new Vector2(border, border)
		);
	}
	
	private Sprite2D FindPlayerSprite()
	{
		if (HasNode("Sprite2D"))
			return GetNode<Sprite2D>("Sprite2D");
			
		if (HasNode("sprite"))
			return GetNode<Sprite2D>("sprite");
		
		foreach (Node child in GetChildren())
		{
			if (child is Sprite2D sprite)
				return sprite;
		}
		
		return null;
	}
	
	public void ShootBullet(Vector2 mousePos)
	{
		var bullet = bulletScene.Instantiate<Bullet>();
		bullet.GlobalPosition = shootyPart.GlobalPosition;
		bullet.Direction = (mousePos - GlobalPosition).Normalized();
		GetNode("/root/game").AddChild(bullet);
		
		PlayShootSound();
	}
	
	public void PlayShootSound(bool isRapidFire = false)
	{
		if (shootSound != null)
		{
			shootSound.PitchScale = isRapidFire ? 1.2f : 1.0f;
			shootSound.Play();
		}
	}
	
	public void CreateDashEffect()
	{
		if (playerSprite != null)
		{
			var tween = CreateTween();
			tween.TweenProperty(playerSprite, "modulate:a", 0.5f, 0.1f);
			tween.TweenProperty(playerSprite, "modulate:a", 1.0f, 0.1f);
		}
	}
	
	public void CreateRapidFireEffect()
	{
		if (playerSprite != null)
		{
			var tween = CreateTween();
			tween.TweenProperty(playerSprite, "modulate", Colors.Orange, 0.2f);
			tween.TweenProperty(playerSprite, "modulate", Colors.White, 0.2f);
		}
	}
	
	public void SetDead(bool dead)
	{
		isDead = dead;
	}
	
	public float GetDashCooldownPercent()
	{
		return abilities.GetDashCooldownPercent();
	}
	
	public float GetRapidFireCooldownPercent()
	{
		return abilities.GetRapidFireCooldownPercent();
	}
	
	public bool IsRapidFiring()
	{
		return abilities.IsRapidFiring();
	}
	
	public float GetRapidFireTimeLeft()
	{
		return abilities.GetRapidFireTimeLeft();
	}
}
