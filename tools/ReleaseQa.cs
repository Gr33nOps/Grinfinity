#if DEBUG
using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class ReleaseQa : Node
{
    private int failures;
    private void Check(bool ok, string name) { GD.Print($"{(ok ? "PASS" : "FAIL")}: {name}"); if (!ok) failures++; }
    private static object Invoke(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(obj, args);
    private async Task Frames(int count = 2) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds, processAlways: true, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
    private static int UpgradeCount(RunState run) { int count = 0; foreach (var upgrade in RunUpgrades.All) count += run.LevelOf(upgrade.Id); return count; }
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
            Check(active.Run.Mass == 0 && UpgradeCount(active.Run) == 0 && active.Run.Weapon == WeaponId.Comet, "retry resets mass upgrades and weapon");
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
            active.Run.ConsumeShield();
            player.KillByBlast("QA contact");
            player.KillByBlast("duplicate contact");
            await Wait(1.8);
            var recap = GetTree().CurrentScene as GameOver;
            Check(recap != null, "lethal contact reaches results");
            Check(PlayerProfile.TotalOrbits == orbits + 1, "duplicate death records exactly one orbit");
            Check(Engine.TimeScale == 1 && !GetTree().Paused, "results restore time and pause state");
            if (recap == null) return;
            ((BaseButton)recap.FindChild(attempt == 2 ? "ReturnToMenu" : "Retry",true,false)).EmitSignal(BaseButton.SignalName.Pressed);
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
    public override async void _Ready()
    {
        if (!OS.GetUserDataDir().Contains("Grinfinity-QA")) { GD.PushError("QA requires an isolated Grinfinity-QA project."); GetTree().Quit(2); return; }
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            foreach (string file in new[] { "highscore.cfg", "leaderboard.cfg", "profile.cfg" })
                foreach (string suffix in new[] { "", ".bak" })
                    if (FileAccess.FileExists("user://" + file + suffix)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://" + file + suffix));
            Check(Leaderboard.Sanitise(" \t\u0001\u202e ") == "PLAYER", "nonprinting names fall back safely");
            Check(Leaderboard.Submit("bad", -1, float.NaN, -1) == -1, "invalid leaderboard submission rejected");
            Check(Leaderboard.Sanitise("A\ud800B").Length > 0, "malformed Unicode names cannot crash results");
            Check(Leaderboard.Submit("FIRST", 500, 10, 2) == 1 && Leaderboard.Submit("SECOND", 500, 12, 3) == 2, "tied scores retain arrival order");
            typeof(Leaderboard).GetField("isLoaded", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(null, false);
            ((System.Collections.Generic.List<Leaderboard.Entry>)typeof(Leaderboard).GetField("entries", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!).Clear();
            Check(Leaderboard.Entries.Count == 2 && Leaderboard.Entries[0].Name == "FIRST", "leaderboard survives reload without changing ties");
            for (int i = 0; i < 12; i++) Leaderboard.Submit("TEST", 10 + i, 1, 1);
            Check(Leaderboard.Entries.Count == 10 && !Leaderboard.WouldPlace(0), "leaderboard stays bounded at ten entries");
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
            bool cleanUpgrades=true;
            foreach(var upgrade in RunUpgrades.All) if(upgrade.Id is RunUpgradeId.WiderPull or RunUpgradeId.RichDebris or RunUpgradeId.HungryDash or RunUpgradeId.SlowField)cleanUpgrades=false;
            Check(cleanUpgrades,"removed utility upgrades are absent from the live catalog");
            bool cleanDrops=true;for(int i=0;i<300;i++)if(PowerUps.Roll() is PowerUpKind.Freeze or PowerUpKind.Magnet)cleanDrops=false;
            Check(cleanDrops,"removed freeze and magnet pickups never drop");
            var state = new RunState(); AddChild(state); state.SetProcess(false);
            Check(state.TryBuy(RunUpgradeId.FireRate), "earned upgrade choices require no currency");
            Check(state.HasShield, "every fresh orbit has one forgiving shield");
            state.GrantPowerUp(PowerUpKind.Freeze);
            state.GrantPowerUp(PowerUpKind.Magnet);
            state.GrantPowerUp(PowerUpKind.Damage);
            Check(state.Frozen && state.Magnetised && state.Overcharged, "temporary pickups activate together");
            Check(RunUpgrades.Get((RunUpgradeId)13) != null, "run has an additive multishot boost");
            state._Process(1000);
            Check(!state.Frozen && !state.Magnetised && !state.Overcharged, "temporary pickups expire cleanly");
            Check(!state.TryBuy(RunUpgradeId.QuickerDash), "ability upgrades require their ability");
            state.GrantWaveMilestones(1);
            Check(state.HasDash&&!state.HasRapidFire&&!state.HasNova,"wave one unlocks only dash");
            state.GrantWaveMilestones(3);
            Check(state.HasRapidFire&&!state.HasNova,"wave three unlocks rapid fire before nova");
            state.GrantWaveMilestones(5);
            Check(state.HasNova,"wave five unlocks nova before splitters arrive");
            int earned=UpgradeCount(state);state.GrantWaveMilestones(5);
            Check(UpgradeCount(state)==earned,"milestone rewards are idempotent");

            state.AddMass(float.NaN);
            Check(float.IsFinite(state.Mass), "nonfinite mass rejected");
            var weaponUpgrade = RunUpgrades.Get((RunUpgradeId)11);
            Check(weaponUpgrade != null, "existing debris cannon is reachable through run progression");
            if (weaponUpgrade != null)
            {
                state.TryBuy(weaponUpgrade.Id);
                Check((WeaponId)(state.GetType().GetProperty("Weapon")?.GetValue(state) ?? WeaponId.Comet) == WeaponId.DebrisCannon, "weapon purchase equips the existing weapon");
            }
            state.Free();

            var game = GD.Load<PackedScene>("res://scenes/game.tscn").Instantiate<GameManager>();
            AddChild(game); await Frames();
            var player = game.GetNode<Player>("player"); player.Invulnerable = true;
            Check(player.FireInterval(false)<=.181f,"starting weapon fires reliably without a spread upgrade");
            var offerPrompt=game.GetNode<UpgradePrompt>("UI/UpgradePrompt");
            var clearedField=typeof(UpgradePrompt).GetField("clearedWave",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var offersField=typeof(UpgradePrompt).GetField("offer",BindingFlags.NonPublic|BindingFlags.Instance)!;
            bool noEarlySpread=true;
            for(int roll=0;roll<30;roll++)
            {
                clearedField.SetValue(offerPrompt,1);Invoke(offerPrompt,"RollOffer");
                noEarlySpread &= !((System.Collections.Generic.List<RunUpgradeId>)offersField.GetValue(offerPrompt)!).Contains(RunUpgradeId.FanShot);
            }
            Check(noEarlySpread,"spread shot never appears in the opening wave choices");
            var runField=typeof(UpgradePrompt).GetField("run",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var focused=new RunState();AddChild(focused);focused.SetProcess(false);focused.GrantWaveMilestones(5);
            focused.TryBuy(RunUpgradeId.FireRate);focused.TryBuy(RunUpgradeId.FireRate);
            focused.TryBuy(RunUpgradeId.QuickerDash);focused.TryBuy(RunUpgradeId.QuickerDash);focused.TryBuy(RunUpgradeId.Piercing);
            runField.SetValue(offerPrompt,focused);clearedField.SetValue(offerPrompt,6);
            bool standardChoice=true,oneWeapon=true;
            for(int roll=0;roll<40;roll++)
            {
                Invoke(offerPrompt,"RollOffer");var choices=(System.Collections.Generic.List<RunUpgradeId>)offersField.GetValue(offerPrompt)!;
                standardChoice &= choices.Contains(RunUpgradeId.BiggerNova);
                oneWeapon &= choices.FindAll(id=>RunUpgrades.Get(id).Equips!=null).Count<=1;
            }
            Check(standardChoice,"offers preserve an eligible standard upgrade instead of forcing spread or weapon swaps");
            Check(oneWeapon,"a choice screen contains at most one weapon replacement");
            runField.SetValue(offerPrompt,game.Run);clearedField.SetValue(offerPrompt,1);focused.Free();
            var pacing=game.GetNode<BodySpawner>("BodySpawner");float initialSpeed=BodySpawner.CurrentSpeed;pacing._Process(120);
            Check(Mathf.IsEqualApprox(initialSpeed,BodySpawner.CurrentSpeed),"taking longer in one wave does not increase enemy speed");
            var waveProperty=typeof(BodySpawner).GetProperty("WaveNumber")!;
            float previousSpeed=0;bool boundedCurve=true;
            for(int wave=1;wave<=50;wave++)
            {
                waveProperty.SetValue(pacing,wave);pacing._Process(0);
                boundedCurve &= BodySpawner.CurrentSpeed>=previousSpeed && BodySpawner.CurrentSpeed<=pacing.MaxSpeed+.01f;
                previousSpeed=BodySpawner.CurrentSpeed;
            }
            Check(boundedCurve,"wave difficulty rises smoothly and remains capped in long runs");
            waveProperty.SetValue(pacing,6);pacing._Process(0);
            Check(!game.BossDue,"first boss cannot interrupt the sixth wave");
            waveProperty.SetValue(pacing,7);pacing._Process(0);
            Check(game.BossDue,"first boss becomes eligible after six clears");
            game.NextBossIndex=1;
            Check(!game.BossDue,"second boss cannot immediately follow the first");
            waveProperty.SetValue(pacing,13);pacing._Process(0);
            Check(game.BossDue,"second boss follows twelve clears");
            game.NextBossIndex=0;waveProperty.SetValue(pacing,1);pacing._Process(0);
            player.Rotation=1.2f;player.GetNode<PlanetVisual>("PlanetVisual")._Process(.016);
            Check(Mathf.Abs(player.GetNode<Sprite2D>("Sprite2D").Rotation)<.2f,"planet artwork rotates with its aiming parent");

            Check(game.GetNode("UI/Hud").FindChild("LiveScore",true,false) != null, "live score is visible during gameplay");
            Check(game.GetNode("UI/Hud").FindChild("RunInfo",true,false) != null, "wave and ability hint have a live readout");
            Check(game.GetNode("UI/Hud").FindChild("HowTo",true,false) != null, "first orbit explains objective and controls");
            Check(game.GetNode("PauseLayer/PauseMenu").FindChild("Restart",true,false) != null, "pause offers a restart");
            Check(game.GetNode("PauseLayer/PauseMenu").FindChild("Options",true,false) != null, "pause offers audio and accessibility options");
            game.GetNode<BodySpawner>("BodySpawner").SetProcess(false);
            int shots = GetTree().GetNodeCountInGroup("player_bullets");
            game.Run.TryBuy(RunUpgradeId.FanShot);
            player.ShootBullet(player.GlobalPosition + Vector2.Right * 300);
            Check(GetTree().GetNodeCountInGroup("player_bullets") - shots == 2, "first spread level adds one projectile instead of tripling firepower");
            bool rankHeld=true;
            for(int roll=0;roll<30;roll++)
            {
                clearedField.SetValue(offerPrompt,9);Invoke(offerPrompt,"RollOffer");
                rankHeld &= !((System.Collections.Generic.List<RunUpgradeId>)offersField.GetValue(offerPrompt)!).Contains(RunUpgradeId.FanShot);
            }
            Check(rankHeld,"second spread level remains locked until wave ten clear");
            clearedField.SetValue(offerPrompt,10);Invoke(offerPrompt,"RollOffer");
            Check(((System.Collections.Generic.List<RunUpgradeId>)offersField.GetValue(offerPrompt)!).Contains(RunUpgradeId.FanShot),"second spread level is offered at its wave ten milestone");
            game.Run.TryBuy(RunUpgradeId.FanShot);
            shots=GetTree().GetNodeCountInGroup("player_bullets");
            player.ShootBullet(player.GlobalPosition+Vector2.Right*300);
            Check(GetTree().GetNodeCountInGroup("player_bullets")-shots==3,"maximum spread adds two projectiles with no five-shot jump");
            Check(!game.Run.TryBuy(RunUpgradeId.FanShot), "multishot has a bounded maximum");
            Input.ActionPress("right"); Input.ActionPress("down");
            Invoke(player, "HandleMovement", 1.0);
            Check(player.Velocity.Length() <= player.CurrentMoveSpeed + 0.01f, "diagonal movement respects speed limit");
            Input.ActionRelease("right"); Input.ActionRelease("down");
            game.Run._Process(1000);

            player.CreateDashEffect();
            var prompt = game.GetNode<UpgradePrompt>("UI/UpgradePrompt");
            Invoke(prompt, "OnWaveCleared", 1);
            Check(game.Run.HasDash,"first wave guarantees dash without spending the upgrade choice");
            Check(GetTree().Paused, "boost choice freezes the simulation for reading");
            var openingOffer=(System.Collections.Generic.List<RunUpgradeId>)typeof(UpgradePrompt).GetField("offer",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(prompt)!;
            Check(openingOffer.Count>0&&openingOffer.TrueForAll(id=>!RunUpgrades.Get(id).IsUnlock&&RunUpgrades.Get(id).MinWave<=1),"opening choices cannot roll late weapons or ability unlocks");

            game.Announce("SHIELD", "test pickup", Colors.White);
            Check(!game.GetNode<Control>("UI/Announcer").Visible, "pickup announcement cannot cover boost choices");
            Invoke(game, "TogglePause");
            Check(!game.GetNode<CanvasLayer>("UI").Visible, "pause hides underlying upgrade and HUD layers");
            Invoke(game, "TogglePause");
            Check(GetTree().Paused && prompt.Visible, "resume restores the frozen boost choice");
            Check(GetViewport().GuiGetFocusOwner()?.IsVisibleInTree()==true,"resume restores a visible upgrade focus target");
            Invoke(prompt, "Buy", 0);
            Check(!GetTree().Paused, "choosing a boost resumes gameplay");
            int chosen = UpgradeCount(game.Run);
            Invoke(prompt, "Buy", 1);
            Check(UpgradeCount(game.Run) == chosen, "rapid clicks cannot choose twice in one break");
            Invoke(prompt,"OnWaveCleared",2);
            var skipUpgrade=prompt.FindChild("SkipUpgrade",true,false) as Button;
            Check(skipUpgrade!=null,"player can keep the current build without accepting an unwanted upgrade");
            if(skipUpgrade!=null)
            {
                skipUpgrade.EmitSignal(BaseButton.SignalName.Pressed);
                Check(!GetTree().Paused&&UpgradeCount(game.Run)==chosen,"skipping an upgrade resumes play without changing the build");
            }
            else Invoke(prompt,"Buy",0);
            game.Notification((int)MainLoop.NotificationApplicationFocusOut);
            Check(game.IsPaused && GetTree().Paused, "focus loss pauses gameplay");
            if (game.IsPaused) Invoke(game, "TogglePause");
            player.GlobalPosition = new Vector2(960, 238);
            Invoke(game, "SpawnBoss", GD.Load<PackedScene>("res://scenes/boss_coil.tscn"), "THE COIL");
            var boss = GetTree().GetFirstNodeInGroup("bosses") as Boss;
            Check(boss != null && boss.GlobalPosition.DistanceTo(player.GlobalPosition) >= 450f, "boss arrival cannot overlap the player");
            boss?.Free();
            game.Run.SetProcess(false);
            game.SetProcess(false);
            foreach (var encounter in new[] { ("boss_coil", 0, 5000), ("boss_brood", 1, 5000), ("boss_black_hole", 2, 12000) })
            {
                game.NextBossIndex = encounter.Item2;
                Invoke(game, "SpawnBoss", GD.Load<PackedScene>($"res://scenes/{encounter.Item1}.tscn"), encounter.Item1);
                boss = GetTree().GetFirstNodeInGroup("bosses") as Boss;
                int prior = game.Run.Score;
                Check(boss.TakeDamage(9999) && !boss.TakeDamage(9999), encounter.Item1 + " defeats once");
                Check(game.Run.Score - prior == encounter.Item3, encounter.Item1 + " pays exactly one bonus");
                await Frames();
            }
            game.Run.GrantPowerUp(PowerUpKind.Shield);
            player.Invulnerable = false;
            player.KillByBlast("QA"); player.KillByBlast("QA");
            Check(!(bool)player.GetType().GetField("isDead", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!, "shield grants recovery against simultaneous hits");
            player.SetPhysicsProcess(false); player.Velocity = Vector2.Zero;
            var contact = GD.Load<PackedScene>("res://scenes/body.tscn").Instantiate<Body>();
            contact.Configure(BodyKind.Drifter); contact.GlobalPosition = player.GlobalPosition;
            game.AddEntity(contact); contact.SetPhysicsProcess(false);
            Engine.TimeScale = 1; await Wait(0.1);
            Check(player.GetNode<Area2D>("HitBox").GetOverlappingBodies().Count > 0, "contact fixture overlaps the player");
            player._PhysicsProcess(1.1);
            Check((bool)player.GetType().GetField("isDead", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(player)!, "remaining inside danger after shield recovery is lethal");
            game.Free(); Engine.TimeScale = 1; GetTree().Paused = false;
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
