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

	private TextureButton restartButton;
	private TextureButton menuButton;
	private Label scoreLabel;
	private Label scoreCaption;
	private Label statsLabel;
	private Label stardustLabel;
	private Label worldUnlockLabel;
	private Label leaderboardLabel;
	private Label highScoreLabel;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;

	public override void _Ready()
	{
		scoreLabel = GetNode<Label>("Recap/ScoreLabel");
		scoreCaption = GetNodeOrNull<Label>("Recap/ScoreCaption");
		statsLabel = GetNodeOrNull<Label>("Recap/StatsLabel");
		stardustLabel = GetNodeOrNull<Label>("Recap/StardustLabel");
		worldUnlockLabel = GetNodeOrNull<Label>("Recap/WorldUnlockLabel");
		leaderboardLabel = GetNodeOrNull<Label>("Recap/LeaderboardLabel");
		highScoreLabel = GetNodeOrNull<Label>("Recap/HighScoreLabel");
		restartButton = GetNode<TextureButton>("GameOverMenu/RestartButton");
		menuButton = GetNode<TextureButton>("GameOverMenu/MenuButton");
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/HoverSound");
		gameOverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverMenu/GameOverSound");

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

		// Which weapon it was scored with, because comparing two orbits without
		// that is comparing nothing.
		if (scoreCaption != null)
			scoreCaption.Text = $"ORBIT SCORE  ·  {Loadout.Profile.Name}";

		if (statsLabel != null)
		{
			// Mass at death is on the recap deliberately: it is the one number
			// that says whether the risk dial was used at all.
			statsLabel.Text =
				$"{ScoreManager.FormatTime(SurvivalTimeToShow)}     KILLS: {KillsToShow}     " +
				$"STREAK: x{BestComboToShow}     MASS: {Mathf.RoundToInt(MassAtDeath * 100)}%     " +
				$"MOONS: {MoonsAtDeath}";
		}

		// The running total, not just what was earned this orbit — "one more
		// orbit" needs the balance to visibly grow, not just flash and vanish.
		if (stardustLabel != null)
			stardustLabel.Text = $"+{StardustEarned:N0} STARDUST  ·  {PlayerProfile.Stardust:N0} TOTAL";

		if (worldUnlockLabel != null)
		{
			// Both are "something new just unlocked" — the only one that can only
			// be known at this exact moment is the achievement, so they share one
			// line rather than fighting for space with their own captions.
			var lines = new System.Collections.Generic.List<string>();

			if (NewlyUnlockedWorlds.Count > 0)
				lines.Add($"NEW WORLD: {string.Join(", ", NewlyUnlockedWorlds.ConvertAll(w => w.Name))}");

			if (NewlyUnlockedAchievement != null)
				lines.Add($"ACHIEVEMENT: {NewlyUnlockedAchievement.Name}");

			worldUnlockLabel.Visible = lines.Count > 0;
			if (worldUnlockLabel.Visible)
				worldUnlockLabel.Text = string.Join("   ·   ", lines);
		}

		if (leaderboardLabel != null)
		{
			leaderboardLabel.Visible = LeaderboardRank > 0;
			if (leaderboardLabel.Visible)
				leaderboardLabel.Text = $"#{LeaderboardRank} ON THE LEADERBOARD";
		}

		if (highScoreLabel == null)
			return;

		if (IsNewBestScore)
		{
			highScoreLabel.Text = "NEW BEST!";
			highScoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.32f));
		}
		else if (IsNewBestTime)
		{
			highScoreLabel.Text = "LONGEST ORBIT YET!";
			highScoreLabel.AddThemeColorOverride("font_color", new Color(1f, 0.72f, 0.32f));
		}
		else
		{
			highScoreLabel.Text = ScoreManager.GetFormattedHighScore();
		}
	}

	private void OnRestartButtonPressed()
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
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
