using System.Collections.Generic;
using Godot;

/// <summary>
/// The Nova: a shockwave that races out from the planet and wipes out
/// everything ordinary it passes. Enemies pop as the front reaches them rather
/// than all at once, so the blast reads as travelling. It also sweeps hostile
/// shots out of the air, and lands one heavy hit on any boss it reaches.
///
/// It runs on unscaled time: kills cause hitstop, and a blast that slowed
/// itself down with its own kills would stall.
/// </summary>
public partial class NovaBlast : Node2D
{
	private static readonly Color Core = new("fff0ce");
	private static readonly Color Edge = new("f5a451");
	private static readonly Color Afterglow = new("ffd66b");

	public float MaxRadius { get; set; } = Balance.NovaRadius;

	private float age;
	private float radius;
	private ulong lastTicks;
	private readonly HashSet<ulong> bossesHit = new();
	private GameManager manager;

	private const float FadeTime = 0.35f;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		lastTicks = Time.GetTicksUsec();
		ZIndex = 5;
	}

	public override void _Process(double delta)
	{
		ulong now = Time.GetTicksUsec();
		float step = Mathf.Min((now - lastTicks) / 1_000_000f, 0.05f);
		lastTicks = now;
		age += step;

		float t = Mathf.Clamp(age / Balance.NovaExpandTime, 0f, 1f);
		radius = MaxRadius * (1f - Mathf.Pow(1f - t, 3f));

		if (t < 1f || age < Balance.NovaExpandTime + step)
			Sweep();

		if (age >= Balance.NovaExpandTime + FadeTime)
		{
			QueueFree();
			return;
		}

		QueueRedraw();
	}

	private void Sweep()
	{
		Vector2 centre = GlobalPosition;

		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is not Body body || !IsInstanceValid(body) || body.IsDestroyed)
				continue;

			Vector2 offset = body.GlobalPosition - centre;
			if (offset.Length() - Arena.RadiusOf(body) > radius)
				continue;

			Body.Remains remains = body.GetRemains();
			if (body.TakeDamage(9999, offset.Normalized(), ignoreArmour: true))
				manager?.RegisterKill(remains, body.GlobalPosition, KillSource.Nova);
		}

		foreach (Node node in GetTree().GetNodesInGroup("bosses"))
		{
			if (node is not Boss boss || !IsInstanceValid(boss) || bossesHit.Contains(boss.GetInstanceId()))
				continue;

			Vector2 offset = boss.GlobalPosition - centre;
			if (offset.Length() - Arena.RadiusOf(boss) > radius)
				continue;

			bossesHit.Add(boss.GetInstanceId());
			boss.TakeShareOfHealth(Balance.NovaBossDamage, offset.Normalized());
			manager?.SpawnImpact(boss.GlobalPosition, Afterglow);
		}

		foreach (Node node in GetTree().GetNodesInGroup("hostile_bullets"))
		{
			if (node is Node2D shot && IsInstanceValid(shot) && shot.GlobalPosition.DistanceTo(centre) <= radius)
				shot.QueueFree();
		}
	}

	public override void _Draw()
	{
		float expand = Mathf.Clamp(age / Balance.NovaExpandTime, 0f, 1f);
		float fade = age <= Balance.NovaExpandTime ? 1f : 1f - (age - Balance.NovaExpandTime) / FadeTime;
		fade = Mathf.Clamp(fade, 0f, 1f);

		// A soft fill behind the front, brightest at the start and gone quickly,
		// so the flash never hides what the blast just cleared.
		DrawCircle(Vector2.Zero, radius, new Color(Afterglow, 0.16f * (1f - expand) * fade + 0.03f * fade));
		DrawCircle(Vector2.Zero, radius * 0.35f * (1f - expand), new Color(Core, 0.5f * (1f - expand)));

		float width = Mathf.Lerp(46f, 10f, expand);
		DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 128, new Color(Edge, 0.85f * fade), width, true);
		DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 128, new Color(Core, 0.9f * fade), width * 0.35f, true);
		DrawArc(Vector2.Zero, radius * 0.82f, 0f, Mathf.Tau, 96, new Color(Afterglow, 0.35f * fade), width * 0.3f, true);
	}
}
