using Godot;

/// <summary>
/// A pickup, dropped where a body died.
///
/// It drifts, and your gravity tugs it toward you — weakly. Strong enough that
/// standing still eventually pays, weak enough that going to fetch it is faster.
/// That gap is the decision: a pickup across the arena is worth the trip only if
/// the trip is survivable.
///
/// Drawn in-engine like the rest of the placeholder art: a coloured disc with a
/// symbol, sized and shaped so the five read apart at a glance.
/// </summary>
public partial class PowerUp : Area2D
{
	[Export] public float Radius { get; set; } = 26.0f;
	[Export] public float PullStrength { get; set; } = 260.0f;
	[Export] public float Drag { get; set; } = 1.1f;
	/// <summary>Seconds before it expires. It blinks for the last quarter.</summary>
	[Export] public float Lifetime { get; set; } = 13.0f;

	private PowerUpKind kind = PowerUpKind.Shield;
	private PowerUps.Profile profile = PowerUps.Shield;
	private Node2D world;
	private Vector2 velocity;
	private float age;
	private bool taken;

	/// <summary>Call before adding to the tree.</summary>
	public void Configure(PowerUpKind pickupKind)
	{
		kind = pickupKind;
		profile = PowerUps.Get(kind);
	}

	public override void _Ready()
	{
		var manager = GameManager.Of(this);
		world = manager?.GetNodeOrNull<Node2D>("player");

		// Layer 1 is the world; the pickup watches for it rather than the other
		// way round, so nothing has to change on the player.
		CollisionLayer = 0;
		CollisionMask = 1;
		BodyEntered += OnBodyEntered;

		velocity = Vector2.FromAngle(GD.Randf() * Mathf.Tau) * (float)GD.RandRange(60, 150);
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		age += step;

		if (age >= Lifetime)
		{
			QueueFree();
			return;
		}

		if (world != null && IsInstanceValid(world))
		{
			Vector2 toWorld = world.GlobalPosition - GlobalPosition;
			float distance = Mathf.Max(toWorld.Length(), 1f);
			velocity += (toWorld / distance) * PullStrength * step;
		}

		velocity *= Mathf.Max(1f - Drag * step, 0f);
		GlobalPosition += velocity * step;

		QueueRedraw();
	}

	private void OnBodyEntered(Node2D hit)
	{
		if (taken || hit is not Player)
			return;

		taken = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		GameManager.Of(this)?.CollectPowerUp(kind, GlobalPosition);
		QueueFree();
	}

	public override void _Draw()
	{
		// Blink out over the last quarter of its life, so a pickup never simply
		// vanishes from under a player who was on their way to it.
		float remaining = 1f - age / Mathf.Max(Lifetime, 0.001f);
		float alpha = remaining > 0.25f ? 1f
			: (Mathf.Sin(age * 18f) > 0f ? 1f : 0.25f);

		float bob = 1f + 0.06f * Mathf.Sin(age * 4f);
		Color body = new Color(profile.Colour, alpha);
		Color ink = new Color(0.10f, 0.04f, 0.13f, alpha);

		DrawCircle(Vector2.Zero, Radius * bob, body);
		DrawArc(Vector2.Zero, Radius * bob, 0f, Mathf.Tau, 28, ink, 3.0f, true);
		DrawSymbol(ink, alpha, bob);
	}

	/// <summary>
	/// One glyph each, drawn rather than written: at pickup size a letter is
	/// unreadable, but a shape is not.
	/// </summary>
	private void DrawSymbol(Color ink, float alpha, float bob)
	{
		float r = Radius * 0.52f * bob;

		switch (kind)
		{
			case PowerUpKind.Shield:
				// A dome over a flat base.
				DrawArc(new Vector2(0, r * 0.35f), r, Mathf.Pi, Mathf.Tau, 16, ink, 4.0f, true);
				DrawLine(new Vector2(-r, r * 0.35f), new Vector2(r, r * 0.35f), ink, 4.0f, true);
				break;

			case PowerUpKind.Freeze:
				// A six-spoke star.
				for (int i = 0; i < 3; i++)
				{
					float a = Mathf.Pi * i / 3f;
					Vector2 arm = Vector2.FromAngle(a) * r;
					DrawLine(-arm, arm, ink, 4.0f, true);
				}
				break;

			case PowerUpKind.Magnet:
				// A horseshoe, open downward.
				DrawArc(Vector2.Zero, r * 0.8f, Mathf.Pi, Mathf.Tau, 16, ink, 5.0f, true);
				DrawLine(new Vector2(-r * 0.8f, 0), new Vector2(-r * 0.8f, r * 0.7f), ink, 5.0f, true);
				DrawLine(new Vector2(r * 0.8f, 0), new Vector2(r * 0.8f, r * 0.7f), ink, 5.0f, true);
				break;

			case PowerUpKind.Nuke:
				// A filled core with a ring blown off it.
				DrawCircle(Vector2.Zero, r * 0.42f, ink);
				DrawArc(Vector2.Zero, r * 0.95f, 0f, Mathf.Tau, 20, ink, 3.0f, true);
				break;

			case PowerUpKind.Damage:
				// A bolt.
				var bolt = new Vector2[]
				{
					new(-r * 0.30f, -r), new(r * 0.42f, -r * 0.15f),
					new(r * 0.06f, -r * 0.15f), new(r * 0.34f, r),
					new(-r * 0.42f, r * 0.10f), new(-r * 0.06f, r * 0.10f)
				};
				DrawColoredPolygon(bolt, ink);
				break;
		}
	}
}
