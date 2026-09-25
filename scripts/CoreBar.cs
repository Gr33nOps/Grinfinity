using Godot;

/// <summary>
/// The CORE bar on the HUD: a tall bar beside the ability column that fills
/// from the bottom with every kill. When it is full it turns orange and
/// breathes, and the upgrade prompt appears under the column. It ticks
/// brighter for a moment each time CORE comes in, so even a single kill shows.
/// </summary>
public partial class CoreBar : Control
{
	private static readonly Color Track = new("2a1a33");
	private static readonly Color Rim = new("986077");
	private static readonly Color Filling = new("c66e80");
	private static readonly Color Full = new("f5a451");
	private static readonly Color Overcharge = new("b58cff");

	/// <summary>Size of the word written up the bar.</summary>
	public int TextSize { get; init; } = 16;

	private float shown;
	private float target;
	private bool ready;
	private bool complete;
	private float tick;
	private float time;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void Refresh(float fraction, bool isReady, bool isComplete)
	{
		if (fraction > target + 0.0001f)
			tick = 1f;
		target = fraction;
		ready = isReady;
		complete = isComplete;
	}

	public override void _Process(double delta)
	{
		float step = (float)delta;
		time += step;
		tick = Mathf.Max(0f, tick - step * 5f);
		// Eases up to the real value, but snaps down when a bar is spent.
		shown = target < shown ? target : Mathf.Lerp(shown, target, 1f - Mathf.Exp(-14f * step));
		QueueRedraw();
	}

	public override void _Draw()
	{
		var area = new Rect2(Vector2.Zero, Size);
		float radius = Size.X * 0.5f;
		DrawStyleBox(Box(Track, ready && !complete ? Full : Rim, radius, 2), area);

		float height = Mathf.Max((Size.Y - 6f) * shown, 0f);
		if (height > 1f)
		{
			Color fill = complete ? Overcharge
				: ready ? Full.Lerp(new Color("ffd66b"), 0.5f + 0.5f * Mathf.Sin(time * 6f))
				: Filling.Lerp(Full, shown * 0.6f);
			fill = fill.Lightened(tick * 0.35f);
			DrawStyleBox(Box(fill, fill, Mathf.Min(radius - 3f, height * 0.5f), 0), new Rect2(3f, Size.Y - 3f - height, Size.X - 6f, height));
		}

		Caption(complete ? "OVERCHARGE" : "CORE", ready ? ArcadeSkin.Ink : ArcadeSkin.Cream);
	}

	/// <summary>The bar's name, reading bottom to top from its foot, outlined so it reads over any fill.</summary>
	private void Caption(string text, Color colour)
	{
		Font font = ArcadeSkin.Font;
		// Turned a quarter anticlockwise, the glyphs' height runs across the
		// bar; this offset centres them on it.
		float across = (font.GetAscent(TextSize) - font.GetDescent(TextSize)) * 0.5f;
		DrawSetTransform(new Vector2(Size.X * 0.5f + across, Size.Y - 12f), -Mathf.Pi * 0.5f, Vector2.One);
		if (colour != ArcadeSkin.Ink)
			DrawStringOutline(font, Vector2.Zero, text, HorizontalAlignment.Left, -1, TextSize, 5, new Color(0.1f, 0.05f, 0.12f));
		DrawString(font, Vector2.Zero, text, HorizontalAlignment.Left, -1, TextSize, colour);
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}

	private static StyleBoxFlat Box(Color fill, Color border, float radius, int line)
	{
		int r = Mathf.RoundToInt(radius);
		return new StyleBoxFlat
		{
			BgColor = fill, BorderColor = border,
			BorderWidthLeft = line, BorderWidthRight = line, BorderWidthTop = line, BorderWidthBottom = line,
			CornerRadiusTopLeft = r, CornerRadiusTopRight = r, CornerRadiusBottomLeft = r, CornerRadiusBottomRight = r
		};
	}
}
