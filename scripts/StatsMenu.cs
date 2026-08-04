using Godot;

/// <summary>
/// Everything played, added up. Purely a display screen — every number here
/// already lives on <see cref="PlayerProfile"/>, tracked since M5's stardust
/// work; this is the one place a player can actually see it.
/// </summary>
public partial class StatsMenu : Control
{
	public override void _Ready()
	{
		SetRow("Orbits", $"{PlayerProfile.TotalOrbits:N0}");
		SetRow("Kills", $"{PlayerProfile.TotalKills:N0}");
		SetRow("Time played", FormatDuration(PlayerProfile.TotalTimePlayed));
		SetRow("Heaviest mass", $"{Mathf.RoundToInt(PlayerProfile.HeaviestMassEver * 100)}%");
		SetRow("Favourite weapon", WeaponProfile.Get(PlayerProfile.FavouriteWeapon).Name);
		SetRow("Worlds unlocked", $"{CountUnlockedWorlds()} / {Worlds.All.Length}");
		SetRow("Achievements", $"{CountUnlockedAchievements()} / {Achievements.All.Length}");
		SetRow("Stardust banked", $"{PlayerProfile.Stardust:N0}");

		var backButton = GetNode<Button>("Layout/BackButton");
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

	/// <summary>
	/// Minutes for anything under an hour, hours-and-minutes past that — a
	/// lifetime total in raw seconds or MM:SS would stop being readable long
	/// before this screen stops being interesting to check back on.
	/// </summary>
	private static string FormatDuration(float seconds)
	{
		int totalMinutes = Mathf.FloorToInt(seconds / 60f);
		if (totalMinutes < 60)
			return $"{totalMinutes} min";

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
		var labelNode = GetNodeOrNull<Label>($"Layout/Rows/{label.Replace(" ", "")}/Label");
		var valueNode = GetNodeOrNull<Label>($"Layout/Rows/{label.Replace(" ", "")}/Value");
		if (labelNode != null)
			labelNode.Text = label.ToUpperInvariant();
		if (valueNode != null)
			valueNode.Text = value;
	}

	private void OnBackPressed()
	{
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
}
