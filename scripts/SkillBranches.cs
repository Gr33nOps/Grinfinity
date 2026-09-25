using System.Collections.Generic;
using Godot;

/// <summary>
/// The ground the skill tree grows on: the CORE at the root, and the branches
/// joining it to each node. A branch lights up in its colour once the node at
/// its end has a rank, so the part of the tree already grown is easy to see.
/// The nodes themselves are <see cref="SkillNode"/> children, drawn over this.
/// </summary>
public partial class SkillBranches : Control
{
	public const float RootRadius = 50f;

	private static readonly Color Unlit = new(0.6f, 0.38f, 0.47f, 0.5f);
	private static readonly Color Track = new("2a1a33");
	private static readonly Color Rim = new("986077");

	/// <summary>Each link runs from a parent (null for the CORE root) up to a child.</summary>
	private readonly List<(SkillNode from, SkillNode to)> links = new();

	public Vector2 Root { get; set; }
	public float Fraction { get; set; }
	public bool IsFull { get; set; }
	public bool Complete { get; set; }

	private Texture2D core;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		core = GD.Load<Texture2D>("res://art/cosmic/icon_core.svg");
	}

	public void Link(SkillNode from, SkillNode to) => links.Add((from, to));

	private static Vector2 CentreOf(SkillNode node) => node.Position + node.Centre;

	public override void _Draw()
	{
		foreach (var (from, to) in links)
		{
			bool lit = to.Level > 0;
			Color colour = lit ? to.Profile.Colour : Unlit;
			if (to.Current == SkillNode.State.Locked)
				colour = new Color(Unlit, 0.2f);
			float width = lit ? 6f : 4f;
			Vector2 end = CentreOf(to);

			if (from == null)
			{
				// From the root: a curve that leaves the CORE towards its branch
				// and turns upward into the first node.
				Vector2 toward = new Vector2(end.X - Root.X, -140f).Normalized();
				Vector2 start = Root + toward * (RootRadius + 2f);
				Vector2 finish = end + Vector2.Down * (SkillNode.Radius + 2f);
				Vector2 bend = new(finish.X, finish.Y + (start.Y - finish.Y) * 0.7f);
				DrawPolyline(Curve(start, start + toward * 30f, bend, finish), colour, width, true);
			}
			else
			{
				Vector2 begin = CentreOf(from);
				Vector2 way = (end - begin).Normalized();
				DrawLine(begin + way * (SkillNode.Radius + 2f), end - way * (SkillNode.Radius + 2f), colour, width, true);
			}
		}

		// The root: the CORE crystal inside a ring that fills like the HUD bar.
		DrawCircle(Root, RootRadius, Track);
		DrawArc(Root, RootRadius, 0f, Mathf.Tau, 64, Rim, 3f, true);
		float share = Complete ? 1f : Fraction;
		if (share > 0.001f)
			DrawArc(Root, RootRadius - 1f, -Mathf.Pi * 0.5f, -Mathf.Pi * 0.5f + Mathf.Tau * share, 64, ArcadeSkin.Orange, IsFull ? 7f : 5f, true);
		float side = RootRadius * 1.3f;
		DrawTextureRect(core, new Rect2(Root - Vector2.One * side * 0.5f, Vector2.One * side), false);

		Font font = ArcadeSkin.Font;
		string reading = Complete ? "ALL DONE" : IsFull ? "FULL" : $"{Mathf.FloorToInt(Fraction * 100f)}%";
		var at = new Vector2(Root.X + RootRadius + 16f, Root.Y - 2f);
		DrawString(font, at, "CORE", HorizontalAlignment.Left, -1, 18, ArcadeSkin.Muted);
		DrawString(font, at + new Vector2(0f, 26f), reading, HorizontalAlignment.Left, -1, 26, IsFull ? ArcadeSkin.Orange : ArcadeSkin.Cream);
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
