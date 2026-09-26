using Godot;

/// <summary>
/// A fast rock that crosses the screen on a straight line and destroys anything
/// in its path — enemies and the planet alike. Its line is shown first, so it is
/// a thing to step out of the way of, never a surprise.
///
/// A comet does not score for the player: crediting a kill nobody aimed would
/// reward standing near the crossing rather than fighting.
///
/// It is drawn to be unmistakably a comet and not one more round thing: a bright
/// teardrop head pointing where it goes, trailing a long icy tail that tapers
/// away and sheds a few sparkles. Its warning is a soft lane exactly as wide as
/// what it hits, with arrows running along it the way it will fly.
/// </summary>
public partial class CometFlyby : Node2D
{
	[Export] public float Speed { get; set; } = 1100.0f;
	[Export] public float HitRadius { get; set; } = 34.0f;
	[Export] public Color CometColor { get; set; } = new Color(0.55f, 0.86f, 1.0f);

	private const float TailLength = 330f;
	private static readonly Color Lane = new(1f, 0.45f, 0.4f);

	private Vector2 start;
	private Vector2 end;
	private Vector2 velocity;
	/// <summary>Sparkles shed from the tail: where each was dropped, and its age in seconds.</summary>
	private readonly Vector2[] sparkAt = new Vector2[14];
	private readonly float[] sparkAge = new float[14];
	private int nextSpark;
	private float time;
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
		System.Array.Fill(sparkAge, 99f);
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
		time += step;

		if (warning > 0f)
		{
			warning -= step;
			QueueRedraw();
			return;
		}

		GlobalPosition += velocity * step;
		travelled += Speed * step;

		for (int i = 0; i < sparkAge.Length; i++)
			sparkAge[i] += step;
		if (RunState.Rng.Randf() < 0.55f)
		{
			// Dropped somewhere along the first part of the tail, a little off its line.
			Vector2 back = -velocity.Normalized();
			sparkAt[nextSpark] = GlobalPosition + back * RunState.Rng.RandfRange(30f, 160f) + back.Orthogonal() * RunState.Rng.RandfRange(-18f, 18f);
			sparkAge[nextSpark] = 0f;
			nextSpark = (nextSpark + 1) % sparkAt.Length;
		}

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
		Vector2 dir = velocity.Normalized(), side = dir.Orthogonal();
		if (dir == Vector2.Zero)
			return;
		if (warning > 0f)
		{
			DrawLaneWarning(dir, side);
			return;
		}

		// The tail: a long wedge fading out behind the head, a paler core inside it.
		float r = HitRadius * 0.7f;
		DrawTail(dir, side, r * 1.0f, TailLength, new Color(CometColor, 0.6f));
		DrawTail(dir, side, r * 0.5f, TailLength * 0.7f, new Color(1f, 0.97f, 0.88f, 0.8f));

		for (int i = 0; i < sparkAt.Length; i++)
		{
			float age = sparkAge[i] / 0.45f;
			if (age >= 1f)
				continue;
			Sparkle(ToLocal(sparkAt[i]), 7f * (1f - age), new Color(1f, 0.97f, 0.88f, 1f - age));
		}

		// The head: a teardrop, round at the front and pulled back into the tail.
		DrawCircle(Vector2.Zero, r * 1.7f, new Color(0.8f, 0.95f, 1f, 0.1f));
		DrawColoredPolygon(Teardrop(dir, side, r + 3f), ArcadeSkin.Ink);
		DrawColoredPolygon(Teardrop(dir, side, r), CometColor);
		DrawCircle(dir * r * 0.2f, r * 0.62f, new Color(1f, 0.98f, 0.92f));
		DrawCircle(dir * r * 0.35f + side * r * 0.25f, r * 0.2f, Colors.White);
	}

	/// <summary>A lane as wide as what the comet hits, with arrows running along it the way it will fly.</summary>
	private void DrawLaneWarning(Vector2 dir, Vector2 side)
	{
		float urgency = 1f - warning / Balance.CometWarning;
		float pulse = 0.5f + 0.5f * Mathf.Sin(time * (8f + 14f * urgency));
		Vector2 from = ToLocal(start), to = ToLocal(end);
		float width = HitRadius;
		DrawColoredPolygon(new[] { from + side * width, to + side * width, to - side * width, from - side * width },
			new Color(Lane, 0.07f + 0.08f * pulse));
		DrawLine(from + side * width, to + side * width, new Color(Lane, 0.35f + 0.25f * pulse), 2f, true);
		DrawLine(from - side * width, to - side * width, new Color(Lane, 0.35f + 0.25f * pulse), 2f, true);

		// Chevrons stream along the lane, faster as the comet gets close.
		float gap = 120f;
		float shift = Mathf.PosMod(time * (260f + 500f * urgency), gap);
		for (float d = shift; d < length; d += gap)
		{
			Vector2 at = from + dir * d;
			float size = width * 0.55f;
			DrawPolyline(new[] { at - dir * size * 0.6f + side * size, at + dir * size * 0.4f, at - dir * size * 0.6f - side * size },
				new Color(Lane, 0.55f + 0.35f * pulse), 5f, true);
		}
	}

	private void DrawTail(Vector2 dir, Vector2 side, float width, float reach, Color colour)
	{
		var clear = new Color(colour, 0f);
		DrawPolygon(new[] { side * width, -side * width, -dir * reach },
			new[] { colour, colour, clear });
	}

	private static Vector2[] Teardrop(Vector2 dir, Vector2 side, float r)
	{
		var points = new Vector2[14];
		for (int i = 0; i < 13; i++)
		{
			float a = -Mathf.Pi * 0.5f + Mathf.Pi * i / 12f;
			points[i] = dir * Mathf.Cos(a) * r + side * Mathf.Sin(a) * r;
		}
		points[13] = -dir * r * 2.1f;
		return points;
	}

	private void Sparkle(Vector2 at, float size, Color colour)
	{
		DrawLine(at - new Vector2(size, 0f), at + new Vector2(size, 0f), colour, 2f, true);
		DrawLine(at - new Vector2(0f, size), at + new Vector2(0f, size), colour, 2f, true);
	}
}
