using Godot;

/// <summary>Shows the local top ten. See <see cref="Leaderboard"/> for the data.</summary>
public partial class LeaderboardMenu : Control
{
	private static readonly Color Idle = new Color(0.85f, 0.85f, 0.9f);

	private VBoxContainer rows;
	private Label emptyLabel;
	private Button backButton;
	private Font font;

	public override void _Ready()
	{
		rows = GetNode<VBoxContainer>("Layout/Rows");
		emptyLabel = GetNode<Label>("Layout/EmptyLabel");
		backButton = GetNode<Button>("Layout/BackButton");
		font = GetNode<Label>("Layout/Title").GetThemeFont("font");

		BuildRows();

		backButton.Pressed += OnBackPressed;

		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
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

		var entries = Leaderboard.Entries;
		emptyLabel.Visible = entries.Count == 0;

		for (int i = 0; i < entries.Count; i++)
		{
			Leaderboard.Entry entry = entries[i];
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 28);

			// Top three read warmer than the rest — the same accent colour the
			// style guide already uses for "this one matters".
			Color rankColour = i < 3 ? new Color(1.0f, 0.72f, 0.32f) : Idle;

			row.AddChild(Cell($"#{i + 1}", 40, rankColour, 90));
			row.AddChild(Cell(entry.Name, 40, rankColour, 300));
			row.AddChild(Cell($"{entry.Score:N0}", 40, rankColour, 220));
			row.AddChild(Cell(ScoreManager.FormatTime(entry.SurvivalTime), 32, Idle, 140));
			row.AddChild(Cell($"{entry.Kills} KILLS", 32, Idle, 170));

			rows.AddChild(row);
		}
	}

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
