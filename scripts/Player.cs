using Godot;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private AudioStreamPlayer2D shootSound;
	private PlayerAbilities abilities;
	private const float MoveSpeed = 200.0f;
	private const float Border = 50f;
	private const float CollisionRadius = 100f; // Adjust this based on your sprite sizes
	private bool isDead = false;

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
		if (isDead)
		{
			return;
		}

		Vector2 mousePos = GetGlobalMousePosition();
		abilities.Update(delta);
		UpdatePlayer(mousePos);
		CheckSpriteCollisions(); // Using distance-based collision detection
		MoveAndSlide();
		StayOnScreen();
	}

	private void UpdatePlayer(Vector2 mousePos)
	{
		if (playerSprite != null)
		{
			playerSprite.FlipV = mousePos.X < GlobalPosition.X;
		}
		LookAt(mousePos);
		HandleMovement();
		abilities.HandleShooting(mousePos);
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
			Velocity = GetRealVelocity().Lerp(moveDirection * MoveSpeed, 0.1f);
		}
	}

	private void CheckSpriteCollisions()
	{
		var enemies = GetTree().GetNodesInGroup("enemies");
		
		foreach (Node enemy in enemies)
		{
			if (enemy is Node2D enemyNode2D)
			{
				float distance = GlobalPosition.DistanceTo(enemyNode2D.GlobalPosition);
				
				if (distance < CollisionRadius)
				{
					Die();
					break;
				}
			}
		}
	}

	private void Die()
	{
		if (isDead)
		{
			return;
		}
		isDead = true;
		GetNode<GameManager>("/root/game").TriggerGameOver();
	}

	private void StayOnScreen()
	{
		var screenSize = GetViewportRect().Size;
		GlobalPosition = GlobalPosition.Clamp(
			new Vector2(Border, Border),
			screenSize - new Vector2(Border, Border)
		);
	}

	private Sprite2D FindPlayerSprite()
	{
		if (HasNode("Sprite2D"))
		{
			return GetNode<Sprite2D>("Sprite2D");
		}
		if (HasNode("sprite"))
		{
			return GetNode<Sprite2D>("sprite");
		}
		foreach (Node child in GetChildren())
		{
			if (child is Sprite2D sprite)
			{
				return sprite;
			}
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
			if (isRapidFire)
			{
				shootSound.PitchScale = 1.2f;
			}
			else
			{
				shootSound.PitchScale = 1.0f;
			}
			shootSound.Play();
		}
	}

	public void CreateDashEffect()
	{
		if (playerSprite == null)
		{
			return;
		}
		var tween = CreateTween();
		tween.TweenProperty(playerSprite, "modulate:a", 0.5f, 0.1f);
		tween.TweenProperty(playerSprite, "modulate:a", 1.0f, 0.1f);
	}

	public void CreateRapidFireEffect()
	{
		if (playerSprite == null)
		{
			return;
		}
		var tween = CreateTween();
		tween.TweenProperty(playerSprite, "modulate", Colors.Orange, 0.2f);
		tween.TweenProperty(playerSprite, "modulate", Colors.White, 0.2f);
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
