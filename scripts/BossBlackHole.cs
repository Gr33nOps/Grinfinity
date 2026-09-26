using System.Collections.Generic;
using Godot;

/// <summary>
/// The Black Hole — third boss, and the thematic centrepiece. A fight about
/// whose gravity wins, in two moves:
///
/// The event horizon. A ring forms around it and fills up over a couple of
/// seconds: walk out before it closes. Anyone still inside is dragged in harder
/// than they can walk, and only a dash breaks free; reaching the core is death.
///
/// The throw. It grabs enemies on screen, holds them spinning round itself for
/// a moment, then hurls them at the planet one after another. With nothing on
/// screen, it has nothing to throw.
///
/// Nothing else: it does not pull shots or enemies about, and any gravity well
/// on the field fades away when it arrives.
///
/// Both get quicker as it gets hurt.
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

	private enum Horizon { Idle, Forming, Pulling }

	[ExportGroup("Event horizon")]
	[Export] public float CoreRadius { get; set; } = 46.0f;
	/// <summary>How far the ring reaches from its centre.</summary>
	[Export] public float HorizonRadius { get; set; } = 540.0f;
	/// <summary>Seconds between one horizon ending and the next forming, shrinking as it gets hurt.</summary>
	[Export] public float HorizonInterval { get; set; } = 7.5f;
	[Export] public float MinHorizonInterval { get; set; } = 5.0f;
	/// <summary>Seconds to get out while the ring fills. Always long enough to walk clear.</summary>
	[Export] public float FormTime { get; set; } = 2.5f;
	[Export] public float MinFormTime { get; set; } = 2.0f;
	[Export] public float PullTime { get; set; } = 2.6f;
	/// <summary>Inward pull at the ring's edge and at the core. Walking is 250: only a dash beats it.</summary>
	[Export] public float EdgePull { get; set; } = 330.0f;
	[Export] public float CorePull { get; set; } = 520.0f;

	[ExportGroup("Throw")]
	[Export] public float ThrowInterval { get; set; } = 4.2f;
	[Export] public float MinThrowInterval { get; set; } = 2.4f;
	[Export] public int MaxThrown { get; set; } = 3;
	/// <summary>Seconds before trying again when there was nothing on screen to grab.</summary>
	[Export] public float RetryGrab { get; set; } = 1.0f;
	[Export] public float HoldTime { get; set; } = 0.8f;
	[Export] public float ThrowSpeed { get; set; } = 760.0f;
	[Export] public float ThrowFlight { get; set; } = 1.3f;
	[Export] public float ThrowGap { get; set; } = 0.22f;

	[ExportGroup("Movement")]
	[Export] public float DriftSpeed { get; set; } = 26.0f;
	[Export] public float SpinSpeed { get; set; } = 0.35f;

	private static readonly Color Danger = new("ff5a7a");

	private Player world;
	private Vector2 driftTarget;
	private Horizon horizon = Horizon.Idle;
	private float horizonLeft, time;
	private float throwTimer, holdLeft, nextThrowIn;
	private readonly List<Body> held = new();

	protected override float Size => 1.6f;

	protected override void OnBossReady()
	{
		// The core that swallows grows with the art; the horizon's reach does not.
		CoreRadius *= Size;
		world = World;

		// Its gravity is the only gravity in this fight.
		foreach (Node node in GetTree().GetNodesInGroup("hazards"))
		{
			if (node is GravityWell well && IsInstanceValid(well))
				well.Dissipate();
		}
		horizonLeft = 3.0f;
		throwTimer = 1.8f;
		PickDriftTarget();
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		time += step;

		// The swirl is the node's own Rotation; the art counter-rotates in Boss.
		Rotation += SpinSpeed * step;

		if (GlobalPosition.DistanceTo(driftTarget) < 50f)
			PickDriftTarget();
		// It holds still while its horizon is up, so the ring stays where it was drawn.
		Velocity = horizon == Horizon.Idle ? (driftTarget - GlobalPosition).Normalized() * DriftSpeed : Vector2.Zero;
		MoveAndSlide();

		UpdateHorizon(step);
		UpdateThrow(step);
		QueueRedraw();
	}

	private void PickDriftTarget()
	{
		// Slow, and always somewhere near: the anchor the fight orbits.
		driftTarget = PointNearPlayer(260f, 520f);
	}

	private bool HasWorld => world != null && IsInstanceValid(world);

	// --- Event horizon ------------------------------------------------------------

	private void UpdateHorizon(float step)
	{
		horizonLeft -= step;
		switch (horizon)
		{
			case Horizon.Idle:
				Windup = 0f;
				if (horizonLeft <= 0f)
				{
					horizon = Horizon.Forming;
					horizonLeft = CurrentFormTime;
				}
				break;

			case Horizon.Forming:
				// It glows brighter as the ring fills: the tell.
				Windup = 1f - horizonLeft / CurrentFormTime;
				if (horizonLeft <= 0f)
				{
					horizon = Horizon.Pulling;
					horizonLeft = PullTime;
				}
				break;

			case Horizon.Pulling:
				Windup = 1f;
				PullPlayer();
				if (horizonLeft <= 0f)
				{
					horizon = Horizon.Idle;
					horizonLeft = Mathf.Lerp(MinHorizonInterval, HorizonInterval, HealthFraction) * CycleTempo;
				}
				break;
		}
	}

	private float CurrentFormTime => Mathf.Lerp(MinFormTime, FormTime, HealthFraction);

	/// <summary>Inside the closed ring, the planet is dragged in harder than it can walk; a dash ignores it.</summary>
	private void PullPlayer()
	{
		if (!HasWorld)
			return;

		Vector2 toCore = GlobalPosition - world.GlobalPosition;
		float distance = toCore.Length();
		if (distance > HorizonRadius)
			return;

		if (distance <= CoreRadius)
		{
			world.KillByBlast(BossName);
			return;
		}

		float closeness = 1f - Mathf.Clamp((distance - CoreRadius) / (HorizonRadius - CoreRadius), 0f, 1f);
		world.ApplyExternalPush(toCore / distance * Mathf.Lerp(EdgePull, CorePull, closeness));
	}

	// --- Throw ------------------------------------------------------------------------

	private void UpdateThrow(float step)
	{
		held.RemoveAll(body => !IsInstanceValid(body) || body.IsDestroyed);

		if (held.Count > 0)
		{
			// Holding them in a ring round itself, turning with it.
			for (int i = 0; i < held.Count; i++)
			{
				float angle = time * 2.4f + Mathf.Tau * i / held.Count;
				held[i].Hold(GlobalPosition + Vector2.FromAngle(angle) * (CoreRadius + 70f));
			}

			if ((holdLeft -= step) > 0f)
				return;
			if ((nextThrowIn -= step) > 0f)
				return;

			Body thrown = held[0];
			held.RemoveAt(0);
			if (HasWorld)
				thrown.Throw((world.GlobalPosition - thrown.GlobalPosition).Normalized() * ThrowSpeed, ThrowFlight);
			else
				thrown.Release();
			nextThrowIn = ThrowGap;
			return;
		}

		// No new grab while its horizon is dragging: one danger at a time.
		if (horizon == Horizon.Pulling || (throwTimer -= step) > 0f)
			return;

		// Nothing on screen: nothing to throw, so look again shortly.
		throwTimer = Grab() ? Mathf.Lerp(MinThrowInterval, ThrowInterval, HealthFraction) * CycleTempo : RetryGrab;
	}

	/// <summary>Takes up to <see cref="MaxThrown"/> of the enemies on screen, nearest it first. False if there were none.</summary>
	private bool Grab()
	{
		Rect2 screen = Arena.View;
		var onScreen = new List<Body>();
		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is Body body && IsInstanceValid(body) && !body.IsDestroyed && !body.IsHeld && !body.IsThrown
				&& screen.HasPoint(body.GlobalPosition))
				onScreen.Add(body);
		}
		onScreen.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(GlobalPosition).CompareTo(b.GlobalPosition.DistanceSquaredTo(GlobalPosition)));

		for (int i = 0; i < onScreen.Count && held.Count < MaxThrown; i++)
			held.Add(onScreen[i]);

		holdLeft = HoldTime;
		nextThrowIn = 0f;
		return held.Count > 0;
	}

	/// <summary>Whatever it is still holding goes free when it falls.</summary>
	protected override void OnBossDefeated()
	{
		foreach (Body body in held)
		{
			if (IsInstanceValid(body))
				body.Release();
		}
		held.Clear();
	}

	// --- Drawing ----------------------------------------------------------------------

	public override void _Draw()
	{
		if (horizon == Horizon.Forming)
		{
			// The ring filling up: a clock for getting out.
			float filled = 1f - horizonLeft / CurrentFormTime;
			float pulse = 0.5f + 0.5f * Mathf.Sin(time * (8f + 10f * filled));
			DrawCircle(Vector2.Zero, HorizonRadius, new Color(Danger, 0.04f + 0.1f * filled));
			for (int i = 0; i < 36; i++)
			{
				float a = i * Mathf.Tau / 36f;
				DrawArc(Vector2.Zero, HorizonRadius, a, a + 0.1f, 4, new Color(Danger, 0.45f + 0.35f * pulse), 4f, true);
			}
			DrawArc(Vector2.Zero, HorizonRadius + 12f, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + Mathf.Tau * filled, 96, Danger, 6f, true);
		}
		else if (horizon == Horizon.Pulling)
		{
			// Closed: a solid ring, and streaks racing inward.
			DrawCircle(Vector2.Zero, HorizonRadius, new Color(Danger, 0.16f));
			DrawArc(Vector2.Zero, HorizonRadius, 0f, Mathf.Tau, 96, new Color(Danger, 0.9f), 6f, true);
			for (int i = 0; i < 14; i++)
			{
				float along = Mathf.PosMod(time * 1.3f + i / 14f, 1f);
				float r = Mathf.Lerp(HorizonRadius, CoreRadius + 20f, along);
				Vector2 dir = Vector2.FromAngle(i * Mathf.Tau / 14f);
				DrawLine(dir * r, dir * Mathf.Max(r - 34f, CoreRadius), new Color(Danger, 0.7f * (1f - along)), 4f, true);
			}
		}
	}
}
