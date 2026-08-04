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

	// v2 moved rapid fire off Q. v3 adds mode and difficulty. Bumping the
	// version lets LoadSettings drop the stale bind instead of restoring the
	// key the migration exists to escape; a v2 file simply has no mode or
	// difficulty on record and falls back to their defaults.
	private const int SaveVersion = 3;

	public float MasterVolume { get; private set; } = 1.0f;
	public float MusicVolume { get; private set; } = 0.8f;
	public float SfxVolume { get; private set; } = 1.0f;
	public bool Fullscreen { get; private set; } = false;

	/// <summary>Screen shake scale, 0 = off. Standing rule 2: shake ships with its slider.</summary>
	public float ShakeIntensity { get; private set; } = 1.0f;

	/// <summary>Last weapon taken into an orbit.</summary>
	public WeaponId Weapon { get; private set; } = WeaponId.Comet;

	/// <summary>Last world (skin) taken into an orbit.</summary>
	public int World { get; private set; } = 1;

	/// <summary>Last mode taken into an orbit.</summary>
	public GameMode Mode { get; private set; } = GameMode.EndlessOrbit;

	/// <summary>Last difficulty taken into an orbit.</summary>
	public Difficulty Difficulty { get; private set; } = Difficulty.Normal;

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
		Loadout.RestoreWorld(worldId);
		SaveSettings();
	}

	/// <summary>Remembers the chosen mode the same way <see cref="SetWeapon"/> remembers the weapon.</summary>
	public void SetMode(GameMode mode)
	{
		Mode = mode;
		Loadout.RestoreMode(mode);
		SaveSettings();
	}

	/// <summary>Remembers the chosen difficulty the same way <see cref="SetWeapon"/> remembers the weapon.</summary>
	public void SetDifficulty(Difficulty difficulty)
	{
		Difficulty = difficulty;
		Loadout.RestoreDifficulty(difficulty);
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
		config.SetValue(Section, "version", SaveVersion);
		config.SetValue(Section, "master_volume", MasterVolume);
		config.SetValue(Section, "music_volume", MusicVolume);
		config.SetValue(Section, "sfx_volume", SfxVolume);
		config.SetValue(Section, "fullscreen", Fullscreen);
		config.SetValue(Section, "shake_intensity", ShakeIntensity);
		config.SetValue(Section, "weapon", (int)Weapon);
		config.SetValue(Section, "world", World);
		config.SetValue(Section, "mode", (int)Mode);
		config.SetValue(Section, "difficulty", (int)Difficulty);

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
		{
			World = storedWorld;
			Loadout.RestoreWorld(World);
		}

		int storedMode = config.GetValue(Section, "mode", (int)GameMode.EndlessOrbit).AsInt32();
		if (System.Enum.IsDefined(typeof(GameMode), storedMode))
		{
			Mode = (GameMode)storedMode;
			Loadout.RestoreMode(Mode);
		}

		int storedDifficulty = config.GetValue(Section, "difficulty", (int)Difficulty.Normal).AsInt32();
		if (System.Enum.IsDefined(typeof(Difficulty), storedDifficulty))
		{
			Difficulty = (Difficulty)storedDifficulty;
			Loadout.RestoreDifficulty(Difficulty);
		}

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
