using Godot;

/// <summary>
/// The Coil — first boss.
///
/// It throws rings of slow shots with one safe gap, and the gap moves. There is
/// no way through except by reading where the gap will be and dashing into it,
/// which is the single skill the rest of the game never forces you to practise.
/// A pattern check, not a DPS check — see <see cref="BossBrood"/> for the other half.
/// </summary>
public partial class BossCoil : Boss
{
	public BossCoil()
	{
		BossName = "THE COIL";
		ArrivalLine = TranslationServer.Translate("BOSS_Coil_ARRIVAL");
	}

	[Export] public float SpinSpeed { get; set; } = 1.1f;
	/// <summary>Seconds between rings. Falls toward the floor as health drops.</summary>
	[Export] public float RingInterval { get; set; } = 2.4f;
	[Export] public float MinRingInterval { get; set; } = 1.1f;
	[Export] public int ShotsPerRing { get; set; } = 26;
	/// <summary>Consecutive shots omitted, making the gap the player dashes through.</summary>
	[Export] public int GapWidth { get; set; } = 4;
	[Export] public float ShotSpeed { get; set; } = 250.0f;
	[Export] public float DriftSpeed { get; set; } = 46.0f;
	[Export] public PackedScene BulletScene { get; set; }

	private float ringTimer;
	private float gapAngle;
	private Vector2 driftTarget;

	protected override void OnBossReady()
	{
		BulletScene ??= GD.Load<PackedScene>("res://scenes/bullet.tscn");
		ringTimer = 1.4f;
		gapAngle = RunState.Rng.Randf() * Mathf.Tau;
		PickDriftTarget();
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		Rotation += SpinSpeed * step;

		// A boss that sits still is a turret. Drifting between points keeps the
		// safe gap moving relative to wherever the player has settled.
		if (GlobalPosition.DistanceTo(driftTarget) < 40f)
			PickDriftTarget();

		Velocity = (driftTarget - GlobalPosition).Normalized() * DriftSpeed;
		MoveAndSlide();

		ringTimer -= step;
		if (ringTimer <= 0f)
		{
			// Wounded means faster, not merely closer to dead.
			ringTimer = Mathf.Lerp(MinRingInterval, RingInterval, HealthFraction);
			FireRing();
		}
	}

	private void PickDriftTarget()
	{
		Vector2 bounds = GetViewportRect().Size;
		driftTarget = new Vector2(
			RunState.Rng.RandfRange(bounds.X * 0.25f, bounds.X * 0.75f),
			RunState.Rng.RandfRange(bounds.Y * 0.25f, bounds.Y * 0.75f));
	}

	private void FireRing()
	{
		if (BulletScene == null)
			return;

		// The gap walks around the ring, so the safe spot is never twice in the
		// same place and standing still is never an answer.
		gapAngle += 0.9f;
		int gapStart = Mathf.PosMod(Mathf.RoundToInt(gapAngle / Mathf.Tau * ShotsPerRing), ShotsPerRing);

		for (int i = 0; i < ShotsPerRing; i++)
		{
			if (Mathf.PosMod(i - gapStart, ShotsPerRing) < GapWidth)
				continue;

			float angle = Mathf.Tau * i / ShotsPerRing + Rotation;
			var shot = BulletScene.Instantiate<Bullet>();
			shot.GlobalPosition = GlobalPosition + Vector2.FromAngle(angle) * 70f;
			shot.Direction = Vector2.FromAngle(angle);
			shot.Speed = ShotSpeed;
			shot.Range = 6.0f;
			shot.MakeHostile();
			GameManager.Spawn(this, shot);
		}
	}
}
