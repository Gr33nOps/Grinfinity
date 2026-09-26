using Godot;

/// <summary>
/// A Satellite's warning before it shoots: an orb swells at its rim, on the side
/// facing you, over the last moment before the shot leaves. The shot is fired
/// from that same spot, so the orb you saw growing is the one coming at you.
/// </summary>
public partial class ShotCharge : Node2D
{
	public const float ChargeTime = 0.5f;

	private Body body;
	private float time;

	public static void Begin(Body owner)
	{
		if (owner.GetNodeOrNull("ShotCharge") != null)
			return;
		owner.AddChild(new ShotCharge { body = owner, Name = "ShotCharge", TopLevel = true, ZIndex = 2 });
	}

	public override void _Process(double delta)
	{
		if (!IsInstanceValid(body) || body.IsDestroyed || body.BehaviourTimer > ChargeTime)
		{
			QueueFree();
			return;
		}
		time += (float)delta;
		GlobalPosition = body.MuzzleToward(body.WorldPosition);
		QueueRedraw();
	}

	public override void _Draw()
	{
		float progress = 1f - Mathf.Clamp(body.BehaviourTimer / ChargeTime, 0f, 1f);
		HostileOrb.Draw(this, Vector2.Zero, 16f * progress, time, 0.55f + 0.45f * progress);
	}
}
