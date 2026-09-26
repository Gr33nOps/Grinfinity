using Godot;

/// <summary>
/// One upgrade on the skill tree: a round badge with its icon, and its name
/// and rank pips beside it. Drawn by hand so it reads as a node on a branch
/// rather than a button in a list, but it is still a Button, so mouse, keys
/// and pad all work the usual way.
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

	private static readonly Color Empty = new("2a1a33");
	private static readonly Color Rim = new("986077");
	private static readonly Color DimRim = new("5a3a55");

	public RunUpgrades.Profile Profile { get; init; }
	/// <summary>Name on the left of the badge instead of the right.</summary>
	public bool LabelOnLeft { get; init; }
	public State Current { get; private set; }
	public int Level { get; private set; }

	private Texture2D icon;
	private float pop;

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
		if (pop <= 0f)
			return;
		pop = Mathf.Max(0f, pop - (float)delta * 3f);
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 c = Centre;
		bool dim = Current is State.Locked or State.Closed;
		Color tint = Profile.Colour;
		float r = Radius * (1f + 0.2f * Mathf.Sin(pop * Mathf.Pi));

		// Pointed at: a cream ring, the same language as a focused menu button.
		if (HasFocus() || IsHovered())
			DrawArc(c, r + 8f, 0f, Mathf.Tau, 56, ArcadeSkin.Cream, 3f, true);

		DrawCircle(c, r, Level > 0 ? tint.Darkened(0.66f) : Empty);
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
		for (int i = 0; i < Profile.MaxLevel; i++)
		{
			var at = new Vector2(pipsX + Pip * 0.5f + i * (Pip + 6f), c.Y + 14f);
			if (i < Level)
				DrawCircle(at, Pip * 0.5f, tint);
			DrawArc(at, Pip * 0.5f, 0f, Mathf.Tau, 20, i < Level ? tint.Lightened(0.3f) : new Color(ArcadeSkin.Muted, dim ? 0.4f : 0.9f), 2f, true);
		}
	}
}
