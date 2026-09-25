using Godot;

/// <summary>
/// Decides when an arena event fires and for how long.
///
/// Events start well into a run, once the player has met the whole roster, and
/// never run during a boss — a boss is a pattern to read, and a rule change on
/// top makes it unreadable.
/// </summary>
public partial class EventDirector : Node
{
	/// <summary>Seconds between events, measured from the end of the last one.</summary>
	[Export] public float GapMin { get; set; } = 40.0f;
	[Export] public float GapMax { get; set; } = 70.0f;
	[Export] public float Duration { get; set; } = 18.0f;

	private RunState run;
	private GameManager manager;
	private float nextAt = Balance.FirstEventAt;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		run = manager?.Run;
	}

	public override void _Process(double delta)
	{
		if (run == null || manager == null)
			return;

		if (run.Event != ArenaEventId.Calm)
		{
			// A boss arriving ends the event early rather than stacking on it.
			if (run.EventTimeLeft <= 0f || manager.BossBusy)
				End();
			return;
		}

		if (manager.BossBusy)
		{
			nextAt = Mathf.Max(nextAt, run.SurvivalTime + GapMin * 0.5f);
			return;
		}

		if (run.SurvivalTime >= nextAt)
			Begin();
	}

	private void Begin()
	{
		ArenaEventId id = ArenaEvents.Roll();
		run.StartEvent(id, Duration);

		ArenaEvents.Profile profile = ArenaEvents.Get(id);
		manager.Announce(profile.Name, profile.Effect, profile.Colour);
		manager.Shake(0.3f);
	}

	private void End()
	{
		run.StartEvent(ArenaEventId.Calm, 0f);
		nextAt = run.SurvivalTime + RunState.Rng.RandfRange(GapMin, GapMax);
	}
}
