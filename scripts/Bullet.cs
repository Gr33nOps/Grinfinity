using Godot;

public partial class Bullet : Area2D
{
	[Export] public float Speed { get; set; } = 950.0f;
	[Export] public int Damage { get; set; } = 1;
	/// <summary>Extra bodies this shot survives after the first. 0 stops on contact.</summary>
	[Export] public int Pierce { get; set; } = 0;

	/// <summary>Points kept in the motion trail. More is longer and softer.</summary>
	[Export] public int TrailLength { get; set; } = 9;

	public Vector2 Direction { get; set; }

	/// <summary>Fired under Overdrive. Only changes the look; the numbers are set by the shooter.</summary>
	public bool Overdriven { get; set; }

	/// <summary>Danger telegraph from the style guide — reserved for "about to hurt you".</summary>
	private static readonly Color HostileTint = new Color(1.0f, 0.30f, 0.30f);
	private static readonly Color ChipTint = new Color(1.0f, 0.95f, 0.8f);

	private Line2D trail;
    private Sprite2D art;
	private bool hasHit = false;
	private bool hostile = false;

	/// <summary>
	/// Turns this into a body's shot: it looks for the world instead of for
	/// bodies, and is tinted the one colour the palette reserves for threats.
	/// </summary>
	public void MakeHostile()
	{
		hostile = true;
		// Layer 1 is the player, layer 2 the bodies.
		CollisionMask = 1;
		Modulate = HostileTint;
	}

	public override void _Ready()
	{
        foreach(Node child in GetChildren()) if(child is Polygon2D polygon)polygon.Hide();
        art=new Sprite2D {Texture=GD.Load<Texture2D>(hostile?"res://art/cosmic/hostile.svg":"res://art/cosmic/shot.svg"),Scale=Vector2.One*(hostile?.065f:.29f)};AddChild(art);
        Modulate=Colors.White;
		BodyEntered += OnBodyEntered;

		// Not just the player's own gun: moon shots count too, since both are
		// "your side". A gravity well bending only the Comet and ignoring a moon
		// would be an arbitrary distinction nobody could learn.
		// Hostile shots are grouped too, so a Nova can sweep them out of the air.
		AddToGroup(hostile ? "hostile_bullets" : "player_bullets");

		trail = GetNodeOrNull<Line2D>("Trail");
		trail?.ClearPoints();
        if(trail!=null){trail.Width=hostile?5:6;trail.DefaultColor=hostile?new Color(1,.4f,.4f,.5f):new Color(1,.75f,.4f,.5f);}
        TrailLength=3;
        if(Overdriven)
        {
            art.Modulate=new Color(1.5f,.85f,1.1f);
            TrailLength=5;
            if(trail!=null){trail.Width=10;trail.Gradient=null;trail.DefaultColor=new Color(PlanetVisual.OverdriveColour,.6f);}
        }

		var lifetime = GetNodeOrNull<Timer>("Timer");
		if (lifetime != null)
		{
			// Lifetime is how a weapon's range is expressed: a shot is not slowed
			// down at the end of its range, it simply stops existing.
			if (Range > 0f)
				lifetime.WaitTime = Range;
			lifetime.Timeout += QueueFree;
		}
	}

	/// <summary>Seconds this shot lives for. Zero keeps the scene's own setting.</summary>
	[Export] public float Range { get; set; } = 0f;

	/// <summary>Applies a weapon's look and behaviour to this shot.</summary>
	public void ApplyProfile(WeaponProfile weapon)
	{
		Speed = weapon.Speed * (1f + RunState.Rng.RandfRange(-weapon.SpeedJitter, weapon.SpeedJitter));
		Damage = Mathf.Max(weapon.Damage, 1);
		Pierce = weapon.Pierce;
		Range = weapon.Range;
		Scale *= weapon.ShotScale;
		Modulate = weapon.Tint;
	}

	public override void _PhysicsProcess(double delta)
	{
		art.Rotation=Direction.Angle();
		GlobalPosition += Direction * Speed * (float)delta;
		if (!Arena.World.HasPoint(GlobalPosition))
		{
			QueueFree();
			return;
		}
		UpdateTrail();
	}

	/// <summary>
	/// Bends this shot toward a point. A well that pulled bodies but let shots fly
	/// straight through would not read as gravity at all — it has to bend
	/// everything or it is just a damage zone.
	/// </summary>
	public void Attract(Vector2 acceleration, float delta)
	{
		Vector2 velocity = Direction * Speed + acceleration * delta;
		float speed = velocity.Length();
		if (speed < 0.01f)
			return;

		Speed = speed;
		Direction = velocity / speed;
	}

	// The trail is top_level, so its points live in global space and do not have
	// to be un-transformed out of the bullet every frame.
	private void UpdateTrail()
	{
		if (trail == null)
			return;

		trail.AddPoint(GlobalPosition);
		while (trail.GetPointCount() > TrailLength)
			trail.RemovePoint(0);
	}

	private void OnBodyEntered(Node2D hit)
	{
		if (hasHit)
			return;

		if (hostile)
		{
			if (hit is not Player world)
				return;

			hasHit = true;
			SetDeferred(Area2D.PropertyName.Monitoring, false);
			world.KillByBlast(TranslationServer.Translate("DEATH_CAUSE_EnemyShot"));
			PopEffect.Spawn(this, GlobalPosition, 30f, HostileTint, 3, 0.3f);
			QueueFree();
			return;
		}

		if (hit is not Body body)
		{
			// Bosses take damage but are not bodies: no score, no debris, no burst.
			// What a kill is worth there is the boss's own business.
			if (hit is IShootable target)
			{
				target.TakeDamage(Damage, Direction);
				PopEffect.Spawn(this, GlobalPosition, 16f, ChipTint, 0, 0.2f);
				SpawnDamageNumber(GlobalPosition);

				if (Pierce > 0)
				{
					Pierce--;
					return;
				}

				hasHit = true;
				SetDeferred(Area2D.PropertyName.Monitoring, false);
				QueueFree();
			}

			return;
		}

		// Captured before the hit, because a lethal one queues the body for free.
		Body.Remains remains = body.GetRemains();

		// Armoured bodies survive several hits, so the kill only scores when it lands.
		// The kill's pop is GameManager's, centred on the body; a hit that only
		// chips gets a small pale tick where the shot landed.
		Vector2 bodyAt = body.GlobalPosition;
		if (body.TakeDamage(Damage, Direction))
			GameManager.Of(this)?.RegisterKill(remains, bodyAt, KillSource.Shot);
		else
			PopEffect.Spawn(this, GlobalPosition, 18f, ChipTint, 0, 0.2f);
		SpawnDamageNumber(GlobalPosition);

		// A piercing shot carries on through the clump. Area2D only reports each
		// body once per entry, so nothing can be hit twice by the same shot.
		if (Pierce > 0)
		{
			Pierce--;
			return;
		}

		// Two bodies can overlap the shot in the same frame; only the first counts.
		hasHit = true;
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		QueueFree();
	}

	private void SpawnDamageNumber(Vector2 at)
	{
		if (GameSettings.Instance?.ShowDamageNumbers != true)
			return;

		var number = new DamageNumber { Amount = Damage };
		number.GlobalPosition = at + new Vector2(RunState.Rng.RandfRange(-10f, 10f), -10f);
		GameManager.Spawn(this, number);
	}
}
