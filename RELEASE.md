# GRINFINITY 1.0.0-rc4

Release candidate for Windows x64, prepared 2026-09-24. One endless arcade survival mode. The updated direction replaces the old multi-mode and currency plans.

## Arcade balance and wording in rc4

Removed the private smile/fighting tagline from menus, credits, packaging text and cover-generation source. Spread Shot replaces the old upgrade name. The shield effect and its pickup icon are circular bubbles.

The starting Comet fires every 0.18 seconds instead of 0.22. Faster Shots cuts the firing interval by 18% per level. Spread adds one side bullet per level instead of two: two total bullets at level 1 and three at level 2 with the default gun. The central aimed shot stays straight; the first side shot alternates left/right at 18 degrees. These ranks first become eligible after waves 5 and 10. Faster Shots and Quicker Dash levels become eligible after waves 1, 4 and 7; Bigger Nova after 6, 9 and 12. The first spread rank and later eligible rank are guaranteed in their introduction-wave offers; choosing them is optional. Offers reserve one eligible standard stat/ability option, show at most one weapon replacement, and fill remaining slots randomly without duplicates or maxed-out options. KEEP CURRENT BUILD lets you skip any offer. Full catalog: GAMEPLAY_GUIDE.md.

## Visual and interface overhaul in rc4

The main menu now uses the same orange, berry, cream and plum illustration system as gameplay. All twelve planets have smooth facial expressions and cleaner surface detail. New illustrated ring halves, a circular shield bubble, armour plates, a faceless eclipse boss, rounded nozzle-sized shots and pickup badges replace the old line overlays. The whole planet rotates with aim, with blinking, recoil and squash independent of collision shapes. Superseded gameplay/pause/results nodes and old sprite dependencies have been removed from those scenes; old sprites are excluded from the package.

Boost selection freezes simulation with no countdown. Pause hides the gameplay UI, and resume restores the current choice and its keyboard/controller focus. Announcements cannot cover choices. The HUD places score and combo top-left, wave/time top-right, and ability/effect icons along the bottom. Rounded supporting menus share the same theme. Results emphasize score and PLAY AGAIN. Restart uses an explicit confirmation panel within pause.

## Shipped changes

**Gameplay:** free one-of-three boosts after a cleared wave, guaranteed ability milestones, two wave-gated levels of spread shot, reachable Debris Cannon and Ion Lance, and immediate feedback on choosing a boost. A new orbit starts with a shield. Shield loss grants one second of recovery; dash protects during its active window. Movement no longer gains diagonal speed. New gravity wells visibly form for 1.2 seconds before becoming dangerous. Hazard/event scheduling holds during wave choices. Boss arrivals choose a corner away from the planet.

**Readability and personality:** current score is the primary HUD number, with survival time and multiplier beneath. Wave/ability hints, streaks, owned abilities and active effects are the remaining readouts. Free boost cards have short descriptions and keyboard/controller selection. The smiling planet, space background and distinct enemy silhouettes remain. Original synthesized shooting, impact, shield, dash, nova, pickup, upgrade, menu and results sounds replace the old effects; a new looping music track responds in volume to danger. Lilita One supplies a consistent rounded font.

**Reliability:** repeated death callbacks cannot record an orbit twice; the world stops advancing during death/results. Focus loss pauses. Pause offers restart and quick audio/shake settings. Restart clears per-run state. SaveStore centralizes atomic save replacement, backups and validated reads. Invalid score/time/name input is rejected or sanitized; tied leaderboard entries retain their arrival order after reload. Clean quitting stops audio before the engine exits.

**Scope:** no new modes, currency, inventory, permanent power purchases or backend dependency. Lifetime cosmetics remain optional and do not change power. A run can continue after all three bosses; ordinary waves and capped escalation keep going.

## Pacing and score

The opening pack has six enemies arriving every 1.35 seconds. Packs grow by 1.5 (rounded) to a cap of 28; regular enemies are capped at 18 simultaneous bodies, with a 24-body split cap. Speed rises from 95 to 160 by wave 12 and 220 by wave 30. Spawn intervals gradually reach 0.72 seconds, then 0.5 seconds. Waiting in a difficult wave never increases either pressure. Growing does not accelerate spawns.

Dash unlocks free after wave 1, rapid fire after wave 3, and nova after wave 5, in addition to the normal boost choice. Quicker Dash requires dash; Bigger Nova requires nova. Weapon alternatives begin after waves 4 and 6, and selecting one commits that weapon for the run. New enemy types enter at waves 3, 4, 6, 8, 10 and 12. Bosses follow completed waves 6, 12 and 18. Choice screens stop simulation and have no timer.

Removed from live offers: Wider Pull, Rich Debris, Hungry Dash and Slow Field. Removed from drops: Magnet and Freeze. Remaining pickups are shield, overcharge and nova burst, at most two on screen. Nova bursts do not drop before wave 5. Comets require wave 5 and 60 seconds; wells require wave 9 and 150 seconds. Hazards have a shared breathing gap and do not launch during bosses, choices or arena events. First arena events require wave 10 and 180 seconds.

Scoring is accumulated live, not reconstructed from final stats:

- Survival: 10 points per second, multiplied by current mass bonus.
- Kill: 25 + 3 per preceding streak link, up to 25 paid links, multiplied by current mass bonus.
- A streak expires after 2.5 seconds without a kill.
- Debris increases the visible mass multiplier smoothly from x1 to x3. Growing heavy also makes the planet larger and slows it, while granting rings and moons.
- Boss awards: Coil 5,000; Brood 5,000; Black Hole 12,000, once each, unaffected by mass.

The leaderboard sorts descending by score and holds ten local entries, showing rank, name, score and survival time. Names are limited to twelve characters. Ties keep earlier entries first. No network request is made and no online ranking is implied. Providing a public global board later requires a chosen service, authentication and server-side score validation. The current board is appropriate for local arcade play; local files are not cheat-proof.

## Saves and settings

Four files in `%APPDATA%/Godot/app_userdata/Grinfinity`: `highscore.cfg`, `leaderboard.cfg`, `profile.cfg`, `settings.cfg`. Writes stage a temporary file, atomically replace the primary, and preserve the previous `.bak`. Missing/unreadable primaries recover from a readable backup; wrong types and nonfinite numeric fields use defaults. If neither copy loads, the game starts with defaults. Save errors warn in the log without blocking play. Older Endless Orbit records migrate forward.

Settings persist audio, display, rebinding and accessibility preferences. Active orbits are not resumed after closing. To reset safely, close the game and rename the entire save directory; keep it to restore progress later. Do not reset the QA folder expecting it to reset the real game.

## Build and package

Required: Godot **4.7.1 .NET**, a .NET SDK supporting `net8.0`, and matching **mono** export templates. Standard Godot cannot load the C# project. The installed Godot MCP was initially configured to a standard binary. Its configuration now points to the installed .NET executable (effective when reconnected); the included MCP client explicitly selects that executable for actual launch/debug/stop checks.

From the repository in PowerShell, with `$Godot` set to the .NET console executable:

```powershell
dotnet build --configuration Debug
& $Godot --headless --path . --editor --import --quit
& tools/run-qa.ps1 -Godot $Godot
& $Godot --headless --path . --export-release 'Windows Desktop' --quit
```

Close or synchronize any open editor before exporting, so stale in-memory scenes cannot overwrite repaired resource references. For this candidate the project was copied to a fresh staging directory (excluding .godot, .git and builds), built/imported there, and exported to the output path below. Check every exit code and inspect the import/export logs for errors. The export path is `builds/windows-rc4/Grinfinity.exe`. Distribute the whole folder: executable, `.pck`, `data_Grinfinity_windows_x86_64`, player instructions and license notices. The runtime is self-contained. Development tools, source art, old font/audio and debug symbols are excluded. The build is unsigned; signing requires the owner's certificate. The Linux preset is retained but this release package targets Windows, and Linux is not claimed as tested.

The artifact is `builds/Grinfinity-1.0.0-rc4-windows-x64.zip`; its SHA-256 is alongside it. See [Godot's Windows export documentation](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_windows.html). This C# project is distributed as a download; the current Godot documentation lists C# web export as unsupported. See [Web export limitations](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html).

## itch.io publishing

1. Sign in to the owner's itch.io account; Dashboard → Create new game.
2. Title: **GRINFINITY**. Classification: Games. Kind: Downloadable. Genre: Action. Suggested tags: arcade, survival, space, 2D, singleplayer.
3. Short description: **An arcade space shooter. Clear enemy waves, upgrade your weapons, defeat three bosses, and chase a high score.**
4. Upload `Grinfinity-1.0.0-rc4-windows-x64.zip`; mark it for Windows. Do not mark it as an HTML browser game.
5. Add controls and extraction instructions from README.md; state that leaderboards are local. Choose price/donations yourself.
6. Add the supplied cover and actual gameplay screenshots from `builds/store/`. Use a 315:250 cover ratio.
7. Save and view the private page. Download the uploaded ZIP, extract it, launch it, finish a run, and retry.
8. Set visibility to Public when the page and uploaded download are approved.

No account was accessed and nothing was published. These steps follow the [itch.io creator guide](https://itch.io/docs/creators/getting-started).

## Verification and remaining limits

The detailed current run results are recorded in `QA_RESULTS.md`. Checks cover the single endless mode, real scene transitions, persistence, shield recovery, real spread projectiles, bosses, repeated retries, rendered menus and actual export startup. Automated input is used for repeatable rendered playtests; screenshots were inspected. These checks establish behaviour, not a promise that every player will find the balance fun.

External dependencies only: public publishing needs the owner's account and final price/visibility choices; online rankings need a backend; code signing needs a certificate. Physical controller hardware, a clean low-end PC and subjective playtesting remain useful release validation, rather than checks silently claimed as completed. The existing artwork's ownership remains the project owner's responsibility; the new gameplay SVGs are original and no external art was introduced. The package includes the openly licensed replacement font and engine/runtime notices.

## Later Steam release

Keep this single mode. Add a small platform adapter around score submission, achievements and cloud-save access rather than importing platform calls into gameplay. Obtain the real Steam App ID and SDK access; map score to a descending integer leaderboard, version the board when scoring changes, and retain the local fallback. Decide how assist-enabled runs should be categorized before introducing a competitive public board. Configure cloud paths for the four save files, with conflict/recovery testing. Test controller navigation and Steam Deck, prepare capsule/store assets and depots, and validate a clean Steam installation before requesting release review. Steam integration is not included in this itch build. See [Steam leaderboard documentation](https://partner.steamgames.com/doc/features/leaderboards).
