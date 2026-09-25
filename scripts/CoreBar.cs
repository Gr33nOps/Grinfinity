using Godot;

/// <summary>
/// The CORE bar on the HUD. Fills with every kill; when it is full it turns
/// orange and breathes, and the upgrade prompt appears above it. It ticks
/// brighter for a moment each time CORE comes in, so even a single kill shows.
/// </summary>
public partial class CoreBar : Control
{
	private static readonly Color Track = new("2a1a33");
	private static readonly Color Rim = new("986077");
	private static readonly Color Filling = new("c66e80");
	private static readonly Color Full = new("f5a451");

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
		float radius = Size.Y * 0.5f;
		DrawStyleBox(Box(Track, Rim, radius, 2), area);

		if (complete)
			return;

		float width = Mathf.Max((Size.X - 6f) * shown, 0f);
		if (width > 1f)
		{
			Color fill = ready ? Full.Lerp(new Color("ffd66b"), 0.5f + 0.5f * Mathf.Sin(time * 6f)) : Filling.Lerp(Full, shown * 0.6f);
			fill = fill.Lightened(tick * 0.35f);
			DrawStyleBox(Box(fill, fill, Mathf.Min(radius - 3f, width * 0.5f), 0), new Rect2(3f, 3f, width, Size.Y - 6f));
		}

		if (ready)
			DrawStyleBox(Box(new Color(0, 0, 0, 0), new Color(Full, 0.5f + 0.4f * Mathf.Sin(time * 6f)), radius + 4f, 3), area.Grow(4f));
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
