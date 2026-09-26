using Godot;

/// <summary>
/// Makes a crowd of Drifters look like a crowd rather than one rock stamped out
/// thirty times. Drifters are lonely: each one is dressed from parts (one of
/// three rock shapes in one of three colours, with one of eight shades of
/// loneliness) and then its face lives a little: it blinks now and then, looks
/// scared while it is close to the planet, and looks shocked for a moment when a
/// Drifter next to it pops. Its fear is its mood's own (a forlorn one dreads, a
/// weary one jolts awake, a lost one gets spiral eyes), each panics at its own
/// distance, and a few never panic at all.
///
/// Looks only. The body's size, hitbox, speed and health are untouched, and the
/// picks stay off the run's seeded generator.
/// </summary>
public partial class DrifterFace : Sprite2D
{
	private static readonly string[] Shapes = { "jagged", "pebble", "lumpy" };
	private static readonly string[] Rocks = { "rose", "stone", "clay" };
	private static readonly Color[] RockColours = { new("bc7f83"), new("a6979b"), new("c9a07e") };
	private static readonly string[] Moods = { "forlorn", "teary", "moping", "weary", "lost", "longing", "sniffly", "wistful" };

	/// <summary>
	/// How close to the planet a Drifter gets before it panics differs from one
	/// to the next, so a crowd closing in changes face a few at a time. Each calms
	/// again a little further out than it panicked.
	/// </summary>
	private const float BravestAt = 190f, JumpiestAt = 320f, CalmMargin = 60f;
	/// <summary>Share of Drifters that never panic at all and just keep their mood.</summary>
	private const float Unbothered = 0.2f;
	/// <summary>How far a pop reaches to startle its neighbours.</summary>
	private const float StartleReach = 170f;

	/// <summary>Four ways to be shocked; each Drifter is given one. Its fear face comes from its mood.</summary>
	private static Texture2D[] shockedFaces;

	private Body body;
	private Texture2D mood, blink, scared, shocked;
	private float blinkIn, blinkLeft, startledLeft, scaredAt;
	private bool frightened;

	/// <summary>Gives <paramref name="owner"/> a random rock and face, drawn over and inside <paramref name="art"/>.</summary>
	public static DrifterFace Dress(Body owner, Sprite2D art)
	{
		shockedFaces ??= Variants("drifter_face_shocked");

		int rockIndex = (int)(GD.Randi() % (uint)Rocks.Length);
		string rock = Rocks[rockIndex];
		// It pops in its own rock's colour (the colourblind palette keeps its own).
		if (GameSettings.Instance?.ColourblindMode != true)
			owner.SetBurstColour(RockColours[rockIndex]);
		string feeling = Moods[GD.Randi() % (uint)Moods.Length];
		art.Texture = Load($"drifter_body_{Shapes[GD.Randi() % (uint)Shapes.Length]}_{rock}");

		var face = new DrifterFace
		{
			Name = "Face",
			body = owner,
			mood = Load($"drifter_face_{rock}_{feeling}"),
			blink = Load($"drifter_face_{rock}_{feeling}_blink"),
			scared = GD.Randf() < Unbothered ? null : Load($"drifter_face_fear_{feeling}"),
			scaredAt = (float)GD.RandRange(BravestAt, JumpiestAt),
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
		frightened = scared != null && (frightened ? distance < scaredAt + CalmMargin : distance < scaredAt);

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
