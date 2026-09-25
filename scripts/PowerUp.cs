using Godot;

/// <summary>
/// A pickup, dropped where a body died.
///
/// It drifts, and your gravity tugs it toward you — weakly. Strong enough that
/// standing still eventually pays, weak enough that going to fetch it is faster.
/// That gap is the decision: a pickup across the arena is worth the trip only if
/// the trip is survivable.
///
/// Illustrated pickup icons sit on pulsing discs, distinct from hostile projectiles.
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
    private Sprite2D icon;

	/// <summary>Call before adding to the tree.</summary>
	public void Configure(PowerUpKind pickupKind)
	{
		kind = pickupKind;
		profile = PowerUps.Get(kind);
	}

	public override void _Ready()
	{
        AddToGroup("pickups");
        string asset=kind switch {PowerUpKind.Shield=>"shield",PowerUpKind.Freeze=>"freeze",PowerUpKind.Magnet=>"magnet",PowerUpKind.Nuke=>"nova",_=>"rapid"};
        icon=new Sprite2D {Texture=GD.Load<Texture2D>($"res://art/cosmic/icon_{asset}.svg"),Scale=Vector2.One*.20f};AddChild(icon);
        var manager = GameManager.Of(this);
		world = manager?.GetNodeOrNull<Node2D>("player");

		// Layer 1 is the world; the pickup watches for it rather than the other
		// way round, so nothing has to change on the player.
		CollisionLayer = 0;
		CollisionMask = 1;
		BodyEntered += OnBodyEntered;

		velocity = Vector2.FromAngle(RunState.Rng.Randf() * Mathf.Tau) * RunState.Rng.RandfRange(60, 150);
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
        float alpha=age>Lifetime*.75f&&Mathf.Sin(age*18)<0?.35f:1;
        icon.Scale=Vector2.One*(.24f+.015f*Mathf.Sin(age*4));
        icon.Modulate=new Color(1,1,1,alpha);
    }
}
