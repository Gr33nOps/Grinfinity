using Godot;

/// <summary>
/// Everything played, added up. Purely a display screen — every number here
/// already lives on <see cref="PlayerProfile"/>, tracked since M5's stardust
/// work; this is the one place a player can actually see it.
/// </summary>
public partial class StatsMenu : Control
{
	private LineEdit nameField;
	private Button worldValue;
	private int browsedWorld = 1;

	public override void _Ready()
	{
		BuildNameField();
		BuildWorldPicker();

		SetRow("Orbits", $"{PlayerProfile.TotalOrbits:N0}");
		SetRow("Kills", $"{PlayerProfile.TotalKills:N0}");
		SetRow("Time played", FormatDuration(PlayerProfile.TotalTimePlayed));
		SetRow("Heaviest mass", $"{Mathf.RoundToInt(PlayerProfile.HeaviestMassEver * 100)}%");
		SetRow("Favourite weapon", WeaponProfile.Get(PlayerProfile.FavouriteWeapon).Name);
		SetRow("Worlds unlocked", $"{CountUnlockedWorlds()} / {Worlds.All.Length}");
		SetRow("Achievements", $"{CountUnlockedAchievements()} / {Achievements.All.Length}");
		SetRow("Stardust earned", $"{PlayerProfile.StardustEarned:N0}");

		var backButton = GetNode<Button>("Layout/BackButton");
		backButton.Pressed += OnBackPressed;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		backButton.GrabFocus();
	}

	/// <summary>
	/// The one line on this screen the player writes rather than earns. Saved on
	/// every edit rather than behind a confirm button — there is nothing to
	/// cancel, and a name that silently failed to save because someone hit Back
	/// would be a small, baffling betrayal.
	/// </summary>
	private void BuildNameField()
	{
		nameField = GetNodeOrNull<LineEdit>("Layout/Rows/NameRow/Field");
		if (nameField == null)
			return;

		var label = GetNodeOrNull<Label>("Layout/Rows/NameRow/Label");
		if (label != null)
			label.Text = TranslationServer.Translate("STATS_NAME");

		nameField.Text = PlayerProfile.PlayerName;
		nameField.TextChanged += OnNameChanged;
	}

	private void OnNameChanged(string text)
	{
		PlayerProfile.SetPlayerName(text);
	}

	/// <summary>
	/// Which world the player wears. Cosmetic, applied on the spot, and never on
	/// the way into a run — worlds were unlocking with nowhere to see or wear
	/// them once the pre-run carousel went, which made every unlock the recap
	/// celebrated a reward the player could not actually collect.
	///
	/// Only unlocked worlds are reachable: a locked one on the carousel is a
	/// menu entry that exists to say no.
	/// </summary>
	private void BuildWorldPicker()
	{
		worldValue = GetNodeOrNull<Button>("Layout/Rows/WorldRow/Value");
		if (worldValue == null)
			return;

		var label = GetNodeOrNull<Label>("Layout/Rows/WorldRow/Label");
		if (label != null)
			label.Text = TranslationServer.Translate("STATS_WORLD");

		browsedWorld = GameSettings.Instance?.World ?? 1;
		if (!PlayerProfile.IsWorldUnlocked(browsedWorld))
			browsedWorld = 1;

		worldValue.Pressed += CycleWorld;
		RefreshWorld();
	}

	/// <summary>
	/// Tap the name to wear the next one. A pair of arrows would be two more
	/// controls for a choice with no wrong answer, and this is the one-tap
	/// picker the design asked for.
	/// </summary>
	private void CycleWorld()
	{
		int count = Worlds.All.Length;
		int id = browsedWorld;

		// Step until the next unlocked one. Bounded by the roster size so a
		// profile with exactly one world unlocked cannot spin forever.
		for (int i = 0; i < count; i++)
		{
			id = id % count + 1;
			if (PlayerProfile.IsWorldUnlocked(id))
				break;
		}

		browsedWorld = id;
		GameSettings.Instance?.SetWorld(id);
		RefreshWorld();
	}

	private void RefreshWorld()
	{
		worldValue.Text = Worlds.Get(browsedWorld).Name;
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

	private void SetRow(string label, string value)
	{
		string nodeName = label.Replace(" ", "");
		var labelNode = GetNodeOrNull<Label>($"Layout/Rows/{nodeName}/Label");
		var valueNode = GetNodeOrNull<Label>($"Layout/Rows/{nodeName}/Value");
		if (labelNode != null)
			labelNode.Text = TranslationServer.Translate($"STATS_{nodeName.ToUpperInvariant()}");
		if (valueNode != null)
			valueNode.Text = value;
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
