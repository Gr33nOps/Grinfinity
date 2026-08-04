using Godot;

public partial class Menu : Node
{
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private ConfirmationDialog quitConfirm;

	public override void _Ready()
	{
		buttonSound = GetNodeOrNull<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNodeOrNull<AudioStreamPlayer>("HoverSound");
		quitConfirm = GetNodeOrNull<ConfirmationDialog>("QuitConfirm");

		var playButton = GetNode<TextureButton>("UI/Buttons/PlayButton");
		var quitButton = GetNode<TextureButton>("UI/Buttons/QuitButton");
		var upgradesButton = GetNode<Button>("UI/SideMenu/UpgradesButton");
		var leaderboardButton = GetNode<Button>("UI/SideMenu/LeaderboardButton");
		// Optional: the side menu does not currently carry a stats entry, but
		// nothing should crash the whole menu if a future pass adds one back.
		var statsButton = GetNodeOrNull<Button>("UI/SideMenu/StatsButton");
		var settingsButton = GetNode<Button>("UI/SideMenu/SettingsButton");
		var creditsButton = GetNode<Button>("UI/SideMenu/CreditsButton");

		// Play goes through the mode and weapon choice; only a restart skips them.
		playButton.Pressed += () => GoTo("res://scenes/mode_select.tscn");
		upgradesButton.Pressed += () => GoTo("res://scenes/upgrades.tscn");
		leaderboardButton.Pressed += () => GoTo("res://scenes/leaderboard.tscn");
		if (statsButton != null)
			statsButton.Pressed += () => GoTo("res://scenes/stats.tscn");
		settingsButton.Pressed += () => GoTo("res://scenes/settings.tscn");
		creditsButton.Pressed += () => GoTo("res://scenes/credits.tscn");
		quitButton.Pressed += OnQuitButtonPressed;

		var hoverButtons = new System.Collections.Generic.List<BaseButton>
			{ playButton, upgradesButton, leaderboardButton, settingsButton, creditsButton, quitButton };
		if (statsButton != null)
			hoverButtons.Add(statsButton);

		foreach (var button in hoverButtons)
			button.MouseEntered += PlayHoverSound;

		if (quitConfirm != null)
			quitConfirm.Confirmed += OnQuitConfirmed;

		ShowRecords();

		playButton.GrabFocus();
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	/// <summary>
	/// The number to beat, on the screen you decide from. "One more orbit" is a
	/// pillar; it needs something on the menu to be one more *than*.
	/// </summary>
	private void ShowRecords()
	{
		var label = GetNodeOrNull<Label>("UI/BestLabel");
		if (label == null)
			return;

		// A fresh install has nothing to beat, and an empty scoreboard is worse
		// than none at all.
		label.Visible = ScoreManager.BestScore > 0 || ScoreManager.BestTime > 0f;
		label.Text = $"BEST {ScoreManager.BestScore:N0}\nLONGEST {ScoreManager.FormatTime(ScoreManager.BestTime)}";
	}

	private void GoTo(string scenePath)
	{
		buttonSound?.Play();
		SceneTransition.Instance.ChangeScene(scenePath);
	}

	private void OnQuitButtonPressed()
	{
		buttonSound?.Play();

		if (quitConfirm == null)
		{
			OnQuitConfirmed();
			return;
		}

		quitConfirm.PopupCentered();
	}

	private async void OnQuitConfirmed()
	{
		buttonSound?.Play();
		await ToSignal(GetTree().CreateTimer(0.3f), SceneTreeTimer.SignalName.Timeout);
		GetTree().Quit();
	}

	private void PlayHoverSound()
	{
		hoverSound?.Play();
	}
}
