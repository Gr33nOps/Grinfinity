using Godot;

/// <summary>
/// An arena hazard: a rival source of gravity, fixed in place, that pulls
/// bodies, debris and bullets — everything the world's own gravity pulls —
/// toward itself instead. Dash is the escape; anything caught and held long
/// enough to touch the core is destroyed, world included.
///
/// This is a hazard, not a body: it never counts toward the spawn cap, is never
/// a nova's target, and sheds nothing when it eventually expires. It is a plain
/// Node2D rather than an Area2D — everything it affects is found by distance
/// check each frame (the same pattern <see cref="Body.Detonate"/> uses), so
/// there is no collision shape or scene file to wire up.
/// </summary>
public partial class GravityWell : Node2D
{
	[Export] public float Radius { get; set; } = 320.0f;
	[Export] public float CoreRadius { get; set; } = 42.0f;
	[Export] public float Strength { get; set; } = 820.0f;
	/// <summary>Seconds before the well collapses on its own.</summary>
	[Export] public float Lifetime { get; set; } = 14.0f;
	[Export] public float SpinSpeed { get; set; } = 0.6f;
	/// <summary>Pull on the world itself, far weaker than on bodies — this is a fight, not a trap.</summary>
	[Export] public float PlayerPullFactor { get; set; } = 0.16f;

	private float age;
	private Player world;
	private GameManager manager;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		world = manager?.GetNodeOrNull<Player>("player");
		AddToGroup("hazards");
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		age += step;
		Rotation += SpinSpeed * step;

		if (age >= Lifetime)
		{
			QueueFree();
			return;
		}

		PullBodies(step);
		PullDebris(step);
		PullBullets(step);
		PullWorld(step);

		// Fade in and out over the first and last second, so it never simply
		// pops into or out of existence mid-fight.
		float fade = Mathf.Min(Mathf.Min(age, Lifetime - age), 1.0f);
		Modulate = new Color(1, 1, 1, Mathf.Clamp(fade, 0.2f, 1f));

		QueueRedraw();
	}

	private void PullBodies(float delta)
	{
		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body body || !IsInstanceValid(body))
				continue;

			Vector2 toCore = GlobalPosition - body.GlobalPosition;
			float distance = toCore.Length();
			if (distance > Radius)
				continue;

			if (distance <= CoreRadius)
			{
				body.TakeDamage(9999, -toCore.Normalized());
				continue;
			}

			body.Drift += toCore.Normalized() * PullAt(distance) * delta;
		}
	}

	private void PullDebris(float delta)
	{
		foreach (Node node in GetTree().GetNodesInGroup("debris"))
		{
			if (node is not Debris mote || !IsInstanceValid(mote))
				continue;

			float distance = GlobalPosition.DistanceTo(mote.GlobalPosition);
			if (distance > Radius || distance <= CoreRadius)
				continue;

			mote.Nudge((GlobalPosition - mote.GlobalPosition).Normalized() * PullAt(distance) * delta);
		}
	}

	private void PullBullets(float delta)
	{
		// Only the player's own shots bend — a well pulling hostile shots off
		// their line would make them unreadable rather than dangerous.
		foreach (Node node in GetTree().GetNodesInGroup("player_bullets"))
		{
			if (node is not Bullet bullet || !IsInstanceValid(bullet))
				continue;

			float distance = GlobalPosition.DistanceTo(bullet.GlobalPosition);
			if (distance > Radius)
				continue;

			bullet.Attract((GlobalPosition - bullet.GlobalPosition).Normalized() * PullAt(distance) * 3.0f, delta);
		}
	}

	/// <summary>
	/// The rival pull on the player: weak, escapable by a single dash (dash sets
	/// velocity outright, so it always wins), and lethal at the core exactly like
	/// wandering into a body would be.
	/// </summary>
	private void PullWorld(float delta)
	{
		if (world == null || !IsInstanceValid(world))
			return;

		Vector2 toCore = GlobalPosition - world.GlobalPosition;
		float distance = toCore.Length();
		if (distance > Radius)
			return;

		if (distance <= CoreRadius)
		{
			world.KillByBlast(TranslationServer.Translate("DEATH_CAUSE_GravityWell"));
			return;
		}

		world.ApplyExternalPush(toCore.Normalized() * PullAt(distance) * PlayerPullFactor * delta);
	}

	/// <summary>Softened falloff, same shape as the world's own pull.</summary>
	private float PullAt(float distance) => Strength * (Radius * 0.4f) / (distance + Radius * 0.4f);

	public override void _Draw()
	{
		var core = new Color(0.2f, 0.05f, 0.3f, 0.95f);
		var ring = new Color(0.55f, 0.2f, 0.75f, 0.5f);

		DrawCircle(Vector2.Zero, CoreRadius, core);

		// Three faint spiral arcs standing in for an accretion swirl, so the pull
		// radius reads at a glance without needing a shader.
		for (int i = 0; i < 3; i++)
		{
			float start = Mathf.Tau * i / 3f;
			DrawArc(Vector2.Zero, Radius * 0.7f, start, start + Mathf.Pi * 0.7f, 24, ring, 3.0f, true);
		}

		DrawArc(Vector2.Zero, Radius, 0f, Mathf.Tau, 48, new Color(ring, 0.22f), 2.0f, true);
	}
}
