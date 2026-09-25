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
	[Export] public float FirstWellTime { get; set; } = 150.0f;
	[Export] public float WellGapMin { get; set; } = 40.0f;
	[Export] public float WellGapMax { get; set; } = 70.0f;
	[Export] public PackedScene WellScene { get; set; }

	[ExportGroup("Comets")]
	[Export] public float FirstCometTime { get; set; } = 60.0f;
	[Export] public float CometGapMin { get; set; } = 14.0f;
	[Export] public float CometGapMax { get; set; } = 26.0f;

	private RunState run;
	private GameManager manager;
	private float nextWellAt;
	private float nextHazardAt;
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
		if (run == null || manager == null || manager.BossActive || manager.InWaveBreak || run.SurvivalTime < nextHazardAt)
			return;

		float dueWell = hadFirstWell ? nextWellAt : FirstWellTime;
        float dueComet = hadFirstComet ? nextCometAt : FirstCometTime;
		if (manager.WaveNumber>=9 && run.SurvivalTime >= dueWell && run.Event==ArenaEventId.Calm)
			SpawnWell();

		else if (manager.WaveNumber>=5 && run.SurvivalTime >= dueComet && run.Event==ArenaEventId.Calm)
			SpawnComet();
	}

	private void SpawnWell()
	{
		hadFirstWell = true;
		nextHazardAt = run.SurvivalTime + 14f;
		nextWellAt = run.SurvivalTime + RunState.Rng.RandfRange(WellGapMin, WellGapMax);

		WellScene ??= GD.Load<PackedScene>("res://scenes/gravity_well.tscn");
		var well = WellScene.Instantiate<GravityWell>();

		Vector2 bounds = manager.GetViewportRect().Size;
		well.GlobalPosition = new Vector2(
			RunState.Rng.RandfRange(bounds.X * 0.2f, bounds.X * 0.8f),
			RunState.Rng.RandfRange(bounds.Y * 0.2f, bounds.Y * 0.8f));

		manager.AddEntity(well);
		manager.Announce("GRAVITY WELL", "A rival pull has opened. Dash clear of the core.", new Color(0.72f, 0.4f, 0.9f));
	}

	private void SpawnComet()
	{
		hadFirstComet = true;
		nextHazardAt = run.SurvivalTime + 10f;
		nextCometAt = run.SurvivalTime + RunState.Rng.RandfRange(CometGapMin, CometGapMax);

		Vector2 bounds = manager.GetViewportRect().Size;
		float margin = 160f;

		// Enters on one edge, exits roughly opposite — a straight arc across the
		// whole arena rather than a clip through one corner.
		bool horizontal = RunState.Rng.Randf() < 0.5f;
		Vector2 start, end;

		if (horizontal)
		{
			float y = RunState.Rng.RandfRange(bounds.Y * 0.15f, bounds.Y * 0.85f);
			bool leftToRight = RunState.Rng.Randf() < 0.5f;
			start = new Vector2(leftToRight ? -margin : bounds.X + margin, y);
			end = new Vector2(leftToRight ? bounds.X + margin : -margin,
				RunState.Rng.RandfRange(bounds.Y * 0.15f, bounds.Y * 0.85f));
		}
		else
		{
			float x = RunState.Rng.RandfRange(bounds.X * 0.15f, bounds.X * 0.85f);
			bool topToBottom = RunState.Rng.Randf() < 0.5f;
			start = new Vector2(x, topToBottom ? -margin : bounds.Y + margin);
			end = new Vector2(RunState.Rng.RandfRange(bounds.X * 0.15f, bounds.X * 0.85f),
				topToBottom ? bounds.Y + margin : -margin);
		}

		var comet = new CometFlyby();
		comet.Launch(start, end - start);
		manager.AddEntity(comet);
	}
}
