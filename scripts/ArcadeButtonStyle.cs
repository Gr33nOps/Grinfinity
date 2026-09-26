using Godot;

/// <summary>
/// The game's button, drawn as a chunky cartoon slab rather than a flat panel:
/// a face with a bright rim, standing on a darker lip that gives it depth, with
/// a soft glossy sheen over its upper part. Pressing it sinks the face onto the
/// lip, like a real arcade button.
///
/// Built from ordinary rounded boxes layered in <see cref="_Draw"/>, so it stays
/// crisp and anti-aliased at any size and UI scale.
/// </summary>
public partial class ArcadeButtonStyle : StyleBox
{
	private const int Radius = 16;

	private StyleBoxFlat face, lip, sheen;
	private float depth, sink;

	/// <param name="rim">The face's edge: lighter than the face, so the button stands off a dark panel.</param>
	/// <param name="sheenAlpha">How strong the gloss across its upper part is.</param>
	/// <param name="depth">How tall the lip under the face is, in pixels.</param>
	/// <param name="sink">How far the face has sunk onto its lip: 0 at rest, nearly <paramref name="depth"/> when pressed.</param>
	public static ArcadeButtonStyle Make(Color faceColour, Color rim, Color lipColour, float sheenAlpha, float depth = 7f, float sink = 0f)
	{
		var style = new ArcadeButtonStyle
		{
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

	public override void _Draw(Rid canvas, Rect2 rect)
	{
		var faceRect = new Rect2(rect.Position + new Vector2(0f, sink), new Vector2(rect.Size.X, rect.Size.Y - depth));
		lip.Draw(canvas, new Rect2(faceRect.Position + new Vector2(0f, depth - sink), faceRect.Size));
		face.Draw(canvas, faceRect);
		sheen.Draw(canvas, new Rect2(faceRect.Position + new Vector2(6f, 5f), new Vector2(faceRect.Size.X - 12f, faceRect.Size.Y * 0.42f)));
	}

	private static StyleBoxFlat Rounded(Color fill, Color border, int radius, int line)
	{
		var box = new StyleBoxFlat { BgColor = fill, BorderColor = border, AntiAliasing = true };
		box.SetBorderWidthAll(line);
		box.SetCornerRadiusAll(radius);
		return box;
	}
}
