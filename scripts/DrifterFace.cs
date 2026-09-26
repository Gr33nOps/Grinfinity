using Godot;

/// <summary>
/// Makes a crowd of Drifters look like a crowd rather than one rock stamped out
/// thirty times. Each one is dressed from parts (one of three rock shapes in one
/// of three colours, with one of eight unhappy moods) and then its face lives a
/// little: it blinks now and then, looks scared while it is close to the planet,
/// and looks shocked for a moment when a Drifter next to it pops. Each one has
/// its own way of being scared and of being shocked, four of each.
///
/// Looks only. The body's size, hitbox, speed and health are untouched, and the
/// picks stay off the run's seeded generator.
/// </summary>
public partial class DrifterFace : Sprite2D
{
	private static readonly string[] Shapes = { "jagged", "pebble", "lumpy" };
	private static readonly string[] Rocks = { "rose", "stone", "clay" };
	private static readonly string[] Moods = { "glum", "teary", "sulking", "sleepy", "nervous", "pouty", "sniffly", "grumbling" };

	/// <summary>Closer than this to the planet and it looks scared; it calms down again past <see cref="CalmAt"/>.</summary>
	private const float ScaredAt = 250f, CalmAt = 310f;
	/// <summary>How far a pop reaches to startle its neighbours.</summary>
	private const float StartleReach = 170f;

	/// <summary>Four ways to be scared and four to be shocked; each Drifter is given one of each.</summary>
	private static Texture2D[] scaredFaces, shockedFaces;

	private Body body;
	private Texture2D mood, blink, scared, shocked;
	private float blinkIn, blinkLeft, startledLeft;
	private bool frightened;

	/// <summary>Gives <paramref name="owner"/> a random rock and face, drawn over and inside <paramref name="art"/>.</summary>
	public static DrifterFace Dress(Body owner, Sprite2D art)
	{
		scaredFaces ??= Variants("drifter_face_scared");
		shockedFaces ??= Variants("drifter_face_shocked");

		string rock = Rocks[GD.Randi() % (uint)Rocks.Length];
		string feeling = Moods[GD.Randi() % (uint)Moods.Length];
		art.Texture = Load($"drifter_body_{Shapes[GD.Randi() % (uint)Shapes.Length]}_{rock}");

		var face = new DrifterFace
		{
			Name = "Face",
			body = owner,
			mood = Load($"drifter_face_{rock}_{feeling}"),
			blink = Load($"drifter_face_{rock}_{feeling}_blink"),
			scared = scaredFaces[GD.Randi() % (uint)scaredFaces.Length],
			shocked = shockedFaces[GD.Randi() % (uint)shockedFaces.Length],
			blinkIn = (float)GD.RandRange(1.0, 5.0)
		};
		face.Texture = face.mood;
		art.AddChild(face);
		return face;
	}

	/// <summary>Every Drifter near one that just popped looks shocked for a moment.</summary>
	public static void StartleAround(Body popped)
	{
		Vector2 at = popped.GlobalPosition;
		foreach (Node node in popped.GetTree().GetNodesInGroup("bodies"))
		{
			if (node is Body other && other != popped && other.Face != null && !other.IsDestroyed
			    && other.GlobalPosition.DistanceSquaredTo(at) < StartleReach * StartleReach)
				other.Face.startledLeft = (float)GD.RandRange(0.45, 0.8);
		}
	}

	public override void _Process(double delta)
	{
		float step = (float)delta;
		FlipH = GetParent<Sprite2D>().FlipH;

		float distance = body.GlobalPosition.DistanceTo(body.WorldPosition);
		frightened = frightened ? distance < CalmAt : distance < ScaredAt;

		if (blinkLeft > 0f)
			blinkLeft -= step;
		else if ((blinkIn -= step) <= 0f)
		{
			blinkLeft = 0.13f;
			blinkIn = (float)GD.RandRange(2.5, 6.0);
		}
		startledLeft = Mathf.Max(0f, startledLeft - step);

		Texture = startledLeft > 0f ? shocked : frightened ? scared : blinkLeft > 0f ? blink : mood;
	}

	private static Texture2D Load(string name) => GD.Load<Texture2D>($"res://art/cosmic/{name}.svg");

	private static Texture2D[] Variants(string name) => new[] { Load(name), Load($"{name}_2"), Load($"{name}_3"), Load($"{name}_4") };
}
