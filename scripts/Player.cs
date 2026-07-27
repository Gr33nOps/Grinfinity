using Godot;

public partial class Player : CharacterBody2D
{
	private PackedScene bulletScene;
	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private AudioStreamPlayer2D shootSound;
	private PlayerAbilities abilities;
	private const float MoveSpeed = 200.0f;
	private const float MoveSmoothing = 12.0f;
	private const float Border = 50f;
	private const float CollisionRadius = 100f;
	private bool isDead = false;

	public override void _Ready()
	{
		bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
		shootyPart = GetNode<Node2D>("shootyPart");
		playerSprite = FindPlayerSprite();
		shootSound = GetNode<AudioStreamPlayer2D>("ShootSound");
		abilities = new PlayerAbilities(this);
		CollisionLayer = 1;
		CollisionMask = 2;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDead)
			return;

		Vector2 mousePos = GetGlobalMousePosition();
		abilities.Update(delta);
		UpdatePlayer(mousePos, delta);
		MoveAndSlide();
		CheckSpriteCollisions();
		StayOnScreen();
	}

	private void UpdatePlayer(Vector2 mousePos, double delta)
	{
		if (playerSprite != null)
		{
			playerSprite.FlipV = mousePos.X < GlobalPosition.X;
		}
		LookAt(mousePos);
		HandleMovement(delta);
		abilities.HandleShooting(mousePos);
	}

	private void HandleMovement(double delta)
	{
		if (abilities.IsDashing())
		{
			Velocity = abilities.GetDashVelocity();
			return;
		}

		Vector2 targetVelocity = new Vector2(
			Input.GetAxis("left", "right"),
			Input.GetAxis("up", "down")
		) * MoveSpeed;

		float t = 1f - Mathf.Exp(-MoveSmoothing * (float)delta);
		Velocity = Velocity.Lerp(targetVelocity, t);
	}

	private void CheckSpriteCollisions()
	{
		var enemies = GetTree().GetNodesInGroup("enemies");

		foreach (Node enemy in enemies)
		{
			if (enemy is not Enemy enemyNode)
				continue;

			float distance = GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
			if (distance < CollisionRadius)
			{
				Die();
				break;
			}
		}
	}

	private void Die()
	{
		if (isDead)
			return;

		isDead = true;
		var gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.TriggerGameOver();
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

		var gameRoot = GetTree().GetFirstNodeInGroup("game_manager") ?? GetTree().CurrentScene;
		gameRoot?.AddChild(bullet);

		PlayShootSound();
	}

	public void PlayShootSound(bool isRapidFire = false)
	{
		if (shootSound == null)
			return;

		shootSound.PitchScale = isRapidFire ? 1.2f : 1.0f;
		shootSound.Play();
	}

	public void CreateDashEffect()
	{
		if (playerSprite == null)
			return;

		var tween = CreateTween();
		tween.TweenProperty(playerSprite, "modulate:a", 0.5f, 0.1f);
		tween.TweenProperty(playerSprite, "modulate:a", 1.0f, 0.1f);
	}

	public void CreateRapidFireEffect()
	{
		if (playerSprite == null)
			return;

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
