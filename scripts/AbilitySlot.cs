using Godot;

/// <summary>
/// One ability on the HUD: a round button-shaped slot with the icon, a
/// clockwise cooldown sweep, seconds left, and the input under it.
///
/// Locked slots stay visible but faint, so the order Dash, Overdrive, Nova is
/// there to read from the first second. Ready is bright with an orange rim; a
/// slot that has just come ready pops once so it is noticed without a sound.
/// </summary>
public partial class AbilitySlot : Control
{
	private static readonly Color Fill = new("392339");
	private static readonly Color Rim = new("986077");
	private static readonly Color Shade = new(0.08f, 0.04f, 0.1f, 0.72f);

	public Ability Ability { get; init; }
	public float Diameter { get; init; } = 88f;

	/// <summary>How far down the slot the button label's letters end.</summary>
	public float LabelBottom => keyLabel == null ? Diameter
		: keyLabel.Position.Y + keyLabel.GetThemeFont("font").GetAscent(keyLabel.GetThemeFontSize("font_size")) + 2f;

	private TextureRect icon;
	private Label keyLabel;
	private Label countLabel;
	private bool owned;
	private bool ready;
	private float readiness = 1f;
	private float cooldownLeft;
	private float activeShare;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		CustomMinimumSize = new Vector2(Diameter + 40f, Diameter + 34f);

		icon = ArcadeSkin.Icon(RunUpgrades.IconFor(Ability), Diameter * 0.52f);
		AddChild(icon);
		icon.Position = new Vector2((CustomMinimumSize.X - icon.CustomMinimumSize.X) * 0.5f, (Diameter - icon.CustomMinimumSize.Y) * 0.5f);

		countLabel = ArcadeSkin.Label("", Mathf.RoundToInt(Diameter * 0.3f));
		countLabel.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.05f, 0.12f));
		countLabel.AddThemeConstantOverride("outline_size", 8);
		countLabel.Size = new Vector2(CustomMinimumSize.X, Diameter);
		countLabel.VerticalAlignment = VerticalAlignment.Center;
		AddChild(countLabel);

		keyLabel = ArcadeSkin.Label("", Mathf.RoundToInt(Diameter * 0.2f), ArcadeSkin.Muted);
		keyLabel.Position = new Vector2(0f, Diameter + 2f);
		keyLabel.Size = new Vector2(CustomMinimumSize.X, 30f);
		AddChild(keyLabel);
	}

	/// <param name="activeShare">Share of an active effect still running (Overdrive), or 0.</param>
	public void Refresh(bool isOwned, float readinessNow, float secondsLeft, float activeShareNow, string hint)
	{
		bool becameReady = isOwned && readinessNow >= 1f && !ready && owned;
		owned = isOwned;
		readiness = Mathf.Clamp(readinessNow, 0f, 1f);
		ready = owned && readiness >= 1f;
		cooldownLeft = secondsLeft;
		activeShare = activeShareNow;

		keyLabel.Text = owned ? hint : RunUpgrades.AbilityName(Ability);
		countLabel.Text = owned && !ready && activeShare <= 0f ? Mathf.CeilToInt(cooldownLeft).ToString() : "";
		icon.Modulate = !owned ? new Color(0.3f, 0.22f, 0.34f, 0.8f) : ready || activeShare > 0f ? Colors.White : new Color(0.62f, 0.56f, 0.68f);
		Modulate = new Color(1, 1, 1, owned ? 1f : 0.55f);

		if (becameReady)
			Pulse();

		QueueRedraw();
	}

	/// <summary>A quick swell — on unlock, and when a cooldown finishes.</summary>
	public void Pulse()
	{
		PivotOffset = new Vector2(CustomMinimumSize.X * 0.5f, Diameter * 0.5f);
		Scale = Vector2.One * 1.22f;
		CreateTween().TweenProperty(this, "scale", Vector2.One, 0.3f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	public override void _Draw()
	{
		var centre = new Vector2(CustomMinimumSize.X * 0.5f, Diameter * 0.5f);
		float radius = Diameter * 0.5f;

		if (activeShare > 0f)
			DrawCircle(centre, radius + 10f, new Color(PlanetVisual.OverdriveColour, 0.25f));

		DrawCircle(centre, radius, Fill);

		if (owned && !ready && activeShare <= 0f)
		{
			// The part still cooling down, shaded, shrinking clockwise from the top.
			float remaining = 1f - readiness;
			DrawSector(centre, radius - 3f, -Mathf.Pi / 2f + Mathf.Tau * readiness, Mathf.Tau * remaining, Shade);
		}

		Color rim = !owned ? Rim.Darkened(0.35f) : ready ? ArcadeSkin.Orange : Rim;
		DrawArc(centre, radius, 0f, Mathf.Tau, 64, rim, ready ? 5f : 3f, true);

		if (owned && !ready && activeShare <= 0f)
			DrawArc(centre, radius, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * readiness, 64, ArcadeSkin.Cream, 5f, true);

		if (activeShare > 0f)
			DrawArc(centre, radius + 4f, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * activeShare, 64, PlanetVisual.OverdriveColour, 7f, true);
	}

	private void DrawSector(Vector2 centre, float radius, float start, float sweep, Color colour)
	{
		if (sweep <= 0.001f)
			return;

		int segments = Mathf.Max(3, Mathf.CeilToInt(sweep / Mathf.Tau * 48f));
		var points = new Vector2[segments + 2];
		points[0] = centre;
		for (int i = 0; i <= segments; i++)
			points[i + 1] = centre + Vector2.FromAngle(start + sweep * i / segments) * radius;
		DrawColoredPolygon(points, colour);
	}
}
