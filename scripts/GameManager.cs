using System.Collections.Generic;
using Godot;

/// <summary>What finished an enemy off. Decides how loud the kill is.</summary>
public enum KillSource
{
	Shot,
	Dash,
	Nova
}

/// <summary>
/// Runs one endless run: the bosses, the kill payoff, the drops, pause, death
/// and the hand-over to the recap. Enemy pressure lives in
/// <see cref="BodySpawner"/>; the run's numbers in <see cref="RunState"/>.
///
/// Bosses come on a clock, not a kill count: Coil, Brood, Black Hole, then round
/// again, forever. The first time round each is fought alone. After that the
/// ordinary enemies keep coming during the fight, more of them each cycle —
/// the bosses get only a little tougher themselves. Beating one scatters three
/// strong upgrades from the wreck.
/// </summary>
public partial class GameManager : Node2D
{
	[ExportGroup("Feel")]
	/// <summary>Time scale held during a hitstop. Low enough to read as a freeze.</summary>
	[Export] public float HitstopScale { get; set; } = 0.04f;
	[Export] public float LightKillHitstop { get; set; } = 0.035f;
	[Export] public float HeavyKillHitstop { get; set; } = 0.075f;
	[Export] public float DeathHitstop { get; set; } = 0.14f;
	[Export] public float LightKillTrauma { get; set; } = 0.15f;
	[Export] public float HeavyKillTrauma { get; set; } = 0.38f;
	[Export] public float DeathTrauma { get; set; } = 1.0f;

	[ExportGroup("Effects")]
	[Export] public PackedScene DebrisScene { get; set; }
	/// <summary>Ceiling on live chunks, so a Nova cannot flood the arena.</summary>
	[Export] public int MaxDebris { get; set; } = 160;
	[Export] public PackedScene PowerUpScene { get; set; }

	[ExportGroup("Bosses")]
	[Export] public PackedScene CoilScene { get; set; }
	[Export] public PackedScene BroodScene { get; set; }
	[Export] public PackedScene BlackHoleScene { get; set; }
	/// <summary>
	/// How many bosses have arrived. The next is Coil, Brood or Black Hole by this
	/// count, and its cycle is this over three. Exported so a tool can skip ahead.
	/// </summary>
	[Export] public int NextBossIndex { get; set; } = 0;
	/// <summary>Survival time the next boss is due. Exported so a tool can bring it forward.</summary>
	[Export] public float NextBossAt { get; set; } = Balance.FirstBossAt;
	/// <summary>Time scale held while a boss dies. Slow motion, not a freeze.</summary>
	[Export] public float BossKillSlowMo { get; set; } = 0.22f;
	[Export] public float BossKillSlowMoTime { get; set; } = 1.1f;

	private PauseMenu pauseMenu;
	private RunState run;
	private BodySpawner bodySpawner;
	private EventDirector eventDirector;
	private HazardDirector hazardDirector;
	private AchievementTracker achievementTracker;
	private UIManager uiManager;
	private PlayerManager playerManager;
	private GameCamera gameCamera;
	private Player player;
	private Node2D entities;
	private AudioStreamPlayer killSound;
	private AudioStreamPlayer streakSound;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private ColorRect flash;
	private Tween flashTween;
	private readonly Dictionary<string, AudioStreamPlayer> cues = new();
	private bool isPaused = false;
	private bool isGameOver = false;
	private bool isDying = false;
	private ulong lastKillSoundMsec;

	private bool hitstopActive = false;
	private ulong hitstopEndMsec = 0;

	private Boss boss;
	private Control bossBar;
	private ProgressBar bossHealth;
	private Label bossNameLabel;
	private Announcer announcer;
	private float warningLeft = -1f;
	private Vector2 incomingAt;
	private Node2D incomingMarker;

	public bool IsPaused => isPaused;

	/// <summary>True from the killing hit on. Read by the playtest tools.</summary>
	public bool IsOver => isDying || isGameOver;

	/// <summary>What ended this run, once it has ended.</summary>
	public string DeathCause { get; private set; } = "";

	/// <summary>True while a boss is on the field.</summary>
	public bool BossActive => boss != null && IsInstanceValid(boss);

	/// <summary>True from the warning until the boss is beaten.</summary>
	public bool BossBusy => BossActive || warningLeft > 0f;

	/// <summary>Which time round the bosses the current or next one belongs to, from 1.</summary>
	public int BossCycle => (BossActive ? NextBossIndex - 1 : NextBossIndex) / 3 + 1;

	public RunState Run => run;

	/// <summary>Seconds survived so far.</summary>
	public float RunTime => run?.SurvivalTime ?? 0f;

	// _EnterTree runs top-down, _Ready bottom-up. The player and everything under
	// it read the run during their own _Ready, which is *before* this node's, so
	// the group registration and RunState have to be in place by here.
	public override void _EnterTree()
	{
		RunState.Rng.Randomize();
		GameOver.NewlyUnlockedAchievement = null;
		GameOver.DeathCause = "";
		AddToGroup("game_manager");
		run = AddPausableChild(new RunState());
	}

	public override void _Ready()
	{
		SetupComponents();
		ConnectSignals();
		AddChild(new ArenaBackdrop());
	}

	/// <summary>The game manager for the current scene, or null outside gameplay.</summary>
	public static GameManager Of(Node context)
	{
		// Skips a run that is on its way out, so nothing in a new run can latch
		// onto the one being freed in the same frame.
		foreach (Node node in context.GetTree().GetNodesInGroup("game_manager"))
		{
			if (node is GameManager manager && !manager.IsQueuedForDeletion())
				return manager;
		}

		return null;
	}

	private void SetupComponents()
	{
		pauseMenu = GetNode<PauseMenu>("PauseLayer/PauseMenu");
		entities = GetNode<Node2D>("Entities");
		gameCamera = GetNodeOrNull<GameCamera>("GameCamera");
		player = GetNode<Player>("player");
		killSound = GetNode<AudioStreamPlayer>("KillSound");
		streakSound = GetNodeOrNull<AudioStreamPlayer>("StreakSound");
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("HoverSound");
		streakSound.Stream = GD.Load<AudioStream>("res://sounds/streak_chirp.wav");

		// Cues with no dedicated sample yet borrow a close one at another pitch.
		AddCue("upgrade_chirp", "upgrade_chirp", -12f, 1f);
		AddCue("shield_get", "pickup_chirp", -9f, 0.8f);
		AddCue("shield_pop", "shield_pop", -8f, 1f);
		AddCue("dash_swish", "dash_swish", -10f, 1f);
		AddCue("nova_boom", "nova_boom", -4f, 0.85f);
		AddCue("overdrive", "upgrade_chirp", -7f, 0.62f);
		AddCue("overdrive_end", "pickup_chirp", -16f, 0.7f);
		AddCue("unlock", "streak_chirp", -6f, 1.2f);
		AddCue("boss_warning", "streak_chirp", -4f, 0.5f);
		AddCue("comet_warning", "dash_swish", -8f, 0.55f);

		DebrisScene ??= GD.Load<PackedScene>("res://scenes/debris.tscn");
		CoilScene ??= GD.Load<PackedScene>("res://scenes/boss_coil.tscn");
		BroodScene ??= GD.Load<PackedScene>("res://scenes/boss_brood.tscn");
		BlackHoleScene ??= GD.Load<PackedScene>("res://scenes/boss_black_hole.tscn");
		PowerUpScene ??= GD.Load<PackedScene>("res://scenes/power_up.tscn");

		announcer = GetNodeOrNull<Announcer>("UI/Announcer");
		bossBar = GetNodeOrNull<Control>("UI/BossBar");
		bossHealth = GetNodeOrNull<ProgressBar>("UI/BossBar/Health");
		bossNameLabel = GetNodeOrNull<Label>("UI/BossBar/Name");
		if (bossBar != null)
			bossBar.Visible = false;

		// Under the HUD, over the world: the one layer a Nova or a broken shield
		// is allowed to wash.
		flash = new ColorRect { Name = "Flash", MouseFilter = Control.MouseFilterEnum.Ignore, Color = new Color(1, 1, 1, 0) };
		var ui = GetNode<CanvasLayer>("UI");
		ui.AddChild(flash);
		ui.MoveChild(flash, 0);
		flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		bodySpawner = AddPausableChild(new BodySpawner());
		eventDirector = AddPausableChild(new EventDirector());
		hazardDirector = AddPausableChild(new HazardDirector());
		achievementTracker = AddPausableChild(new AchievementTracker());
		uiManager = AddPausableChild(new UIManager());
		playerManager = AddPausableChild(new PlayerManager());
	}

	private void AddCue(string cue, string file, float volume, float pitch)
	{
		var sound = new AudioStreamPlayer { Stream = GD.Load<AudioStream>($"res://sounds/{file}.wav"), Bus = "SFX", VolumeDb = volume, PitchScale = pitch };
		AddChild(sound);
		cues[cue] = sound;
	}

	// The game root runs with ProcessMode.Always so it can still read the pause
	// key while the tree is paused. Everything it owns must opt back out.
	private T AddPausableChild<T>(T node) where T : Node
	{
		node.ProcessMode = ProcessModeEnum.Pausable;
		node.Name = typeof(T).Name;
		AddChild(node);
		return node;
	}

	private void ConnectSignals()
	{
		pauseMenu.ResumeGame += OnResumeGame;
		pauseMenu.GiveUpGame += OnGiveUpGame;
		run.StreakChanged += OnStreakChanged;
		run.AbilityUnlocked += OnAbilityUnlocked;
	}

	/// <summary>Shouts something at the top of the screen. See <see cref="Announcer"/>.</summary>
	public void Announce(string title, string detail, Color colour)
	{
		if (isPaused || isDying || isGameOver) return;
		announcer?.Announce(title, detail, colour);
	}

	/// <summary>A small, quick line above the ability bar — for pickups, never for bosses.</summary>
	public void Toast(string text, Color colour)
	{
		if (isPaused || isDying || isGameOver) return;
		uiManager?.Toast(text, colour);
	}

	/// <summary>Parents runtime-spawned nodes under the pausable entity container.</summary>
	public void AddEntity(Node entity)
	{
		entities.AddChild(entity);
	}

	/// <summary>
	/// Parents a runtime-spawned node under the pausable entity container so it
	/// pauses along with the rest of the game. Falls back to the current scene.
	/// </summary>
	public static void Spawn(Node context, Node node)
	{
		if (context.GetTree().GetFirstNodeInGroup("game_manager") is GameManager manager)
		{
			manager.AddEntity(node);
			return;
		}

		Node fallback = context.GetTree().CurrentScene;
		if (fallback != null)
			fallback.AddChild(node);
		else
			node.QueueFree();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (isGameOver || isDying)
			return;

		if (inputEvent.IsActionPressed("pause"))
		{
			if (isPaused && pauseMenu.CloseOptions()) { GetViewport().SetInputAsHandled(); return; }
			TogglePause();
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationApplicationFocusOut && IsNodeReady() && !isPaused && !isDying && !isGameOver)
			TogglePause();
	}

	// Runs with ProcessMode.Always, so hitstop is measured against wall-clock
	// time: delta is scaled by Engine.TimeScale and would stretch with it.
	public override void _Process(double delta)
	{
		if (!isPaused && !isGameOver && !isDying)
			UpdateBosses((float)delta);

		if (!hitstopActive)
			return;

		if (isPaused || isGameOver || Time.GetTicksMsec() >= hitstopEndMsec)
			EndHitstop();
	}

	// --- Bosses ---------------------------------------------------------------

	/// <summary>Scheduled arrival of the nth boss, before any breather pushes it back.</summary>
	public static float ScheduledBossTime(int index)
	{
		if (index < 3)
			return Balance.FirstBossAt + index * Balance.BossGapFirstCycle;
		return Balance.FirstBossAt + 2 * Balance.BossGapFirstCycle + (index - 2) * Balance.BossGapLaterCycles;
	}

	private void UpdateBosses(float delta)
	{
		int cycle = BossCycle;

		if (BossActive)
		{
			bodySpawner.Support = Balance.BossSupport(cycle);
			return;
		}

		if (warningLeft > 0f)
		{
			// First time round, the arena is left to thin out before the boss.
			bodySpawner.Support = Balance.BossSupport(cycle);
			warningLeft -= delta;
			if (warningLeft <= 0f)
				SpawnBoss();
			return;
		}

		bodySpawner.Support = 1f;
		if (run.SurvivalTime >= NextBossAt)
			BeginBossWarning();
	}

	private (PackedScene scene, string name, Color colour) NextBoss() => (NextBossIndex % 3) switch
	{
		0 => (CoilScene, "THE COIL", new Color(0.86f, 0.72f, 1.0f)),
		1 => (BroodScene, "THE BROOD", new Color(0.58f, 0.82f, 0.4f)),
		_ => (BlackHoleScene, "THE BLACK HOLE", new Color(0.62f, 0.32f, 0.82f))
	};

	private string ArrivalLine() => (NextBossIndex % 3) switch
	{
		0 => TranslationServer.Translate("BOSS_Coil_ARRIVAL"),
		1 => TranslationServer.Translate("BOSS_Brood_ARRIVAL"),
		_ => TranslationServer.Translate("BOSS_BlackHole_ARRIVAL")
	};

	/// <summary>
	/// A clear warning first: the name at the top of the screen and a pulsing
	/// marker where it will appear, well away from the planet.
	/// </summary>
	private void BeginBossWarning()
	{
		var (_, name, colour) = NextBoss();
		warningLeft = Balance.BossWarning;
		incomingAt = PickBossArrival();

		string detail = BossCycle > 1 ? $"ROUND {BossCycle}  •  {ArrivalLine()}" : ArrivalLine();
		Announce($"{name} IS COMING", detail, colour);
		PlayCue("boss_warning");
		Shake(0.3f);

		incomingMarker = new BossMarker { Colour = colour, Duration = Balance.BossWarning };
		incomingMarker.GlobalPosition = incomingAt;
		AddEntity(incomingMarker);
	}

	/// <summary>
	/// Toward open space from the planet, inside the arena. The offset is an
	/// ellipse rather than a circle: wide sideways, shorter up and down, so the
	/// marker and the boss land on screen however the planet is placed.
	/// </summary>
	private Vector2 PickBossArrival()
	{
		Vector2 from = player.GlobalPosition;
		Vector2 toward = (Arena.Centre - from).LengthSquared() > 1f
			? (Arena.Centre - from).Normalized()
			: Vector2.FromAngle(RunState.Rng.Randf() * Mathf.Tau);

		Vector2 best = from;
		float bestDistance = -1f;
		foreach (float turn in new[] { 0f, 0.6f, -0.6f, 1.2f, -1.2f, 2.0f, -2.0f, Mathf.Pi })
		{
			Vector2 dir = toward.Rotated(turn);
			Vector2 offset = new Vector2(dir.X, dir.Y * 0.62f) * Balance.BossArrivalDistance;
			Vector2 candidate = Arena.ClampToPlayable(from + offset, 220f);
			float distance = candidate.DistanceTo(from);
			if (distance >= offset.Length() * 0.9f)
				return candidate;
			if (distance > bestDistance)
			{
				bestDistance = distance;
				best = candidate;
			}
		}

		return best;
	}

	private void SpawnBoss()
	{
		warningLeft = -1f;
		if (IsInstanceValid(incomingMarker))
			incomingMarker.QueueFree();

		var (scene, name, _) = NextBoss();
		if (scene == null)
			return;

		// The planet may have wandered toward the marker during the warning.
		// Arriving on top of it would be the one unfair thing a boss can do.
		Vector2 at = incomingAt;
		Vector2 away = at - player.GlobalPosition;
		if (away.Length() < 400f)
			at = Arena.ClampToPlayable(player.GlobalPosition + (away.LengthSquared() > 1f ? away.Normalized() : Vector2.Right) * 520f, 220f);
		if (at.DistanceTo(player.GlobalPosition) < 400f)
			at = PickBossArrival();

		boss = scene.Instantiate<Boss>();
		boss.Cycle = NextBossIndex / 3 + 1;
		boss.GlobalPosition = at;
		NextBossIndex++;

		boss.HealthChanged += OnBossHealthChanged;
		boss.Defeated += OnBossDefeated;
		AddEntity(boss);

		if (bossNameLabel != null)
			bossNameLabel.Text = boss.Cycle > 1 ? $"{name}  •  ROUND {boss.Cycle}" : name;
		if (bossHealth != null)
		{
			// A fresh stylebox per encounter, so each boss reads in its own colour.
			bossHealth.AddThemeStyleboxOverride("fill", new StyleBoxFlat
			{
				BgColor = boss.BossColor,
				CornerRadiusTopLeft = 6,
				CornerRadiusTopRight = 6,
				CornerRadiusBottomRight = 6,
				CornerRadiusBottomLeft = 6
			});
		}
		OnBossHealthChanged(1.0f);
		if (bossBar != null)
			bossBar.Visible = true;

		SpawnBlast(at, 360f, boss.BossColor);
		Shake(0.55f);
		PlayStreakSting(0.6f);
	}

	private void OnBossHealthChanged(float fraction)
	{
		if (bossHealth != null)
			bossHealth.Value = fraction * 100.0;
	}

	private void OnBossDefeated()
	{
		if (bossBar != null)
			bossBar.Visible = false;

		Vector2 at = IsInstanceValid(boss) ? boss.GlobalPosition : player.GlobalPosition;
		Color colour = IsInstanceValid(boss) ? boss.BossColor : new Color(0.86f, 0.72f, 1.0f);
		int cycle = IsInstanceValid(boss) ? boss.Cycle : 1;
		int kind = (NextBossIndex - 1) % 3;
		boss = null;

		NextBossAt = Mathf.Max(ScheduledBossTime(NextBossIndex), run.SurvivalTime + Balance.BossBreather);

		// Slow motion rather than a freeze: the payoff is watching it come apart.
		Hitstop(BossKillSlowMoTime, BossKillSlowMo);
		Shake(1.1f);
		SpawnBlast(at, 560f, colour);
		ShedChunks(at, 26, colour);
		Flash(colour, 0.22f, 0.4f);
		run.AddBonus(Balance.BossScoreBonus * cycle);
		PlayStreakSting(0.45f);
		ClearHostileShots();

		DropBossRewards(at);

		UnlockBossAchievement(kind switch
		{
			0 => AchievementId.BeatCoil,
			1 => AchievementId.BeatBrood,
			_ => AchievementId.BeatBlackHole
		});
	}

	/// <summary>
	/// A beaten boss takes its shots with it. Otherwise the reward is the last
	/// ring still in the air, and the win turns into a death on the way to it.
	/// </summary>
	private void ClearHostileShots()
	{
		foreach (Node node in GetTree().GetNodesInGroup("hostile_bullets"))
		{
			if (node is not Node2D shot || !IsInstanceValid(shot))
				continue;
			SpawnImpact(shot.GlobalPosition, new Color(1f, 0.55f, 0.5f));
			shot.QueueFree();
		}
	}

	/// <summary>
	/// Three strong, useful upgrades thrown out of the wreck — random, but only
	/// from things that would help right now. Never a menu.
	/// </summary>
	private void DropBossRewards(Vector2 at)
	{
		var pending = PendingRewards();
		var batch = new List<Reward>();
		for (int i = 0; i < Balance.BossRewardCount; i++)
		{
			var considered = new List<Reward>(pending);
			considered.AddRange(batch);
			if (UpgradeDrops.TryRoll(run, considered, boss: true, out Reward reward))
				batch.Add(reward);
		}

		if (batch.Count == 0)
		{
			// A finished build: nothing left to give but points.
			run.AddBonus(Balance.BossScoreBonus);
			Toast($"FULLY LOADED  •  +{Balance.BossScoreBonus:N0}", ArcadeSkin.Orange);
			return;
		}

		float start = RunState.Rng.Randf() * Mathf.Tau;
		for (int i = 0; i < batch.Count; i++)
		{
			if (batch[i].IsShield)
				run.LastShieldDropAt = run.SurvivalTime;
			Vector2 rest = at + Vector2.FromAngle(start + Mathf.Tau * i / batch.Count) * 170f;
			SpawnPickup(batch[i], at, Balance.BossRewardLifetime, rest);
		}
	}

	private void UnlockBossAchievement(AchievementId id)
	{
		if (!PlayerProfile.UnlockAchievement(id))
			return;

		Achievements.Profile profile = Achievements.Get(id);
		Toast($"ACHIEVEMENT  •  {profile.Name}", new Color(1.0f, 0.72f, 0.32f));
	}

	// --- Abilities -----------------------------------------------------------

	private void OnAbilityUnlocked(int which)
	{
		var ability = (Ability)which;
		Announce($"{RunUpgrades.AbilityName(ability)} ONLINE", $"Press {UIManager.ControlHint(ability)} to {RunUpgrades.AbilityVerb(ability)}", ArcadeSkin.Orange);
		PlayCue("unlock");
		Shake(0.25f);
		SpawnBlast(player.GlobalPosition, 300f, ArcadeSkin.Orange);
		player.GetNodeOrNull<PlanetVisual>("PlanetVisual")?.Celebrate();
		uiManager?.PulseAbility(ability);
	}

	/// <summary>Sets off a Nova here. The planet decides when; this does the rest.</summary>
	public void DetonateNova(Vector2 at, float radius)
	{
		var blast = new NovaBlast { MaxRadius = radius };
		blast.GlobalPosition = at;
		AddEntity(blast);

		ShedChunks(at, 16, new Color(1f, 0.86f, 0.55f), 3.2f);
		PopEffect.Spawn(this, at, 150f, new Color("ffd66b"), 14, 0.6f);

		PlayCue("nova_boom");
		// Warm and short. A cooler or longer wash read as the screen going grey.
		Flash(new Color("ffc46b"), 0.16f, 0.2f);
		Hitstop(0.1f);
	}

	// --- Run flow -----------------------------------------------------------

	public override void _ExitTree()
	{
		// Engine.TimeScale is global; a scene change mid-freeze must not leak it.
		if (hitstopActive)
			EndHitstop();
	}

	private void TogglePause()
	{
		isPaused = !isPaused;
		GetTree().Paused = isPaused;
		GetNode<CanvasLayer>("UI").Visible = !isPaused;
		uiManager.SetGameplayVisible(!isPaused);

		if (isPaused)
		{
			EndHitstop();
			pauseMenu.ShowPauseMenu();
			uiManager.ShowCursor();
		}
		else
		{
			pauseMenu.HidePauseMenu();
			uiManager.HideCursor();
		}
	}

	public void RestartOrbit()
	{
		if (isGameOver || isDying) return;
		isGameOver = true;
		EndHitstop();
		FreezeSimulation();
		SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
	}

	private void FreezeSimulation()
	{
		run.ProcessMode = ProcessModeEnum.Disabled;
		entities.ProcessMode = ProcessModeEnum.Disabled;
		bodySpawner.ProcessMode = ProcessModeEnum.Disabled;
		eventDirector.ProcessMode = ProcessModeEnum.Disabled;
		hazardDirector.ProcessMode = ProcessModeEnum.Disabled;
		player.ProcessMode = ProcessModeEnum.Disabled;
	}

	/// <summary>
	/// Briefly drops the engine time scale so an impact lands. Overlapping calls
	/// extend the freeze rather than cutting it short.
	/// </summary>
	/// <param name="scale">Time scale to hold. A boss kill passes a higher value for slow motion.</param>
	public void Hitstop(float seconds, float scale = -1f)
	{
		if (isGameOver || seconds <= 0f || isPaused)
			return;

		ulong end = Time.GetTicksMsec() + (ulong)(seconds * 1000f);
		if (hitstopActive && end <= hitstopEndMsec)
			return;

		hitstopEndMsec = end;
		hitstopActive = true;
		Engine.TimeScale = scale > 0f ? scale : HitstopScale;
	}

	private void EndHitstop()
	{
		if (!hitstopActive)
			return;

		hitstopActive = false;
		Engine.TimeScale = 1.0;
	}

	/// <summary>Adds screen shake. 0.2 is a light kill, 1.0 is death.</summary>
	public void Shake(float trauma)
	{
		gameCamera?.AddTrauma(trauma);
	}

	/// <summary>Washes the screen with a colour for a moment. Kept faint: it must never hide a threat.</summary>
	public void Flash(Color colour, float alpha, float seconds)
	{
		if (flash == null || GameSettings.Instance?.ShakeIntensity <= 0f)
			return;

		flashTween?.Kill();
		flash.Color = new Color(colour, alpha);
		flashTween = CreateTween();
		flashTween.TweenProperty(flash, "color:a", 0f, seconds);
	}

	public void TriggerGameOver()
	{
		if (isGameOver)
			return;

		isGameOver = true;
		isPaused = false;
		FreezeSimulation();
		GetTree().Paused = false;
		pauseMenu.HidePauseMenu();

		// The transition runs on an AnimationPlayer, which obeys Engine.TimeScale.
		EndHitstop();

		GameOver.SurvivalTimeToShow = run.SurvivalTime;
		GameOver.KillsToShow = run.Kills;
		GameOver.BestComboToShow = run.BestStreak;
		GameOver.ScoreToShow = run.Score;
		GameOver.BossesBeaten = Mathf.Max(0, NextBossIndex - (BossActive ? 1 : 0));
		GameOver.StardustEarned = run.StardustEarned;

		var records = ScoreManager.SaveRun(run.SurvivalTime, run.Kills, run.BestStreak, run.Score);
		GameOver.IsNewBestScore = records.NewBestScore;
		GameOver.IsNewBestTime = records.NewBestTime;

		PlayerProfile.RecordOrbit(run.StardustEarned, run.Kills, run.SurvivalTime, run.PeakBuildFraction, run.Weapon);

		GameOver.LeaderboardRank = Leaderboard.Submit(
			PlayerProfile.PlayerName, run.Score, run.SurvivalTime, run.Kills);

		// Checked after RecordOrbit: a world can be earned by the very run that
		// satisfies it, and the recap is the only place left to say so.
		GameOver.NewlyUnlockedWorlds = Worlds.RefreshUnlocks();

		SceneTransition.Instance.ChangeScene("res://scenes/gameOver.tscn");
	}

	/// <summary>
	/// Death juice. Freezes and shakes first, then hands over to the recap, so the
	/// hit registers before the screen starts fading.
	/// </summary>
	public async void OnPlayerKilled(string cause)
	{
		if (isDying || isGameOver)
			return;

		isDying = true;
		run.SetProcess(false);
		GameOver.DeathCause = cause;
		DeathCause = cause;
		Shake(DeathTrauma);
		Hitstop(DeathHitstop);
		Flash(new Color(1f, 0.82f, 0.35f), 0.25f, 0.6f);

		// A beat longer than the freeze, so the planet visibly bursts before the fade.
		await ToSignal(
			GetTree().CreateTimer(0.75f, processAlways: true, processInPhysics: false, ignoreTimeScale: true),
			SceneTreeTimer.SignalName.Timeout);

		if (IsInstanceValid(this))
			TriggerGameOver();
	}

	// --- Kills and drops -----------------------------------------------------

	/// <summary>
	/// Every kill comes through here, whatever made it. Owns the payoff — score,
	/// chunks, sound, freeze, shake and the drop roll — so the weighting stays in
	/// one place. Dash and Nova kills skip the per-kill freeze: they arrive in
	/// bunches and have their own, bigger moment.
	/// </summary>
	public void RegisterKill(in Body.Remains remains, Vector2 at, KillSource source)
	{
		if (isDying || isGameOver) return;
		bool heavy = remains.Kind is BodyKind.Planetoid or BodyKind.Bulwark or BodyKind.Flare;

		run.AddKill();
		// The pop, centred on what died and sized to it, plus a few chunks.
		PopEffect.Spawn(this, at, 30f + 28f * remains.BurstScale, remains.BurstColor, 5 + Mathf.RoundToInt(remains.BurstScale * 2f));
		ShedChunks(at, Mathf.Min(remains.DebrisCount + 1, 4), remains.BurstColor);

		run.AddDropProgress(UpgradeDrops.PointsFor(remains.Kind));
		TryDropUpgrade(at);

		// Many kills in one frame would stack into one ugly blare.
		if (Time.GetTicksMsec() - lastKillSoundMsec > 45)
		{
			lastKillSoundMsec = Time.GetTicksMsec();
			PlayKillSound(heavy);
		}

		if (source == KillSource.Shot)
		{
			Hitstop(heavy ? HeavyKillHitstop : LightKillHitstop);
			Shake(heavy ? HeavyKillTrauma : LightKillTrauma);
		}
		else
		{
			Shake(0.08f);
		}
	}

	private void TryDropUpgrade(Vector2 at)
	{
		if (!run.DropDue)
			return;

		// Nothing useful right now: the meter stays full and the next kill tries again.
		if (!UpgradeDrops.TryRoll(run, PendingRewards(), boss: false, out Reward reward))
			return;

		run.SpendDrop();
		if (reward.IsShield)
			run.LastShieldDropAt = run.SurvivalTime;
		SpawnPickup(reward, at, Balance.DropLifetime, null);
	}

	/// <summary>
	/// What is already lying in the arena, or about to be, so the roll can count
	/// it as taken. Pickups enter the tree a frame late, so those still on their
	/// way in are tracked separately.
	/// </summary>
	private List<Reward> PendingRewards()
	{
		var pending = new List<Reward>(arriving);
		foreach (Node node in GetTree().GetNodesInGroup("pickups"))
		{
			if (node is PowerUp pickup && IsInstanceValid(pickup) && !pickup.IsQueuedForDeletion())
				pending.Add(pickup.Reward);
		}
		return pending;
	}

	private readonly List<Reward> arriving = new();

	private void SpawnPickup(Reward reward, Vector2 at, float lifetime, Vector2? flyTo)
	{
		if (PowerUpScene == null)
			return;

		var pickup = PowerUpScene.Instantiate<PowerUp>();
		pickup.Configure(reward, lifetime);
		pickup.GlobalPosition = Arena.ClampToPlayable(at, 40f);
		if (flyTo is Vector2 target)
			pickup.FlyTo(target);

		// Kills happen inside collision callbacks, and inserting an Area2D while
		// the physics server is flushing queries is an error.
		arriving.Add(reward);
		Callable.From(() =>
		{
			arriving.Remove(reward);
			if (IsInstanceValid(this) && IsInstanceValid(pickup))
				AddEntity(pickup);
			else
				pickup.QueueFree();
		}).CallDeferred();
	}

	/// <summary>
	/// For the asset gallery tool: places a pickup directly. 0-7 are the
	/// upgrades in catalogue order, anything else is a shield.
	/// </summary>
	public void PlaceRewardForTools(int index, Vector2 at)
	{
		Reward reward = index >= 0 && index < RunUpgrades.All.Length ? new Reward(RunUpgrades.All[index].Id) : Reward.Shield;
		SpawnPickup(reward, at, 60f, null);
	}

	/// <summary>Applies a pickup the planet just touched. Instant; nothing pauses.</summary>
	public void CollectReward(Reward reward, Vector2 at)
	{
		if (reward.IsShield)
		{
			run.GrantShield();
			Toast("SHIELD  •  BLOCKS ONE HIT", UpgradeDrops.ShieldColour);
			PlayCue("shield_get");
		}
		else if (run.TryGrant(reward.Upgrade))
		{
			RunUpgrades.Profile profile = RunUpgrades.Get(reward.Upgrade);
			int level = run.LevelOf(profile.Id);
			string rank = profile.Equips != null ? "EQUIPPED" : level >= profile.MaxLevel ? "MAX" : $"LV {level}";
			Toast($"{profile.Name}  •  {rank}", profile.Colour);
			PlayCue("upgrade_chirp");
		}
		else
		{
			// Only reachable if the build changed between drop and pickup.
			run.AddBonus(500);
			Toast("BONUS  •  +500", ArcadeSkin.Orange);
			PlayCue("upgrade_chirp");
		}

		SpawnBlast(at, 170f, reward.Colour);
		Shake(0.15f);
		player.GetNodeOrNull<PlanetVisual>("PlanetVisual")?.Celebrate();
	}

	// --- Effects ----------------------------------------------------------------

	/// <summary>
	/// A shockwave ring and its burst. The ring is not decoration: it shows how far
	/// the blast actually reached.
	/// </summary>
	public void SpawnBlast(Vector2 at, float radius, Color colour)
	{
		if (!Arena.IsNearView(at, radius))
			return;

		var wave = new NovaWave { MaxRadius = radius, WaveColor = colour };
		wave.GlobalPosition = at;
		AddEntity(wave);

		PopEffect.Spawn(this, at, Mathf.Min(radius * 0.32f, 170f), colour, radius > 300f ? 12 : 8, 0.5f);
	}

	/// <summary>A small bright pop where a dash, a Nova or a cleared shot connected.</summary>
	public void SpawnImpact(Vector2 at, Color colour)
	{
		PopEffect.Spawn(this, at, 28f, colour, 4, 0.3f);
	}

	/// <summary>Chunks that tumble off something breaking apart. Skipped off screen.</summary>
	public void ShedChunks(Vector2 at, int count, Color colour, float speedScale = 1f)
	{
		if (DebrisScene == null || count <= 0 || !Arena.IsNearView(at, 200f))
			return;

		int budget = MaxDebris - GetTree().GetNodeCountInGroup("debris");
		count = Mathf.Min(count, budget);

		for (int i = 0; i < count; i++)
		{
			var chunk = DebrisScene.Instantiate<Debris>();
			chunk.GlobalPosition = at;
			chunk.Modulate = colour;
			chunk.LaunchSpeedMin *= speedScale;
			chunk.LaunchSpeedMax *= speedScale;
			chunk.AddToGroup("debris");
			AddEntity(chunk);
		}
	}

	private void OnResumeGame()
	{
		if (isPaused)
			TogglePause();
	}

	private void OnGiveUpGame()
	{
		// Not a death — no cause left over from a previous run should show.
		GameOver.DeathCause = "";
		TriggerGameOver();
	}

	/// <summary>
	/// One sample stands in for the light and heavy kill sounds: heavy kills drop
	/// the pitch and gain volume, and every shot gets a little jitter.
	/// </summary>
	private void PlayKillSound(bool heavy)
	{
		float jitter = RunState.Rng.RandfRange(-0.06f, 0.06f);
		killSound.PitchScale = (heavy ? 0.72f : 1.14f) + jitter;
		killSound.VolumeDb = heavy ? -6.0f : -10.0f;
		killSound.Play();
	}

	private void OnStreakChanged(int streak, bool milestone)
	{
		if (!milestone || streakSound == null)
			return;

		PlayStreakSting(streak >= 25 ? 2.0f : streak >= 10 ? 1.7f : 1.45f);
		Shake(0.12f);
	}

	private void PlayStreakSting(float pitch)
	{
		if (streakSound == null)
			return;

		streakSound.PitchScale = pitch;
		streakSound.Play();
	}

	public void PlayButtonSound()
	{
		buttonSound.Play();
	}

	public void PlayHoverSound()
	{
		hoverSound.Play();
	}

	public void PlayCue(string cue)
	{
		if (cues.TryGetValue(cue, out var sound)) sound.Play();
	}
}
