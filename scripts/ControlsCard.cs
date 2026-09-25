using Godot;

/// <summary>
/// The how-to shown for the first few seconds of a run: pictures of the keys
/// or buttons to press, each with one word under it. The basics (move, aim,
/// shoot) on the left, the three abilities on the right, and only the controls
/// the player is actually holding.
/// </summary>
public partial class ControlsCard : PanelContainer
{
	private static readonly Color Ground = new(0.12f, 0.07f, 0.15f, 0.82f);

	public float UiScale { get; init; } = 1f;

	private HBoxContainer row;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		var panel = ArcadeSkin.Box(Ground, new Color("986077", 0.6f), 22, 2);
		panel.ShadowSize = 0;
		panel.ContentMarginLeft = panel.ContentMarginRight = 30 * UiScale;
		panel.ContentMarginTop = 16 * UiScale;
		panel.ContentMarginBottom = 12 * UiScale;
		AddThemeStyleboxOverride("panel", panel);
		row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", Mathf.RoundToInt(30 * UiScale));
		AddChild(row);
		Build();
	}

	/// <summary>Redraws for keyboard or controller, whichever <see cref="InputDevice"/> says is in use.</summary>
	public void Build()
	{
		if (row == null)
			return;
		foreach (Node child in row.GetChildren())
		{
			row.RemoveChild(child);
			child.QueueFree();
		}

		float unit = 34f * UiScale;
		if (InputDevice.Pad)
		{
			Item("MOVE", Cap(KeyCap.Shape.Stick, "L", unit));
			Item("AIM", Cap(KeyCap.Shape.Stick, "R", unit));
			Item("SHOOT", Cap(KeyCap.Shape.Trigger, "RT", unit));
			Divider(unit);
			Item("DASH", Cap(KeyCap.Shape.FaceButton, "B", unit, new Color("ef6f6c")));
			Item("OVERDRIVE", Cap(KeyCap.Shape.FaceButton, "X", unit, new Color("6fa8ef")));
			Item("NOVA", Cap(KeyCap.Shape.FaceButton, "Y", unit, new Color("f3c76b")));
		}
		else
		{
			// Move as the WASD cluster everyone recognises, from the bound keys.
			var cluster = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
			cluster.AddThemeConstantOverride("separation", Mathf.RoundToInt(2 * UiScale));
			float small = unit * 0.78f;
			var top = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
			top.AddChild(Cap(KeyCap.Shape.Key, Key("up"), small));
			var bottom = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
			bottom.AddThemeConstantOverride("separation", Mathf.RoundToInt(3 * UiScale));
			foreach (string action in new[] { "left", "down", "right" })
				bottom.AddChild(Cap(KeyCap.Shape.Key, Key(action), small));
			cluster.AddChild(top);
			cluster.AddChild(bottom);
			Item("MOVE", cluster);
			Item("AIM", Cap(KeyCap.Shape.Mouse, "", unit * 1.3f));
			Item("SHOOT", Cap(KeyCap.Shape.MouseClick, "", unit * 1.3f));
			Divider(unit);
			Item("DASH", Cap(KeyCap.Shape.Key, Key("dash"), unit));
			Item("OVERDRIVE", Cap(KeyCap.Shape.Key, Key("rapid_fire"), unit));
			Item("NOVA", Cap(KeyCap.Shape.Key, Key("nova"), unit));
		}
	}

	private static string Key(string action) =>
		OS.GetKeycodeString(GameSettings.GetActionKey(action)).ToUpperInvariant();

	private static KeyCap Cap(KeyCap.Shape shape, string text, float unit, Color? tint = null) =>
		new() { Kind = shape, Text = text, Unit = unit, Tint = tint ?? ArcadeSkin.Cream, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };

	/// <summary>One control: the picture, bottom-aligned so every word sits on one line.</summary>
	private void Item(string word, Control picture)
	{
		var item = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
		item.AddThemeConstantOverride("separation", Mathf.RoundToInt(6 * UiScale));
		picture.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		picture.SizeFlagsVertical = SizeFlags.ShrinkEnd | SizeFlags.Expand;
		item.AddChild(picture);
		item.AddChild(ArcadeSkin.Label(word, Mathf.RoundToInt(17 * UiScale), ArcadeSkin.Muted));
		row.AddChild(item);
	}

	private void Divider(float unit)
	{
		var line = new ColorRect { Color = new Color("986077", 0.5f), CustomMinimumSize = new Vector2(2f, unit * 1.6f), MouseFilter = MouseFilterEnum.Ignore };
		line.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		row.AddChild(line);
	}
}
