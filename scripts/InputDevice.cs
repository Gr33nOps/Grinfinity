using System;
using Godot;

/// <summary>
/// Which kind of controls the player is holding right now: keyboard and mouse,
/// or a controller. Every on-screen hint asks this and shows only that one set
/// of buttons, so nobody has to read "SHIFT / B" and work out which half is
/// theirs.
///
/// The last thing touched wins. A stick has to be pushed properly, and the
/// mouse has to actually travel, so a resting stick's drift or a nudged desk
/// does not flip the hints back and forth.
/// </summary>
public partial class InputDevice : Node
{
	private const float StickThreshold = 0.35f;
	private const float MouseThreshold = 6f;

	/// <summary>True while the player is on a controller.</summary>
	public static bool Pad { get; private set; }

	/// <summary>Fires when the player switches between keyboard and controller.</summary>
	public static event Action Changed;

	public override void _Ready()
	{
		// Keeps listening while the game is paused, so the upgrade screen and
		// pause menu switch too.
		ProcessMode = ProcessModeEnum.Always;
		Pad = Input.GetConnectedJoypads().Count > 0;
		Input.JoyConnectionChanged += OnJoyConnectionChanged;
	}

	public override void _ExitTree()
	{
		Input.JoyConnectionChanged -= OnJoyConnectionChanged;
	}

	public override void _Input(InputEvent inputEvent)
	{
		bool pad = inputEvent is InputEventJoypadButton { Pressed: true }
			|| inputEvent is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > StickThreshold;
		bool keys = inputEvent is InputEventKey { Pressed: true }
			|| inputEvent is InputEventMouseButton { Pressed: true }
			|| inputEvent is InputEventMouseMotion mouse && mouse.Relative.Length() > MouseThreshold;

		if (pad)
			Use(true);
		else if (keys)
			Use(false);
	}

	private static void OnJoyConnectionChanged(long device, bool connected)
	{
		// Plugging a controller in is a strong hint it is about to be used;
		// pulling the last one out means it certainly is not.
		if (connected)
			Use(true);
		else if (Input.GetConnectedJoypads().Count == 0)
			Use(false);
	}

	private static void Use(bool pad)
	{
		if (Pad == pad)
			return;
		Pad = pad;
		Changed?.Invoke();
	}
}
