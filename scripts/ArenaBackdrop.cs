using Godot;

/// <summary>
/// The arena's look, in three depths plus the edge:
///
/// - the sky (<c>arena_space.gdshader</c>): nebula, ribbons and far stars,
///   drawn on the screen and scrolled by a fraction of the camera's movement;
/// - distant scenery, every piece different (planets, a crescent, a galaxy,
///   a star cluster, a comet), moving at a third of camera speed, so it reads
///   as far away and gives each part of the arena its own landmark;
/// - a sparse layer of near stars that moves with the world;
/// - the edge, where the lit arena fades out into plain dark.
///
/// Everything is kept dim on purpose. Nothing here collides, and nothing may
/// compete with an enemy, a shot or a pickup for the player's eye.
/// </summary>
public partial class ArenaBackdrop : Node2D
{
	/// <summary>How fast the distant planets move relative to the camera.</summary>
	private const float FarDepth = 0.33f;
	private const float ChunkSize = 1400f;

	private static readonly Color Cream = new("fff0ce");
	/// <summary>Roughly the sky behind the far planets, for blending them back.</summary>
	private static readonly Color Sky = new("1d1530");
	/// <summary>What lies past the arena: nothing, and no light.</summary>
	private static readonly Color Void = new(0.027f, 0.016f, 0.047f, 1f);

	private ShaderMaterial sky;
	private Node2D far;

	public override void _Ready()
	{
		ZIndex = -90;
		var rng = new RandomNumberGenerator { Seed = 4242 };

		var skyLayer = new CanvasLayer { Layer = -60 };
		AddChild(skyLayer);
		sky = new ShaderMaterial { Shader = GD.Load<Shader>("res://scenes/arena_space.gdshader") };
		var rect = new ColorRect { Material = sky, MouseFilter = Control.MouseFilterEnum.Ignore };
		skyLayer.AddChild(rect);
		rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		far = new Node2D { ZIndex = -5 };
		AddChild(far);
		AddDistantBodies(rng);

		for (float x = 0f; x < Balance.ArenaSize.X; x += ChunkSize)
		{
			for (float y = 0f; y < Balance.ArenaSize.Y; y += ChunkSize)
			{
				var area = new Rect2(x, y, Mathf.Min(ChunkSize, Balance.ArenaSize.X - x), Mathf.Min(ChunkSize, Balance.ArenaSize.Y - y));
				ulong seed = rng.Randi();
				AddChild(new Painted(area, item => DrawNearStars(item, area, seed)));
			}
		}

		AddEdge();
		Follow();
	}

	private CanvasItem menuBackground;

	public override void _EnterTree()
	{
		// The autoloaded menu background would otherwise keep rendering, unseen,
		// underneath the sky for the whole run.
		menuBackground = GetNodeOrNull<CanvasLayer>("/root/Background") is CanvasLayer layer ? layer.GetChildOrNull<CanvasItem>(0) : null;
		if (menuBackground != null)
			menuBackground.Visible = false;
	}

	public override void _ExitTree()
	{
		if (menuBackground != null && IsInstanceValid(menuBackground))
			menuBackground.Visible = true;
	}

	public override void _Process(double delta)
	{
		Follow();
	}

	/// <summary>Keeps the sky and the far layer in step with the camera.</summary>
	private void Follow()
	{
		Rect2 view = Arena.View;
		sky.SetShaderParameter("camera", view.Position);
		sky.SetShaderParameter("view_size", view.Size);
		far.Position = view.GetCenter() * (1f - FarDepth);
		FadeSceneryAtEdges();
	}

	// --- Distant scenery ----------------------------------------------------------

	private enum Scenery { RingedGiant, BandedGiant, Crescent, PlanetAndMoon, Galaxy, Cluster, Comet, IceMoon, Nothing }

	/// <summary>One slot per kind, so no two pieces look alike, and one slot left empty to breathe. The crescent sits in the middle, where every run starts.</summary>
	private static readonly Scenery[] Layout =
	{
		Scenery.RingedGiant, Scenery.Cluster, Scenery.Nothing,
		Scenery.Galaxy, Scenery.Crescent, Scenery.BandedGiant,
		Scenery.Comet, Scenery.PlanetAndMoon, Scenery.IceMoon
	};

	private readonly System.Collections.Generic.List<(Node2D node, Vector2 at, float extent)> scenery = new();

	/// <summary>Scenery never gets more opaque than this. It is set dressing, not a landmark to stare at.</summary>
	private const float SceneryStrength = 0.45f;

	/// <summary>
	/// Placed in the far layer's own space, on a loose three-by-three grid. The
	/// camera's centre ranges over the arena minus half a screen, and at a third
	/// of its speed that maps to this smaller region.
	/// </summary>
	private void AddDistantBodies(RandomNumberGenerator rng)
	{
		Vector2 half = new(960f, 540f);
		Vector2 from = half * FarDepth - half;
		Vector2 to = (Balance.ArenaSize - half) * FarDepth + half;

		for (int i = 0; i < Layout.Length; i++)
		{
			var cell = new Vector2((i % 3 + 0.5f) / 3f, (i / 3 + 0.5f) / 3f);
			Vector2 at = from + (to - from) * cell + new Vector2(rng.RandfRange(-140, 140), rng.RandfRange(-90, 90));
			ulong seed = rng.Randi();
			Scenery kind = Layout[i];
			if (kind == Scenery.Nothing)
				continue;

			float extent = kind switch
			{
				Scenery.RingedGiant => 230f,
				Scenery.BandedGiant => 150f,
				Scenery.Galaxy => 190f,
				Scenery.Comet => 170f,
				Scenery.Cluster => 110f,
				Scenery.PlanetAndMoon => 110f,
				_ => 90f
			};

			var node = new Painted(new Rect2(at - Vector2.One * extent, Vector2.One * extent * 2f), item => DrawScenery(item, kind, at, seed));
			far.AddChild(node);
			scenery.Add((node, at, extent));
		}
	}

	/// <summary>
	/// Hides scenery as it nears the arena's edge, so nothing is ever seen
	/// floating out over the dark past it.
	/// </summary>
	private void FadeSceneryAtEdges()
	{
		Rect2 lit = Arena.Playable;
		foreach (var (node, at, extent) in scenery)
		{
			Vector2 seen = far.Position + at;
			float room = Mathf.Min(Mathf.Min(seen.X - lit.Position.X, lit.End.X - seen.X), Mathf.Min(seen.Y - lit.Position.Y, lit.End.Y - seen.Y)) - extent;
			node.Modulate = new Color(1f, 1f, 1f, SceneryStrength * Mathf.Clamp(room / 320f, 0f, 1f));
		}
	}

	// Each piece is flat, dusky and drawn in the menu's shapes. All of them are
	// blended well back into the sky, so none competes with the fight.
	private static Color Dim(string hex) => new Color(hex).Lerp(Sky, 0.72f);

	private static void DrawScenery(CanvasItem item, Scenery kind, Vector2 at, ulong seed)
	{
		var rng = new RandomNumberGenerator { Seed = seed };
		switch (kind)
		{
			case Scenery.RingedGiant:
				DrawRing(item, at, 115f, -0.25f, back: true);
				DrawBody(item, at, 115f, Dim("3d2a5c"));
				DrawRing(item, at, 115f, -0.25f, back: false);
				break;

			case Scenery.BandedGiant:
				DrawBody(item, at, 100f, Dim("25415a"));
				foreach (var (y, height) in new[] { (-48f, 16f), (-6f, 22f), (40f, 12f) })
					DrawBand(item, at, 100f, y, height, Dim("2f5670"));
				item.DrawArc(at, 74f, -2.75f, -1.85f, 16, new Color(Dim("6fa3b4"), 0.35f), 7f, true);
				break;

			case Scenery.Crescent:
				item.DrawCircle(at, 64f, Dim("8a6a86"));
				item.DrawCircle(at + new Vector2(24f, -12f), 60f, Sky);
				break;

			case Scenery.PlanetAndMoon:
				DrawBody(item, at, 52f, Dim("6a3450"));
				DrawBody(item, at + new Vector2(76f, -44f), 15f, Dim("7d6a8a"));
				break;

			case Scenery.IceMoon:
				DrawBody(item, at, 36f, Dim("6d6590"));
				// One shallow crater rim, off to the side. A pair of dots would read as eyes.
				item.DrawArc(at + new Vector2(10f, 9f), 11f, 0.3f, 3.6f, 12, new Color(Dim("4f4870"), 0.9f), 3f, true);
				break;

			case Scenery.Galaxy:
				item.DrawSetTransform(at, rng.RandfRange(-0.5f, 0.5f), new Vector2(1f, 0.45f));
				for (int ring = 5; ring >= 1; ring--)
					item.DrawCircle(Vector2.Zero, ring * 30f, new Color(0.62f, 0.45f, 0.72f, 0.03f));
				for (int arm = 0; arm < 2; arm++)
				{
					for (int i = 0; i < 26; i++)
					{
						Vector2 dot = Vector2.FromAngle(i * 0.22f + arm * Mathf.Pi) * (12f + i * 6f);
						item.DrawCircle(dot, 3.2f - i * 0.08f, new Color(Cream, 0.22f - i * 0.006f));
					}
				}
				item.DrawCircle(Vector2.Zero, 12f, new Color(Cream, 0.28f));
				item.DrawSetTransformMatrix(Transform2D.Identity);
				break;

			case Scenery.Cluster:
				item.DrawCircle(at, 80f, new Color(0.55f, 0.45f, 0.75f, 0.035f));
				item.DrawCircle(at, 45f, new Color(0.55f, 0.45f, 0.75f, 0.04f));
				for (int i = 0; i < 11; i++)
				{
					Vector2 star = at + Vector2.FromAngle(rng.Randf() * Mathf.Tau) * rng.RandfRange(0f, 70f);
					item.DrawColoredPolygon(StarShape(star, rng.RandfRange(2.5f, 6f)), new Color(Cream, 0.32f));
				}
				break;

			case Scenery.Comet:
				float heading = rng.RandfRange(2.4f, 2.9f);
				Vector2 back = Vector2.FromAngle(heading) * 200f;
				Vector2 side = Vector2.FromAngle(heading).Orthogonal() * 12f;
				var glow = new Color(Cream, 0.16f);
				item.DrawPolygon(new[] { at + side, at - side, at + back }, new[] { glow, glow, new Color(Cream, 0f) });
				item.DrawCircle(at, 8f, new Color(Cream, 0.35f));
				break;
		}
	}

	private static void DrawBody(CanvasItem item, Vector2 at, float radius, Color colour)
	{
		item.DrawCircle(at + new Vector2(-radius * 0.05f, -radius * 0.06f), radius, colour.Lightened(0.18f));
		item.DrawCircle(at, radius, colour);
		item.DrawArc(at, radius * 0.74f, -2.75f, -1.85f, 16, new Color(colour.Lightened(0.3f), 0.5f), Mathf.Max(radius * 0.08f, 2.5f), true);
	}

	/// <summary>A stripe across a disc, kept inside its outline.</summary>
	private static void DrawBand(CanvasItem item, Vector2 at, float radius, float y, float height, Color colour)
	{
		var points = new System.Collections.Generic.List<Vector2>();
		for (int i = 0; i <= 8; i++)
		{
			float by = y + height * i / 8f;
			points.Add(at + new Vector2(-Mathf.Sqrt(Mathf.Max(radius * radius - by * by, 0f)) * 0.98f, by));
		}
		for (int i = 8; i >= 0; i--)
		{
			float by = y + height * i / 8f;
			points.Add(at + new Vector2(Mathf.Sqrt(Mathf.Max(radius * radius - by * by, 0f)) * 0.98f, by));
		}
		item.DrawColoredPolygon(points.ToArray(), colour);
	}

	/// <summary>One half of a tilted ring: the back half goes behind the body, the front across it.</summary>
	private static void DrawRing(CanvasItem item, Vector2 centre, float radius, float tilt, bool back)
	{
		var points = new Vector2[33];
		for (int i = 0; i < points.Length; i++)
		{
			float angle = (back ? Mathf.Pi : 0f) + Mathf.Pi * i / (points.Length - 1);
			points[i] = centre + new Vector2(Mathf.Cos(angle) * radius * 1.75f, Mathf.Sin(angle) * radius * 0.4f).Rotated(tilt);
		}
		item.DrawPolyline(points, new Color(0.55f, 0.38f, 0.56f, back ? 0.12f : 0.2f), radius * 0.08f, true);
	}

	private static Vector2[] StarShape(Vector2 at, float size) => new[]
	{
		at + new Vector2(0, -size), at + new Vector2(size * 0.22f, -size * 0.22f), at + new Vector2(size, 0),
		at + new Vector2(size * 0.22f, size * 0.22f), at + new Vector2(0, size), at + new Vector2(-size * 0.22f, size * 0.22f),
		at + new Vector2(-size, 0), at + new Vector2(-size * 0.22f, -size * 0.22f)
	};

	// --- Near stars ------------------------------------------------------------

	private static void DrawNearStars(CanvasItem item, Rect2 area, ulong seed)
	{
		var rng = new RandomNumberGenerator { Seed = seed };
		int count = Mathf.RoundToInt(area.Size.X * area.Size.Y / 90000f);
		for (int i = 0; i < count; i++)
		{
			var at = new Vector2(rng.RandfRange(area.Position.X, area.End.X), rng.RandfRange(area.Position.Y, area.End.Y));
			float roll = rng.Randf();
			if (roll < 0.08f)
			{
				// The menu's four-point sparkle.
				float size = rng.RandfRange(5f, 8f);
				var colour = new Color(Cream, 0.18f);
				item.DrawColoredPolygon(new[]
				{
					at + new Vector2(0, -size), at + new Vector2(size * 0.22f, -size * 0.22f), at + new Vector2(size, 0),
					at + new Vector2(size * 0.22f, size * 0.22f), at + new Vector2(0, size), at + new Vector2(-size * 0.22f, size * 0.22f),
					at + new Vector2(-size, 0), at + new Vector2(-size * 0.22f, -size * 0.22f)
				}, colour);
			}
			else
			{
				item.DrawCircle(at, rng.RandfRange(1.1f, 2.2f), new Color(Cream, rng.RandfRange(0.08f, 0.2f)));
			}
		}
	}

	// --- The edge ------------------------------------------------------------------

	/// <summary>
	/// The arena simply runs out: a short fade that starts just inside the edge
	/// and ends in plain empty dark, with no line and no light. Nothing past it,
	/// so the edge reads as the end of the lit part of space rather than a wall.
	/// </summary>
	private void AddEdge()
	{
		Rect2 world = Arena.World.Grow(40f);
		Rect2 fadeFrom = Arena.Playable.Grow(-60f);
		Rect2 fadeTo = Arena.Playable.Grow(70f);
		var clear = new Color(Void, 0f);

		AddChild(new Painted(world, item =>
		{
			// Solid dark beyond the fade. Top and bottom span the full width.
			item.DrawRect(new Rect2(world.Position, new Vector2(world.Size.X, fadeTo.Position.Y - world.Position.Y)), Void);
			item.DrawRect(new Rect2(new Vector2(world.Position.X, fadeTo.End.Y), new Vector2(world.Size.X, world.End.Y - fadeTo.End.Y)), Void);
			item.DrawRect(new Rect2(new Vector2(world.Position.X, fadeTo.Position.Y), new Vector2(fadeTo.Position.X - world.Position.X, fadeTo.Size.Y)), Void);
			item.DrawRect(new Rect2(new Vector2(fadeTo.End.X, fadeTo.Position.Y), new Vector2(world.End.X - fadeTo.End.X, fadeTo.Size.Y)), Void);

			// The fade itself: four mitred strips, so the corners blend too.
			Vector2 a0 = fadeFrom.Position, a1 = new(fadeFrom.End.X, fadeFrom.Position.Y), a2 = fadeFrom.End, a3 = new(fadeFrom.Position.X, fadeFrom.End.Y);
			Vector2 b0 = fadeTo.Position, b1 = new(fadeTo.End.X, fadeTo.Position.Y), b2 = fadeTo.End, b3 = new(fadeTo.Position.X, fadeTo.End.Y);
			item.DrawPolygon(new[] { a0, a1, b1, b0 }, new[] { clear, clear, Void, Void });
			item.DrawPolygon(new[] { a1, a2, b2, b1 }, new[] { clear, clear, Void, Void });
			item.DrawPolygon(new[] { a2, a3, b3, b2 }, new[] { clear, clear, Void, Void });
			item.DrawPolygon(new[] { a3, a0, b0, b3 }, new[] { clear, clear, Void, Void });
		}) { ZIndex = 2 });
	}
}
