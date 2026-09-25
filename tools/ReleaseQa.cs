#if DEBUG
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class ReleaseQa : Node
{
    private int failures;
    private void Check(bool ok, string name) { GD.Print($"{(ok ? "PASS" : "FAIL")}: {name}"); if (!ok) failures++; }
    private static object Invoke(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(obj, args);
    private async Task Frames(int count = 2) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task PhysicsFrames(int count = 2) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame); }
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds, processAlways: true, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
    private static bool IsDead(Player player) => (bool)typeof(Player).GetField("isDead", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!;

    private static RunState FreshRun(Node parent)
    {
        var state = new RunState();
        parent.AddChild(state);
        state.SetProcess(false);
        return state;
    }

    private Body Drifter(GameManager game, Vector2 at)
    {
        var body = GD.Load<PackedScene>("res://scenes/body.tscn").Instantiate<Body>();
        body.Configure(BodyKind.Drifter);
        body.GlobalPosition = at;
        game.AddEntity(body);
        body.SetPhysicsProcess(false);
        return body;
    }

    private async Task Journey()
    {
        GetTree().CurrentScene = null; // Keep the harness alive when the real game changes scenes.
        var menu = GD.Load<PackedScene>("res://scenes/menu.tscn").Instantiate();
        GetTree().Root.AddChild(menu); GetTree().CurrentScene = menu;
        menu.GetNode<BaseButton>("UI/Buttons/PlayButton").EmitSignal(BaseButton.SignalName.Pressed);
        await Wait(1.6);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var active = GetTree().CurrentScene as GameManager;
            Check(active != null, $"journey {attempt}: enters arena");
            if (active == null) return;
            Check(active.Run.TotalLevels == 0 && !active.Run.HasShield && active.Run.Weapon == WeaponId.Comet, "retry resets upgrades, shield and weapon");
            var player = active.GetNode<Player>("player"); player.Invulnerable = true;
            Invoke(active, "TogglePause");
            float time = active.RunTime;
            await Wait(0.2);
            Check(active.RunTime == time, "pause freezes simulation time");
            Invoke(active, "TogglePause");
            await Wait(0.1);
            Check(active.RunTime > time, "resume advances simulation");
            int orbits = PlayerProfile.TotalOrbits;
            player.Invulnerable = false;
            player.KillByBlast("QA contact");
            player.KillByBlast("duplicate contact");
            await Wait(2.2);
            var recap = GetTree().CurrentScene as GameOver;
            Check(recap != null, "one lethal hit with no shield reaches results");
            Check(PlayerProfile.TotalOrbits == orbits + 1, "duplicate death records exactly one run");
            Check(Engine.TimeScale == 1 && !GetTree().Paused, "results restore time and pause state");
            if (recap == null) return;
            ((BaseButton)recap.FindChild(attempt == 2 ? "ReturnToMenu" : "Retry", true, false)).EmitSignal(BaseButton.SignalName.Pressed);
            await Wait(1.6);
        }
        Check(GetTree().CurrentScene is Menu, "results returns to main menu");
        foreach (string name in new[] { "settings", "controls", "accessibility", "leaderboard", "stats", "credits" })
        {
            SceneTransition.Instance.ChangeScene($"res://scenes/{name}.tscn");
            await Wait(1.6);
            Check(GetTree().CurrentScene.SceneFilePath == $"res://scenes/{name}.tscn", name + " screen loads");
        }
    }

    private void Persistence()
    {
        foreach (string file in new[] { "highscore.cfg", "leaderboard.cfg", "profile.cfg" })
            foreach (string suffix in new[] { "", ".bak" })
                if (FileAccess.FileExists("user://" + file + suffix)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://" + file + suffix));
        Check(Leaderboard.Sanitise(" \t\u0001‮ ") == "PLAYER", "nonprinting names fall back safely");
        Check(Leaderboard.Submit("bad", -1, float.NaN, -1) == -1, "invalid leaderboard submission rejected");
        Check(Leaderboard.Sanitise("A\ud800B").Length > 0, "malformed Unicode names cannot crash results");
        Check(Leaderboard.Submit("FIRST", 500, 60, 2) == 1 && Leaderboard.Submit("SECOND", 500, 60, 3) == 2, "tied runs retain arrival order");
        Check(Leaderboard.Submit("LONGER", 10, 90, 1) == 1, "leaderboard ranks by survival time, not score");
        typeof(Leaderboard).GetField("isLoaded", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, false);
        ((List<Leaderboard.Entry>)typeof(Leaderboard).GetField("entries", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();
        Check(Leaderboard.Entries.Count == 3 && Leaderboard.Entries[0].Name == "LONGER" && Leaderboard.Entries[1].Name == "FIRST", "leaderboard survives reload in time order");
        for (int i = 0; i < 12; i++) Leaderboard.Submit("TEST", 10 + i, 1 + i, 1);
        Check(Leaderboard.Entries.Count == 10 && !Leaderboard.WouldPlace(0f), "leaderboard stays bounded at ten entries");
        var cfg = new ConfigFile(); cfg.SetValue("test", "wrong", "not a number"); cfg.SetValue("test", "nan", double.NaN);
        Check(SaveStore.Value(cfg, "test", "wrong", 7).AsInt32() == 7 && SaveStore.Value(cfg, "test", "nan", 7).AsInt32() == 7, "invalid saved values use safe defaults");
        var settings = GameSettings.Instance;
        float volume = settings.MasterVolume;
        settings.SetMasterVolume(0.35f); settings.SaveSettings(); settings.SetMasterVolume(0.9f);
        Invoke(settings, "LoadSettings");
        Check(Mathf.IsEqualApprox(settings.MasterVolume, 0.35f), "settings survive a save and reload");
        settings.SetMasterVolume(volume); settings.SaveSettings();
        int before = ScoreManager.BestScore;
        ScoreManager.SaveRun(float.NaN, 1, 1, int.MaxValue);
        Check(ScoreManager.BestScore == before && float.IsFinite(ScoreManager.BestTime), "invalid runs cannot poison records");
        ScoreManager.SaveRun(10, 1, 1, 100);
        ScoreManager.SaveRun(20, 2, 2, 200);
        Check(FileAccess.FileExists("user://highscore.cfg.bak"), "saving retains a recoverable previous record");
        DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://highscore.cfg"));
        typeof(ScoreManager).GetField("isLoaded", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, false);
        Check(ScoreManager.BestScore == 100, "missing primary save recovers the backup");
    }

    private void RunRules()
    {
        var state = FreshRun(this);
        Check(!state.HasShield, "a run starts without a shield");
        state._Process(0.01f);
        if (Balance.NovaUnlockAt <= 0f)
            Check(state.HasDash && state.HasOverdrive && state.HasNova, "all three abilities are ready from the first frame");
        else
        {
            state._Process(Balance.NovaUnlockAt);
            Check(state.HasDash && state.HasOverdrive && state.HasNova, "abilities unlock by time");
        }
        Check(Balance.DashUnlockAt <= Balance.OverdriveUnlockAt && Balance.OverdriveUnlockAt <= Balance.NovaUnlockAt, "unlock order is Dash, Overdrive, Nova");
        Check(Balance.DashCooldown < Balance.OverdriveCooldown && Balance.OverdriveCooldown < Balance.NovaCooldown, "cooldowns rise with ability strength");
        state.Free();

        var locked = FreshRun(this);
        Check(!locked.TryGrant(RunUpgradeId.DashBoost) && !locked.TryGrant(RunUpgradeId.BiggerNova) && !locked.TryGrant(RunUpgradeId.OverdriveBoost), "ability upgrades need their ability");
        for (int i = 0; i < 10; i++) locked.TryGrant(RunUpgradeId.SpreadShot);
        Check(locked.LevelOf(RunUpgradeId.SpreadShot) == RunUpgrades.SpreadShot.MaxLevel, "upgrades stop at their maximum");
        Check(locked.TryGrant(RunUpgradeId.DebrisCannon) && locked.Weapon == WeaponId.DebrisCannon, "debris cannon replaces the comet");
        Check(!locked.TryGrant(RunUpgradeId.IonLance) && locked.Weapon == WeaponId.DebrisCannon, "weapon swaps are mutually exclusive");
        locked.GrantShield(); locked.GrantShield();
        Check(locked.HasShield && locked.ConsumeShield() && !locked.ConsumeShield(), "at most one shield, spent by one hit");
        locked.Free();

        // Drops: never maxed, never locked, never a second shield or weapon, over many rolls in several states.
        var drops = FreshRun(this);
        bool clean = true;
        for (int round = 0; round < 3; round++)
        {
            if (round == 1) { drops.Unlock(Ability.Dash); drops.GrantShield(); drops.TryGrant(RunUpgradeId.IonLance); }
            if (round == 2) { drops.Unlock(Ability.Overdrive); drops.Unlock(Ability.Nova); for (int i = 0; i < 4; i++) drops.TryGrant(RunUpgradeId.FireRate); }
            for (int roll = 0; roll < 400; roll++)
            {
                var pending = new List<Reward>();
                if (!UpgradeDrops.TryRoll(drops, pending, roll % 2 == 0, out Reward reward)) continue;
                if (reward.IsShield) { clean &= !drops.HasShield; continue; }
                var profile = RunUpgrades.Get(reward.Upgrade);
                clean &= !drops.IsMaxed(reward.Upgrade);
                clean &= profile.Requires is not Ability need || drops.IsUnlocked(need);
                clean &= profile.Equips == null || drops.Weapon == WeaponId.Comet;
            }
        }
        Check(clean, "drops are never maxed, locked, a second weapon or a second shield");
        var lastLevel = new List<Reward> { new(RunUpgradeId.FireRate) };
        var fresh = FreshRun(this);
        for (int i = 0; i < RunUpgrades.FireRate.MaxLevel - 1; i++) fresh.TryGrant(RunUpgradeId.FireRate);
        bool noDoubleLast = true;
        for (int roll = 0; roll < 300; roll++)
            if (UpgradeDrops.TryRoll(fresh, lastLevel, false, out Reward r) && !r.IsShield && r.Upgrade == RunUpgradeId.FireRate) noDoubleLast = false;
        Check(noDoubleLast, "a pickup already on the field counts as taken");
        fresh.Free();

        // Drop cadence: a gap in a fixed window, and only for a player who is fighting.
        var meter = FreshRun(this);
        meter.AddDropProgress(100f);
        Check(!meter.DropDue, "no drop before the first gap has passed");
        meter._Process(Balance.DropGapMax + 1f);
        Check(meter.DropDue, "a fighting player always gets a drop within the gap window");
        meter.SpendDrop();
        meter._Process(Balance.DropGapMax * Balance.DropGapGrowthCap + 1f);
        Check(!meter.DropDue, "a player who stops fighting earns no drops");
        meter.AddDropProgress(Balance.DropMinFighting);
        Check(meter.DropDue, "fighting again makes the waiting drop fall");
        drops.Free(); meter.Free();
    }

    private async Task ArenaChecks(GameManager game, Player player)
    {
        Check(Balance.ArenaSize.X >= 2.5f * 1920f && Balance.ArenaSize.Y >= 2.5f * 1080f, "the world is several screens across");
        var camera = game.GetNode<GameCamera>("GameCamera");
        player.GlobalPosition = Arena.Playable.Position;
        for (int i = 0; i < 90; i++) camera._Process(0.05);
        Check(Arena.World.Encloses(Arena.View), "camera never shows past the arena edge");
        player.GlobalPosition = Arena.Centre;
        for (int i = 0; i < 90; i++) camera._Process(0.05);
        Check(Arena.View.GetCenter().DistanceTo(player.GlobalPosition) < Balance.CameraDeadZone + Balance.CameraAimLead + 2f, "camera follows the planet");
        player.GlobalPosition = new Vector2(-500, -500);
        Invoke(player, "StayInArena");
        Check(Arena.Playable.HasPoint(player.GlobalPosition), "the planet cannot leave the arena");

        bool safeSpawns = true, offscreen = true;
        foreach (Vector2 from in new[] { Arena.Centre, Arena.Playable.Position + new Vector2(80, 80), Arena.Playable.End - new Vector2(80, 80) })
        {
            player.GlobalPosition = from;
            for (int i = 0; i < 90; i++) camera._Process(0.05);
            for (int i = 0; i < 150; i++)
            {
                if (!Arena.TryFindSpawnPoint(from, out Vector2 at)) { safeSpawns = false; continue; }
                safeSpawns &= Arena.Playable.HasPoint(at) && at.DistanceTo(from) >= Balance.SpawnMinDistance * 0.85f;
                if (from == Arena.Centre) offscreen &= !Arena.View.HasPoint(at);
            }
        }
        Check(safeSpawns, "enemies always enter inside the arena and far from the planet, even in corners");
        Check(offscreen, "in open space, enemies enter from off screen");
        player.GlobalPosition = Arena.Centre;
        for (int i = 0; i < 90; i++) camera._Process(0.05);
        await Frames();
    }

    private async Task Dash(GameManager game, Player player)
    {
        player.Invulnerable = false;
        player.SetPhysicsProcess(false);
        game.Run.Unlock(Ability.Dash);
        player.GlobalPosition = Arena.Centre;
        player.Velocity = Vector2.Zero;
        var line = new List<Body>();
        for (int i = 1; i <= 5; i++) line.Add(Drifter(game, player.GlobalPosition + Vector2.Right * 62f * i));
        await PhysicsFrames(2);

        Input.ActionPress("right");
        Invoke(player.Abilities, "StartDash");
        Input.ActionRelease("right");
        int frames = 0;
        while (player.IsDashing && frames++ < 60) player._PhysicsProcess(1.0 / 60.0);
        bool allGone = line.TrueForAll(b => !IsInstanceValid(b) || b.IsDestroyed);
        Check(allGone, "a dash through five enemies destroys all five");
        Check(!IsDead(player), "the planet cannot die during a valid dash");
        Check(player.IsBlinking, "a dash ends in a blinking grace period");

        // The same test at a quarter of the frame rate: the sweep must still see everything.
        await PhysicsFrames(2);
        player.GlobalPosition = Arena.Centre + new Vector2(0, 400);
        for (int i = 0; i < 200; i++) player.Abilities.Update(0.05);
        line.Clear();
        for (int i = 1; i <= 5; i++) line.Add(Drifter(game, player.GlobalPosition + Vector2.Right * 62f * i));
        await PhysicsFrames(2);
        Input.ActionPress("right");
        Invoke(player.Abilities, "StartDash");
        Input.ActionRelease("right");
        frames = 0;
        while (player.IsDashing && frames++ < 60) player._PhysicsProcess(1.0 / 15.0);
        Check(line.TrueForAll(b => !IsInstanceValid(b) || b.IsDestroyed) && !IsDead(player), "dash kills are frame-rate safe");

        var survivor = Drifter(game, player.GlobalPosition);
        await PhysicsFrames(2);
        player._PhysicsProcess(1.0 / 60.0);
        Check(!IsDead(player) && IsInstanceValid(survivor) && !survivor.IsDestroyed, "grace protects, but does not destroy what it touches");
        for (int i = 0; i < 60 && player.IsBlinking; i++) player._PhysicsProcess(1.0 / 60.0);
        player._PhysicsProcess(1.0 / 60.0);
        Check(IsDead(player), "staying inside an enemy after the blink ends is lethal");
    }

    public override async void _Ready()
    {
        if (!OS.GetUserDataDir().Contains("Grinfinity-QA")) { GD.PushError("QA requires an isolated Grinfinity-QA project."); GetTree().Quit(2); return; }
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            Persistence();
            RunRules();

            var game = GD.Load<PackedScene>("res://scenes/game.tscn").Instantiate<GameManager>();
            AddChild(game); await Frames();
            var player = game.GetNode<Player>("player"); player.Invulnerable = true;
            game.GetNode<BodySpawner>("BodySpawner").SetProcess(false);
            game.GetNode<HazardDirector>("HazardDirector").SetProcess(false);
            game.Run.SetProcess(false);

            Check(game.GetNode("UI/Hud").FindChild("RunInfo", true, false) is Label, "survival time is on the HUD");
            Check(game.GetNode("UI/Hud").FindChild("LiveScore", true, false) != null, "score is on the HUD");
            bool noWave = true;
            foreach (Node node in game.GetNode("UI/Hud").FindChildren("*", "Label", true, false)) noWave &= !((Label)node).Text.StartsWith("WAVE");
            Check(noWave, "no wave number on the HUD");
            Check(game.GetNodeOrNull("UI/UpgradePrompt") == null, "no upgrade menu exists");
            Check(game.GetNode("PauseLayer/PauseMenu").FindChild("Restart", true, false) != null, "pause offers a restart");

            await ArenaChecks(game, player);

            Check(player.FireInterval(false) <= .181f, "starting weapon fires reliably");
            game.Run.Unlock(Ability.Overdrive);
            Check(player.FireInterval(true) < player.FireInterval(false) * 0.5f, "overdrive fires far faster");
            int shots = GetTree().GetNodeCountInGroup("player_bullets");
            game.Run.TryGrant(RunUpgradeId.SpreadShot);
            player.ShootBullet(player.GlobalPosition + Vector2.Right * 300);
            await Frames(1);
            Check(GetTree().GetNodeCountInGroup("player_bullets") - shots == 2, "first spread level adds one projectile");
            var bullets = GetTree().GetNodesInGroup("player_bullets");
            foreach (Node node in bullets) node.QueueFree();
            await Frames(1);
            player.ShootBullet(player.GlobalPosition + Vector2.Right * 300, overdriven: true);
            await Frames(1);
            bool empowered = true;
            foreach (Node node in GetTree().GetNodesInGroup("player_bullets")) empowered &= ((Bullet)node).Damage == 2 && ((Bullet)node).Pierce >= 1 && ((Bullet)node).Overdriven;
            Check(empowered, "overdrive empowers the current build's own shots");
            foreach (Node node in GetTree().GetNodesInGroup("player_bullets")) node.QueueFree();

            Input.ActionPress("right"); Input.ActionPress("down");
            Invoke(player, "HandleMovement", 1.0);
            Check(player.Velocity.Length() <= player.MoveSpeed + 0.01f, "diagonal movement respects speed limit");
            Input.ActionRelease("right"); Input.ActionRelease("down");

            game.Notification((int)MainLoop.NotificationApplicationFocusOut);
            Check(game.IsPaused && GetTree().Paused, "focus loss pauses gameplay");
            if (game.IsPaused) Invoke(game, "TogglePause");

            // Bosses: warned, placed away from the planet, hurt but never ended by one Nova or Dash.
            player.GlobalPosition = Arena.Centre;
            game.NextBossAt = 0f;
            game.Run.SetProcess(true);
            game._Process(0.01);
            Check(game.BossBusy && !game.BossActive, "a boss is announced before it arrives");
            for (int i = 0; i < 80 && !game.BossActive; i++) game._Process(0.05);
            var boss = GetTree().GetFirstNodeInGroup("bosses") as Boss;
            Check(boss != null && boss.GlobalPosition.DistanceTo(player.GlobalPosition) >= 400f, "boss arrives well away from the planet");
            Check(game.GetNode<BodySpawner>("BodySpawner").Support == 0f, "the first Coil is fought alone");
            if (boss != null)
            {
                boss.SetPhysicsProcess(false);
                boss.TakeShareOfHealth(Balance.NovaBossDamage);
                Check(boss.HealthFraction > 0.85f && boss.HealthFraction < 0.95f, "nova takes a tenth of a boss, no more");
                float beforeDash = boss.HealthFraction;
                game.Run.Unlock(Ability.Dash);
                player.GlobalPosition = boss.GlobalPosition - new Vector2(200, 0);
                for (int i = 0; i < 200; i++) player.Abilities.Update(0.05);
                Input.ActionPress("right");
                Invoke(player.Abilities, "StartDash");
                Input.ActionRelease("right");
                for (int i = 0; i < 30 && player.IsDashing; i++) player.Abilities.Sweep(player.GlobalPosition, boss.GlobalPosition);
                float lost = beforeDash - boss.HealthFraction;
                Check(lost > 0.02f && lost < 0.05f, "a dash hits a boss exactly once");

                int pickups = GetTree().GetNodeCountInGroup("pickups");
                boss.TakeDamage(99999);
                await Frames(3);
                Check(!IsInstanceValid(boss) || boss.IsQueuedForDeletion(), "boss defeats once");
                Check(GetTree().GetNodeCountInGroup("pickups") - pickups == Balance.BossRewardCount, "a boss drops three rewards into the arena");
                Check(game.NextBossAt > game.Run.SurvivalTime, "the next boss is scheduled, not immediate");
            }
            foreach (Node node in GetTree().GetNodesInGroup("pickups")) node.QueueFree();

            game.NextBossIndex = 3;
            Check(game.BossCycle == 2 && Balance.BossSupport(2) > 0f, "the second time round, enemies join the boss fight");
            Check(Balance.BossSupport(4) > Balance.BossSupport(2), "boss support keeps growing each cycle");
            Check(BodySpawner.SpeedAt(3600f) > BodySpawner.SpeedAt(1800f) || BodySpawner.SpeedAt(3600f) >= Balance.EnemySpeedCeiling, "pressure keeps rising after half an hour");
            Check(GameManager.ScheduledBossTime(9) > GameManager.ScheduledBossTime(8), "bosses keep coming after the Black Hole");
            game.Run.SetProcess(false);
            foreach (Node node in GetTree().GetNodesInGroup("bosses")) node.Free();

            for (int i = 0; i < 20; i++) player.Abilities.Update(0.1);
            game.Run.GrantShield();
            player.Invulnerable = false;
            player.KillByBlast("QA"); player.KillByBlast("QA");
            Check(!IsDead(player) && !game.Run.HasShield, "a shield blocks one lethal hit and breaks");

            await Dash(game, player);

            game.Free(); Engine.TimeScale = 1; GetTree().Paused = false;
            Input.ActionRelease("right");
            await Frames();
            await Journey();
            var music = GetNode<MusicManager>("/root/MusicManager");
            music.GetNode<AudioStreamPlayer>("BackgroundMusic").Stop();
            await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
        }
        catch (Exception ex) { failures++; GD.PrintErr(ex); }
        GD.Print($"QA RESULT: {failures} failure(s)");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
#endif
