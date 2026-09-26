using Godot;

/// <summary>
/// One upgrade on the star chart: a little planet with its icon, lit from the
/// upper left, and its name and rank stars beside it. One you can buy right now
/// has a pulsing orange halo; a bought one takes on its branch's colour. Drawn
/// by hand so it reads as a world on the chart rather than a button in a list,
/// but it is still a Button, so mouse, keys and pad all work the usual way.
/// </summary>
public partial class SkillNode : Button
{
	public enum State
	{
		/// <summary>Its ability has not come online yet.</summary>
		Locked,
		/// <summary>Needs the node below it first, or the other weapon was picked.</summary>
		Closed,
		/// <summary>Can be taken once the CORE bar is full.</summary>
		Open,
		/// <summary>The bar is full: this one can be bought now.</summary>
		Buyable,
		Maxed
	}

	public const float Radius = 40f;
	private const float Gap = 14f;
	private const float LabelWidth = 214f;
	private const int NameSize = 20;
	private const float Pip = 11f;

	private static readonly Color Empty = new("221633");
	private static readonly Color Rim = new("986077");
	private static readonly Color DimRim = new("5a3a55");

	public RunUpgrades.Profile Profile { get; init; }
	/// <summary>Name on the left of the badge instead of the right.</summary>
	public bool LabelOnLeft { get; init; }
	public State Current { get; private set; }
	public int Level { get; private set; }

	private Texture2D icon;
	private float pop, time;

	public static Vector2 Footprint => new(Radius * 2f + Gap + LabelWidth, Radius * 2f + 8f);

	/// <summary>Where the badge's centre sits inside this control.</summary>
	public Vector2 Centre => new(LabelOnLeft ? Footprint.X - Radius : Radius, Footprint.Y * 0.5f);

	public override void _Ready()
	{
		icon = GD.Load<Texture2D>($"res://art/cosmic/icon_{Profile.Icon}.svg");
		CustomMinimumSize = Footprint;
		Size = Footprint;
		FocusMode = FocusModeEnum.All;
		MouseDefaultCursorShape = CursorShape.PointingHand;
		var none = new StyleBoxEmpty();
		foreach (string style in new[] { "normal", "hover", "pressed", "hover_pressed", "focus", "disabled" })
			AddThemeStyleboxOverride(style, none);

		FocusEntered += QueueRedraw;
		FocusExited += QueueRedraw;
		MouseEntered += QueueRedraw;
		MouseExited += QueueRedraw;
	}

	public void Set(State state, int level)
	{
		Current = state;
		Level = level;
		QueueRedraw();
	}

	/// <summary>A quick swell when bought.</summary>
	public void Pop() => pop = 1f;

	public override void _Process(double delta)
	{
		time += (float)delta;
		pop = Mathf.Max(0f, pop - (float)delta * 3f);
		// Only a buyable node or one mid-swell is moving; the rest stay still.
		if (pop > 0f || Current == State.Buyable)
			QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 c = Centre;
		bool dim = Current is State.Locked or State.Closed;
		Color tint = Profile.Colour;
		float r = Radius * (1f + 0.2f * Mathf.Sin(pop * Mathf.Pi));

		// Buyable now: a pulsing orange halo, the one thing on the chart that moves.
		if (Current == State.Buyable)
		{
			float beat = 0.5f + 0.5f * Mathf.Sin(time * 5f);
			DrawCircle(c, r + 12f + 4f * beat, new Color(ArcadeSkin.Orange, 0.12f + 0.1f * beat));
		}
		// Pointed at: a cream ring, the same language as a focused menu button.
		if (HasFocus() || IsHovered())
			DrawArc(c, r + 8f, 0f, Mathf.Tau, 56, ArcadeSkin.Cream, 3f, true);

		// The planet: its body, then a soft light on its upper left.
		DrawCircle(c, r, Level > 0 ? tint.Darkened(0.55f) : Empty);
		DrawCircle(c + new Vector2(-r * 0.2f, -r * 0.22f), r * 0.72f, new Color(1f, 0.95f, 0.9f, dim ? 0.03f : 0.07f));
		Color rim = Current switch
		{
			State.Maxed => tint,
			State.Buyable => ArcadeSkin.Orange,
			State.Open => Level > 0 ? tint.Darkened(0.2f) : Rim,
			_ => DimRim
		};
		DrawArc(c, r, 0f, Mathf.Tau, 56, rim, Current is State.Buyable or State.Maxed ? 4f : 3f, true);

		float side = r * 1.2f;
		DrawTextureRect(icon, new Rect2(c - Vector2.One * side * 0.5f, Vector2.One * side), false,
			dim ? new Color(1, 1, 1, 0.3f) : Colors.White);

		// Name, then one pip per rank underneath it.
		Font font = ArcadeSkin.Font;
		float nameWidth = font.GetStringSize(Profile.Name, HorizontalAlignment.Left, -1, NameSize).X;
		float pipsWidth = Profile.MaxLevel * Pip + (Profile.MaxLevel - 1) * 6f;
		float left = LabelOnLeft ? c.X - Radius - Gap : c.X + Radius + Gap;
		float nameX = LabelOnLeft ? left - nameWidth : left;
		float pipsX = LabelOnLeft ? left - pipsWidth : left;

		Color text = dim ? new Color(ArcadeSkin.Muted, 0.55f) : ArcadeSkin.Cream;
		DrawString(font, new Vector2(nameX, c.Y - 4f), Profile.Name, HorizontalAlignment.Left, -1, NameSize, text);
		// One little star per rank: lit in the branch's colour once earned.
		for (int i = 0; i < Profile.MaxLevel; i++)
		{
			var at = new Vector2(pipsX + Pip * 0.5f + i * (Pip + 6f), c.Y + 14f);
			Vector2[] star = Star(at, Pip * 0.7f);
			if (i < Level)
				DrawColoredPolygon(star, tint);
			var outline = new Vector2[star.Length + 1];
			star.CopyTo(outline, 0);
			outline[^1] = star[0];
			DrawPolyline(outline, i < Level ? tint.Lightened(0.35f) : new Color(ArcadeSkin.Muted, dim ? 0.4f : 0.9f), 1.6f, true);
		}
	}

	private static Vector2[] Star(Vector2 at, float size)
	{
		var points = new Vector2[8];
		for (int i = 0; i < 8; i++)
			points[i] = at + Vector2.FromAngle(-Mathf.Pi * 0.5f + Mathf.Tau * i / 8f) * (i % 2 == 0 ? size : size * 0.45f);
		return points;
	}
}
