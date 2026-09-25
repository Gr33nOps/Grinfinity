using Godot;

/// <summary>
/// Where things are allowed to be. The world is a fixed rectangle larger than the
/// screen; the camera shows part of it and never looks past its edge.
///
/// <see cref="View"/> is written by <see cref="GameCamera"/> every frame, the same
/// static shortcut <see cref="RunState.ElapsedSeconds"/> takes, so spawners and
/// effects can ask "is this on screen?" without a camera reference.
/// </summary>
public static class Arena
{
	/// <summary>The whole world, and the camera's limits.</summary>
	public static Rect2 World => new(Vector2.Zero, Balance.ArenaSize);

	/// <summary>Inside the perimeter belt. The planet, enemies and bosses stay in here.</summary>
	public static Rect2 Playable => World.Grow(-Balance.ArenaBorder);

	public static Vector2 Centre => Balance.ArenaSize * 0.5f;

	/// <summary>The part of the world currently on screen.</summary>
	public static Rect2 View { get; set; } = new(Vector2.Zero, new Vector2(1920f, 1080f));

	public static Vector2 ClampToPlayable(Vector2 point, float margin = 0f)
	{
		Rect2 area = Playable.Grow(-margin);
		return point.Clamp(area.Position, area.End);
	}

	/// <summary>True if the point is on screen or within <paramref name="margin"/> of it.</summary>
	public static bool IsNearView(Vector2 point, float margin = 0f) => View.Grow(margin).HasPoint(point);

	/// <summary>
	/// A place for an enemy to enter: just outside the screen, inside the arena,
	/// and never close to the planet.
	///
	/// When the planet is backed into a corner, most of the space beyond the
	/// screen is outside the arena. Enemies then come in from the visible stretch
	/// of the perimeter belt instead, far from the planet, so a corner is no
	/// safer than open space.
	/// </summary>
	public static bool TryFindSpawnPoint(Vector2 player, out Vector2 point)
	{
		Rect2 inside = Playable.Grow(-30f);
		Rect2 ring = View.Grow(Balance.SpawnBeyondView);

		for (int attempt = 0; attempt < 16; attempt++)
		{
			Vector2 candidate = PointOnEdge(ring, RunState.Rng.RandfRange(0f, 220f));
			if (inside.HasPoint(candidate) && candidate.DistanceTo(player) >= Balance.SpawnMinDistance)
			{
				point = candidate;
				return true;
			}
		}

		for (int attempt = 0; attempt < 16; attempt++)
		{
			Vector2 candidate = PointOnEdge(inside, 0f);
			if (candidate.DistanceTo(player) >= Balance.SpawnMinDistance * 0.85f)
			{
				point = candidate;
				return true;
			}
		}

		point = default;
		return false;
	}

	/// <summary>A random point on a rectangle's outline, pushed <paramref name="depth"/> further out.</summary>
	private static Vector2 PointOnEdge(Rect2 rect, float depth)
	{
		// Weighted by edge length, so a wide screen does not crowd the short sides.
		float perimeter = 2f * (rect.Size.X + rect.Size.Y);
		float along = RunState.Rng.RandfRange(0f, perimeter);

		if (along < rect.Size.X)
			return new Vector2(rect.Position.X + along, rect.Position.Y - depth);
		along -= rect.Size.X;
		if (along < rect.Size.Y)
			return new Vector2(rect.End.X + depth, rect.Position.Y + along);
		along -= rect.Size.Y;
		if (along < rect.Size.X)
			return new Vector2(rect.Position.X + along, rect.End.Y + depth);
		along -= rect.Size.X;
		return new Vector2(rect.Position.X - depth, rect.Position.Y + along);
	}

	/// <summary>
	/// Collision radius of a body, boss or the planet in world units: the first
	/// circle shape found, times the node's scale.
	/// </summary>
	public static float RadiusOf(Node2D node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is CollisionShape2D { Shape: CircleShape2D circle } shape)
				return circle.Radius * Mathf.Abs(shape.GlobalScale.X);
		}

		return 40f * Mathf.Abs(node.GlobalScale.X);
	}
}
