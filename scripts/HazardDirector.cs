using Godot;

/// <summary>
/// Schedules the two environmental hazards — gravity wells and comet flybys —
/// independently of <see cref="EventDirector"/>. They are not announced
/// modifiers with a name and a duration; they are things that simply happen in
/// the arena, the way a comet would.
/// </summary>
public partial class HazardDirector : Node
{
	[ExportGroup("Gravity wells")]
	[Export] public float FirstWellTime { get; set; } = 75.0f;
	[Export] public float WellGapMin { get; set; } = 40.0f;
	[Export] public float WellGapMax { get; set; } = 70.0f;
	[Export] public PackedScene WellScene { get; set; }

	[ExportGroup("Comets")]
	[Export] public float FirstCometTime { get; set; } = 25.0f;
	[Export] public float CometGapMin { get; set; } = 14.0f;
	[Export] public float CometGapMax { get; set; } = 26.0f;

	private RunState run;
	private GameManager manager;
	private float nextWellAt;
	private float nextCometAt;
	private bool hadFirstWell;
	private bool hadFirstComet;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		run = manager?.Run;
	}

	public override void _Process(double delta)
	{
		if (run == null || manager == null || manager.BossActive)
			return;

		float dueWell = hadFirstWell ? nextWellAt : FirstWellTime;
		if (run.SurvivalTime >= dueWell)
			SpawnWell();

		float dueComet = hadFirstComet ? nextCometAt : FirstCometTime;
		if (run.SurvivalTime >= dueComet)
			SpawnComet();
	}

	private void SpawnWell()
	{
		hadFirstWell = true;
		nextWellAt = run.SurvivalTime + (float)GD.RandRange(WellGapMin, WellGapMax);

		WellScene ??= GD.Load<PackedScene>("res://scenes/gravity_well.tscn");
		var well = WellScene.Instantiate<GravityWell>();

		Vector2 bounds = manager.GetViewportRect().Size;
		well.GlobalPosition = new Vector2(
			(float)GD.RandRange(bounds.X * 0.2, bounds.X * 0.8),
			(float)GD.RandRange(bounds.Y * 0.2, bounds.Y * 0.8));

		manager.AddEntity(well);
		manager.Announce("GRAVITY WELL", "A rival pull has opened. Dash clear of the core.", new Color(0.72f, 0.4f, 0.9f));
	}

	private void SpawnComet()
	{
		hadFirstComet = true;
		nextCometAt = run.SurvivalTime + (float)GD.RandRange(CometGapMin, CometGapMax);

		Vector2 bounds = manager.GetViewportRect().Size;
		float margin = 160f;

		// Enters on one edge, exits roughly opposite — a straight arc across the
		// whole arena rather than a clip through one corner.
		bool horizontal = GD.Randf() < 0.5f;
		Vector2 start, end;

		if (horizontal)
		{
			float y = (float)GD.RandRange(bounds.Y * 0.15, bounds.Y * 0.85);
			bool leftToRight = GD.Randf() < 0.5f;
			start = new Vector2(leftToRight ? -margin : bounds.X + margin, y);
			end = new Vector2(leftToRight ? bounds.X + margin : -margin,
				(float)GD.RandRange(bounds.Y * 0.15, bounds.Y * 0.85));
		}
		else
		{
			float x = (float)GD.RandRange(bounds.X * 0.15, bounds.X * 0.85);
			bool topToBottom = GD.Randf() < 0.5f;
			start = new Vector2(x, topToBottom ? -margin : bounds.Y + margin);
			end = new Vector2((float)GD.RandRange(bounds.X * 0.15, bounds.X * 0.85),
				topToBottom ? bounds.Y + margin : -margin);
		}

		var comet = new CometFlyby();
		comet.Launch(start, end - start);
		manager.AddEntity(comet);
	}
}
