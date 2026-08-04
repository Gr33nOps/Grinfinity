using Godot;

public partial class GameOver : Control
{
	public static float SurvivalTimeToShow = 0f;
	public static int KillsToShow = 0;
	public static int BestComboToShow = 0;
	public static int ScoreToShow = 0;
	/// <summary>Mass at death, 0..1. Tells the player whether the risk dial got used.</summary>
	public static float MassAtDeath = 0f;
	/// <summary>Moons still held when the orbit ended — whether going heavy paid.</summary>
	public static int MoonsAtDeath = 0;
	public static int StardustEarned = 0;
	public static System.Collections.Generic.List<Worlds.Profile> NewlyUnlockedWorlds = new();
	/// <summary>The one achievement that can only be known at the exact moment an orbit ends.</summary>
	public static Achievements.Profile NewlyUnlockedAchievement = null;
	/// <summary>1-based leaderboard placement this orbit earned, or -1 if it did not place.</summary>
	public static int LeaderboardRank = -1;
	public static bool IsNewBestTime = false;
	public static bool IsNewBestScore = false;
	/// <summary>What ended the orbit — a body kind, a boss, a hazard. Empty for a clean, non-death ending (Flyby's clock, giving up).</summary>
	public static string DeathCause = "";

	private TextureButton restartButton;
	private TextureButton menuButton;
	private Label scoreLabel;
	private Label scoreCaption;
	private Label statsLabel;
	private Label deathCauseLabel;
	private Label stardustLabel;
	private Label worldUnlockLabel;
	private Label leaderboardLabel;
	private Label highScoreLabel;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;

	public override void _Ready()
	{
		scoreLabel = GetNode<Label>("Layout/Recap/ScoreLabel");
		scoreCaption = GetNodeOrNull<Label>("Layout/Recap/ScoreCaption");
		statsLabel = GetNodeOrNull<Label>("Layout/Recap/StatsLabel");
		deathCauseLabel = GetNodeOrNull<Label>("Layout/Recap/DeathCauseLabel");
		stardustLabel = GetNodeOrNull<Label>("Layout/Recap/StardustLabel");
		worldUnlockLabel = GetNodeOrNull<Label>("Layout/Recap/WorldUnlockLabel");
		leaderboardLabel = GetNodeOrNull<Label>("Layout/Recap/LeaderboardLabel");
		highScoreLabel = GetNodeOrNull<Label>("Layout/Recap/HighScoreLabel");
		restartButton = GetNode<TextureButton>("Layout/Buttons/RestartButton");
		menuButton = GetNode<TextureButton>("Layout/Buttons/MenuButton");
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("HoverSound");
		gameOverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverSound");

		gameOverSound?.Play();
		ShowRecap();

		restartButton.Pressed += OnRestartButtonPressed;
		menuButton.Pressed += OnMenuButtonPressed;
		restartButton.MouseEntered += PlayHoverSound;
		menuButton.MouseEntered += PlayHoverSound;

		restartButton.GrabFocus();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void ShowRecap()
	{
		scoreLabel.Text = $"{ScoreToShow:N0}";

		// Which mode and weapon it was scored with, because comparing two
		// orbits without that is comparing nothing.
		if (scoreCaption != null)
			scoreCaption.Text = $"{Loadout.ModeProfile.Name}  ·  {Loadout.Profile.Name}";

		if (statsLabel != null)
		{
			// Mass at death is on the recap deliberately: it is the one number
			// that says whether the risk dial was used at all.
			statsLabel.Text = string.Format(TranslationServer.Translate("UI_RECAP_STATS"),
				ScoreManager.FormatTime(SurvivalTimeToShow), KillsToShow, BestComboToShow,
				Mathf.RoundToInt(MassAtDeath * 100), MoonsAtDeath);
		}

		// A one-line "what actually got you" — a number alone is a tally, this
		// is what turns it into something you remember and try to avoid next time.
		if (deathCauseLabel != null)
		{
			deathCauseLabel.Visible = !string.IsNullOrEmpty(DeathCause);
			if (deathCauseLabel.Visible)
				deathCauseLabel.Text = string.Format(TranslationServer.Translate("UI_DEATH_CAUSE"), DeathCause);
		}

		// The running total, not just what was earned this orbit — "one more
		// orbit" needs the balance to visibly grow, not just flash and vanish.
		if (stardustLabel != null)
			stardustLabel.Text = string.Format(TranslationServer.Translate("UI_STARDUST_LINE"),
				StardustEarned.ToString("N0"), PlayerProfile.Stardust.ToString("N0"));

		if (worldUnlockLabel != null)
		{
			// Both are "something new just unlocked" — the only one that can only
			// be known at this exact moment is the achievement, so they share one
			// line rather than fighting for space with their own captions.
			var lines = new System.Collections.Generic.List<string>();

			if (NewlyUnlockedWorlds.Count > 0)
				lines.Add(string.Format(TranslationServer.Translate("UI_NEW_WORLD"),
					string.Join(", ", NewlyUnlockedWorlds.ConvertAll(w => w.Name))));

			if (NewlyUnlockedAchievement != null)
				lines.Add(string.Format(TranslationServer.Translate("UI_NEW_ACHIEVEMENT"), NewlyUnlockedAchievement.Name));

			worldUnlockLabel.Visible = lines.Count > 0;
			if (worldUnlockLabel.Visible)
				worldUnlockLabel.Text = string.Join("   ·   ", lines);
		}

		if (leaderboardLabel != null)
		{
			leaderboardLabel.Visible = LeaderboardRank > 0;
			if (leaderboardLabel.Visible)
				leaderboardLabel.Text = string.Format(TranslationServer.Translate("UI_LEADERBOARD_RANK"), LeaderboardRank);
		}

		if (highScoreLabel == null)
			return;

		if (IsNewBestScore)
		{
			highScoreLabel.Text = TranslationServer.Translate("UI_NEW_BEST");
			highScoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.32f));
		}
		else if (IsNewBestTime)
		{
			highScoreLabel.Text = TranslationServer.Translate("UI_LONGEST_YET");
			highScoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.32f));
		}
		else
		{
			highScoreLabel.Text = string.Format(TranslationServer.Translate("UI_BEST_SCORE_LABEL"), ScoreManager.BestScoreFor(Loadout.Mode));
		}
	}

	private void OnRestartButtonPressed()
	{
		buttonSound?.Play();

		// Daily Alignment has no restart — today's one attempt is already
		// spent. Mode select is the honest next stop, not straight back in.
		SceneTransition.Instance.ChangeScene(Loadout.Mode == GameMode.DailyAlignment
			? "res://scenes/mode_select.tscn"
			: "res://scenes/game.tscn");
	}

	private void OnMenuButtonPressed()
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}

	private void PlayHoverSound()
	{
		hoverSound?.Play();
	}
}
