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

	private Button restartButton;
	private Button menuButton;
	private Label scoreLabel;
	private Label statsLabel;
	private Label deathCauseLabel;
	private Label worldUnlockLabel;
	private Label leaderboardLabel;
	private Label highScoreLabel;
	private HBoxContainer nameRow;
	private LineEdit nameField;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;

	public override void _Ready()
	{
        var rows=ArcadeSkin.Modal(this,"GAME OVER",850);
        rows.AddChild(ArcadeSkin.Label("RUN COMPLETE",22,ArcadeSkin.Orange));
        scoreLabel=ArcadeSkin.Label("0",92);scoreLabel.Name="FinalScore";rows.AddChild(scoreLabel);
        statsLabel=ArcadeSkin.Label("",25);rows.AddChild(statsLabel);
        deathCauseLabel=ArcadeSkin.Label("",22,ArcadeSkin.Muted);rows.AddChild(deathCauseLabel);
        highScoreLabel=ArcadeSkin.Label("",30,ArcadeSkin.Orange);rows.AddChild(highScoreLabel);
        leaderboardLabel=ArcadeSkin.Label("",24);rows.AddChild(leaderboardLabel);
        worldUnlockLabel=ArcadeSkin.Label("",22,ArcadeSkin.Muted);worldUnlockLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;rows.AddChild(worldUnlockLabel);
        nameRow=new HBoxContainer {Alignment=BoxContainer.AlignmentMode.Center};rows.AddChild(nameRow);
        var prompt=ArcadeSkin.Label("YOUR NAME",22);prompt.Name="Prompt";nameRow.AddChild(prompt);
        nameField=new LineEdit {Name="Field",PlaceholderText="PLAYER",MaxLength=12,CustomMinimumSize=new Vector2(260,54)};nameRow.AddChild(nameField);
        restartButton=ArcadeSkin.Button("PLAY AGAIN",()=>{},true);restartButton.Name="Retry";rows.AddChild(restartButton);
        menuButton=ArcadeSkin.Button("MAIN MENU",()=>{});menuButton.Name="ReturnToMenu";rows.AddChild(menuButton);

		buttonSound = GetNodeOrNull<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("HoverSound");
		gameOverSound = GetNodeOrNull<AudioStreamPlayer>("GameOverSound");

		gameOverSound?.Play();
        Callable.From(()=>ArcadeSkin.Pop(rows.GetParent<Control>())).CallDeferred();
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

		if (statsLabel != null)
		{
			// Mass at death is on the recap deliberately: it is the one number
			// that says whether the risk dial was used at all.
			statsLabel.Text = $"{ScoreManager.FormatTime(SurvivalTimeToShow)} SURVIVED  •  {KillsToShow} POPPED  •  BEST STREAK x{BestComboToShow}";
		}

		// A one-line "what actually got you" — a number alone is a tally, this
		// is what turns it into something you remember and try to avoid next time.
		if (deathCauseLabel != null)
		{
			deathCauseLabel.Visible = !string.IsNullOrEmpty(DeathCause);
			if (deathCauseLabel.Visible)
				deathCauseLabel.Text = $"CAUGHT BY {DeathCause.ToUpperInvariant()}. GO AGAIN!";
		}

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

		ShowNamePrompt();

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
			highScoreLabel.Text = string.Format(TranslationServer.Translate("UI_BEST_SCORE_LABEL"), ScoreManager.BestScore);
		}
	}

	/// <summary>
	/// Asks who was playing, but only at the one moment it is worth interrupting
	/// for: a run that actually placed, by someone who has not already said. A
	/// first run is never gated behind a text field, and a named player is never
	/// asked twice.
	/// </summary>
	private void ShowNamePrompt()
	{
		if (nameRow == null || nameField == null)
			return;

		bool unnamed = PlayerProfile.PlayerName == "PLAYER";
		nameRow.Visible = LeaderboardRank > 0 && unnamed;
		if (!nameRow.Visible)
			return;

		var prompt = nameRow.GetNodeOrNull<Label>("Prompt");
		if (prompt != null)
			prompt.Text = TranslationServer.Translate("UI_NAME_PROMPT");

		nameField.Text = "";
		nameField.TextChanged += OnNameTyped;
	}

	private void OnNameTyped(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return;

		PlayerProfile.SetPlayerName(text);
		// The run was recorded before it could be asked who played it, so the
		// row already on the board gets the answer retro-fitted.
		Leaderboard.RenameAt(LeaderboardRank, text);
	}

	private void OnRestartButtonPressed()
	{
		buttonSound?.Play();

		// Straight back in. "One more orbit" should cost one button, not a
		// trip back through a menu.
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
