using Godot;

/// <summary>
/// A fast body crosses the arena on a fixed arc and hurts anything in its
/// path — bodies included. Free spectacle, pure space, and a reminder that the
/// arena is not only about the player's own gravity.
///
/// A comet does not score for the player: it is not a shot, and crediting a
/// kill nobody aimed would make the multiplier easier to inflate by standing
/// near the crossing rather than by fighting. Bodies it destroys simply vanish;
/// only a shot or a hazard the player actually chose to use pays out.
///
/// Pure code, like <see cref="NovaWave"/> — a moving shape and a trail, drawn in
/// _Draw, needs no scene of its own.
/// </summary>
public partial class CometFlyby : Node2D
{
	[Export] public float Speed { get; set; } = 900.0f;
	[Export] public float HitRadius { get; set; } = 34.0f;
	[Export] public float CullMargin { get; set; } = 200.0f;
	[Export] public Color CometColor { get; set; } = new Color(0.75f, 0.9f, 1.0f);

	private Vector2 velocity;
	private Vector2[] tail = new Vector2[10];
	private Player world;

	/// <summary>Call before adding to the tree.</summary>
	public void Launch(Vector2 start, Vector2 direction)
	{
		GlobalPosition = start;
		velocity = direction.Normalized() * Speed;
		System.Array.Fill(tail, start);
	}

	public override void _Ready()
	{
		world = GameManager.Of(this)?.GetNodeOrNull<Player>("player");
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;
		GlobalPosition += velocity * step;

		for (int i = tail.Length - 1; i > 0; i--)
			tail[i] = tail[i - 1];
		tail[0] = GlobalPosition;

		CheckHits();

		Vector2 bounds = GetViewportRect().Size;
		if (GlobalPosition.X < -CullMargin || GlobalPosition.X > bounds.X + CullMargin ||
			GlobalPosition.Y < -CullMargin || GlobalPosition.Y > bounds.Y + CullMargin)
		{
			QueueFree();
			return;
		}

		QueueRedraw();
	}

	private void CheckHits()
	{
		if (world != null && IsInstanceValid(world) && GlobalPosition.DistanceTo(world.GlobalPosition) <= HitRadius)
		{
			world.KillByBlast(TranslationServer.Translate("DEATH_CAUSE_Comet"));
			return;
		}

		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is Body body && IsInstanceValid(body)
				&& GlobalPosition.DistanceTo(body.GlobalPosition) <= HitRadius)
			{
				body.QueueFree();
			}
		}
	}

	public override void _Draw()
	{
		for (int i = tail.Length - 1; i >= 0; i--)
		{
			float t = 1f - (float)i / tail.Length;
			var colour = new Color(CometColor, t * t);
			DrawCircle(ToLocal(tail[i]), HitRadius * 0.55f * t, colour);
		}

		DrawCircle(Vector2.Zero, HitRadius * 0.65f, Colors.White);
		DrawArc(Vector2.Zero, HitRadius * 0.65f, 0f, Mathf.Tau, 16, CometColor, 3.0f, true);
	}
}
