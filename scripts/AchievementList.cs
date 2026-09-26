using Godot;

/// <summary>
/// Every achievement on its own card, set apart from the lifetime numbers
/// above it: a heading with the count, then a filled badge and bright name for
/// the ones earned, a hollow badge and faded text for the rest, and each one's
/// rule beside it so a player knows what is still left to do.
/// </summary>
public partial class AchievementList : Control
{
	private const float Pad = 18f;
	private const float HeaderHeight = 46f;
	private const float RowHeight = 32f;
	private const float NameColumn = 250f;
	private static readonly Color Card = new("2a1a33");
	private static readonly Color Edge = new("986077");
	private static readonly Color Faded = new(0.77f, 0.66f, 0.75f, 0.55f);

	private Font font;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		font = GD.Load<Font>("res://fonts/LilitaOne.ttf");
		CustomMinimumSize = new Vector2(0f, Pad + HeaderHeight + RowHeight * Achievements.All.Length + Pad * 0.6f);
	}

	public override void _Draw()
	{
		DrawStyleBox(new StyleBoxFlat
		{
			BgColor = Card, BorderColor = Edge,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			CornerRadiusTopLeft = 18, CornerRadiusTopRight = 18, CornerRadiusBottomLeft = 18, CornerRadiusBottomRight = 18
		}, new Rect2(Vector2.Zero, Size));

		int earnedCount = 0;
		foreach (Achievements.Profile achievement in Achievements.All)
			if (PlayerProfile.IsAchievementUnlocked(achievement.Id))
				earnedCount++;

		float headerBase = Pad + 26f;
		DrawString(font, new Vector2(Pad, headerBase), "ACHIEVEMENTS", HorizontalAlignment.Left, -1, 28, ArcadeSkin.Orange);
		DrawString(font, new Vector2(Pad, headerBase), $"{earnedCount} / {Achievements.All.Length}", HorizontalAlignment.Right, Size.X - Pad * 2f, 28, ArcadeSkin.Cream);
		float rule = Pad + HeaderHeight - 8f;
		DrawLine(new Vector2(Pad, rule), new Vector2(Size.X - Pad, rule), new Color(Edge, 0.6f), 2f);

		for (int i = 0; i < Achievements.All.Length; i++)
		{
			Achievements.Profile achievement = Achievements.All[i];
			bool earned = PlayerProfile.IsAchievementUnlocked(achievement.Id);
			float middle = Pad + HeaderHeight + RowHeight * i + RowHeight * 0.5f;

			var badge = new Vector2(Pad + 11f, middle);
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
			DrawString(font, new Vector2(Pad + 32f, baseline), achievement.Name, HorizontalAlignment.Left, NameColumn - Pad - 32f, 22,
				earned ? ArcadeSkin.Orange : Faded);
			DrawString(font, new Vector2(NameColumn, baseline), achievement.Description, HorizontalAlignment.Left, Size.X - NameColumn - Pad, 20,
				earned ? ArcadeSkin.Cream : Faded);
		}
	}
}
