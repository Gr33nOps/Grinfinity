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
	{
		Id = AchievementId.SurviveFiveMinutes,
		Name = TranslationServer.Translate("ACHIEVEMENT_SurviveFiveMinutes_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_SurviveFiveMinutes_DESC")
	};

	public static readonly Profile HundredKills = new()
	{
		Id = AchievementId.HundredKills,
		Name = TranslationServer.Translate("ACHIEVEMENT_HundredKills_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_HundredKills_DESC")
	};

	public static readonly Profile Streak25 = new()
	{
		Id = AchievementId.Streak25,
		Name = TranslationServer.Translate("ACHIEVEMENT_Streak25_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_Streak25_DESC")
	};

	public static readonly Profile NoHitMinute = new()
	{
		Id = AchievementId.NoHitMinute,
		Name = TranslationServer.Translate("ACHIEVEMENT_NoHitMinute_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_NoHitMinute_DESC")
	};

	public static readonly Profile BeatCoil = new()
	{
		Id = AchievementId.BeatCoil,
		Name = TranslationServer.Translate("ACHIEVEMENT_BeatCoil_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_BeatCoil_DESC")
	};

	public static readonly Profile BeatBrood = new()
	{
		Id = AchievementId.BeatBrood,
		Name = TranslationServer.Translate("ACHIEVEMENT_BeatBrood_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_BeatBrood_DESC")
	};

	public static readonly Profile BeatBlackHole = new()
	{
		Id = AchievementId.BeatBlackHole,
		Name = TranslationServer.Translate("ACHIEVEMENT_BeatBlackHole_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_BeatBlackHole_DESC")
	};

	public static readonly Profile MaxMass = new()
	{
		Id = AchievementId.MaxMass,
		Name = TranslationServer.Translate("ACHIEVEMENT_MaxMass_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_MaxMass_DESC")
	};

	public static readonly Profile MinMassFinish = new()
	{
		Id = AchievementId.MinMassFinish,
		Name = TranslationServer.Translate("ACHIEVEMENT_MinMassFinish_NAME"),
		Description = TranslationServer.Translate("ACHIEVEMENT_MinMassFinish_DESC")
	};

	// Declared last: static field initialisers run in source order.
	public static readonly Profile[] All =
	{
		SurviveFiveMinutes, HundredKills, Streak25, NoHitMinute,
		BeatCoil, BeatBrood, BeatBlackHole, MaxMass, MinMassFinish
	};

	public static Profile Get(AchievementId id) => All[(int)id];
}
