using Godot;

/// <summary>
/// The boss test picker, opened from the main menu in debug builds: two
/// switches (start fully upgraded, can't die) and one button per boss. See
/// <see cref="BossTest"/>.
/// </summary>
public partial class BossTestPanel : Control
{
	private Button first;

	public override void _Ready()
	{
		VBoxContainer rows = ArcadeSkin.Modal(this, "BOSS TEST", 640);
		rows.AddChild(ArcadeSkin.Label("Just you and a boss. Test runs are never saved.", 22, ArcadeSkin.Muted));

		rows.AddChild(Toggle("START FULLY UPGRADED", BossTest.Upgraded, on => BossTest.Upgraded = on));
		rows.AddChild(Toggle("CAN'T DIE", BossTest.Invincible, on => BossTest.Invincible = on));

		first = Fight(rows, "THE COIL", 0);
		Fight(rows, "THE BROOD", 1);
		Fight(rows, "THE BLACK HOLE", 2);

		Button back = ArcadeSkin.Button("BACK", QueueFree, false, "back");
		rows.AddChild(back);

		ArcadeSkin.Pop(rows.GetParent<Control>());
		first.GrabFocus();
	}

	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("ui_cancel"))
		{
			GetViewport().SetInputAsHandled();
			QueueFree();
		}
	}

	private static HBoxContainer Toggle(string text, bool on, System.Action<bool> changed)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		var label = ArcadeSkin.Label(text, 24);
		label.HorizontalAlignment = HorizontalAlignment.Left;
		label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		row.AddChild(label);
		var toggle = new Button { ToggleMode = true, ButtonPressed = on, Text = on ? "ON" : "OFF", CustomMinimumSize = new Vector2(180, 52) };
		toggle.Toggled += pressed =>
		{
			toggle.Text = pressed ? "ON" : "OFF";
			changed(pressed);
		};
		UiIcons.Switch(toggle);
		row.AddChild(toggle);
		return row;
	}

	private static Button Fight(VBoxContainer rows, string name, int boss)
	{
		Button button = ArcadeSkin.Button($"FIGHT {name}", () =>
		{
			BossTest.FirstBoss = boss;
			BossTest.Active = true;
			SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
		}, boss == 0, "play");
		rows.AddChild(button);
		return button;
	}
}
