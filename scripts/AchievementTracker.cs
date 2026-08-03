using Godot;

/// <summary>
/// Watches the live orbit for the achievements that are earned mid-run rather
/// than at a single triggering moment — see <see cref="GameManager"/> for the
/// other kind (boss defeats, finishing at minimum mass), which fire from the
/// events that already exist for them instead of being polled here.
/// </summary>
public partial class AchievementTracker : Node
{
	[Export] public float NoHitWindow { get; set; } = 60.0f;

	private RunState run;
	private GameManager manager;
	private Player world;
	private float noHitTimer;

	public override void _Ready()
	{
		manager = GameManager.Of(this);
		run = manager?.Run;
		world = manager?.GetNodeOrNull<Player>("player");

		if (world != null)
			world.HitTaken += OnHitTaken;
	}

	public override void _ExitTree()
	{
		if (world != null && IsInstanceValid(world))
			world.HitTaken -= OnHitTaken;
	}

	private void OnHitTaken()
	{
		noHitTimer = 0f;
	}

	public override void _Process(double delta)
	{
		if (run == null || manager == null)
			return;

		noHitTimer += (float)delta;
		if (noHitTimer >= NoHitWindow)
			Unlock(AchievementId.NoHitMinute);

		if (run.SurvivalTime >= 300f)
			Unlock(AchievementId.SurviveFiveMinutes);

		if (run.Kills >= 100)
			Unlock(AchievementId.HundredKills);

		if (run.Streak >= 25)
			Unlock(AchievementId.Streak25);

		if (run.MassNormalised >= 0.999f)
			Unlock(AchievementId.MaxMass);
	}

	private void Unlock(AchievementId id)
	{
		if (!PlayerProfile.UnlockAchievement(id))
			return;

		Achievements.Profile profile = Achievements.Get(id);
		manager.Announce(profile.Name, profile.Description, new Color(1.0f, 0.72f, 0.32f));
	}
}
