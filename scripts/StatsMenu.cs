using Godot;

/// <summary>
/// My Planet: the planet you wear, every planet there is to earn, your lifetime
/// numbers and every achievement. Purely a display screen — every number here
/// already lives on <see cref="PlayerProfile"/>, tracked across every run; this
/// is the one place a player can actually see it.
/// </summary>
public partial class StatsMenu : Control
{
	private const string Rows = "Layout/Columns/Rows";
	private const string Showcase = "Layout/Columns/Showcase";
	private static readonly Color Faded = new(0.77f, 0.66f, 0.75f);

	private LineEdit nameField;
	private PlanetShowcase planet;
	private Label planetName, planetNote, planetCount;
	private int browsedWorld = 1;

	public override void _Ready()
	{
		// Queued in this order, so the screen's own sizes land after the shared skin's.
		Callable.From(() => ArcadeSkin.SupportingScreen(this)).CallDeferred();
		Callable.From(Polish).CallDeferred();
		BuildNameField();
		BuildWorldPicker();

		SetRow("Orbits", $"{PlayerProfile.TotalOrbits:N0}");
		SetRow("Kills", $"{PlayerProfile.TotalKills:N0}");
		SetRow("Timeplayed", FormatDuration(PlayerProfile.TotalTimePlayed));
		SetRow("Heaviestmass", $"{Mathf.RoundToInt(PlayerProfile.HeaviestMassEver * 100)}%");
		SetRow("Achievements", $"{CountUnlockedAchievements()} / {Achievements.All.Length}");

		var backButton = GetNode<Button>("Layout/BackButton");
		backButton.Pressed += OnBackPressed;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	/// <summary>The planet's name leads its column; the note under it is quieter.</summary>
	private void Polish()
	{
		GetNode<BoxContainer>("Layout/Columns").AddThemeConstantOverride("separation", 44);
		planetName.AddThemeFontSizeOverride("font_size", 36);
		planetName.AddThemeColorOverride("font_color", ArcadeSkin.Orange);
		planetNote.AddThemeFontSizeOverride("font_size", 21);
		planetCount.AddThemeFontSizeOverride("font_size", 22);
		planetCount.AddThemeColorOverride("font_color", Faded);
	}

	/// <summary>
	/// The one line on this screen the player writes rather than earns. Saved on
	/// every edit rather than behind a confirm button — there is nothing to
	/// cancel, and a name that silently failed to save because someone hit Back
	/// would be a small, baffling betrayal.
	/// </summary>
	private void BuildNameField()
	{
		nameField = GetNode<LineEdit>($"{Rows}/NameRow/Field");
		GetNode<Label>($"{Rows}/NameRow/Label").Text = TranslationServer.Translate("STATS_NAME");
		nameField.Text = PlayerProfile.PlayerName;
		nameField.TextChanged += PlayerProfile.SetPlayerName;
	}

	/// <summary>
	/// Every planet can be browsed, so a player can see what is still out there
	/// and what it takes. Landing on an unlocked one wears it on the spot;
	/// a locked one shows as a dark shape with its unlock rule underneath.
	/// </summary>
	private void BuildWorldPicker()
	{
		planet = GetNode<PlanetShowcase>($"{Showcase}/Planet");
		planetName = GetNode<Label>($"{Showcase}/PlanetName");
		planetNote = GetNode<Label>($"{Showcase}/PlanetNote");
		planetCount = GetNode<Label>($"{Showcase}/Picker/Count");
		GetNode<Button>($"{Showcase}/Picker/Prev").Pressed += () => Browse(-1);
		GetNode<Button>($"{Showcase}/Picker/Next").Pressed += () => Browse(1);

		browsedWorld = GameSettings.Instance?.World ?? 1;
		if (!PlayerProfile.IsWorldUnlocked(browsedWorld))
			browsedWorld = 1;
		RefreshWorld();
	}

	private void Browse(int step)
	{
		int count = Worlds.All.Length;
		browsedWorld = Mathf.PosMod(browsedWorld - 1 + step, count) + 1;
		if (PlayerProfile.IsWorldUnlocked(browsedWorld))
			GameSettings.Instance?.SetWorld(browsedWorld);
		RefreshWorld();
	}

	private void RefreshWorld()
	{
		Worlds.Profile world = Worlds.Get(browsedWorld);
		bool unlocked = PlayerProfile.IsWorldUnlocked(world.Id);
		planet.Present(world.Id, !unlocked);
		planetName.Text = unlocked ? world.Name : $"{world.Name}  (LOCKED)";
		planetNote.Text = unlocked ? world.Flavour : $"To unlock: {world.UnlockHint}";
		planetNote.AddThemeColorOverride("font_color", unlocked ? ArcadeSkin.Cream : Faded);
		planetCount.Text = $"{world.Id} OF {Worlds.All.Length}  •  {CountUnlockedWorlds()} UNLOCKED";
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		// Escape while typing should leave the field, not the screen — otherwise
		// there is no way to stop editing without also backing out.
		if (nameField != null && nameField.HasFocus())
			return;

		if (inputEvent.IsActionPressed("pause"))
		{
			OnBackPressed();
			GetViewport().SetInputAsHandled();
		}
	}

	/// <summary>
	/// Minutes for anything under an hour, hours-and-minutes past that — a
	/// lifetime total in raw seconds or MM:SS would stop being readable long
	/// before this screen stops being interesting to check back on.
	/// </summary>
	private static string FormatDuration(float seconds)
	{
		int totalMinutes = Mathf.FloorToInt(seconds / 60f);
		if (totalMinutes < 60)
			return $"{totalMinutes} {TranslationServer.Translate("UI_MINUTES_SHORT")}";

		int hours = totalMinutes / 60;
		int minutes = totalMinutes % 60;
		return $"{hours}h {minutes:D2}m";
	}

	private static int CountUnlockedWorlds()
	{
		int count = 0;
		foreach (Worlds.Profile world in Worlds.All)
		{
			if (PlayerProfile.IsWorldUnlocked(world.Id))
				count++;
		}
		return count;
	}

	private static int CountUnlockedAchievements()
	{
		int count = 0;
		foreach (Achievements.Profile achievement in Achievements.All)
		{
			if (PlayerProfile.IsAchievementUnlocked(achievement.Id))
				count++;
		}
		return count;
	}

	private void SetRow(string nodeName, string value)
	{
		GetNode<Label>($"{Rows}/{nodeName}/Label").Text = TranslationServer.Translate($"STATS_{nodeName.ToUpperInvariant()}");
		GetNode<Label>($"{Rows}/{nodeName}/Value").Text = value;
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
