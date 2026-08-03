using Godot;

/// <summary>The fixed achievement list from ROADMAP.md M5.</summary>
public enum AchievementId
{
	SurviveFiveMinutes,
	HundredKills,
	Streak25,
	NoHitMinute,
	BeatCoil,
	BeatBrood,
	BeatBlackHole,
	MaxMass,
	MinMassFinish
}

public static class Achievements
{
	public sealed class Profile
	{
		public required AchievementId Id { get; init; }
		public required string Name { get; init; }
		public required string Description { get; init; }
	}

	public static readonly Profile SurviveFiveMinutes = new()
	{ Id = AchievementId.SurviveFiveMinutes, Name = "LONG ORBIT", Description = "Survive 5:00." };

	public static readonly Profile HundredKills = new()
	{ Id = AchievementId.HundredKills, Name = "CENTURY", Description = "100 kills in one orbit." };

	public static readonly Profile Streak25 = new()
	{ Id = AchievementId.Streak25, Name = "UNBROKEN", Description = "A x25 streak." };

	public static readonly Profile NoHitMinute = new()
	{ Id = AchievementId.NoHitMinute, Name = "UNTOUCHED", Description = "60 seconds without taking a hit." };

	public static readonly Profile BeatCoil = new()
	{ Id = AchievementId.BeatCoil, Name = "THREAD THE GAP", Description = "Beat The Coil." };

	public static readonly Profile BeatBrood = new()
	{ Id = AchievementId.BeatBrood, Name = "STEM THE TIDE", Description = "Beat The Brood." };

	public static readonly Profile BeatBlackHole = new()
	{ Id = AchievementId.BeatBlackHole, Name = "HEAVIER STILL", Description = "Beat The Black Hole." };

	public static readonly Profile MaxMass = new()
	{ Id = AchievementId.MaxMass, Name = "FULL PLANET", Description = "Reach maximum mass." };

	public static readonly Profile MinMassFinish = new()
	{ Id = AchievementId.MinMassFinish, Name = "FEATHERWEIGHT", Description = "Finish an orbit at minimum mass." };

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		SurviveFiveMinutes, HundredKills, Streak25, NoHitMinute,
		BeatCoil, BeatBrood, BeatBlackHole, MaxMass, MinMassFinish
	};

	public static Profile Get(AchievementId id) => All[(int)id];
}
