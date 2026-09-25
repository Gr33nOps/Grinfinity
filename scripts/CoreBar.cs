using Godot;

/// <summary>
/// The CORE bar along the bottom of the HUD. Fills left to right with every
/// kill and ticks brighter for a moment each time CORE comes in, so even a
/// single kill shows.
///
/// When it is full it turns orange and says so itself — "UPGRADE READY" and
/// the button to press, written inside the bar — so the notice is exactly
/// where the player has been watching it fill. It can be clicked, too. Once
/// every upgrade is bought it becomes the purple Overcharge bar.
/// </summary>
public partial class CoreBar : Control
{
	private static readonly Color Track = new("2a1a33");
	private static readonly Color Rim = new("986077");
	private static readonly Color Filling = new("c66e80");
	private static readonly Color Full = new("f5a451");
	private static readonly Color Overcharge = new("b58cff");

	/// <summary>Size of the words written inside the bar.</summary>
	public int TextSize { get; init; } = 17;
	/// <summary>Shown inside the bar while an upgrade is ready, e.g. "UPGRADE READY • PRESS TAB".</summary>
	public string ReadyText { get; set; } = "UPGRADE READY";
	/// <summary>Called when the full bar is clicked.</summary>
	public System.Action Pressed { get; set; }

	private float shown;
	private float target;
	private bool ready;
	private bool complete;
	private float tick;
	private float pop;
	private float time;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void Refresh(float fraction, bool isReady, bool isComplete)
	{
		if (fraction > target + 0.0001f)
			tick = 1f;
		if (isReady && !ready)
			pop = 1f;
		target = fraction;
		ready = isReady;
		complete = isComplete;
		// Only clickable while there is something to click for.
		MouseFilter = ready ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
		MouseDefaultCursorShape = ready ? CursorShape.PointingHand : CursorShape.Arrow;
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (ready && inputEvent is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
		{
			Pressed?.Invoke();
			AcceptEvent();
		}
	}

	public override void _Process(double delta)
	{
		float step = (float)delta;
		time += step;
		tick = Mathf.Max(0f, tick - step * 5f);
		pop = Mathf.Max(0f, pop - step * 3f);
		// Eases up to the real value, but snaps down when a bar is spent.
		shown = target < shown ? target : Mathf.Lerp(shown, target, 1f - Mathf.Exp(-14f * step));
		QueueRedraw();
	}

	public override void _Draw()
	{
		// A quick swell the moment it fills, so the change is noticed.
		float swell = 1f + 0.08f * Mathf.Sin(pop * Mathf.Pi);
		Vector2 grow = Size * (swell - 1f) * 0.5f;
		var area = new Rect2(-grow, Size * swell);
		float radius = area.Size.Y * 0.5f;
		DrawStyleBox(Box(Track, ready && !complete ? Full : Rim, radius, 2), area);

		float width = Mathf.Max((area.Size.X - 6f) * shown, 0f);
		if (width > 1f)
		{
			Color fill = complete ? Overcharge
				: ready ? Full.Lerp(new Color("ffd66b"), 0.5f + 0.5f * Mathf.Sin(time * 5f))
				: Filling.Lerp(Full, shown * 0.6f);
			fill = fill.Lightened(tick * 0.35f);
			DrawStyleBox(Box(fill, fill, Mathf.Min(radius - 3f, width * 0.5f), 0), new Rect2(area.Position + new Vector2(3f, 3f), new Vector2(width, area.Size.Y - 6f)));
		}

		string words = complete ? "OVERCHARGE" : ready ? ReadyText : "CORE";
		Caption(words, area, ready ? ArcadeSkin.Ink : ArcadeSkin.Cream);
	}

	/// <summary>Centred in the bar, outlined when light so it reads over any fill.</summary>
	private void Caption(string text, Rect2 area, Color colour)
	{
		Font font = ArcadeSkin.Font;
		Vector2 size = font.GetStringSize(text, HorizontalAlignment.Left, -1, TextSize);
		var at = new Vector2(area.Position.X + (area.Size.X - size.X) * 0.5f,
			area.Position.Y + (area.Size.Y + font.GetAscent(TextSize) - font.GetDescent(TextSize)) * 0.5f);
		if (colour != ArcadeSkin.Ink)
			DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, TextSize, 5, new Color(0.1f, 0.05f, 0.12f));
		DrawString(font, at, text, HorizontalAlignment.Left, -1, TextSize, colour);
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
