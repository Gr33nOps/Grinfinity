using Godot;

public partial class ScoreManager : Node
{
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

	private Label scoreLabel;
	private Label highScoreLabel;
	private Label killsLabel;
	private Label comboLabel;

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
		comboLabel.Visible = combo >= 2;
		comboLabel.Text = $"x{combo} STREAK";
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
