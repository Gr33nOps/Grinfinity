using Godot;

/// <summary>
/// The ring badge. It is worn by a planet whose player has a run on the
/// leaderboard — a mark of having made the board, shown here in the game and
/// on the menu planet alike. It does nothing in play.
///
/// Two illustrated halves let the near band pass in front of the planet; one
/// of these nodes draws each half.
/// </summary>
public partial class WorldRings : Node2D
{
	[Export] public bool NearHalf { get; set; }

	private Node2D world;

	/// <summary>Whether the player's name is on the leaderboard, and so wears the ring.</summary>
	public static bool Earned => Leaderboard.Includes(PlayerProfile.PlayerName);

	public override void _Ready()
	{
		world = GetParent<Node2D>();
		TopLevel = true;
		AddChild(new Sprite2D
		{
			Texture = GD.Load<Texture2D>($"res://art/cosmic/ring_2_{(NearHalf ? "front" : "back")}.svg"),
			Scale = Vector2.One * 0.25f
		});
		// Decided once per run: a run that makes the board shows it from the next one.
		Visible = Earned;
	}

	public override void _Process(double delta)
	{
		GlobalPosition = world.GlobalPosition;
		Scale = world.Scale;
	}
}
