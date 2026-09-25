using System.Collections.Generic;
using Godot;

/// <summary>
/// The three abilities and the trigger. Each ability has its own cooldown and
/// nothing else — no shared meter, no resource.
///
/// Dash is the one that has to be exact, because the planet dies in one hit.
/// While it moves, the planet cannot be hurt, and everything its path crosses is
/// destroyed. The path is tested as a swept circle from where the planet was to
/// where it is, every physics frame, so nothing can slip between two frames
/// however fast the dash or slow the machine. When the movement stops, a short
/// blinking grace follows: still safe, but no longer destroying anything, so the
/// player has a moment to get clear. Then the planet is mortal again.
/// </summary>
public class PlayerAbilities
{
	/// <summary>Added to the combined radii in the dash sweep, so a visible graze counts.</summary>
	private const float DashReach = 10f;

	private readonly Player player;

	private bool isDashing;
	private float dashRemaining;
	private float dashAge;
	private float dashCooldownLeft;
	private float graceLeft;
	private Vector2 dashVelocity;
	private readonly HashSet<ulong> bossesHitThisDash = new();

	private float overdriveLeft;
	private float overdriveCooldownLeft;
	private float novaCooldownLeft;

	private float shootTimer;

	public PlayerAbilities(Player playerRef)
	{
		player = playerRef;
	}

	public bool IsDashing => isDashing;
	/// <summary>The blinking window after a dash stops.</summary>
	public bool InGrace => graceLeft > 0f;
	/// <summary>Nothing can hurt the planet: dashing, or blinking just after.</summary>
	public bool IsProtected => isDashing || graceLeft > 0f;
	public bool IsOverdriven => overdriveLeft > 0f;
	public Vector2 DashVelocity => dashVelocity;

	public void Update(double delta)
	{
		float step = (float)delta;
		dashCooldownLeft = Mathf.Max(0f, dashCooldownLeft - step);
		overdriveCooldownLeft = Mathf.Max(0f, overdriveCooldownLeft - step);
		novaCooldownLeft = Mathf.Max(0f, novaCooldownLeft - step);
		shootTimer = Mathf.Max(0f, shootTimer - step);

		if (!isDashing && graceLeft > 0f)
			graceLeft = Mathf.Max(0f, graceLeft - step);

		// A dash is normally finished by its distance. This is only a backstop, so
		// nothing can ever leave the planet protected indefinitely.
		if (isDashing && (dashAge += step) > Balance.DashDuration * 3f)
			EndDash();

		if (overdriveLeft > 0f)
		{
			overdriveLeft -= step;
			if (overdriveLeft <= 0f)
				EndOverdrive();
		}

		HandleInput();
	}

	private void HandleInput()
	{
		if (Input.IsActionJustPressed("dash") && !isDashing && dashCooldownLeft <= 0f && player.CanUse(Ability.Dash))
			StartDash();

		if (player.CanUse(Ability.Overdrive))
		{
			if (GameSettings.Instance?.RapidFireHoldMode == true)
			{
				// Accessibility: runs only while held, and ends the moment it is
				// let go. Same budget and cooldown, a different feel.
				bool held = Input.IsActionPressed("rapid_fire");
				if (held && !IsOverdriven && overdriveCooldownLeft <= 0f)
					StartOverdrive();
				else if (!held && IsOverdriven)
					EndOverdrive();
			}
			else if (Input.IsActionJustPressed("rapid_fire") && !IsOverdriven && overdriveCooldownLeft <= 0f)
			{
				StartOverdrive();
			}
		}

		if (Input.IsActionJustPressed("nova") && novaCooldownLeft <= 0f && player.CanUse(Ability.Nova))
		{
			novaCooldownLeft = NovaCooldown;
			player.FireNova();
		}
	}

	// --- Dash ----------------------------------------------------------------

	private void StartDash()
	{
		Vector2 direction = new Vector2(Input.GetAxis("left", "right"), Input.GetAxis("up", "down"));
		if (direction.LengthSquared() < 0.04f)
			direction = player.AimPosition - player.GlobalPosition;
		if (direction.LengthSquared() < 0.01f)
			direction = Vector2.Right.Rotated(player.Rotation);

		float distance = player.Run?.DashDistance ?? Balance.DashDistance;
		dashVelocity = direction.Normalized() * distance / Balance.DashDuration;
		isDashing = true;
		dashRemaining = distance;
		dashAge = 0f;
		dashCooldownLeft = Balance.DashCooldown;
		graceLeft = 0f;
		bossesHitThisDash.Clear();

		player.CreateDashEffect();
		// Anything already touching the planet when the dash starts is in its path.
		Sweep(player.GlobalPosition, player.GlobalPosition);
	}

	/// <summary>
	/// How far the planet moves this frame of the dash. Measured out of a fixed
	/// distance rather than a timer, so a dash always covers exactly the same
	/// ground whatever the frame rate. The frame that finishes it starts the grace.
	/// </summary>
	public Vector2 TakeDashStep(float delta)
	{
		if (!isDashing)
			return Vector2.Zero;

		float step = Mathf.Min(dashVelocity.Length() * delta, dashRemaining);
		dashRemaining -= step;
		Vector2 move = dashVelocity.Normalized() * step;
		if (dashRemaining <= 0.01f)
			EndDash();
		return move;
	}

	private void EndDash()
	{
		isDashing = false;
		dashRemaining = 0f;
		graceLeft = player.Run?.DashGrace ?? Balance.DashGrace;
	}

	/// <summary>
	/// Destroys every enemy the planet passed through between two positions, and
	/// lands one hit on each boss it touched. Called by the player each physics
	/// frame of a dash, after it has moved.
	/// </summary>
	public void Sweep(Vector2 from, Vector2 to)
	{
		var manager = GameManager.Of(player);
		float reach = Arena.RadiusOf(player) + DashReach;
		Vector2 direction = dashVelocity.Normalized();

		foreach (Node node in player.GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body body || !IsInstanceValid(body) || body.IsDestroyed)
				continue;

			if (!Touches(from, to, body.GlobalPosition, reach + Arena.RadiusOf(body)))
				continue;

			Body.Remains remains = body.GetRemains();
			if (body.TakeDamage(9999, direction, ignoreArmour: true))
				manager?.RegisterKill(remains, body.GlobalPosition, KillSource.Dash);
		}

		foreach (Node node in player.GetTree().GetNodesInGroup("bosses"))
		{
			if (node is not Boss boss || !IsInstanceValid(boss) || bossesHitThisDash.Contains(boss.GetInstanceId()))
				continue;

			if (!Touches(from, to, boss.GlobalPosition, reach + Arena.RadiusOf(boss)))
				continue;

			bossesHitThisDash.Add(boss.GetInstanceId());
			boss.TakeShareOfHealth(Balance.DashBossDamage, direction);
			manager?.SpawnImpact(boss.GlobalPosition.MoveToward(player.GlobalPosition, Arena.RadiusOf(boss)), boss.BossColor);
			manager?.Shake(0.3f);
		}
	}

	private static bool Touches(Vector2 from, Vector2 to, Vector2 centre, float radius)
	{
		Vector2 closest = Geometry2D.GetClosestPointToSegment(centre, from, to);
		return closest.DistanceSquaredTo(centre) <= radius * radius;
	}

	private static bool IsInstanceValid(GodotObject target) => GodotObject.IsInstanceValid(target);

	// --- Overdrive -------------------------------------------------------------

	private void StartOverdrive()
	{
		overdriveLeft = player.Run?.OverdriveDuration ?? Balance.OverdriveDuration;
		overdriveCooldownLeft = Balance.OverdriveCooldown;
		player.OnOverdriveStarted();
	}

	private void EndOverdrive()
	{
		overdriveLeft = 0f;
		player.OnOverdriveEnded();
	}

	// --- Shooting ----------------------------------------------------------------

	public void HandleShooting(Vector2 aimPosition)
	{
		if (!Input.IsActionPressed("shoot") || shootTimer > 0f)
			return;

		player.ShootBullet(aimPosition, IsOverdriven);
		player.PlayShootSound(IsOverdriven);
		shootTimer = player.FireInterval(IsOverdriven);
	}

	// --- For the HUD ----------------------------------------------------------------

	/// <summary>0 just used, 1 ready.</summary>
	public float Readiness(Ability ability) => ability switch
	{
		Ability.Dash => 1f - dashCooldownLeft / Balance.DashCooldown,
		Ability.Overdrive => 1f - overdriveCooldownLeft / Balance.OverdriveCooldown,
		_ => 1f - novaCooldownLeft / NovaCooldown
	};

	private float NovaCooldown => player.Run?.NovaCooldown ?? Balance.NovaCooldown;

	/// <summary>A Power Cell: every ability ready now. An Overdrive already running keeps running.</summary>
	public void ResetCooldowns()
	{
		dashCooldownLeft = 0f;
		overdriveCooldownLeft = 0f;
		novaCooldownLeft = 0f;
	}

	public float CooldownLeft(Ability ability) => ability switch
	{
		Ability.Dash => dashCooldownLeft,
		Ability.Overdrive => overdriveCooldownLeft,
		_ => novaCooldownLeft
	};

	/// <summary>Share of the current Overdrive still to run, for the HUD. 0 when off.</summary>
	public float OverdriveRemaining => overdriveLeft <= 0f ? 0f : overdriveLeft / (player.Run?.OverdriveDuration ?? Balance.OverdriveDuration);
}
