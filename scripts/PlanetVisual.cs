using Godot;

/// <summary>
/// The planet's look: smiling face, blaster, its moons, and the states the
/// abilities put it in. Pure presentation — nothing here changes the hitbox.
///
/// Each moon in orbit is one shield. They circle on a tilted path, passing
/// behind the planet on the far side, so the count can be read at a glance
/// without looking away from the fight.
///
/// Blinking means "safe for now": after a dash and after a moon is lost. The
/// flicker is the only warning the player gets that it is about to end, so it
/// speeds up over the last stretch.
/// </summary>
public partial class PlanetVisual : Node2D
{
	/// <summary>Overdrive's colour: the same hot gold as its shots, on the glow, the burst and the HUD ring.</summary>
	public static readonly Color OverdriveColour = new("ffc34d");
	private static readonly Color DashGhost = new(1f, 0.93f, 0.8f, 0.38f);

	private Player player;
	private Sprite2D body, face, gun;
	/// <summary>Carries the moons, turned against the aim so their orbit stays level.</summary>
	private Node2D orbit;
	private readonly Sprite2D[] moons = new Sprite2D[Balance.MaxMoons];
	/// <summary>Each moon's place round the orbit, easing toward even spacing when the count changes.</summary>
	private readonly float[] moonOffset = new float[Balance.MaxMoons];
	/// <summary>Counts up from 0 as a new moon swells into place.</summary>
	private readonly float[] moonGrow = new float[Balance.MaxMoons];
	private int shownMoons;

	private const float OrbitWide = 64f, OrbitTall = 24f, OrbitTilt = -0.24f, OrbitSpeed = 1.15f, MoonScale = 0.115f;
	private Painted aura;
	private Node2D muzzle;
	private float time, recoil, celebration, hurt, ghostTimer;
	/// <summary>Where the face is glancing, eased toward the aim.</summary>
	private Vector2 look = Vector2.Right;
	private Texture2D normal, blink, happy;

	public override void _Ready()
	{
		player = GetParent<Player>();
		body = player.GetNode<Sprite2D>("Sprite2D");
		body.Texture = GD.Load<Texture2D>($"res://art/cosmic/planet_{GameSettings.Instance?.World ?? 1}.svg");
		body.Position = Vector2.Zero; body.Scale = Vector2.One * .32f; body.FlipH = body.FlipV = false;
		int world = GameSettings.Instance?.World ?? 1; normal = Worlds.Face(world); blink = Worlds.Face(world, true); happy = GD.Load<Texture2D>("res://art/cosmic/face_happy.svg");
		face = new Sprite2D { Texture = normal, ZIndex = 2 }; body.AddChild(face);
		gun = new Sprite2D { Texture = GD.Load<Texture2D>("res://art/cosmic/blaster.svg"), Position = new Vector2(49, 12), Scale = Vector2.One * .27f, ZIndex = 3 }; AddChild(gun);
		muzzle = player.GetNode<Node2D>("shootyPart");
		muzzle.Position = new Vector2(75, 12);
		orbit = new Node2D { Name = "Moons" }; AddChild(orbit);
		Texture2D moonArt = GD.Load<Texture2D>("res://art/cosmic/moon.svg");
		for (int i = 0; i < moons.Length; i++)
		{
			moons[i] = new Sprite2D { Texture = moonArt, Visible = false };
			orbit.AddChild(moons[i]);
		}

		// Overdrive's glow sits behind the planet, so the face stays readable.
		aura = new Painted(new Rect2(-90, -90, 180, 180), item =>
		{
			for (int ring = 0; ring < 4; ring++)
				item.DrawCircle(Vector2.Zero, 44f + ring * 11f, new Color(OverdriveColour, 0.16f - ring * 0.03f));
		}) { ZIndex = -2, Visible = false };
		AddChild(aura);

		player.HitTaken += () => hurt = .3f;
	}

	public void Kick() { recoil = 1; }
	public void Celebrate() { celebration = 1.1f; }

	/// <summary>Leaves a few fading copies of the planet along a dash.</summary>
	public void StartDashTrail() { ghostTimer = 0f; }

	public override void _Process(double delta)
	{
		float step = (float)delta; time += step; recoil = Mathf.MoveToward(recoil, 0, step * 9); celebration = Mathf.Max(0, celebration - step); hurt = Mathf.Max(0, hurt - step);
		// The planet never flips or turns upside down: it stays upright and its
		// face glances toward where you aim. Only the blaster goes round, and the
		// player's rotation that carries it is itself smoothed (see Player).
		Vector2 aimDir = Vector2.FromAngle(player.Rotation);
		look = look.Lerp(aimDir, 1f - Mathf.Exp(-10f * step));

		float lean = Mathf.Clamp(player.Velocity.X * .00015f, -.12f, .12f);
		body.Rotation = lean - player.Rotation;
		float pulse = 1f + .018f * Mathf.Sin(time * 3) + .065f * recoil;
		body.Scale = new Vector2(.32f / pulse, .32f * pulse);
		face.Position = look * 16f;
		face.Texture = celebration > 0 || player.IsOverdriven ? happy : time % 4.2f > 4.06f ? blink : normal;

		// On the aim line, so nothing jumps when the aim crosses straight up or
		// down; the art is only mirrored so it is never drawn upside down.
		gun.Position = new Vector2(49 - recoil * 5, 0f);
		gun.Rotation = -recoil * .1f;
		gun.FlipV = aimDir.X < 0f;
		muzzle.Position = new Vector2(75, 0f);
		gun.Visible = body.Visible;
		UpdateMoons(step);

		bool overdriven = player.IsOverdriven;
		aura.Visible = overdriven && body.Visible;
		if (overdriven)
		{
			float beat = .5f + .5f * Mathf.Sin(time * 14f);
			aura.Scale = Vector2.One * (1f + .12f * beat);
			body.SelfModulate = new Color(1f, 1f, 1f).Lerp(new Color(1.3f, 1.12f, .78f), .35f + .25f * beat);
			gun.SelfModulate = new Color(1.4f, 1.15f, .7f);
		}
		else
		{
			body.SelfModulate = hurt > 0 ? new Color(1.5f, .8f, .85f) : Colors.White;
			gun.SelfModulate = Colors.White;
		}

		// Blinking: safe but not for long. Faster flicker is the "get clear" cue.
		float alpha = 1f;
		if (player.IsBlinking)
			alpha = Mathf.PosMod(time * 14f, 1f) < .5f ? .28f : .8f;
		else if (player.IsDashing)
			alpha = .7f;
		player.Modulate = new Color(1f, 1f, 1f, alpha);

		if (player.IsDashing)
		{
			ghostTimer -= step;
			if (ghostTimer <= 0f)
			{
				ghostTimer = .04f;
				LeaveGhost();
			}
		}
	}

	private void UpdateMoons(float step)
	{
		orbit.Rotation = -player.Rotation;
		int count = Mathf.Clamp(player.Run?.Moons ?? 0, 0, moons.Length);

		// A moon lost to a hit bursts where it was; a new one swells in.
		for (int i = count; i < shownMoons; i++)
			PopEffect.Spawn(this, moons[i].GlobalPosition, 26f, Pickups.ShieldColour, 6, 0.35f);
		for (int i = shownMoons; i < count; i++)
			moonGrow[i] = 0f;
		shownMoons = count;

		for (int i = 0; i < moons.Length; i++)
		{
			Sprite2D moon = moons[i];
			moon.Visible = i < count && body.Visible;
			if (i >= count)
				continue;

			moonOffset[i] = Mathf.LerpAngle(moonOffset[i], Mathf.Tau * i / count, 1f - Mathf.Exp(-6f * step));
			moonGrow[i] = Mathf.Min(1f, moonGrow[i] + step * 4f);

			float angle = time * OrbitSpeed + moonOffset[i];
			var flat = new Vector2(Mathf.Cos(angle) * OrbitWide, Mathf.Sin(angle) * OrbitTall);
			moon.Position = flat.Rotated(OrbitTilt);

			// The far side of the orbit passes behind the planet, a touch smaller and dimmer.
			float depth = Mathf.Sin(angle);
			moon.ZIndex = depth < 0f ? -1 : 5;
			float swell = moonGrow[i] < 1f ? Mathf.Sin(moonGrow[i] * Mathf.Pi * 0.5f) * (1f + 0.25f * Mathf.Sin(moonGrow[i] * Mathf.Pi)) : 1f;
			moon.Scale = Vector2.One * MoonScale * (1f + 0.1f * depth) * swell;
			moon.SelfModulate = depth < 0f ? new Color(0.78f, 0.74f, 0.86f) : Colors.White;
		}
	}

	private void LeaveGhost()
	{
		var ghost = new Sprite2D
		{
			Texture = body.Texture,
			Transform = body.GlobalTransform,
			Modulate = DashGhost,
			ZIndex = -1
		};
		GameManager.Spawn(this, ghost);
		// Warm and shrinking, so the trail reads as speed rather than as copies.
		var fade = ghost.CreateTween().SetParallel();
		fade.TweenProperty(ghost, "modulate:a", 0f, .22f);
		fade.TweenProperty(ghost, "scale", ghost.Scale * 0.7f, .22f);
		fade.Chain();
		fade.TweenCallback(Callable.From(ghost.QueueFree));
	}
}
