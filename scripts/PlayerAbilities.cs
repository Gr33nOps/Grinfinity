using Godot;

public class PlayerAbilities
{
	private Player player;
	
	private const float DashSpeed = 600.0f;
	private const float DashDuration = 0.2f;
	private const float DashCooldown = 2.0f;
	private const float NormalFireRate = 0.3f;
	private const float RapidFireRate = 0.08f;
	private const float RapidFireDuration = 3.0f;
	private const float RapidFireCooldown = 5.0f;
	
	private float dashTimer = 0.0f;
	private bool isDashing = false;
	private float dashTimeLeft = 0.0f;
	private Vector2 dashDirection = Vector2.Zero;
	
	private float rapidFireTimer = 0.0f;
	private float shootTimer = 0.0f;
	private bool isRapidFiring = false;
	private float rapidFireTimeLeft = 0.0f;
	private bool canShoot = true;
	
	public PlayerAbilities(Player playerRef) => player = playerRef;
	
	public void Update(double delta)
	{
		UpdateTimers(delta);
		HandleInput();
	}
	
	private void UpdateTimers(double delta)
	{
		float deltaF = (float)delta;
		
		if (dashTimer > 0) dashTimer -= deltaF;
		if (rapidFireTimer > 0) rapidFireTimer -= deltaF;
		if (shootTimer > 0)
		{
			shootTimer -= deltaF;
			if (shootTimer <= 0) canShoot = true;
		}
		
		if (isDashing)
		{
			dashTimeLeft -= deltaF;
			if (dashTimeLeft <= 0) isDashing = false;
		}
		
		if (isRapidFiring)
		{
			rapidFireTimeLeft -= deltaF;
			if (rapidFireTimeLeft <= 0)
			{
				isRapidFiring = false;
				rapidFireTimer = RapidFireCooldown;
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
			inputDirection = (player.GetGlobalMousePosition() - player.GlobalPosition).Normalized();
		
		dashDirection = inputDirection;
		isDashing = true;
		dashTimeLeft = DashDuration;
		dashTimer = DashCooldown;
		player.CreateDashEffect();
	}
	
	private void StartRapidFire()
	{
		isRapidFiring = true;
		rapidFireTimeLeft = RapidFireDuration;
		player.CreateRapidFireEffect();
	}
	
	public void HandleShooting(Vector2 mousePos)
	{
		if (Input.IsActionPressed("shoot") && canShoot)
		{
			player.ShootBullet(mousePos);
			shootTimer = isRapidFiring ? RapidFireRate : NormalFireRate;
			canShoot = false;
			player.PlayShootSound(isRapidFiring);
		}
	}
	
	public bool IsDashing() => isDashing;
	public Vector2 GetDashVelocity() => dashDirection * DashSpeed;
	public bool IsRapidFiring() => isRapidFiring;
	public float GetRapidFireTimeLeft() => rapidFireTimeLeft;
	
	public float GetDashCooldownPercent() => 
		Mathf.Clamp(1.0f - (dashTimer / DashCooldown), 0.0f, 1.0f);
	
	public float GetRapidFireCooldownPercent() => 
		Mathf.Clamp(1.0f - (rapidFireTimer / RapidFireCooldown), 0.0f, 1.0f);
}
