using Godot;

/// <summary>
/// The game's button: a plain rounded face with a thin lighter edge, sitting on
/// a slightly darker base so it reads as something to press. Pressing it sinks
/// the face onto its base. Nothing else: the icon and the text do the talking.
///
/// Built from two rounded boxes layered in <see cref="_Draw"/>, so it stays
/// crisp and anti-aliased at any size and UI scale.
/// </summary>
public partial class ArcadeButtonStyle : StyleBox
{
	private const int Radius = 14;

	private StyleBoxFlat face, lip;
	private float depth, sink;

	/// <param name="rim">The face's edge: a little lighter than the face, so the button stands off a dark panel.</param>
	/// <param name="depth">How tall the base under the face is, in pixels.</param>
	/// <param name="sink">How far the face has sunk onto its base: 0 at rest, nearly <paramref name="depth"/> when pressed.</param>
	public static ArcadeButtonStyle Make(Color faceColour, Color rim, Color lipColour, float depth = 4f, float sink = 0f)
	{
		var style = new ArcadeButtonStyle
		{
			face = Rounded(faceColour, rim),
			lip = Rounded(lipColour, lipColour),
			depth = depth,
			sink = sink
		};
		// The label rides on the face, so it sinks with it.
		style.ContentMarginLeft = style.ContentMarginRight = 22;
		style.ContentMarginTop = 8 + sink;
		style.ContentMarginBottom = 8 + depth - sink;
		return style;
	}

	public override void _Draw(Rid canvas, Rect2 rect)
	{
		var faceRect = new Rect2(rect.Position + new Vector2(0f, sink), new Vector2(rect.Size.X, rect.Size.Y - depth));
		lip.Draw(canvas, new Rect2(faceRect.Position + new Vector2(0f, depth - sink), faceRect.Size));
		face.Draw(canvas, faceRect);
	}

	private static StyleBoxFlat Rounded(Color fill, Color border)
	{
		var box = new StyleBoxFlat { BgColor = fill, BorderColor = border, AntiAliasing = true };
		box.SetBorderWidthAll(2);
		box.SetCornerRadiusAll(Radius);
		return box;
	}
}
