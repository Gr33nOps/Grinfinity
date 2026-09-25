using Godot;

/// <summary>
/// A drawn picture of one control: a keyboard key, the mouse, a controller
/// face button, trigger or stick. Used by <see cref="ControlsCard"/> so the
/// how-to shows the thing to press rather than spelling it out.
/// </summary>
public partial class KeyCap : Control
{
	public enum Shape
	{
		Key,
		Mouse,
		MouseClick,
		FaceButton,
		Trigger,
		Stick
	}

	private static readonly Color Top = new("4d2c49");
	private static readonly Color Lip = new("241528");
	private static readonly Color Line = new("986077");

	public Shape Kind { get; init; } = Shape.Key;
	public string Text { get; init; } = "";
	/// <summary>Face buttons are coloured like the pad's own: A green, B red, X blue, Y yellow.</summary>
	public Color Tint { get; init; } = ArcadeSkin.Cream;
	public float Unit { get; init; } = 34f;

	private int FontSize => Mathf.RoundToInt(Unit * (Text.Length > 2 ? 0.44f : 0.56f));

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		float width = Kind switch
		{
			Shape.Key => Mathf.Max(Unit, ArcadeSkin.Font.GetStringSize(Text, HorizontalAlignment.Left, -1, FontSize).X + Unit * 0.6f),
			Shape.Mouse or Shape.MouseClick => Unit * 0.8f,
			Shape.Trigger => Unit * 1.3f,
			_ => Unit
		};
		CustomMinimumSize = new Vector2(width, Unit + 4f);
	}

	public override void _Draw()
	{
		var area = new Rect2(0f, 0f, Size.X, Unit);
		Vector2 centre = area.GetCenter();
		float r = Unit * 0.5f;

		switch (Kind)
		{
			case Shape.Key:
				// A key has a lip under it, so it reads as something to press.
				DrawStyleBox(Box(Lip, Lip, 8, 0), area with { Position = area.Position + new Vector2(0f, 4f) });
				DrawStyleBox(Box(Top, Line, 8, 2), area);
				Word(Text, centre, ArcadeSkin.Cream);
				break;

			case Shape.Mouse:
			case Shape.MouseClick:
				var body = new Rect2(centre.X - Size.X * 0.5f, 0f, Size.X, Unit + 4f);
				DrawStyleBox(Box(Top, Line, Mathf.RoundToInt(Size.X * 0.5f), 2), body);
				float split = body.Position.Y + body.Size.Y * 0.42f;
				if (Kind == Shape.MouseClick)
				{
					var button = new Rect2(body.Position + new Vector2(3f, 3f), new Vector2(body.Size.X * 0.5f - 3f, split - body.Position.Y - 3f));
					DrawStyleBox(Box(ArcadeSkin.Orange, ArcadeSkin.Orange, Mathf.RoundToInt(Size.X * 0.4f), 0) , button);
				}
				DrawLine(new Vector2(body.Position.X + 2f, split), new Vector2(body.End.X - 2f, split), Line, 2f, true);
				DrawLine(new Vector2(centre.X, body.Position.Y + 2f), new Vector2(centre.X, split), Line, 2f, true);
				break;

			case Shape.FaceButton:
				DrawCircle(centre + new Vector2(0f, 3f), r, Lip);
				DrawCircle(centre, r, Tint);
				DrawArc(centre, r, 0f, Mathf.Tau, 40, ArcadeSkin.Ink, 2f, true);
				Word(Text, centre, ArcadeSkin.Ink);
				break;

			case Shape.Trigger:
				DrawStyleBox(Box(Lip, Lip, 10, 0), area with { Position = area.Position + new Vector2(0f, 4f) });
				var trigger = Box(Top, Line, 14, 2);
				trigger.CornerRadiusBottomLeft = trigger.CornerRadiusBottomRight = 6;
				DrawStyleBox(trigger, area);
				Word(Text, centre, ArcadeSkin.Cream);
				break;

			case Shape.Stick:
				DrawCircle(centre, r, Lip);
				DrawCircle(centre, r * 0.78f, Top);
				DrawArc(centre, r * 0.78f, 0f, Mathf.Tau, 40, Line, 2f, true);
				Word(Text, centre, ArcadeSkin.Cream);
				break;
		}
	}

	private void Word(string text, Vector2 centre, Color colour)
	{
		Font font = ArcadeSkin.Font;
		Vector2 size = font.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
		var at = new Vector2(centre.X - size.X * 0.5f, centre.Y + (font.GetAscent(FontSize) - font.GetDescent(FontSize)) * 0.5f);
		DrawString(font, at, text, HorizontalAlignment.Left, -1, FontSize, colour);
	}

	private static StyleBoxFlat Box(Color fill, Color border, int radius, int line) => new()
	{
		BgColor = fill, BorderColor = border,
		BorderWidthLeft = line, BorderWidthRight = line, BorderWidthTop = line, BorderWidthBottom = line,
		CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius
	};
}
