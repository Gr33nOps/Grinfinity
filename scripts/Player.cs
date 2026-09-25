using Godot;

/// <summary>
/// The planet. It moves, aims, shoots and dies in one hit — unless a shield takes
/// it, or it is dashing, or it is still blinking after a dash or a broken shield.
/// </summary>
public partial class Player : CharacterBody2D
{
	/// <summary>Raised on every real hit — shield-blocked or lethal.</summary>
	[Signal] public delegate void HitTakenEventHandler();

	[Export] public float MoveSpeed { get; set; } = 250.0f;
	[Export] public float MoveSmoothing { get; set; } = 14.0f;
	/// <summary>
	/// Ignores every source of death. Off in play; the playtest tools turn it on
	/// so an unattended run can reach the late game.
	/// </summary>
	[Export] public bool Invulnerable { get; set; } = false;
	[Export] public PackedScene BulletScene { get; set; }

	[ExportGroup("Feel")]
	/// <summary>Screen shake added by a dash. Small — it happens constantly.</summary>
	[Export] public float DashTrauma { get; set; } = 0.16f;
	[Export] public float MuzzleFlashTime { get; set; } = 0.055f;
	/// <summary>How quickly the blaster swings round to the aim. Higher is snappier.</summary>
	[Export] public float AimTurnRate { get; set; } = 22.0f;
	[Export] public float NovaTrauma { get; set; } = 0.95f;

	/// <summary>How hard Solar Wind shoves the planet about.</summary>
	[Export] public float SolarWindPush { get; set; } = 95.0f;

	// How far in front of the player the gamepad aim point sits.
	private const float GamepadAimDistance = 400.0f;
	private const float StickDeadzoneSq = 0.0625f;

	// Aim assist only considers bodies within this half-angle of the stick's raw
	// direction (~25 degrees) and this close, then softly pulls toward whichever
	// one is most aligned — a nudge, not a snap.
	private const float AimAssistConeCosine = 0.9f;
	private const float AimAssistRange = 900.0f;
	private const float AimAssistStrength = 0.35f;

	private Node2D shootyPart;
	private Sprite2D playerSprite;
	private Node2D muzzleFlash;
	private Tween muzzleTween;
	private AudioStreamPlayer2D shootSound;
	private Area2D hitBox;
	private PlayerAbilities abilities;
	private RunState run;
	private Vector2 externalPush = Vector2.Zero;
	private Vector2 lastMousePosition;
	private Vector2 gamepadAimDirection = Vector2.Right;
	private bool usingGamepadAim = false;
	private bool isDead = false;
	private bool alternateSpread;
	private float hitRecovery;

	/// <summary>World-space point the player is currently aiming at.</summary>
	public Vector2 AimPosition { get; private set; }

	public RunState Run => run;
	public PlayerAbilities Abilities => abilities;

	/// <summary>Blinking and safe: after a dash, or after a shield breaks.</summary>
	public bool IsBlinking => hitRecovery > 0f || abilities.InGrace;
	public bool IsDashing => abilities.IsDashing;
	public bool IsOverdriven => abilities.IsOverdriven;

	/// <summary>
	/// An ability is usable once the run has unlocked it. Thrusters Out closes the
	/// dash for its duration. Outside a run — a tool, a test — everything is open.
	/// </summary>
	public bool CanUse(Ability ability)
	{
		if (run == null)
			return true;
		if (!run.IsUnlocked(ability))
			return false;
		return ability != Ability.Dash || !run.During(ArenaEventId.NoDash);
	}

	public override void _Ready()
	{
		BulletScene ??= GD.Load<PackedScene>("res://scenes/bullet.tscn");

		shootyPart = GetNode<Node2D>("shootyPart");
		playerSprite = FindPlayerSprite();
		if (playerSprite != null && GameSettings.Instance?.HighContrastOutlines == true)
			playerSprite.Material = OutlineMaterial.Get();
		shootSound = GetNodeOrNull<AudioStreamPlayer2D>("ShootSound");
		abilities = new PlayerAbilities(this);

		muzzleFlash = shootyPart.GetNodeOrNull<Node2D>("MuzzleFlash");
		if (muzzleFlash != null)
			muzzleFlash.Visible = false;

		run = GameManager.Of(this)?.Run;

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
		hitRecovery = Mathf.Max(0f, hitRecovery - (float)delta);

		AimPosition = ResolveAimPosition();
		abilities.Update(delta);
		// Turn toward the aim quickly but not instantly, so the blaster sweeps
		// round the planet instead of snapping. Shots still fly at the exact
		// aim point; only the look is eased.
		Vector2 toAim = AimPosition - GlobalPosition;
		if (toAim.LengthSquared() > 1f)
			Rotation = Mathf.LerpAngle(Rotation, toAim.Angle(), 1f - Mathf.Exp(-AimTurnRate * (float)delta));
		HandleMovement(delta);
		abilities.HandleShooting(AimPosition);

		if (abilities.IsDashing)
		{
			// Moved by hand rather than MoveAndSlide, so the dash covers its exact
			// distance, and swept, so nothing it crosses is missed between frames.
			Vector2 before = GlobalPosition;
			GlobalPosition += abilities.TakeDashStep((float)delta);
			StayInArena();
			abilities.Sweep(before, GlobalPosition);
		}
		else
		{
			MoveAndSlide();
			StayInArena();
		}

		// BodyEntered does not fire again if protection expires during the same
		// overlap. Recheck existing contacts, so staying inside an enemy after
		// the blink ends is lethal, exactly as touching one would be.
		if (!Invulnerable && !IsProtected && hitBox != null)
			foreach (Node2D contact in hitBox.GetOverlappingBodies())
				OnHitBoxBodyEntered(contact);
	}

	private bool IsProtected => hitRecovery > 0f || abilities.IsProtected;

	/// <summary>
	/// A blinking safe window, used when coming back from the upgrade screen.
	/// Safety only: unlike a dash, it destroys nothing.
	/// </summary>
	public void GiveGrace(float seconds)
	{
		hitRecovery = Mathf.Max(hitRecovery, seconds);
	}

	/// <summary>Fires the Nova from here. The cooldown is the ability's business, not this.</summary>
	public void FireNova()
	{
		var manager = GameManager.Of(this);
		manager?.DetonateNova(GlobalPosition, run?.NovaRadius ?? Balance.NovaRadius);
		manager?.Shake(NovaTrauma);
		GetNodeOrNull<PlanetVisual>("PlanetVisual")?.Celebrate();
	}

	public void OnOverdriveStarted()
	{
		var manager = GameManager.Of(this);
		manager?.PlayCue("overdrive");
		manager?.Shake(0.35f);
		manager?.SpawnBlast(GlobalPosition, 260f, PlanetVisual.OverdriveColour);
		GetNodeOrNull<PlanetVisual>("PlanetVisual")?.Celebrate();
	}

	public void OnOverdriveEnded()
	{
		GameManager.Of(this)?.PlayCue("overdrive_end");
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
			Vector2 direction = stick.Normalized();
			if (GameSettings.Instance?.GamepadAimAssist == true)
				direction = ApplyAimAssist(direction);

			gamepadAimDirection = direction;
			usingGamepadAim = true;
		}

		// Compared in screen space: the camera moving under a still mouse must
		// not count as the mouse being used.
		Vector2 mouseScreen = GetViewport().GetMousePosition();
		if (!mouseScreen.IsEqualApprox(lastMousePosition))
		{
			lastMousePosition = mouseScreen;
			usingGamepadAim = false;
		}

		return usingGamepadAim
			? GlobalPosition + gamepadAimDirection * GamepadAimDistance
			: GetGlobalMousePosition();
	}

	/// <summary>
	/// Pulls a raw stick direction toward the nearest body within a narrow cone
	/// ahead of it, if there is one. Settings-gated, gamepad only.
	/// </summary>
	private Vector2 ApplyAimAssist(Vector2 rawDirection)
	{
		Node2D best = null;
		float bestAlignment = AimAssistConeCosine;

		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body body || !IsInstanceValid(body))
				continue;

			Vector2 toBody = body.GlobalPosition - GlobalPosition;
			float distance = toBody.Length();
			if (distance < 1f || distance > AimAssistRange)
				continue;

			float alignment = (toBody / distance).Dot(rawDirection);
			if (alignment > bestAlignment)
			{
				bestAlignment = alignment;
				best = body;
			}
		}

		if (best == null)
			return rawDirection;

		Vector2 towardBest = (best.GlobalPosition - GlobalPosition).Normalized();
		return rawDirection.Lerp(towardBest, AimAssistStrength).Normalized();
	}

	/// <summary>
	/// A push from outside — a gravity well or the Black Hole. Accumulated rather
	/// than applied immediately, since it may arrive from another node's
	/// _PhysicsProcess in either order relative to this one's.
	/// </summary>
	public void ApplyExternalPush(Vector2 accel)
	{
		externalPush += accel;
	}

	private void HandleMovement(double delta)
	{
		if (abilities.IsDashing)
		{
			// The dash moves the planet itself and ignores every pull. Velocity is
			// left at walking pace, so the planet comes out of it without skidding.
			Velocity = abilities.DashVelocity.Normalized() * MoveSpeed;
			externalPush = Vector2.Zero;
			return;
		}

		Vector2 targetVelocity = new Vector2(
			Input.GetAxis("left", "right"),
			Input.GetAxis("up", "down")
		).LimitLength(1f) * MoveSpeed;

		if (run != null && run.During(ArenaEventId.SolarWind))
			targetVelocity += run.WindDirection * SolarWindPush;

		targetVelocity += externalPush;
		externalPush = Vector2.Zero;

		float t = 1f - Mathf.Exp(-MoveSmoothing * (float)delta);
		Velocity = Velocity.Lerp(targetVelocity, t);
	}

	private void OnHitBoxBodyEntered(Node2D hit)
	{
		if (hit is Body body)
			Die($"a {body.Kind}");
		else if (hit is Boss boss)
			Die(boss.BossName);
		else if (hit.IsInGroup("hazards"))
			Die(TranslationServer.Translate("DEATH_CAUSE_GravityWell"));
	}

	/// <summary>Killed by something other than contact — a blast, a comet, or a hostile shot.</summary>
	public void KillByBlast(string cause = "")
	{
		Die(cause);
	}

	private void Die(string cause = "")
	{
		if (isDead || Invulnerable || IsProtected)
			return;

		EmitSignal(SignalName.HitTaken);

		// Every source of death passes through here, so one check covers them all.
		if (run != null && run.ConsumeShield())
		{
			hitRecovery = Balance.ShieldBreakGrace;
			var manager = GameManager.Of(this);
			manager?.PlayCue("shield_pop");
			manager?.Shake(0.5f);
			manager?.Hitstop(0.09f);
			manager?.SpawnBlast(GlobalPosition, 240f, Pickups.ShieldColour);
			manager?.Flash(Pickups.ShieldColour, 0.18f, 0.25f);
			manager?.Toast("SHIELD BROKEN", Pickups.ShieldColour);
			return;
		}

		isDead = true;
		Velocity = Vector2.Zero;
		SpawnDeathEffect();

		GameManager.Of(this)?.OnPlayerKilled(cause);
	}

	private void SpawnDeathEffect()
	{
		var manager = GameManager.Of(this);
		manager?.SpawnBlast(GlobalPosition, 360f, new Color(1f, 0.82f, 0.35f));
		manager?.ShedChunks(GlobalPosition, 14, new Color(0.6f, 0.9f, 0.8f));

		if (playerSprite != null)
			playerSprite.Visible = false;
	}

	private void StayInArena()
	{
		GlobalPosition = Arena.ClampToPlayable(GlobalPosition, Arena.RadiusOf(this));
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

	/// <summary>The weapon this run is carrying.</summary>
	public WeaponProfile Weapon => WeaponProfile.Get(run?.Weapon ?? WeaponId.Comet);

	/// <summary>Seconds until the weapon can fire again.</summary>
	public float FireInterval(bool overdriven)
	{
		float overdrive = overdriven ? (run?.OverdriveFireScale ?? Balance.OverdriveFireScale) : 1f;
		return Weapon.FireInterval * overdrive * (run?.FireIntervalScale ?? 1.0f);
	}

	/// <summary>
	/// One volley. Overdrive does not swap the gun — it takes whatever this run
	/// has built and pushes all of it harder: more damage, one more pierce,
	/// bigger and faster shots, on top of every spread and pierce level held.
	/// </summary>
	public void ShootBullet(Vector2 aimPosition, bool overdriven = false)
	{
		WeaponProfile weapon = Weapon;
		Vector2 aim = (aimPosition - GlobalPosition).Normalized();
		int spreadLevel = run?.SpreadLevel ?? 0;
		int pellets = weapon.Pellets + spreadLevel;

		// Pellets are spread evenly across the cone rather than randomly, so a
		// shotgun pattern is something a player can learn to place.
		for (int i = 0; i < pellets; i++)
		{
			float offset;
			if (i < weapon.Pellets)
				offset = weapon.Pellets <= 1 ? 0f : weapon.Spread * (i / (float)(weapon.Pellets - 1) - 0.5f);
			else
			{
				// Spread Shot's extra shots sit outside the weapon's own cone. One
				// level alternates sides each volley; two fires both.
				int side = spreadLevel == 1 ? (alternateSpread ? -1 : 1) : (i - weapon.Pellets == 0 ? -1 : 1);
				offset = side * (weapon.Spread * 0.5f + Mathf.DegToRad(18));
			}

			var bullet = BulletScene.Instantiate<Bullet>();
			bullet.ApplyProfile(weapon);
			if (run != null)
				bullet.Pierce += run.ExtraPierce;

			if (overdriven)
			{
				bullet.Damage *= Balance.OverdriveDamageMultiplier;
				bullet.Pierce += Balance.OverdriveExtraPierce;
				bullet.Speed *= 1.15f;
				bullet.Scale *= 1.3f;
				bullet.Overdriven = true;
			}

			bullet.GlobalPosition = shootyPart.GlobalPosition;
			bullet.Direction = aim.Rotated(offset);
			GameManager.Spawn(this, bullet);
		}

		FlashMuzzle(overdriven);
		alternateSpread = !alternateSpread;
		GetNodeOrNull<PlanetVisual>("PlanetVisual")?.Kick();
	}

	/// <summary>
	/// One frame of light at the barrel. Randomised scale and roll so a held
	/// trigger does not look like a strobing decal.
	/// </summary>
	private void FlashMuzzle(bool overdriven)
	{
		if (muzzleFlash == null)
			return;

		muzzleTween?.Kill();
		muzzleFlash.Visible = true;
		muzzleFlash.Rotation = RunState.Rng.RandfRange(-0.5f, 0.5f);
		muzzleFlash.Scale = Vector2.One * RunState.Rng.RandfRange(0.32f, 0.48f) * (overdriven ? 1.5f : 1f);
		muzzleFlash.Modulate = overdriven ? new Color(1.4f, 0.8f, 1f) : Colors.White;

		muzzleTween = CreateTween();
		muzzleTween.TweenProperty(muzzleFlash, "modulate:a", 0.0f, MuzzleFlashTime);
		muzzleTween.TweenCallback(Callable.From(() => muzzleFlash.Visible = false));
	}

	public void PlayShootSound(bool overdriven = false)
	{
		if (shootSound == null)
			return;

		shootSound.PitchScale = overdriven ? 1.28f + RunState.Rng.RandfRange(-0.04f, 0.04f) : 1.0f;
		shootSound.Play();
	}

	public void CreateDashEffect()
	{
		var manager = GameManager.Of(this);
		manager?.PlayCue("dash_swish");
		manager?.Shake(DashTrauma);
		GetNodeOrNull<PlanetVisual>("PlanetVisual")?.StartDashTrail();
	}
}
