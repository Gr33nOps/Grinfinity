using Godot;

/// <summary>Shows the local top ten. See <see cref="Leaderboard"/> for the data.</summary>
public partial class LeaderboardMenu : Control
{
	private VBoxContainer rows;
	private Label emptyLabel;
	private Button backButton;
	private Label modeLabel;
	private Button modePrevButton;
	private Button modeNextButton;
	private Font font;
	private GameMode browsedMode;

	public override void _Ready()
	{
		rows = GetNode<VBoxContainer>("Layout/Rows");
		emptyLabel = GetNode<Label>("Layout/EmptyLabel");
		backButton = GetNode<Button>("Layout/BackButton");
		modeLabel = GetNode<Label>("Layout/ModeRow/ModeLabel");
		modePrevButton = GetNode<Button>("Layout/ModeRow/Prev");
		modeNextButton = GetNode<Button>("Layout/ModeRow/Next");
		font = GetNode<Label>("Layout/Title").GetThemeFont("font");

		modeLabel.AddThemeFontOverride("font", font);
		modePrevButton.Pressed += () => BrowseMode(-1);
		modeNextButton.Pressed += () => BrowseMode(1);

		// Opens on whatever mode was last played — the board someone just
		// finished a run in is the one they came here wanting to see.
		browsedMode = Loadout.Mode;
		RefreshMode();

		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	private void BrowseMode(int direction)
	{
		int count = Modes.All.Length;
		int index = ((int)browsedMode + direction + count) % count;
		browsedMode = (GameMode)index;
		RefreshMode();
	}

	private void RefreshMode()
	{
		modeLabel.Text = Modes.Get(browsedMode).Name;
		BuildRows();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("pause"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildRows()
	{
		foreach (Node child in rows.GetChildren())
			child.QueueFree();

		var entries = Leaderboard.EntriesFor(browsedMode);
		emptyLabel.Visible = entries.Count == 0;

		for (int i = 0; i < entries.Count; i++)
		{
			Leaderboard.Entry entry = entries[i];
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 24);

			// Top three read warmer than the rest — the same accent colour the
			// style guide already uses for "this one matters".
			Color rankColour = i < 3 ? new Color(1.0f, 0.72f, 0.32f) : new Color(0.85f, 0.85f, 0.9f);

			row.AddChild(Cell($"#{i + 1}", 40, rankColour, 90));
			row.AddChild(Cell($"{entry.Score:N0}", 40, rankColour, 220));
			row.AddChild(Cell(ScoreManager.FormatTime(entry.SurvivalTime), 32, Idle, 130));
			row.AddChild(Cell($"{entry.Kills} KILLS", 32, Idle, 160));
			row.AddChild(Cell(WeaponProfile.Get(entry.Weapon).Name, 30, Idle, 240));
			row.AddChild(Cell(Worlds.Get(entry.World).Name, 30, Idle, 240));
			row.AddChild(Cell(entry.Date, 26, new Color(0.6f, 0.6f, 0.68f), 160));

			rows.AddChild(row);
		}
	}

	private static readonly Color Idle = new Color(0.85f, 0.85f, 0.9f);

	private Label Cell(string text, int size, Color colour, float width)
	{
		var label = new Label
		{
			Text = text,
			CustomMinimumSize = new Vector2(width, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			ClipText = true
		};
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", colour);
		return label;
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
