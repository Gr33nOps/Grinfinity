using System.Collections.Generic;
using Godot;

/// <summary>
/// Personal space for enemies. Without it everything falls toward the planet
/// along the same lines and piles into one heap, where a crowd of faces becomes
/// an unreadable smear. Each body is nudged away from any neighbour it overlaps,
/// the smaller one giving way more, so a crowd spreads into a readable pack that
/// still comes at you just as hard.
///
/// A soft push, not a collision: bodies may still brush, and nothing ever
/// jams. Neighbours are found through a coarse grid rebuilt once per physics
/// frame, so it stays cheap at the enemy cap.
/// </summary>
public static class Crowd
{
	/// <summary>Grid cell size; at least the largest two bodies' reach, so the 3x3 search never misses a neighbour.</summary>
	private const float Cell = 220f;
	/// <summary>How close two bodies may come, as a fraction of their two radii added up.</summary>
	private const float Spacing = 0.86f;
	/// <summary>How fast an overlap is pushed apart: this many times the overlap, per second.</summary>
	private const float Firmness = 5.5f;
	private const float MaxPush = 260f;

	private static readonly Dictionary<Vector2I, List<Body>> grid = new();
	private static readonly Stack<List<Body>> spare = new();
	private static ulong builtOn = ulong.MaxValue;

	/// <summary>The velocity that eases <paramref name="body"/> out of its neighbours this frame.</summary>
	public static Vector2 Separation(Body body)
	{
		ulong frame = Engine.GetPhysicsFrames();
		if (frame != builtOn)
			Build(body.GetTree(), frame);

		Vector2 at = body.GlobalPosition;
		float radius = body.CrowdRadius;
		Vector2I home = CellOf(at);
		Vector2 push = Vector2.Zero;

		for (int x = -1; x <= 1; x++)
		for (int y = -1; y <= 1; y++)
		{
			if (!grid.TryGetValue(home + new Vector2I(x, y), out List<Body> neighbours))
				continue;
			foreach (Body other in neighbours)
			{
				if (other == body)
					continue;
				float reach = (radius + other.CrowdRadius) * Spacing;
				Vector2 apart = at - other.GlobalPosition;
				float distance = apart.Length();
				if (distance >= reach)
					continue;
				// Two bodies on the exact same spot split along a fixed, per-pair direction.
				Vector2 away = distance > 0.01f ? apart / distance : Vector2.FromAngle((body.GetInstanceId() % 628) / 100f);
				push += away * (reach - distance) * (other.CrowdRadius / (radius + other.CrowdRadius));
			}
		}

		return (push * Firmness).LimitLength(MaxPush);
	}

	private static void Build(SceneTree tree, ulong frame)
	{
		builtOn = frame;
		foreach (List<Body> list in grid.Values)
		{
			list.Clear();
			spare.Push(list);
		}
		grid.Clear();

		foreach (Node node in tree.GetNodesInGroup("bodies"))
		{
			if (node is not Body body || body.IsDestroyed)
				continue;
			Vector2I cell = CellOf(body.GlobalPosition);
			if (!grid.TryGetValue(cell, out List<Body> list))
				grid[cell] = list = spare.Count > 0 ? spare.Pop() : new List<Body>();
			list.Add(body);
		}
	}

	private static Vector2I CellOf(Vector2 at) => new(Mathf.FloorToInt(at.X / Cell), Mathf.FloorToInt(at.Y / Cell));
}
