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
		("nova", "Nova"),
		("pause", "Pause")
	};

	private const string SavePath = "user://settings.cfg";
	private const string Section = "settings";
	private const string InputSection = "input";
	private const string MusicBus = "Music";
	private const string SfxBus = "SFX";
	private const string MasterBus = "Master";
	private const float MinAudibleLinear = 0.001f;

	// v2 moved rapid fire off Q. v3 adds mode and difficulty. v4 adds every
	// M7 option and accessibility toggle. Bumping the version lets
	// LoadSettings drop the stale bind instead of restoring the key the
	// migration exists to escape; an older save simply has none of the new
	// fields on record and falls back to their defaults.
	private const int SaveVersion = 4;

	/// <summary>Windowed-mode choices. Fullscreen ignores this and uses the display's own size.</summary>
	public static readonly (int Width, int Height)[] Resolutions =
	{
		(1280, 720), (1600, 900), (1920, 1080), (2560, 1440)
	};

	/// <summary>FPS cap choices. 0 is Godot's own sentinel for "uncapped".</summary>
	public static readonly int[] FpsCaps = { 0, 30, 60, 120, 144 };

	public float MasterVolume { get; private set; } = 1.0f;
	public float MusicVolume { get; private set; } = 0.8f;
	public float SfxVolume { get; private set; } = 1.0f;
	public bool Fullscreen { get; private set; } = false;

	/// <summary>Screen shake scale, 0 = off. Standing rule 2: shake ships with its slider.</summary>
	public float ShakeIntensity { get; private set; } = 1.0f;

	/// <summary>Last weapon taken into an orbit.</summary>
	public WeaponId Weapon { get; private set; } = WeaponId.Comet;

	/// <summary>Last world (skin) worn. Cosmetic only — never gates a run.</summary>
	public int World { get; private set; } = 1;

	// --- Video --------------------------------------------------------------
	/// <summary>Index into <see cref="Resolutions"/>. Only applied while not fullscreen.</summary>
	public int ResolutionIndex { get; private set; } = 2;
	public bool VSyncEnabled { get; private set; } = true;
	/// <summary>Index into <see cref="FpsCaps"/>.</summary>
	public int FpsCapIndex { get; private set; } = 0;
	/// <summary>Scales the in-game HUD only — gameplay art stays at its native size.</summary>
	public float UiScale { get; private set; } = 1.0f;

	// --- Accessibility --------------------------------------------------------
	public bool ShowDamageNumbers { get; private set; } = true;
	/// <summary>Gently pulls gamepad aim toward the nearest body within a narrow cone.</summary>
	public bool GamepadAimAssist { get; private set; } = false;
	public bool ColourblindMode { get; private set; } = false;
	public bool HighContrastOutlines { get; private set; } = false;
	/// <summary>True: rapid fire only runs while the button is held. False (default): press once, it runs for its own duration.</summary>
	public bool RapidFireHoldMode { get; private set; } = false;
	/// <summary>An extra, gentler speed cut on top of whatever Difficulty already applies.</summary>
	public bool AssistMode { get; private set; } = false;

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
		ApplyVSync();
		ApplyFpsCap();
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

	public void SetShakeIntensity(float scale)
	{
		ShakeIntensity = Mathf.Clamp(scale, 0f, 1f);
	}

	/// <summary>
	/// Remembers the weapon so a restart does not ask again. Also pushes it into
	/// <see cref="Loadout"/>: persisting without doing so left the file and the
	/// live choice one launch out of step.
	/// </summary>
	public void SetWeapon(WeaponId weapon)
	{
		Weapon = weapon;
		Loadout.Restore(weapon);
		SaveSettings();
	}

	/// <summary>Remembers the chosen world the same way <see cref="SetWeapon"/> remembers the weapon.</summary>
	public void SetWorld(int worldId)
	{
		World = worldId;
		SaveSettings();
	}

	public void SetResolutionIndex(int index)
	{
		ResolutionIndex = Mathf.Clamp(index, 0, Resolutions.Length - 1);
		ApplyWindowMode();
		SaveSettings();
	}

	public void SetVSyncEnabled(bool enabled)
	{
		VSyncEnabled = enabled;
		ApplyVSync();
		SaveSettings();
	}

	public void SetFpsCapIndex(int index)
	{
		FpsCapIndex = Mathf.Clamp(index, 0, FpsCaps.Length - 1);
		ApplyFpsCap();
		SaveSettings();
	}

	public void SetUiScale(float scale)
	{
		UiScale = Mathf.Clamp(scale, 0.85f, 1.3f);
		SaveSettings();
	}

	public void SetShowDamageNumbers(bool enabled)
	{
		ShowDamageNumbers = enabled;
		SaveSettings();
	}

	public void SetGamepadAimAssist(bool enabled)
	{
		GamepadAimAssist = enabled;
		SaveSettings();
	}

	public void SetColourblindMode(bool enabled)
	{
		ColourblindMode = enabled;
		SaveSettings();
	}

	public void SetHighContrastOutlines(bool enabled)
	{
		HighContrastOutlines = enabled;
		SaveSettings();
	}

	public void SetRapidFireHoldMode(bool enabled)
	{
		RapidFireHoldMode = enabled;
		SaveSettings();
	}

	public void SetAssistMode(bool enabled)
	{
		AssistMode = enabled;
		SaveSettings();
	}

	private void ApplyAllVolumes()
	{
		ApplyBusVolume(MasterBus, MasterVolume);
		ApplyBusVolume(MusicBus, MusicVolume);
		ApplyBusVolume(SfxBus, SfxVolume);
	}

	private void ApplyWindowMode()
	{
		if (Fullscreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			return;
		}

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);

		(int width, int height) = Resolutions[Mathf.Clamp(ResolutionIndex, 0, Resolutions.Length - 1)];
		var size = new Vector2I(width, height);
		DisplayServer.WindowSetSize(size);

		// A windowed resolution that opens off to one side reads as broken.
		Vector2I screen = DisplayServer.ScreenGetSize();
		DisplayServer.WindowSetPosition((screen - size) / 2);
	}

	private void ApplyVSync()
	{
		DisplayServer.WindowSetVsyncMode(VSyncEnabled
			? DisplayServer.VSyncMode.Enabled
			: DisplayServer.VSyncMode.Disabled);
	}

	private void ApplyFpsCap()
	{
		Engine.MaxFps = FpsCaps[Mathf.Clamp(FpsCapIndex, 0, FpsCaps.Length - 1)];
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
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "master_volume", MasterVolume);
		config.SetValue(Section, "music_volume", MusicVolume);
		config.SetValue(Section, "sfx_volume", SfxVolume);
		config.SetValue(Section, "fullscreen", Fullscreen);
		config.SetValue(Section, "shake_intensity", ShakeIntensity);
		config.SetValue(Section, "weapon", (int)Weapon);
		config.SetValue(Section, "world", World);
		config.SetValue(Section, "resolution_index", ResolutionIndex);
		config.SetValue(Section, "vsync", VSyncEnabled);
		config.SetValue(Section, "fps_cap_index", FpsCapIndex);
		config.SetValue(Section, "ui_scale", UiScale);
		config.SetValue(Section, "show_damage_numbers", ShowDamageNumbers);
		config.SetValue(Section, "gamepad_aim_assist", GamepadAimAssist);
		config.SetValue(Section, "colourblind_mode", ColourblindMode);
		config.SetValue(Section, "high_contrast_outlines", HighContrastOutlines);
		config.SetValue(Section, "rapid_fire_hold_mode", RapidFireHoldMode);
		config.SetValue(Section, "assist_mode", AssistMode);

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
		ShakeIntensity = Mathf.Clamp(config.GetValue(Section, "shake_intensity", ShakeIntensity).AsSingle(), 0f, 1f);

		int storedWeapon = config.GetValue(Section, "weapon", 0).AsInt32();
		if (System.Enum.IsDefined(typeof(WeaponId), storedWeapon))
		{
			Weapon = (WeaponId)storedWeapon;
			Loadout.Restore(Weapon);
		}

		int storedWorld = config.GetValue(Section, "world", 1).AsInt32();
		if (storedWorld is >= 1 and <= 12)
			World = storedWorld;

		// "mode" and "difficulty" may still be sitting in an older settings
		// file. They are read by nothing now and are left to rot rather than
		// migrated — there is nothing they could be migrated into.

		ResolutionIndex = Mathf.Clamp(config.GetValue(Section, "resolution_index", ResolutionIndex).AsInt32(), 0, Resolutions.Length - 1);
		VSyncEnabled = config.GetValue(Section, "vsync", VSyncEnabled).AsBool();
		FpsCapIndex = Mathf.Clamp(config.GetValue(Section, "fps_cap_index", FpsCapIndex).AsInt32(), 0, FpsCaps.Length - 1);
		UiScale = Mathf.Clamp(config.GetValue(Section, "ui_scale", UiScale).AsSingle(), 0.85f, 1.3f);
		ShowDamageNumbers = config.GetValue(Section, "show_damage_numbers", ShowDamageNumbers).AsBool();
		GamepadAimAssist = config.GetValue(Section, "gamepad_aim_assist", GamepadAimAssist).AsBool();
		ColourblindMode = config.GetValue(Section, "colourblind_mode", ColourblindMode).AsBool();
		HighContrastOutlines = config.GetValue(Section, "high_contrast_outlines", HighContrastOutlines).AsBool();
		RapidFireHoldMode = config.GetValue(Section, "rapid_fire_hold_mode", RapidFireHoldMode).AsBool();
		AssistMode = config.GetValue(Section, "assist_mode", AssistMode).AsBool();

		int version = config.GetValue(Section, "version", 1).AsInt32();

		foreach (var (action, _) in RebindableActions)
		{
			var stored = config.GetValue(InputSection, action, 0).AsInt32();
			if (stored == 0)
				continue;

			// v1 saves carry the old Q default for rapid fire. Some setups emit
			// phantom Q presses, so restoring it would self-trigger the ability.
			if (version < SaveVersion && action == "rapid_fire" && (Key)stored == Key.Q)
				continue;

			SetActionKey(action, (Key)stored);
		}
	}
}
