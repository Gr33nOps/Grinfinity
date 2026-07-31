using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float MoveSpeed { get; set; } = 200.0f;
	[Export] public float MoveSmoothing { get; set; } = 12.0f;
	[Export] public float ScreenBorder { get; set; } = 50.0f;
	[Export] public PackedScene BulletScene { get; set; }
	[Export] public PackedScene DeathEffectScene { get; set; }

	[ExportGroup("Abilities")]
	[Export] public float DashSpeed { get; set; } = 600.0f;
	[Export] public float DashDuration { get; set; } = 0.2f;
	[Export] public float DashCooldown { get; set; } = 2.0f;
	[Export] public float NormalFireRate { get; set; } = 0.3f;
	[Export] public float RapidFireRate { get; set; } = 0.08f;
	[Export] public float RapidFireDuration { get; set; } = 3.0f;
	[Export] public float RapidFireCooldown { get; set; } = 5.0f;

	// How far in front of the player the gamepad aim point sits.
	private const float GamepadAimDistance = 400.0f;
	private const float StickDeadzoneSq = 0.0625f;

	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private AudioStreamPlayer2D shootSound;
	private Area2D hitBox;
	private PlayerAbilities abilities;
	private Vector2 lastMousePosition;
	private Vector2 gamepadAimDirection = Vector2.Right;
	private bool usingGamepadAim = false;
	private bool isDead = false;

	/// <summary>World-space point the player is currently aiming at.</summary>
	public Vector2 AimPosition { get; private set; }

	public override void _Ready()
	{
		BulletScene ??= GD.Load<PackedScene>("res://scenes/bullet.tscn");
		DeathEffectScene ??= GD.Load<PackedScene>("res://scenes/explosion.tscn");

		shootyPart = GetNode<Node2D>("shootyPart");
		playerSprite = FindPlayerSprite();
		shootSound = GetNodeOrNull<AudioStreamPlayer2D>("ShootSound");
		abilities = new PlayerAbilities(this);

		hitBox = GetNodeOrNull<Area2D>("HitBox");
		if (hitBox != null)
			hitBox.BodyEntered += OnHitBoxBodyEntered;
		else
			GD.PushError("Player: HitBox Area2D is missing, contact damage will not work.");

		lastMousePosition = GetGlobalMousePosition();
		AimPosition = lastMousePosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDead)
			return;

		AimPosition = ResolveAimPosition();
		abilities.Update(delta);
		UpdatePlayer(AimPosition, delta);
		MoveAndSlide();
		StayOnScreen();
	}

	/// <summary>
	/// Aims with the right stick when it is being used, otherwise falls back to the
	/// mouse. Moving the mouse hands control back to it.
	/// </summary>
	private Vector2 ResolveAimPosition()
	{
		Vector2 stick = new Vector2(
			Input.GetAxis("aim_left", "aim_right"),
			Input.GetAxis("aim_up", "aim_down")
		);

		if (stick.LengthSquared() > StickDeadzoneSq)
		{
			gamepadAimDirection = stick.Normalized();
			usingGamepadAim = true;
		}

		Vector2 mousePosition = GetGlobalMousePosition();
		if (!mousePosition.IsEqualApprox(lastMousePosition))
		{
			lastMousePosition = mousePosition;
			usingGamepadAim = false;
		}

		return usingGamepadAim
			? GlobalPosition + gamepadAimDirection * GamepadAimDistance
			: mousePosition;
	}

	private void UpdatePlayer(Vector2 aimPosition, double delta)
	{
		if (playerSprite != null)
		{
			playerSprite.FlipV = aimPosition.X < GlobalPosition.X;
		}
		LookAt(aimPosition);
		HandleMovement(delta);
		abilities.HandleShooting(aimPosition);
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

	private void OnHitBoxBodyEntered(Node2D body)
	{
		if (body.IsInGroup("enemies"))
			Die();
	}

	private void Die()
	{
		if (isDead)
			return;

		isDead = true;
		Velocity = Vector2.Zero;
		SpawnDeathEffect();

		var gameManager = GetTree().GetFirstNodeInGroup("game_manager") as GameManager;
		gameManager?.TriggerGameOver();
	}

	private void SpawnDeathEffect()
	{
		if (DeathEffectScene == null)
			return;

		var effect = DeathEffectScene.Instantiate<CpuParticles2D>();
		effect.GlobalPosition = GlobalPosition;
		effect.Amount = 120;
		effect.Lifetime = 0.8f;
		effect.Modulate = new Color(1f, 0.85f, 0.4f);
		effect.Emitting = true;
		GameManager.Spawn(this,effect);

		if (playerSprite != null)
			playerSprite.Visible = false;
	}

	private void StayOnScreen()
	{
		var screenSize = GetViewportRect().Size;
		GlobalPosition = GlobalPosition.Clamp(
			new Vector2(ScreenBorder, ScreenBorder),
			screenSize - new Vector2(ScreenBorder, ScreenBorder)
		);
	}

	private Sprite2D FindPlayerSprite()
	{
		if (HasNode("Sprite2D"))
			return GetNode<Sprite2D>("Sprite2D");

		foreach (Node child in GetChildren())
		{
			if (child is Sprite2D sprite)
				return sprite;
		}

		return null;
	}

	public void ShootBullet(Vector2 aimPosition)
	{
		var bullet = BulletScene.Instantiate<Bullet>();
		bullet.GlobalPosition = shootyPart.GlobalPosition;
		bullet.Direction = (aimPosition - GlobalPosition).Normalized();
		GameManager.Spawn(this,bullet);
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
}
