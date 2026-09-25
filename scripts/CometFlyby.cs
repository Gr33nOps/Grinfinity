using Godot;

/// <summary>
/// A fast rock that crosses the screen on a straight line and destroys anything
/// in its path — enemies and the planet alike. Its line is shown first, so it is
/// a thing to step out of the way of, never a surprise.
///
/// A comet does not score for the player: crediting a kill nobody aimed would
/// reward standing near the crossing rather than fighting.
/// </summary>
public partial class CometFlyby : Node2D
{
	[Export] public float Speed { get; set; } = 1100.0f;
	[Export] public float HitRadius { get; set; } = 34.0f;
	[Export] public Color CometColor { get; set; } = new Color(0.75f, 0.9f, 1.0f);

	private Vector2 start;
	private Vector2 end;
	private Vector2 velocity;
	private readonly Vector2[] tail = new Vector2[10];
	private Player world;
	private float warning = Balance.CometWarning;
	private float travelled;
	private float length;

	public Vector2 PathStart => start;
	public Vector2 PathEnd => end;

	/// <summary>Call before adding to the tree.</summary>
	public void Launch(Vector2 from, Vector2 to)
	{
		start = from;
		end = to;
		GlobalPosition = from;
		length = from.DistanceTo(to);
		velocity = (to - from).Normalized() * Speed;
		System.Array.Fill(tail, from);
	}

	public override void _Ready()
	{
		world = GameManager.Of(this)?.GetNodeOrNull<Player>("player");
		ZIndex = 4;
		// Heard as well as seen: the line can be off to one side of where the
		// player is looking.
		GameManager.Of(this)?.PlayCue("comet_warning");
	}

	public override void _PhysicsProcess(double delta)
	{
		float step = (float)delta;

		if (warning > 0f)
		{
			warning -= step;
			QueueRedraw();
			return;
		}

		GlobalPosition += velocity * step;
		travelled += Speed * step;

		for (int i = tail.Length - 1; i > 0; i--)
			tail[i] = tail[i - 1];
		tail[0] = GlobalPosition;

		CheckHits();

		if (travelled > length + 200f)
		{
			QueueFree();
			return;
		}

		QueueRedraw();
	}

	private void CheckHits()
	{
		if (world != null && IsInstanceValid(world) && GlobalPosition.DistanceTo(world.GlobalPosition) <= HitRadius + Arena.RadiusOf(world) * 0.6f)
			world.KillByBlast(TranslationServer.Translate("DEATH_CAUSE_Comet"));

		foreach (Node node in GetTree().GetNodesInGroup("bodies"))
		{
			if (node is Body body && IsInstanceValid(body) && !body.IsDestroyed
				&& GlobalPosition.DistanceTo(body.GlobalPosition) <= HitRadius + Arena.RadiusOf(body))
			{
				body.TakeDamage(9999, velocity.Normalized(), ignoreArmour: true);
			}
		}
	}

	public override void _Draw()
	{
		if (warning > 0f)
		{
			// Dashes along the path, blinking faster as the comet gets close.
			float blink = Mathf.Sin((Balance.CometWarning - warning) * 22f) > 0f ? 0.6f : 0.3f;
			var colour = new Color(1f, 0.45f, 0.4f, blink);
			Vector2 from = ToLocal(start), to = ToLocal(end);
			Vector2 dir = (to - from).Normalized();
			for (float d = 0f; d < length; d += 70f)
				DrawLine(from + dir * d, from + dir * Mathf.Min(d + 38f, length), colour, 6f, true);
			return;
		}

		for (int i = tail.Length - 1; i >= 0; i--)
		{
			float t = 1f - (float)i / tail.Length;
			DrawCircle(ToLocal(tail[i]), HitRadius * 0.55f * t, new Color(CometColor, t * t));
		}

		DrawCircle(Vector2.Zero, HitRadius * 0.65f, Colors.White);
		DrawArc(Vector2.Zero, HitRadius * 0.65f, 0f, Mathf.Tau, 16, CometColor, 3.0f, true);
	}
}
