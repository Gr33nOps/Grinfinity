using Godot;

public partial class Bullet : Area2D
{
	[Export] public float Speed { get; set; } = 950.0f;
	[Export] public int Damage { get; set; } = 1;
	/// <summary>Extra bodies this shot survives after the first. 0 stops on contact.</summary>
	[Export] public int Pierce { get; set; } = 0;
	[Export] public PackedScene ExplosionScene { get; set; }

	/// <summary>Points kept in the motion trail. More is longer and softer.</summary>
	[Export] public int TrailLength { get; set; } = 9;

	public Vector2 Direction { get; set; }

	/// <summary>Danger telegraph from the style guide — reserved for "about to hurt you".</summary>
	private static readonly Color HostileTint = new Color(1.0f, 0.30f, 0.30f);

	private Line2D trail;
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
		ExplosionScene ??= GD.Load<PackedScene>("res://scenes/explosion.tscn");
		BodyEntered += OnBodyEntered;

		// Not just the player's own gun: moon shots count too, since both are
		// "your side". A gravity well bending only the Comet and ignoring a moon
		// would be an arbitrary distinction nobody could learn.
		if (!hostile)
			AddToGroup("player_bullets");

		trail = GetNodeOrNull<Line2D>("Trail");
		trail?.ClearPoints();

		var lifetime = GetNodeOrNull<Timer>("Timer");
		if (lifetime != null)
		{
			// Lifetime is how a weapon's range is expressed: the Debris Cannon's
			// spread is not slow, it simply stops existing before it gets far.
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
		Damage = weapon.Damage;
		Pierce = weapon.Pierce;
		Range = weapon.Range;
		Scale *= weapon.ShotScale;
		Modulate = weapon.Tint;
	}

	public override void _PhysicsProcess(double delta)
	{
		GlobalPosition += Direction * Speed * (float)delta;
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
			world.KillByBlast();
			SpawnBurst(20, 0.6f, HostileTint, 0.6f);
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
				SpawnBurst(10, 0.4f, new Color(1.0f, 0.95f, 0.8f), 0.5f);

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
		if (body.TakeDamage(Damage, Direction))
		{
			GameManager.Of(this)?.RegisterKill(remains, GlobalPosition);
			SpawnBurst(remains.BurstAmount, remains.BurstScale, remains.BurstColor, 1.0f);
		}
		else
		{
			// A chip hit gets a small pale spark instead of a full death burst.
			SpawnBurst(14, 0.45f, new Color(1.0f, 0.95f, 0.8f), 0.55f);
		}

		// A piercing shot carries on through the clump. Area2D only reports each
		// body once per entry, so nothing can be hit twice by the same lance.
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

	private void SpawnBurst(int amount, float scale, Color color, float lifetimeScale)
	{
		if (ExplosionScene == null)
			return;

		var burst = ExplosionScene.Instantiate<CpuParticles2D>();
		burst.GlobalPosition = GlobalPosition;
		burst.Amount = Mathf.Max(amount, 1);
		// Node scale carries the particle velocities with it, so one burst scene
		// covers a swarmer pop and a tank detonation.
		burst.Scale = new Vector2(scale, scale);
		burst.Color = color;
		burst.Lifetime = RunState.Rng.RandfRange(0.5f, 0.7f) * lifetimeScale;
		burst.Emitting = true;
		// explosion.tscn is one_shot, so Finished fires once the burst is done.
		burst.Finished += burst.QueueFree;
		GameManager.Spawn(this, burst);
	}
}
