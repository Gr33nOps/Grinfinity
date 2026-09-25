using Godot;

/// <summary>
/// The Black Hole — third boss, and the thematic centrepiece. A rival gravity
/// well with health: while it lives, bodies fall toward *it*, your shots bend
/// toward it, and the motes you need for mass get dragged into it before you
/// can reach them. Everything <see cref="GravityWell"/> does to the arena, this
/// does to you specifically — it is the hazard made personal.
///
/// It is not a pattern check like The Coil, and not a DPS-under-pressure check
/// like The Brood. It is a fight about whose gravity wins, which is the one
/// question the rest of the game never has to ask.
/// </summary>
public partial class BossBlackHole : Boss
{
	public BossBlackHole()
	{
		BossName = "THE BLACK HOLE";
		ArrivalLine = TranslationServer.Translate("BOSS_BlackHole_ARRIVAL");
		BossColor = new Color(0.62f, 0.32f, 0.82f);
		MaxHealth = 1100;
	}

	[ExportGroup("Pull")]
	[Export] public float CoreRadius { get; set; } = 46.0f;
	[Export] public float PullRadius { get; set; } = 620.0f;
	[Export] public float BasePullStrength { get; set; } = 900.0f;
	/// <summary>How much fiercer the pull gets as health drops — it grasps harder while dying.</summary>
	[Export] public float WoundedPullMultiplier { get; set; } = 1.8f;
	[Export] public float PlayerPullFactor { get; set; } = 0.22f;

	[ExportGroup("Movement")]
	[Export] public float DriftSpeed { get; set; } = 26.0f;
	[Export] public float SpinSpeed { get; set; } = 0.35f;

	[ExportGroup("Reprisal")]
	/// <summary>What it flings back at you from everything it has pulled in. Seconds between flings.</summary>
	[Export] public float FlingInterval { get; set; } = 2.6f;
	[Export] public float MinFlingInterval { get; set; } = 1.1f;
	[Export] public float FlingSpeed { get; set; } = 480.0f;
	[Export] public PackedScene BulletScene { get; set; }

	private Player world;
	private Vector2 driftTarget;
	private float flingTimer;

	protected override void OnBossReady()
	{
		BulletScene ??= GD.Load<PackedScene>("res://scenes/bullet.tscn");
		world = World;
		flingTimer = 1.6f;
		PickDriftTarget();
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;

		// The swirl is the node's own Rotation, not a redraw — the drawn shapes
		// are static and Godot re-transforms them every frame for free.
		Rotation += SpinSpeed * step;

		if (GlobalPosition.DistanceTo(driftTarget) < 50f)
			PickDriftTarget();
		Velocity = (driftTarget - GlobalPosition).Normalized() * DriftSpeed;
		MoveAndSlide();

		// Wounded means hungrier, not weaker — the opposite of backing off.
		float pull = BasePullStrength * Mathf.Lerp(1.0f, WoundedPullMultiplier, 1.0f - HealthFraction);

		PullBodies(step, pull);
		PullDebris(step, pull);
		PullBullets(step, pull);
		PullPlayer(step, pull);

		flingTimer -= step;
        Windup=Mathf.Clamp(1-flingTimer/.45f,0,1);
		if (flingTimer <= 0f)
		{
			flingTimer = Mathf.Lerp(MinFlingInterval, FlingInterval, HealthFraction) * CycleTempo;
			FlingAtPlayer();
		}
	}

	private void PickDriftTarget()
	{
		// Slow, and always somewhere near: the anchor the fight orbits, never
		// something that can simply be walked away from.
		driftTarget = PointNearPlayer(260f, 520f);
	}

	private float PullAt(float distance, float strength) => strength * (PullRadius * 0.35f) / (distance + PullRadius * 0.35f);

	/// <summary>Bodies stop falling toward you and start falling toward it — your pull, stolen outright.</summary>
	private void PullBodies(float delta, float strength)
	{
		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body body || !IsInstanceValid(body))
				continue;

			Vector2 toCore = GlobalPosition - body.GlobalPosition;
			float distance = toCore.Length();
			if (distance > PullRadius)
				continue;

			if (distance <= CoreRadius)
			{
				body.TakeDamage(9999, -toCore.Normalized());
				continue;
			}

			body.Drift += toCore.Normalized() * PullAt(distance, strength) * delta;
		}
	}

	/// <summary>Motes you need for mass, dragged into it before you can reach them.</summary>
	private void PullDebris(float delta, float strength)
	{
		foreach (Node node in GetTree().GetNodesInGroup("debris"))
		{
			if (node is not Debris mote || !IsInstanceValid(mote))
				continue;

			float distance = GlobalPosition.DistanceTo(mote.GlobalPosition);
			if (distance > PullRadius || distance <= CoreRadius)
				continue;

			mote.Nudge((GlobalPosition - mote.GlobalPosition).Normalized() * PullAt(distance, strength) * delta);
		}
	}

	/// <summary>Your own shots, bent toward it — the same theft <see cref="GravityWell"/> commits, aimed at you specifically.</summary>
	private void PullBullets(float delta, float strength)
	{
		foreach (Node node in GetTree().GetNodesInGroup("player_bullets"))
		{
			if (node is not Bullet bullet || !IsInstanceValid(bullet))
				continue;

			float distance = GlobalPosition.DistanceTo(bullet.GlobalPosition);
			if (distance > PullRadius)
				continue;

			bullet.Attract((GlobalPosition - bullet.GlobalPosition).Normalized() * PullAt(distance, strength) * 3.0f, delta);
		}
	}

	/// <summary>
	/// The pull on the world itself. Weak and escapable by a single dash — the
	/// same asymmetry the plain gravity well hazard uses, so the lesson it
	/// taught earlier in the run pays off here.
	/// </summary>
	private void PullPlayer(float delta, float strength)
	{
		if (world == null || !IsInstanceValid(world))
			return;

		Vector2 toCore = GlobalPosition - world.GlobalPosition;
		float distance = toCore.Length();
		if (distance > PullRadius)
			return;

		if (distance <= CoreRadius)
		{
			world.KillByBlast(BossName);
			return;
		}

		world.ApplyExternalPush(toCore.Normalized() * PullAt(distance, strength) * PlayerPullFactor * delta);
	}

	/// <summary>Throws back a piece of what it has pulled in. Everything it steals, it can spend on you.</summary>
	private void FlingAtPlayer()
	{
		if (BulletScene == null || world == null || !IsInstanceValid(world))
			return;

		var shot = BulletScene.Instantiate<Bullet>();
		shot.GlobalPosition = GlobalPosition + Vector2.FromAngle(RunState.Rng.Randf() * Mathf.Tau) * 90f;
		shot.Direction = (world.GlobalPosition - shot.GlobalPosition).Normalized();
		shot.Speed = FlingSpeed;
		shot.Range = 5.0f;
		shot.MakeHostile();
		GameManager.Spawn(this, shot);
	}

    public override void _Draw()
    {
        for(int i=0;i<16;i++)DrawArc(Vector2.Zero,PullRadius,i*Mathf.Tau/16,i*Mathf.Tau/16+.12f,6,new Color(.8f,.5f,.6f,.15f),2,true);
    }
}
