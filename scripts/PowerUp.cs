using Godot;

/// <summary>
/// A pickup lying in the arena: one upgrade level or a shield. Touch it and it is
/// yours, straight away, with no menu and no pause.
///
/// It drifts where it fell until the planet comes close, then slides in, so a
/// pickup is always worth a short trip but never needs pixel-perfect steering.
/// The glowing disc is the upgrade's own colour, so two pickups that share an
/// icon still read as different things.
/// </summary>
public partial class PowerUp : Area2D
{
	[Export] public float PullStrength { get; set; } = 1500.0f;
	[Export] public float Drag { get; set; } = 2.4f;

	private Reward reward = Reward.Shield;
	private float lifetime = Balance.DropLifetime;
	private Node2D world;
	private Vector2 velocity;
	private Vector2 flyTo;
	private bool flying;
	private float age;
	private bool taken;
	private Sprite2D icon;

	public Reward Reward => reward;

	/// <summary>Call before adding to the tree.</summary>
	public void Configure(Reward what, float seconds)
	{
		reward = what;
		lifetime = seconds;
	}

	/// <summary>Boss rewards are thrown out from the wreck to where they come to rest.</summary>
	public void FlyTo(Vector2 target)
	{
		flyTo = Arena.ClampToPlayable(target, 60f);
		flying = true;
	}

	public override void _Ready()
	{
		AddToGroup("pickups");
		icon = new Sprite2D { Texture = GD.Load<Texture2D>($"res://art/cosmic/icon_{reward.Icon}.svg"), Scale = Vector2.One * 0.24f };
		AddChild(icon);
		world = GameManager.Of(this)?.GetNodeOrNull<Node2D>("player");

		// Layer 1 is the planet; the pickup watches for it.
		CollisionLayer = 0;
		CollisionMask = 1;
		BodyEntered += OnBodyEntered;

		if (!flying)
			velocity = Vector2.FromAngle(RunState.Rng.Randf() * Mathf.Tau) * RunState.Rng.RandfRange(60, 140);

		Scale = Vector2.One * 0.3f;
		CreateTween().TweenProperty(this, "scale", Vector2.One, 0.35f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		age += step;

		if (age >= lifetime)
		{
			QueueFree();
			return;
		}

		if (flying)
		{
			GlobalPosition = GlobalPosition.Lerp(flyTo, 1f - Mathf.Exp(-5f * step));
			if (GlobalPosition.DistanceTo(flyTo) < 6f || age > 1.2f)
				flying = false;
		}
		else
		{
			if (world != null && IsInstanceValid(world))
			{
				Vector2 toWorld = world.GlobalPosition - GlobalPosition;
				float distance = toWorld.Length();
				if (distance < Balance.PickupMagnetRadius && distance > 1f)
				{
					// Stronger the closer it gets, so it snaps in rather than orbits.
					float pull = PullStrength * (1f - distance / Balance.PickupMagnetRadius) + 300f;
					velocity += toWorld / distance * pull * step;
				}
			}

			velocity *= Mathf.Max(1f - Drag * step, 0f);
			GlobalPosition = Arena.ClampToPlayable(GlobalPosition + velocity * step, 40f);
		}

		if (Arena.IsNearView(GlobalPosition, 200f))
			QueueRedraw();
	}

	private void OnBodyEntered(Node2D hit)
	{
		if (taken || hit is not Player)
			return;

		taken = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		GameManager.Of(this)?.CollectReward(reward, GlobalPosition);
		QueueFree();
	}

	public override void _Draw()
	{
		bool blinkOut = age > lifetime * 0.75f && Mathf.Sin(age * 18f) < 0f;
		float alpha = blinkOut ? 0.35f : 1f;
		float pulse = 0.5f + 0.5f * Mathf.Sin(age * 5f);
		Color colour = reward.Colour;

		// The icon carries its own disc; this adds the upgrade's colour as a
		// rim and a soft halo, so pickups sharing an icon still read apart.
		DrawCircle(Vector2.Zero, 44f + 6f * pulse, new Color(colour, 0.16f * alpha));
		DrawArc(Vector2.Zero, 34f, 0f, Mathf.Tau, 48, new Color(colour, alpha), 5f, true);

		icon.Scale = Vector2.One * (0.24f + 0.012f * pulse);
		icon.Modulate = new Color(1f, 1f, 1f, alpha);
	}
}
