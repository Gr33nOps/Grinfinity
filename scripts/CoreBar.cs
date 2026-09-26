using Godot;

/// <summary>
/// The CORE bar along the bottom of the HUD. Fills left to right with every
/// kill, and each time CORE comes in the leading edge of the fill flares with a
/// few sparks, so even a single kill shows — down here, out of the fight,
/// rather than anything flying across the screen.
///
/// Full bars are saved, up to three: the pips at the right end count them, and
/// the bar turns orange and says how many are ready and which button spends
/// them — written inside the bar, exactly where the player has been watching
/// it fill. It keeps filling toward the next one meanwhile. It can be clicked,
/// too. Once every upgrade is bought it becomes the purple Overcharge bar.
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
	/// <summary>The button that spends a saved bar, e.g. "PRESS TAB".</summary>
	public string PressText { get; set; } = "PRESS TAB";
	/// <summary>Called when the full bar is clicked.</summary>
	public System.Action Pressed { get; set; }

	private float shown;
	private float target;
	private bool ready;
	private bool complete;
	private int banked;
	private float tick;
	private float pop;
	private float time;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void Refresh(float fraction, bool isReady, bool isComplete, int bankedNow)
	{
		if (fraction > target + 0.0001f)
			tick = 1f;
		// A swell for every bar saved, not just the first.
		if (bankedNow > banked)
			pop = 1f;
		banked = bankedNow;
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
		tick = Mathf.Max(0f, tick - step * 3f);
		pop = Mathf.Max(0f, pop - step * 3f);
		// Eases up to the real value, but snaps down when a bar rolls over.
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
		// With upgrades saved the whole bar is orange; the lighter band is progress to the next.
		DrawStyleBox(Box(ready && !complete ? Full : Track, ready && !complete ? new Color("ffcd85") : Rim, radius, 2), area);

		float width = Mathf.Max((area.Size.X - 6f) * shown, 0f);
		if (width > 1f && !ready && !complete && tick > 0f)
			Flare(area.Position + new Vector2(3f + width, area.Size.Y * 0.5f), area.Size.Y);
		if (width > 1f)
		{
			Color fill = complete ? Overcharge
				: ready ? new Color("ffe08a").Lerp(new Color("fff0ce"), 0.3f + 0.3f * Mathf.Sin(time * 5f))
				: Filling.Lerp(Full, shown * 0.6f);
			fill = fill.Lightened(tick * 0.35f);
			DrawStyleBox(Box(fill, fill, Mathf.Min(radius - 3f, width * 0.5f), 0), new Rect2(area.Position + new Vector2(3f, 3f), new Vector2(width, area.Size.Y - 6f)));
		}

		string words = complete ? "OVERCHARGE"
			: !ready ? "CORE"
			: banked > 1 ? $"{banked} UPGRADES READY  •  {PressText}"
			: $"UPGRADE READY  •  {PressText}";
		Caption(words, area, ready ? ArcadeSkin.Ink : ArcadeSkin.Cream);

		if (!complete)
			Pips(area);
	}

	/// <summary>One pip per bar that can be saved, filled for each one that is.</summary>
	private void Pips(Rect2 area)
	{
		float r = area.Size.Y * 0.2f;
		float gap = r * 2.8f;
		for (int i = 0; i < Balance.MaxBankedUpgrades; i++)
		{
			var at = new Vector2(area.End.X - area.Size.Y * 0.5f - (Balance.MaxBankedUpgrades - 1 - i) * gap, area.GetCenter().Y);
			bool saved = i < banked;
			DrawCircle(at, r, saved ? ArcadeSkin.Cream : new Color(Track, 0.85f));
			DrawArc(at, r, 0f, Mathf.Tau, 20, saved ? ArcadeSkin.Ink : new Color(ArcadeSkin.Cream, 0.6f), 2f, true);
		}
	}

	/// <summary>A short flare at the fill's edge when CORE comes in: a glow and a few sparks rising.</summary>
	private void Flare(Vector2 edge, float height)
	{
		float age = 1f - tick;
		DrawCircle(edge, height * (0.5f + 0.4f * age), new Color(Full, 0.35f * tick));
		for (int i = 0; i < 3; i++)
		{
			float side = (i - 1) * 7f;
			var spark = edge + new Vector2(side, -height * 0.4f - age * height * 0.9f);
			DrawCircle(spark, 2.2f * tick + 0.4f, new Color(ArcadeSkin.Cream, tick));
		}
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
