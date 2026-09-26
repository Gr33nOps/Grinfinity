using System.Collections.Generic;
using Godot;

/// <summary>
/// The ground the skill tree grows on, drawn as a star chart: a patch of deep
/// space with its own stars and nebula glow, faint orbits circling the CORE, and
/// the CORE itself burning at the root like a little sun. The branches are
/// constellation lines: dotted while nothing on them is bought, glowing in the
/// branch's colour once the node at their end has a rank, so the part of the
/// tree already grown is easy to see. The nodes themselves are
/// <see cref="SkillNode"/> children, drawn over this.
/// </summary>
public partial class SkillBranches : Control
{
	public const float RootRadius = 46f;

	private static readonly Color Space = new("150c22");
	private static readonly Color SpaceEdge = new("4a2d52");
	private static readonly Color Unlit = new(0.77f, 0.66f, 0.75f, 0.55f);
	private static readonly Color Track = new("241733");
	private static readonly Color Sun = new("ffb057");

	/// <summary>Each link runs from a parent (null for the CORE root) up to a child.</summary>
	private readonly List<(SkillNode from, SkillNode to)> links = new();
	private Vector2[] stars;
	private float[] starSizes, starPhases;
	private float time;

	public Vector2 Root { get; set; }
	public float Fraction { get; set; }
	public bool IsFull { get; set; }
	/// <summary>Full bars saved and waiting to be spent.</summary>
	public int Banked { get; set; }
	public bool Complete { get; set; }

	private Texture2D core;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		core = GD.Load<Texture2D>("res://art/cosmic/icon_core.svg");

		// The same sky every time the chart opens: a fixed seed, and only looks.
		var rng = new RandomNumberGenerator { Seed = 7331 };
		stars = new Vector2[110];
		starSizes = new float[stars.Length];
		starPhases = new float[stars.Length];
		for (int i = 0; i < stars.Length; i++)
		{
			stars[i] = new Vector2(rng.RandfRange(12f, CustomMinimumSize.X - 12f), rng.RandfRange(12f, CustomMinimumSize.Y - 12f));
			starSizes[i] = rng.RandfRange(0.8f, 2.2f);
			starPhases[i] = rng.Randf() * Mathf.Tau;
		}
	}

	public override void _Process(double delta)
	{
		time += (float)delta;
		QueueRedraw();
	}

	public void Link(SkillNode from, SkillNode to) => links.Add((from, to));

	private static Vector2 CentreOf(SkillNode node) => node.Position + node.Centre;

	public override void _Draw()
	{
		DrawSky();
		foreach (var (from, to) in links)
			DrawLink(from, to);
		DrawCore();
	}

	private void DrawSky()
	{
		var sky = new StyleBoxFlat { BgColor = Space, BorderColor = SpaceEdge, AntiAliasing = true };
		sky.SetBorderWidthAll(2);
		sky.SetCornerRadiusAll(22);
		DrawStyleBox(sky, new Rect2(Vector2.Zero, Size));

		// Nebula: a few big, very faint clouds of colour, soft all the way to their edges.
		Cloud(new Vector2(Size.X * 0.16f, Size.Y * 0.3f), 250f, new Color(0.55f, 0.3f, 0.75f), 0.1f);
		Cloud(new Vector2(Size.X * 0.84f, Size.Y * 0.35f), 280f, new Color(0.3f, 0.45f, 0.8f), 0.09f);
		Cloud(Root, 280f, Sun, 0.08f);

		for (int i = 0; i < stars.Length; i++)
		{
			float twinkle = 0.45f + 0.35f * Mathf.Sin(time * 1.6f + starPhases[i]);
			DrawCircle(stars[i], starSizes[i], new Color(1f, 0.95f, 0.88f, twinkle));
		}

		// Orbits around the CORE, dashed, fading outward.
		for (int ring = 1; ring <= 4; ring++)
		{
			float radius = ring * 135f;
			var colour = new Color(ArcadeSkin.Muted, 0.13f - ring * 0.02f);
			for (int dash = 0; dash < 36 + ring * 12; dash++)
			{
				float from = Mathf.Pi + dash * Mathf.Tau / (36 + ring * 12);
				DrawArc(Root, radius, from, from + 0.05f, 4, colour, 2f, true);
			}
		}
	}

	/// <summary>
	/// A glow that fades out towards its rim: stacked discs, each adding a little
	/// more colour nearer the middle, so there is no hard edge anywhere.
	/// </summary>
	private void Cloud(Vector2 at, float radius, Color colour, float strength)
	{
		const int Layers = 10;
		for (int i = 0; i < Layers; i++)
			DrawCircle(at, radius * (1f - i / (float)Layers), new Color(colour, strength / Layers));
	}

	private void DrawLink(SkillNode from, SkillNode to)
	{
		bool lit = to.Level > 0;
		bool locked = to.Current == SkillNode.State.Locked;
		Vector2 end = CentreOf(to);
		Vector2[] path;
		if (from == null)
		{
			// From the root: a curve that leaves the CORE towards its branch and
			// turns upward into the first node.
			Vector2 toward = new Vector2(end.X - Root.X, -140f).Normalized();
			Vector2 start = Root + toward * (RootRadius + 6f);
			Vector2 finish = end + Vector2.Down * (SkillNode.Radius + 6f);
			Vector2 bend = new(finish.X, finish.Y + (start.Y - finish.Y) * 0.7f);
			path = Curve(start, start + toward * 30f, bend, finish);
		}
		else
		{
			Vector2 begin = CentreOf(from);
			Vector2 way = (end - begin).Normalized();
			path = new[] { begin + way * (SkillNode.Radius + 6f), end - way * (SkillNode.Radius + 6f) };
		}

		if (lit)
		{
			// A constellation line that has been drawn in: a soft glow and a bright core.
			Color tint = to.Profile.Colour;
			DrawPolyline(path, new Color(tint, 0.2f), 14f, true);
			DrawPolyline(path, tint, 4f, true);
			return;
		}

		// Not yet: a line of small dots, fainter still while its ability is locked.
		var dot = new Color(Unlit, locked ? 0.18f : 0.6f);
		float carried = 0f;
		for (int i = 1; i < path.Length; i++)
		{
			Vector2 a = path[i - 1], b = path[i];
			float length = a.DistanceTo(b);
			for (float d = 14f - carried; d < length; d += 14f)
				DrawCircle(a.Lerp(b, d / length), 2.2f, dot);
			carried = (carried + length) % 14f;
		}
	}

	private void DrawCore()
	{
		// The CORE burns at the root like a small sun, brighter when a bar is full.
		float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3f);
		float heat = IsFull ? 1f : 0.55f;
		Cloud(Root, RootRadius * 2.2f, Sun, (0.3f + 0.1f * pulse) * heat);

		DrawCircle(Root, RootRadius, Track);
		DrawArc(Root, RootRadius, 0f, Mathf.Tau, 64, new Color(Sun, 0.45f), 3f, true);
		float share = Complete ? 1f : Fraction;
		if (share > 0.001f)
			DrawArc(Root, RootRadius - 1f, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + Mathf.Tau * share, 64, ArcadeSkin.Orange, IsFull ? 7f : 5f, true);
		float side = RootRadius * 1.3f;
		DrawTextureRect(core, new Rect2(Root - Vector2.One * side * 0.5f, Vector2.One * side), false);

		// Its reading sits centred under it.
		Font font = ArcadeSkin.Font;
		string reading = Complete ? "ALL DONE" : IsFull ? $"{Banked} READY" : $"CORE  {Mathf.FloorToInt(Fraction * 100f)}%";
		DrawString(font, new Vector2(Root.X - 150f, Root.Y + RootRadius + 34f), reading, HorizontalAlignment.Center, 300f, 24,
			IsFull ? ArcadeSkin.Orange : ArcadeSkin.Cream);
	}

	private static Vector2[] Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		var points = new Vector2[28];
		for (int i = 0; i < points.Length; i++)
		{
			float t = i / (float)(points.Length - 1);
			float u = 1f - t;
			points[i] = u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
		}
		return points;
	}
}
