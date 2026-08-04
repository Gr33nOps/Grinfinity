using Godot;

/// <summary>
/// A hit's damage, drawn once and left to rise and fade. Built the same way
/// <see cref="NovaWave"/> is — a bare Node2D with its own _Draw, instantiated
/// directly with `new`, no scene file needed for something this small.
///
/// Gated by <see cref="GameSettings.ShowDamageNumbers"/> at the call site, not
/// in here: a disabled toggle should mean this class is never even asked to
/// exist for a frame, not that it exists invisibly.
/// </summary>
public partial class DamageNumber : Node2D
{
	[Export] public float Duration { get; set; } = 0.55f;
	[Export] public float RiseDistance { get; set; } = 44.0f;
	[Export] public int FontSize { get; set; } = 26;
	[Export] public Color TextColor { get; set; } = new Color(1.0f, 0.95f, 0.8f);

	public int Amount { get; set; } = 1;

	private static Font font;
	private float age;
	private Vector2 startPosition;

	private static Font EnsureFont()
	{
		font ??= GD.Load<Font>("res://fonts/Bubblegum.ttf");
		return font;
	}

	public override void _Ready()
	{
		startPosition = GlobalPosition;
		ZIndex = 5;
		// A crit-sized hit reads bigger, the same way a kill-chain banner does.
		if (Amount >= 5)
			FontSize += 8;
	}

	public override void _Process(double delta)
	{
		age += (float)delta;
		if (age >= Duration)
		{
			QueueFree();
			return;
		}

		float t = age / Duration;
		// Fast up, settling — the same easing shape NovaWave uses for its ring.
		float eased = 1.0f - Mathf.Pow(1.0f - t, 2.0f);
		GlobalPosition = startPosition + Vector2.Up * RiseDistance * eased;
		QueueRedraw();
	}

	public override void _Draw()
	{
		float t = Mathf.Clamp(age / Duration, 0f, 1f);
		// Holds full opacity briefly, then fades — long enough to actually read.
		float alpha = t < 0.35f ? 1.0f : 1.0f - (t - 0.35f) / 0.65f;
		var colour = new Color(TextColor.R, TextColor.G, TextColor.B, TextColor.A * alpha);

		Font drawFont = EnsureFont();
		string text = Amount.ToString();
		Vector2 size = drawFont.GetStringSize(text, HorizontalAlignment.Left, -1, FontSize);
		DrawString(drawFont, new Vector2(-size.X * 0.5f, 0f), text, HorizontalAlignment.Left, -1, FontSize, colour);
	}
}
