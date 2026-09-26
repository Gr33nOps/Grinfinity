using Godot;

/// <summary>
/// The game's button, drawn as a chunky cartoon slab rather than a flat panel:
/// a face with a bright rim, standing on a darker lip that gives it depth, with
/// a soft glossy sheen over its upper part. Pressing it sinks the face onto the
/// lip, like a real arcade button.
///
/// A button with an icon wears it in a round porthole set into the left of the
/// face, with its label lined up after it, so a column of them reads like the
/// control panel of a little ship rather than a list of boxes.
///
/// Built from ordinary rounded boxes layered in <see cref="_Draw"/>, so it stays
/// crisp and anti-aliased at any size and UI scale.
/// </summary>
public partial class ArcadeButtonStyle : StyleBox
{
	private const int Radius = 16;
	/// <summary>Gap between the face's left edge and the porthole.</summary>
	private const float BadgeInset = 9f;
	/// <summary>How much bigger than its icon the porthole is, all round.</summary>
	private const float BadgeMargin = 9f;

	private Color faceColour, rimColour, lipColour;
	private float sheenAlpha;
	private StyleBoxFlat face, lip, sheen, badge;
	private float depth, sink, badgeRadius;

	/// <param name="rim">The face's edge: lighter than the face, so the button stands off a dark panel.</param>
	/// <param name="sheenAlpha">How strong the gloss across its upper part is.</param>
	/// <param name="depth">How tall the lip under the face is, in pixels.</param>
	/// <param name="sink">How far the face has sunk onto its lip: 0 at rest, nearly <paramref name="depth"/> when pressed.</param>
	public static ArcadeButtonStyle Make(Color faceColour, Color rim, Color lipColour, float sheenAlpha, float depth = 7f, float sink = 0f)
	{
		var style = new ArcadeButtonStyle
		{
			faceColour = faceColour, rimColour = rim, lipColour = lipColour, sheenAlpha = sheenAlpha,
			face = Rounded(faceColour, rim, Radius, 3),
			lip = Rounded(lipColour, lipColour.Darkened(0.35f), Radius, 3),
			sheen = Rounded(new Color(1f, 0.97f, 0.9f, sheenAlpha), Colors.Transparent, Radius - 5, 0),
			depth = depth,
			sink = sink
		};
		// The label rides on the face, so it sinks with it.
		style.ContentMarginLeft = style.ContentMarginRight = 24;
		style.ContentMarginTop = 8 + sink;
		style.ContentMarginBottom = 8 + depth - sink;
		return style;
	}

	/// <summary>
	/// The same button with a porthole for an icon <paramref name="iconSize"/> wide.
	/// The margins put the icon at the porthole's centre and the label just after it.
	/// </summary>
	public ArcadeButtonStyle WithBadge(float iconSize)
	{
		ArcadeButtonStyle style = Make(faceColour, rimColour, lipColour, sheenAlpha, depth, sink);
		style.badgeRadius = iconSize * 0.5f + BadgeMargin;
		style.badge = Rounded(faceColour.Darkened(0.32f), rimColour, (int)style.badgeRadius, 3);
		style.ContentMarginLeft = BadgeInset + BadgeMargin + 3f;
		return style;
	}

	/// <summary>Space to leave between a porthole's icon and the label.</summary>
	public static int LabelGap => (int)(BadgeMargin + 18f);

	public override void _Draw(Rid canvas, Rect2 rect)
	{
		var faceRect = new Rect2(rect.Position + new Vector2(0f, sink), new Vector2(rect.Size.X, rect.Size.Y - depth));
		lip.Draw(canvas, new Rect2(faceRect.Position + new Vector2(0f, depth - sink), faceRect.Size));
		face.Draw(canvas, faceRect);
		sheen.Draw(canvas, new Rect2(faceRect.Position + new Vector2(6f, 5f), new Vector2(faceRect.Size.X - 12f, faceRect.Size.Y * 0.42f)));

		if (badge != null)
		{
			// The porthole: a recessed disc in the face, centred on where the icon sits.
			var centre = new Vector2(faceRect.Position.X + BadgeInset + 3f + badgeRadius, faceRect.GetCenter().Y);
			badge.Draw(canvas, new Rect2(centre - Vector2.One * badgeRadius, Vector2.One * badgeRadius * 2f));
		}
	}

	private static StyleBoxFlat Rounded(Color fill, Color border, int radius, int line)
	{
		var box = new StyleBoxFlat { BgColor = fill, BorderColor = border, AntiAliasing = true };
		box.SetBorderWidthAll(line);
		box.SetCornerRadiusAll(radius);
		return box;
	}
}
