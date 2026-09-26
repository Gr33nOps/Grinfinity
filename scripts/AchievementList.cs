using Godot;

/// <summary>
/// Every achievement on one card: a filled badge and bright name for the ones
/// earned, a hollow badge and faded text for the rest, and each one's rule
/// beside it so a player knows what is still left to do.
/// </summary>
public partial class AchievementList : Control
{
	private const float RowHeight = 34f;
	private const float NameColumn = 232f;
	private static readonly Color Faded = new(0.77f, 0.66f, 0.75f, 0.55f);

	private Font font;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		font = GD.Load<Font>("res://fonts/LilitaOne.ttf");
		CustomMinimumSize = new Vector2(0f, RowHeight * Achievements.All.Length);
	}

	public override void _Draw()
	{
		for (int i = 0; i < Achievements.All.Length; i++)
		{
			Achievements.Profile achievement = Achievements.All[i];
			bool earned = PlayerProfile.IsAchievementUnlocked(achievement.Id);
			float middle = RowHeight * i + RowHeight * 0.5f;

			var badge = new Vector2(11f, middle);
			if (earned)
			{
				DrawCircle(badge, 10f, ArcadeSkin.Orange);
				DrawPolyline(new[] { badge + new Vector2(-5f, 0f), badge + new Vector2(-1f, 4f), badge + new Vector2(5f, -4f) }, ArcadeSkin.Ink, 2.6f, true);
			}
			else
			{
				DrawArc(badge, 9f, 0f, Mathf.Tau, 24, Faded, 2f, true);
			}

			float baseline = middle + 8f;
			DrawString(font, new Vector2(32f, baseline), achievement.Name, HorizontalAlignment.Left, NameColumn - 32f, 22,
				earned ? ArcadeSkin.Orange : Faded);
			DrawString(font, new Vector2(NameColumn, baseline), achievement.Description, HorizontalAlignment.Left, Size.X - NameColumn, 20,
				earned ? ArcadeSkin.Cream : Faded);
		}
	}
}
