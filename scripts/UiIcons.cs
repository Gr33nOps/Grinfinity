using System.Collections.Generic;
using Godot;

/// <summary>
/// The interface icons (art/ui, drawn by tools/make_ui_icons.py) and where each
/// one goes. Buttons carry theirs on the left in the button's own text colour;
/// a setting's row leads with an orange one so a long list can be scanned by
/// shape; a screen's title and its hint line get one too.
///
/// Rows and buttons in the scene files are matched by node name, so the scenes
/// themselves stay plain and every icon choice lives in this one table.
/// </summary>
public static class UiIcons
{
	public const int ButtonIcon = 30;
	public const int RowIcon = 34;

	/// <summary>Row and button node names, and screen roots for their titles.</summary>
	private static readonly Dictionary<string, string> ByNode = new()
	{
		// Buttons
		["BackButton"] = "back", ["ControlsButton"] = "gamepad", ["AccessibilityButton"] = "access",
		["ResetButton"] = "restart",
		// Settings
		["MasterRow"] = "volume", ["MusicRow"] = "music", ["SfxRow"] = "sfx", ["ShakeRow"] = "shake",
		["FullscreenRow"] = "fullscreen", ["ResolutionRow"] = "screen", ["VSyncRow"] = "vsync",
		["FpsCapRow"] = "speed", ["UiScaleRow"] = "textsize",
		// Accessibility
		["ColourblindRow"] = "eye", ["OutlinesRow"] = "contrast", ["RapidFireRow"] = "bolt",
		["AssistRow"] = "snail", ["DamageNumbersRow"] = "number", ["AimAssistRow"] = "crosshair",
		// My Planet
		["NameRow"] = "person", ["Orbits"] = "rocket", ["Kills"] = "pop", ["Timeplayed"] = "clock",
		["Heaviestmass"] = "bars",
		// Controls, one row per action
		["Bind_up"] = "arrow_up", ["Bind_down"] = "arrow_down", ["Bind_left"] = "arrow_left",
		["Bind_right"] = "arrow_right", ["Bind_shoot"] = "crosshair", ["Bind_dash"] = "dash",
		["Bind_rapid_fire"] = "bolt", ["Bind_nova"] = "nova", ["Bind_upgrades"] = "upgrade", ["Bind_pause"] = "pause",
		// Screen titles, by the scene's root node
		["settings"] = "gear", ["controls"] = "gamepad", ["accessibility"] = "access",
		["leaderboard"] = "trophy", ["credits"] = "star", ["stats"] = "planet",
	};

	public static Texture2D Get(string name) => GD.Load<Texture2D>($"res://art/ui/{name}.svg");

	/// <summary>The icon a row, button or screen named <paramref name="node"/> wears, or null.</summary>
	public static string For(string node) => ByNode.TryGetValue(node, out string icon) ? icon : null;

	/// <summary>
	/// Puts <paramref name="name"/> on the button's left, tinted like its text,
	/// inside a round porthole set into the button, with the label lined up
	/// just after it. <paramref name="porthole"/> false is for an icon-only
	/// button, where the icon simply sits in the middle.
	/// </summary>
	public static T On<T>(T button, string name, int size = ButtonIcon, bool porthole = true) where T : Button
	{
		button.Icon = Get(name);
		button.IconAlignment = HorizontalAlignment.Left;
		button.AddThemeConstantOverride("icon_max_width", size);
		button.AddThemeConstantOverride("h_separation", 14);
		if (!porthole)
			return button;

		// A button not yet in the tree cannot see its theme, so it gets its
		// porthole once it is ready.
		void Porthole()
		{
			foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
				if (button.GetThemeStylebox(state) is ArcadeButtonStyle style)
					button.AddThemeStyleboxOverride(state, style.WithBadge(size));
		}
		if (button.IsNodeReady())
			Porthole();
		else
			button.Ready += Porthole;
		button.AddThemeConstantOverride("h_separation", ArcadeButtonStyle.LabelGap);
		button.Alignment = HorizontalAlignment.Left;
		// Tall enough for the porthole to sit inside the face with room to spare.
		button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X, Mathf.Max(button.CustomMinimumSize.Y, size + 34f));
		return button;
	}

	/// <summary>A standalone icon, orange unless told otherwise.</summary>
	public static TextureRect Mark(string name, float size = RowIcon, Color? tint = null) => new()
	{
		Texture = Get(name),
		CustomMinimumSize = new Vector2(size, size),
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
		SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
		SelfModulate = tint ?? ArcadeSkin.Orange,
		MouseFilter = Control.MouseFilterEnum.Ignore,
		Name = "Icon"
	};

	/// <summary>Leads a row with its icon, once.</summary>
	public static void Lead(HBoxContainer row, string name)
	{
		if (row.GetNodeOrNull("Icon") != null)
			return;
		TextureRect mark = Mark(name);
		row.AddChild(mark);
		row.MoveChild(mark, 0);
	}

	/// <summary>
	/// Sets an icon beside a standalone label (a title, a hint) by moving the
	/// label into a centred row with the icon in front of it, in the label's place.
	/// </summary>
	public static HBoxContainer Beside(Label label, string name, float size, Color tint)
	{
		Node parent = label.GetParent();
		int index = label.GetIndex();
		var row = new HBoxContainer { Name = $"{label.Name}Row", Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
		row.AddThemeConstantOverride("separation", 14);
		parent.RemoveChild(label);
		parent.AddChild(row);
		parent.MoveChild(row, index);
		row.AddChild(Mark(name, size, tint));
		row.AddChild(label);
		label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		return row;
	}

	/// <summary>
	/// An on/off button shows a little switch at its right, flipped with the
	/// button, beside its ON / OFF text. The switch keeps its own colours, and
	/// the button itself looks the same either way: the switch says which.
	/// </summary>
	public static void Switch(Button toggle)
	{
		toggle.AddThemeStyleboxOverride("pressed", toggle.GetThemeStylebox("normal"));
		toggle.AddThemeStyleboxOverride("hover_pressed", toggle.GetThemeStylebox("hover"));
		toggle.AddThemeColorOverride("font_pressed_color", ArcadeSkin.Cream);
		toggle.AddThemeColorOverride("font_hover_pressed_color", ArcadeSkin.Cream);
		toggle.IconAlignment = HorizontalAlignment.Right;
		toggle.Alignment = HorizontalAlignment.Left;
		toggle.AddThemeConstantOverride("icon_max_width", 64);
		foreach (string state in new[] { "icon_normal_color", "icon_hover_color", "icon_focus_color", "icon_pressed_color", "icon_hover_pressed_color", "icon_disabled_color" })
			toggle.AddThemeColorOverride(state, Colors.White);
		void Show(bool on) => toggle.Icon = Get(on ? "switch_on" : "switch_off");
		Show(toggle.ButtonPressed);
		toggle.Toggled += Show;
	}
}
