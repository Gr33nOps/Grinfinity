using Godot;

/// <summary>
/// Decides when an arena event fires and for how long.
///
/// It leaves the opening minute alone: a modifier landing before the player has
/// met the third body kind is noise, not variety. It also never runs an event
/// during a boss fight, for the same reason the spawner stands down — a boss is
/// a pattern to read, and a rule change on top makes it unreadable.
/// </summary>
public partial class EventDirector : Node
{
	[Export] public float FirstEventTime { get; set; } = 60.0f;
	/// <summary>Seconds between events, measured from the end of the last one.</summary>
	[Export] public float GapMin { get; set; } = 32.0f;
	[Export] public float GapMax { get; set; } = 55.0f;
	[Export] public float Duration { get; set; } = 20.0f;

	private RunState run;
	private GameManager manager;
	private float nextAt;
	private bool hadFirst;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		run = manager?.Run;
	}

	/// <summary>
	/// When the next event is due. FirstEventTime stays authoritative until one
	/// has actually fired, rather than being copied into a field at _Ready —
	/// copying it meant a later change to the export was silently ignored.
	/// </summary>
	private float DueAt => hadFirst ? nextAt : FirstEventTime;

	public override void _Process(double delta)
	{
		if (run == null || manager == null)
			return;

		if (run.Event != ArenaEventId.Calm)
		{
			if (run.EventTimeLeft <= 0f)
				End();
			return;
		}

		// Hold the clock while a boss is up rather than skipping the slot, so an
		// event still lands soon after the fight instead of being lost.
		if (manager.BossActive)
		{
			hadFirst = true;
			nextAt = run.SurvivalTime + GapMin;
			return;
		}

		if (run.SurvivalTime >= DueAt)
			Begin();
	}

	private void Begin()
	{
		hadFirst = true;
		ArenaEventId id = ArenaEvents.Roll();
		run.StartEvent(id, Duration);

		ArenaEvents.Profile profile = ArenaEvents.Get(id);
		manager.Announce(profile.Name, profile.Effect, profile.Colour);
		manager.Shake(0.3f);
	}

	private void End()
	{
		run.StartEvent(ArenaEventId.Calm, 0f);
		nextAt = run.SurvivalTime + (float)GD.RandRange(GapMin, GapMax);
	}
}
