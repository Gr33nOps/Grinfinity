using Godot;

/// <summary>
/// Schedules the two environmental hazards — gravity wells and comet flybys —
/// independently of <see cref="EventDirector"/>. They are not announced
/// modifiers; they simply happen near the planet, on screen, with a visible
/// warning first. Both stand down during boss fights.
/// </summary>
public partial class HazardDirector : Node
{
	[Export] public PackedScene WellScene { get; set; }

	private RunState run;
	private GameManager manager;
	private float nextWellAt = Balance.FirstWellAt;
	private float nextCometAt = Balance.FirstCometAt;
	private float quietUntil;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		run = manager?.Run;
	}

	public override void _Process(double delta)
	{
		if (run == null || manager == null || run.Event != ArenaEventId.Calm)
			return;

		float now = run.SurvivalTime;
		// No gravity well while a Black Hole is out: the only gravity there is its own.
		if (IsInstanceValid(blackHole) || FindBlackHole())
			nextWellAt = Mathf.Max(nextWellAt, now + 12f);
		if (manager.BossBusy)
		{
			// Hold the clocks rather than firing the moment the fight ends.
			nextWellAt = Mathf.Max(nextWellAt, now + 12f);
			nextCometAt = Mathf.Max(nextCometAt, now + 8f);
			return;
		}

		if (now < quietUntil)
			return;

		if (now >= nextWellAt)
			SpawnWell(now);
		else if (now >= nextCometAt)
			SpawnComet(now);
	}

	private BossBlackHole blackHole;

	private bool FindBlackHole()
	{
		foreach (Node node in GetTree().GetNodesInGroup("bosses"))
		{
			if (node is BossBlackHole hole && IsInstanceValid(hole))
			{
				blackHole = hole;
				return true;
			}
		}
		return false;
	}

	private void SpawnWell(float now)
	{
		nextWellAt = now + RunState.Rng.RandfRange(Balance.WellGapMin, Balance.WellGapMax);
		quietUntil = now + 12f;

		var player = manager.GetNode<Node2D>("player");
		Rect2 view = Arena.View.Grow(-160f);
		Vector2 at = player.GlobalPosition;
		for (int attempt = 0; attempt < 12 && at.DistanceTo(player.GlobalPosition) < 520f; attempt++)
			at = new Vector2(RunState.Rng.RandfRange(view.Position.X, view.End.X), RunState.Rng.RandfRange(view.Position.Y, view.End.Y));
		if (at.DistanceTo(player.GlobalPosition) < 520f)
			return;

		WellScene ??= GD.Load<PackedScene>("res://scenes/gravity_well.tscn");
		var well = WellScene.Instantiate<GravityWell>();
		well.GlobalPosition = Arena.ClampToPlayable(at, 200f);
		manager.AddEntity(well);
		manager.Toast("GRAVITY WELL!  DASH OUT OF ITS PULL", new Color(0.72f, 0.4f, 0.9f));
	}

	/// <summary>A straight run across the screen, edge to far edge, with its path shown first.</summary>
	private void SpawnComet(float now)
	{
		nextCometAt = now + RunState.Rng.RandfRange(Balance.CometGapMin, Balance.CometGapMax);
		quietUntil = now + 6f;

		Rect2 view = Arena.View;
		float margin = 180f;
		bool horizontal = RunState.Rng.Randf() < 0.5f;
		Vector2 start, end;

		if (horizontal)
		{
			bool leftToRight = RunState.Rng.Randf() < 0.5f;
			float left = view.Position.X - margin, right = view.End.X + margin;
			start = new Vector2(leftToRight ? left : right, RunState.Rng.RandfRange(view.Position.Y + view.Size.Y * 0.15f, view.End.Y - view.Size.Y * 0.15f));
			end = new Vector2(leftToRight ? right : left, RunState.Rng.RandfRange(view.Position.Y + view.Size.Y * 0.15f, view.End.Y - view.Size.Y * 0.15f));
		}
		else
		{
			bool topToBottom = RunState.Rng.Randf() < 0.5f;
			float top = view.Position.Y - margin, bottom = view.End.Y + margin;
			start = new Vector2(RunState.Rng.RandfRange(view.Position.X + view.Size.X * 0.15f, view.End.X - view.Size.X * 0.15f), topToBottom ? top : bottom);
			end = new Vector2(RunState.Rng.RandfRange(view.Position.X + view.Size.X * 0.15f, view.End.X - view.Size.X * 0.15f), topToBottom ? bottom : top);
		}

		var comet = new CometFlyby();
		comet.Launch(start, end);
		manager.AddEntity(comet);
	}
}
