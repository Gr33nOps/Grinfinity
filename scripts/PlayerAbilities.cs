using Godot;

/// <summary>
/// Dash and rapid-fire state machine. Tuning lives on <see cref="Player"/> so it
/// stays editable from the inspector.
/// </summary>
public class PlayerAbilities
{
	private readonly Player player;

	private float dashTimer = 0.0f;
	private float dashTimeLeft = 0.0f;
	private bool isDashing = false;
	private Vector2 dashDirection = Vector2.Zero;

	private float rapidFireTimer = 0.0f;
	private float rapidFireTimeLeft = 0.0f;
	private float shootTimer = 0.0f;
	private bool isRapidFiring = false;
	private bool canShoot = true;

	public PlayerAbilities(Player playerRef)
	{
		player = playerRef;
	}

	public void Update(double delta)
	{
		UpdateTimers(delta);
		HandleInput();
	}

	private void UpdateTimers(double delta)
	{
		float deltaF = (float)delta;

		if (dashTimer > 0)
			dashTimer -= deltaF;

		if (rapidFireTimer > 0)
			rapidFireTimer -= deltaF;

		if (shootTimer > 0)
		{
			shootTimer -= deltaF;
			if (shootTimer <= 0)
				canShoot = true;
		}

		if (isDashing)
		{
			dashTimeLeft -= deltaF;
			if (dashTimeLeft <= 0)
				isDashing = false;
		}

		if (isRapidFiring)
		{
			rapidFireTimeLeft -= deltaF;
			if (rapidFireTimeLeft <= 0)
			{
				isRapidFiring = false;
				rapidFireTimer = player.RapidFireCooldown;
			}
		}
	}

	private void HandleInput()
	{
		if (Input.IsActionJustPressed("dash") && dashTimer <= 0 && !isDashing)
			StartDash();

		if (Input.IsActionJustPressed("rapid_fire") && rapidFireTimer <= 0 && !isRapidFiring)
			StartRapidFire();
	}

	private void StartDash()
	{
		Vector2 inputDirection = new Vector2(
			Input.GetAxis("left", "right"),
			Input.GetAxis("up", "down")
		).Normalized();

		if (inputDirection == Vector2.Zero)
		{
			inputDirection = (player.AimPosition - player.GlobalPosition).Normalized();
		}

		dashDirection = inputDirection;
		isDashing = true;
		dashTimeLeft = player.DashDuration;
		dashTimer = player.DashCooldown;
		player.CreateDashEffect();
	}

	private void StartRapidFire()
	{
		isRapidFiring = true;
		rapidFireTimeLeft = player.RapidFireDuration;
		player.CreateRapidFireEffect();
	}

	public void HandleShooting(Vector2 aimPosition)
	{
		if (!Input.IsActionPressed("shoot") || !canShoot)
			return;

		player.ShootBullet(aimPosition);
		player.PlayShootSound(isRapidFiring);

		shootTimer = isRapidFiring ? player.RapidFireRate : player.NormalFireRate;
		canShoot = false;
	}

	public bool IsDashing()
	{
		return isDashing;
	}

	public Vector2 GetDashVelocity()
	{
		return dashDirection * player.DashSpeed;
	}

	public bool IsRapidFiring()
	{
		return isRapidFiring;
	}

	public float GetDashCooldownPercent()
	{
		return Mathf.Clamp(1.0f - (dashTimer / player.DashCooldown), 0.0f, 1.0f);
	}

	public float GetRapidFireCooldownPercent()
	{
		return Mathf.Clamp(1.0f - (rapidFireTimer / player.RapidFireCooldown), 0.0f, 1.0f);
	}
}
