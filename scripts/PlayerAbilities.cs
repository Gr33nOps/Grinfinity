// PlayerAbilities.cs - Handles dash and rapid fire abilities
using Godot;

public class PlayerAbilities
{
	private Player player;
	
	private float dashSpeed = 600.0f;
	private float dashDuration = 0.2f;
	private float dashCooldown = 2.0f;
	private float dashTimer = 0.0f;
	private bool isDashing = false;
	private float dashTimeLeft = 0.0f;
	private Vector2 dashDirection = Vector2.Zero;
	
	private float normalFireRate = 0.3f;     // Time between normal shots
	private float rapidFireRate = 0.08f;     // Time between rapid fire shots
	private float rapidFireDuration = 3.0f;  // How long rapid fire lasts
	private float rapidFireCooldown = 5.0f;  // Cooldown before using again
	private float rapidFireTimer = 0.0f;
	private float shootTimer = 0.0f;
	private bool isRapidFiring = false;
	private float rapidFireTimeLeft = 0.0f;
	private bool canShoot = true;
	
	public PlayerAbilities(Player playerRef)
	{
		player = playerRef;
	}
	
	public void Update(double delta)
	{
		UpdateDashSystem(delta);
		UpdateRapidFireSystem(delta);
		UpdateShootingTimer(delta);
		
		HandleDashInput();
		HandleRapidFireInput();
	}
	
	private void UpdateDashSystem(double delta)
	{
		if (dashTimer > 0)
			dashTimer -= (float)delta;
		
		if (isDashing)
		{
			dashTimeLeft -= (float)delta;
			if (dashTimeLeft <= 0)
			{
				isDashing = false;
			}
		}
	}
	
	private void HandleDashInput()
	{
		bool wantsToDash = Input.IsActionJustPressed("dash");
		bool canDash = dashTimer <= 0 && !isDashing;
		
		if (wantsToDash && canDash)
		{
			StartDash();
		}
	}
	
	private void StartDash()
	{
		Vector2 inputDirection = new Vector2(
			Input.GetAxis("left", "right"),
			Input.GetAxis("up", "down")
		).Normalized();
		
		if (inputDirection == Vector2.Zero)
		{
			Vector2 mousePos = player.GetGlobalMousePosition();
			inputDirection = (mousePos - player.GlobalPosition).Normalized();
		}
		
		dashDirection = inputDirection;
		isDashing = true;
		dashTimeLeft = dashDuration;
		dashTimer = dashCooldown;
		
		player.CreateDashEffect();
	}
	
	public bool IsDashing()
	{
		return isDashing;
	}
	
	public Vector2 GetDashVelocity()
	{
		return dashDirection * dashSpeed;
	}
	
	private void UpdateRapidFireSystem(double delta)
	{
		if (rapidFireTimer > 0)
			rapidFireTimer -= (float)delta;
		
		if (isRapidFiring)
		{
			rapidFireTimeLeft -= (float)delta;
			if (rapidFireTimeLeft <= 0)
			{
				isRapidFiring = false;
				rapidFireTimer = rapidFireCooldown; // Start cooldown
			}
		}
	}
	
	private void UpdateShootingTimer(double delta)
	{
		if (shootTimer > 0)
		{
			shootTimer -= (float)delta;
			if (shootTimer <= 0)
				canShoot = true;
		}
	}
	
	private void HandleRapidFireInput()
	{
		bool wantsRapidFire = Input.IsActionJustPressed("rapid_fire");
		bool canUseRapidFire = rapidFireTimer <= 0 && !isRapidFiring;
		
		if (wantsRapidFire && canUseRapidFire)
		{
			StartRapidFire();
		}
	}
	
	private void StartRapidFire()
	{
		isRapidFiring = true;
		rapidFireTimeLeft = rapidFireDuration;
		
		player.CreateRapidFireEffect();
	}
	
	public void HandleShooting(Vector2 mousePos)
	{
		bool wantsToShoot = Input.IsActionPressed("shoot");
		
		if (wantsToShoot && canShoot)
		{
			player.ShootBullet(mousePos);
			
			float fireRate = isRapidFiring ? rapidFireRate : normalFireRate;
			shootTimer = fireRate;
			canShoot = false;
			
			player.PlayShootSound(isRapidFiring);
		}
	}
	
	public float GetDashCooldownPercent()
	{
		return Mathf.Clamp(1.0f - (dashTimer / dashCooldown), 0.0f, 1.0f);
	}
	
	public float GetRapidFireCooldownPercent()
	{
		return Mathf.Clamp(1.0f - (rapidFireTimer / rapidFireCooldown), 0.0f, 1.0f);
	}
	
	public bool IsRapidFiring()
	{
		return isRapidFiring;
	}
	
	public float GetRapidFireTimeLeft()
	{
		return rapidFireTimeLeft;
	}
}
