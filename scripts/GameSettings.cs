using Godot;

/// <summary>
/// Autoloaded settings store. Owns the audio bus volumes and window mode, and
/// persists them to user://settings.cfg.
/// </summary>
public partial class GameSettings : Node
{
	public static GameSettings Instance { get; private set; }

	/// <summary>Actions the player may rebind, in the order the controls screen lists them.</summary>
	public static readonly (string Action, string Label)[] RebindableActions =
	{
		("up", "Move Up"),
		("down", "Move Down"),
		("left", "Move Left"),
		("right", "Move Right"),
		("shoot", "Shoot"),
		("dash", "Dash"),
		("rapid_fire", "Rapid Fire"),
		("pause", "Pause")
	};

	private const string SavePath = "user://settings.cfg";
	private const string Section = "settings";
	private const string InputSection = "input";
	private const string MusicBus = "Music";
	private const string SfxBus = "SFX";
	private const string MasterBus = "Master";
	private const float MinAudibleLinear = 0.001f;

	public float MasterVolume { get; private set; } = 1.0f;
	public float MusicVolume { get; private set; } = 0.8f;
	public float SfxVolume { get; private set; } = 1.0f;
	public bool Fullscreen { get; private set; } = false;

	private readonly System.Collections.Generic.Dictionary<string, Key> defaultKeys = new();

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		// Captured before any saved bindings are applied, so Reset can restore them.
		CaptureDefaultKeys();

		LoadSettings();
		ApplyAllVolumes();
		ApplyWindowMode();
	}

	private void CaptureDefaultKeys()
	{
		foreach (var (action, _) in RebindableActions)
			defaultKeys[action] = GetActionKey(action);
	}

	/// <summary>The keyboard key currently bound to an action, or None.</summary>
	public static Key GetActionKey(string action)
	{
		if (!InputMap.HasAction(action))
			return Key.None;

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey key)
				return key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
		}

		return Key.None;
	}

	/// <summary>
	/// Rebinds an action's keyboard key. Mouse and gamepad events on the same
	/// action are deliberately left alone.
	/// </summary>
	public void SetActionKey(string action, Key key)
	{
		if (!InputMap.HasAction(action))
			return;

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
		{
			if (inputEvent is InputEventKey existing)
				InputMap.ActionEraseEvent(action, existing);
		}

		// Key.None means "leave this action without a keyboard bind".
		if (key != Key.None)
			InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
	}

	/// <summary>Returns the action already using this key, or null if it is free.</summary>
	public static string FindConflict(string action, Key key)
	{
		foreach (var (other, _) in RebindableActions)
		{
			if (other != action && GetActionKey(other) == key)
				return other;
		}

		return null;
	}

	public void ResetBindings()
	{
		foreach (var (action, _) in RebindableActions)
		{
			if (defaultKeys.TryGetValue(action, out Key key))
				SetActionKey(action, key);
		}

		SaveSettings();
	}

	public override void _Input(InputEvent inputEvent)
	{
		// Always-available escape hatch from fullscreen, independent of any menu.
		if (inputEvent.IsActionPressed("fullscreen_toggle"))
		{
			SetFullscreen(!Fullscreen);
			SaveSettings();
			GetViewport().SetInputAsHandled();
		}
	}

	public void SetMasterVolume(float linear)
	{
		MasterVolume = Mathf.Clamp(linear, 0f, 1f);
		ApplyBusVolume(MasterBus, MasterVolume);
	}

	public void SetMusicVolume(float linear)
	{
		MusicVolume = Mathf.Clamp(linear, 0f, 1f);
		ApplyBusVolume(MusicBus, MusicVolume);
	}

	public void SetSfxVolume(float linear)
	{
		SfxVolume = Mathf.Clamp(linear, 0f, 1f);
		ApplyBusVolume(SfxBus, SfxVolume);
	}

	public void SetFullscreen(bool enabled)
	{
		Fullscreen = enabled;
		ApplyWindowMode();
	}

	private void ApplyAllVolumes()
	{
		ApplyBusVolume(MasterBus, MasterVolume);
		ApplyBusVolume(MusicBus, MusicVolume);
		ApplyBusVolume(SfxBus, SfxVolume);
	}

	private void ApplyWindowMode()
	{
		DisplayServer.WindowSetMode(Fullscreen
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Maximized);
	}

	private static void ApplyBusVolume(string busName, float linear)
	{
		int index = AudioServer.GetBusIndex(busName);
		if (index < 0)
		{
			GD.PushWarning($"GameSettings: audio bus '{busName}' not found.");
			return;
		}

		AudioServer.SetBusMute(index, linear <= MinAudibleLinear);
		AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(Mathf.Max(linear, MinAudibleLinear)));
	}

	public void SaveSettings()
	{
		var config = new ConfigFile();
		config.SetValue(Section, "master_volume", MasterVolume);
		config.SetValue(Section, "music_volume", MusicVolume);
		config.SetValue(Section, "sfx_volume", SfxVolume);
		config.SetValue(Section, "fullscreen", Fullscreen);

		foreach (var (action, _) in RebindableActions)
			config.SetValue(InputSection, action, (int)GetActionKey(action));

		Error error = config.Save(SavePath);
		if (error != Error.Ok)
			GD.PushWarning($"GameSettings: could not write '{SavePath}' ({error}).");
	}

	private void LoadSettings()
	{
		var config = new ConfigFile();
		if (config.Load(SavePath) != Error.Ok)
			return;

		MasterVolume = Mathf.Clamp(config.GetValue(Section, "master_volume", MasterVolume).AsSingle(), 0f, 1f);
		MusicVolume = Mathf.Clamp(config.GetValue(Section, "music_volume", MusicVolume).AsSingle(), 0f, 1f);
		SfxVolume = Mathf.Clamp(config.GetValue(Section, "sfx_volume", SfxVolume).AsSingle(), 0f, 1f);
		Fullscreen = config.GetValue(Section, "fullscreen", Fullscreen).AsBool();

		foreach (var (action, _) in RebindableActions)
		{
			var stored = config.GetValue(InputSection, action, 0).AsInt32();
			if (stored != 0)
				SetActionKey(action, (Key)stored);
		}
	}
}
