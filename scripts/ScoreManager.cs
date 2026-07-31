using Godot;

public partial class ScoreManager : Node
{
	/// <summary>Raised when a streak crosses one of <see cref="StreakMilestones"/>.</summary>
	[Signal]
	public delegate void StreakMilestoneEventHandler(int combo);

	/// <summary>Streak lengths worth shouting about, ascending.</summary>
	private static readonly int[] StreakMilestones = { 5, 10, 25, 50, 100 };

	/// <summary>Resting streak colour — the UI accent from the style guide.</summary>
	private static readonly Color ComboIdle = new Color(1.0f, 0.72f, 0.32f);
	private static readonly Color ComboFlash = Colors.White;

	private const string SavePath = "user://highscore.cfg";
	private const string LegacySavePath = "user://highscore.save";
	private const string Section = "score";
	private const int SaveVersion = 2;
	private const float MaxPlausibleTime = 36000.0f;

	/// <summary>How long a streak survives without a kill before it resets.</summary>
	private const float ComboWindow = 2.5f;

	private static float bestTime = 0.0f;
	private static int bestKills = 0;
	private static int bestCombo = 0;
	private static bool isLoaded = false;

	private float survivalTime = 0.0f;
	private int kills = 0;
	private int combo = 0;
	private int runBestCombo = 0;
	private float comboTimer = 0.0f;
	private int nextMilestone = 0;

	private Label scoreLabel;
	private Label highScoreLabel;
	private Label killsLabel;
	private Label comboLabel;
	private Tween comboPop;

	public override void _Ready()
	{
		EnsureLoaded();

		var gameRoot = GetParent();
		scoreLabel = gameRoot?.GetNodeOrNull<Label>("UI/ScoreLabel");
		highScoreLabel = gameRoot?.GetNodeOrNull<Label>("UI/HighScoreLabel");
		killsLabel = gameRoot?.GetNodeOrNull<Label>("UI/KillsLabel");
		comboLabel = gameRoot?.GetNodeOrNull<Label>("UI/ComboLabel");

		// The best time cannot change mid-run, so it only needs writing once.
		if (highScoreLabel != null)
			highScoreLabel.Text = GetFormattedHighScore();

		// Scale tweens have to grow from the middle of the banner, not its corner.
		if (comboLabel != null)
			comboLabel.PivotOffset = comboLabel.Size * 0.5f;

		RefreshKills();
		RefreshCombo();
	}

	public override void _Process(double delta)
	{
		survivalTime += (float)delta;

		if (scoreLabel != null)
			scoreLabel.Text = FormatTime(survivalTime);

		if (comboTimer > 0)
		{
			comboTimer -= (float)delta;
			if (comboTimer <= 0 && combo > 0)
			{
				combo = 0;
				nextMilestone = 0;
				RefreshCombo();
			}
		}
	}

	public void AddKill()
	{
		kills++;
		combo++;
		comboTimer = ComboWindow;

		if (combo > runBestCombo)
			runBestCombo = combo;

		RefreshKills();
		RefreshCombo();

		bool milestone = nextMilestone < StreakMilestones.Length
			&& combo >= StreakMilestones[nextMilestone];

		// A milestone gets a much bigger punch so it reads without being counted.
		PopCombo(milestone ? 1.85f : 1.26f);

		if (milestone)
		{
			EmitSignal(SignalName.StreakMilestone, StreakMilestones[nextMilestone]);
			nextMilestone++;
		}
	}

	/// <summary>Scale-and-flash punch on the streak banner, restarted on every kill.</summary>
	private void PopCombo(float scale)
	{
		if (comboLabel == null || !comboLabel.Visible)
			return;

		comboPop?.Kill();
		comboLabel.Scale = new Vector2(scale, scale);
		comboLabel.AddThemeColorOverride("font_color", ComboFlash);

		comboPop = CreateTween().SetParallel();
		comboPop.TweenProperty(comboLabel, "scale", Vector2.One, 0.24f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		comboPop.TweenProperty(comboLabel, "theme_override_colors/font_color", ComboIdle, 0.3f);
	}

	private void RefreshKills()
	{
		if (killsLabel != null)
			killsLabel.Text = $"KILLS: {kills}";
	}

	private void RefreshCombo()
	{
		if (comboLabel == null)
			return;

		// A streak of one is just a kill; only shout about actual chains.
		bool wasVisible = comboLabel.Visible;
		comboLabel.Visible = combo >= 2;
		comboLabel.Text = $"x{combo} STREAK";

		if (wasVisible && !comboLabel.Visible)
		{
			// A dropped streak must not leave the banner mid-pop for the next one.
			comboPop?.Kill();
			comboLabel.Scale = Vector2.One;
			comboLabel.AddThemeColorOverride("font_color", ComboIdle);
		}
	}

	public float GetSurvivalTime() => survivalTime;
	public int GetKills() => kills;
	public int GetBestCombo() => runBestCombo;

	public static string FormatTime(float seconds)
	{
		int minutes = (int)(seconds / 60);
		int remainder = (int)(seconds % 60);
		return $"{minutes:D2}:{remainder:D2}";
	}

	public static string GetFormattedHighScore()
	{
		EnsureLoaded();
		return $"BEST: {FormatTime(bestTime)}";
	}

	public static float GetBestTime()
	{
		EnsureLoaded();
		return bestTime;
	}

	/// <summary>
	/// Records a finished run. Returns true if it set a new best time, so the
	/// game over screen can celebrate it.
	/// </summary>
	public static bool SaveRun(float time, int runKills, int runCombo)
	{
		EnsureLoaded();

		bool newBestTime = time > bestTime;
		bool improved = newBestTime || runKills > bestKills || runCombo > bestCombo;

		if (newBestTime)
			bestTime = time;
		if (runKills > bestKills)
			bestKills = runKills;
		if (runCombo > bestCombo)
			bestCombo = runCombo;

		if (improved)
			SaveToFile();

		return newBestTime;
	}

	private static void EnsureLoaded()
	{
		if (isLoaded)
			return;

		isLoaded = true;

		var config = new ConfigFile();
		if (config.Load(SavePath) == Error.Ok)
		{
			bestTime = config.GetValue(Section, "best_time", 0.0f).AsSingle();
			bestKills = config.GetValue(Section, "best_kills", 0).AsInt32();
			bestCombo = config.GetValue(Section, "best_combo", 0).AsInt32();
		}
		else if (FileAccess.FileExists(LegacySavePath))
		{
			// Carry a pre-existing best time over from the old raw-float format.
			bestTime = ReadLegacyBestTime();
			if (bestTime > 0)
				SaveToFile();
		}

		if (!float.IsFinite(bestTime) || bestTime < 0 || bestTime > MaxPlausibleTime)
			bestTime = 0;

		if (bestKills < 0)
			bestKills = 0;
		if (bestCombo < 0)
			bestCombo = 0;
	}

	private static float ReadLegacyBestTime()
	{
		using var file = FileAccess.Open(LegacySavePath, FileAccess.ModeFlags.Read);
		if (file == null || file.GetLength() < sizeof(float))
			return 0f;

		return file.GetFloat();
	}

	private static void SaveToFile()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "best_time", bestTime);
		config.SetValue(Section, "best_kills", bestKills);
		config.SetValue(Section, "best_combo", bestCombo);

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"ScoreManager: could not write '{SavePath}' ({error}).");
	}
}
