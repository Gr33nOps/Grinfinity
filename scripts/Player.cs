using Godot;

public partial class Player : CharacterBody2D
{
	[Export] public float MoveSpeed { get; set; } = 235.0f;
	[Export] public float MoveSmoothing { get; set; } = 14.0f;
	[Export] public float ScreenBorder { get; set; } = 50.0f;
	[Export] public PackedScene BulletScene { get; set; }
	[Export] public PackedScene DeathEffectScene { get; set; }

	[ExportGroup("Abilities")]
	[Export] public float DashSpeed { get; set; } = 880.0f;
	[Export] public float DashDuration { get; set; } = 0.16f;
	[Export] public float DashCooldown { get; set; } = 1.4f;
	[Export] public float NormalFireRate { get; set; } = 0.22f;
	[Export] public float RapidFireRate { get; set; } = 0.07f;
	[Export] public float RapidFireDuration { get; set; } = 3.5f;
	[Export] public float RapidFireCooldown { get; set; } = 7.0f;

	[ExportGroup("Feel")]
	/// <summary>Screen shake added by a dash. Small — it happens constantly.</summary>
	[Export] public float DashTrauma { get; set; } = 0.14f;
	[Export] public float MuzzleFlashTime { get; set; } = 0.055f;

	// How far in front of the player the gamepad aim point sits.
	private const float GamepadAimDistance = 400.0f;
	private const float StickDeadzoneSq = 0.0625f;

	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private Node2D muzzleFlash;
	private Tween muzzleTween;
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

		muzzleFlash = shootyPart.GetNodeOrNull<Node2D>("MuzzleFlash");
		if (muzzleFlash != null)
			muzzleFlash.Visible = false;

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

		GameManager.Of(this)?.OnPlayerKilled();
	}

	private void SpawnDeathEffect()
	{
		if (DeathEffectScene == null)
			return;

		var effect = DeathEffectScene.Instantiate<CpuParticles2D>();
		effect.GlobalPosition = GlobalPosition;
		effect.Amount = 180;
		effect.Scale = new Vector2(2.2f, 2.2f);
		effect.Lifetime = 0.9f;
		effect.Color = new Color(1f, 0.82f, 0.35f);
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
		FlashMuzzle();
	}

	/// <summary>
	/// One frame of light at the barrel. Randomised scale and roll so a held
	/// trigger does not look like a strobing decal.
	/// </summary>
	private void FlashMuzzle()
	{
		if (muzzleFlash == null)
			return;

		muzzleTween?.Kill();
		muzzleFlash.Visible = true;
		muzzleFlash.Rotation = (float)GD.RandRange(-0.5, 0.5);
		muzzleFlash.Scale = Vector2.One * (float)GD.RandRange(0.85, 1.2);
		muzzleFlash.Modulate = new Color(1f, 1f, 1f, 1f);

		muzzleTween = CreateTween();
		muzzleTween.TweenProperty(muzzleFlash, "modulate:a", 0.0f, MuzzleFlashTime);
		muzzleTween.TweenCallback(Callable.From(() => muzzleFlash.Visible = false));
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
		GameManager.Of(this)?.Shake(DashTrauma);

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
