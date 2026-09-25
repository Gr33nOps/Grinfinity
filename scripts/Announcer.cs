using Godot;

/// <summary>
/// The one place the game shouts something at the player: the relic an orbit
/// rolled, an arena event starting, a boss arriving.
///
/// A single banner rather than one per feature, because two announcements
/// fighting for the middle of the screen is worse than either alone — the later
/// one simply replaces whatever is showing.
/// </summary>
public partial class Announcer : Control
{
	[Export] public float HoldTime { get; set; } = 2.6f;
	[Export] public float FadeTime { get; set; } = 0.45f;

	private Label titleLabel;
	private Label detailLabel;
	private Tween tween;

	public override void _Ready()
	{
        foreach(Node child in GetChildren()) if(child is CanvasItem item)item.Hide();
        AnchorLeft=.5f;AnchorRight=.5f;AnchorTop=0;AnchorBottom=0;
        OffsetLeft=-290;OffsetRight=290;OffsetTop=36;OffsetBottom=118;
        MouseFilter=MouseFilterEnum.Ignore;
        var panel=new PanelContainer {MouseFilter=MouseFilterEnum.Ignore};AddChild(panel);ArcadeSkin.Fill(panel);
        panel.AddThemeStyleboxOverride("panel",ArcadeSkin.Box(new Color("392339"),new Color("986077"),20,2));
        var rows=new VBoxContainer {MouseFilter=MouseFilterEnum.Ignore};panel.AddChild(rows);
        titleLabel=ArcadeSkin.Label("",28,ArcadeSkin.Orange);rows.AddChild(titleLabel);
        detailLabel=ArcadeSkin.Label("",21,ArcadeSkin.Muted);rows.AddChild(detailLabel);

		Modulate = new Color(1, 1, 1, 0);
		Visible = false;
	}

	public void Announce(string title, string detail, Color colour)
	{
		if (titleLabel == null)
			return;

		titleLabel.Text = title;
		titleLabel.AddThemeColorOverride("font_color", colour);
		detailLabel.Text = detail;

		tween?.Kill();
		Visible = true;
		Modulate = new Color(1, 1, 1, 0);

		// Punch in, hold, fade. The scale overshoot is what makes it read as an
		// event rather than as a line of text appearing.
		Scale = new Vector2(0.82f, 0.82f);
		PivotOffset = Size * 0.5f;

		tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "modulate:a", 1.0f, FadeTime * 0.5f);
		tween.TweenProperty(this, "scale", Vector2.One, FadeTime)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);

		tween.SetParallel(false);
		tween.TweenInterval(HoldTime);
		tween.TweenProperty(this, "modulate:a", 0.0f, FadeTime);
		tween.TweenCallback(Callable.From(() => Visible = false));
	}
}
