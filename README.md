# GRINFINITY

A tiny smiling planet with a big gun, surviving endless space. How long can you last?

Move, aim and shoot in an arena about three screens across. Enemies never stop coming and the run gets harder the longer you survive. Dash, Overdrive and Nova are ready from the start, every kill fills a CORE bar you spend on a small skill tree, and bosses turn up on a clock and keep coming round. One hit ends the run unless you are carrying a shield. Retry straight from the results screen.

## Play the Windows build

Extract `builds/Grinfinity-1.0.0-rc4-windows-x64.zip` into a folder and launch `Grinfinity.exe`. Keep the `.pck` and `data_Grinfinity_windows_x86_64` folder beside it. No separate .NET installation is required for the exported game.

| Action | Keyboard / mouse | Xbox-style controller |
|---|---|---|
| Move | WASD | Left stick |
| Aim | Mouse | Right stick |
| Shoot | Left mouse | Right trigger |
| Dash | Shift | B |
| Overdrive | E | X |
| Nova | Q | Y |
| Upgrades | Tab | Back / View |
| Pause | Escape | Start |

Keys can be rebound. Settings include audio, fullscreen, resolution, VSync, frame cap, HUD text scale, reduced shake, colourblind palette, high-contrast outlines, aim assist and assist speed. Cosmetic planet choices and your leaderboard name live in Stats. Cosmetics have no power advantage.

## What is in this version

- One endless survival run in a bounded arena with a following camera. No waves, no pauses, no upgrade menu.
- Seven enemy kinds (plus the Fracture's Splinters) that join the mix over time, rushes in five formations, telegraphed comets, gravity wells and brief arena events.
- Three abilities on their own cooldowns, all charging from the first second: Dash, Overdrive, Nova.
- A CORE bar filled by every kill, spent on a small four-branch skill tree (Gun, Dash, Overdrive, Nova) while the game waits. Moon, CORE Burst and Power Cell pickups drop along the way; up to three moons orbit the planet, each blocking one hit.
- The Coil, The Brood and The Black Hole, cycling forever with more enemies joining each round.
- Local top ten ranked by survival time, personal bests, lifetime stats, cosmetic planets and achievements. Fully offline.

Every timing, cooldown and drop rule is in [GAMEPLAY_GUIDE.md](GAMEPLAY_GUIDE.md), and every tuning number in `scripts/Balance.cs`. Release verification is in [RELEASE.md](RELEASE.md).

## Develop

Use **Godot 4.7.1 .NET**, the .NET 8 SDK or a compatible newer SDK, and the matching **mono** export templates. The standard Godot binary cannot load this project's C# scripts.

```powershell
dotnet build --configuration Debug
& $Godot --headless --path . --editor --import --quit
& $Godot --path .
```

Set `$Godot` to the .NET engine console executable. For the isolated integration suite:

```powershell
& tools/run-qa.ps1 -Godot $Godot
```

It copies the project to a builds/qa/isolated-project directory and uses a separate `Grinfinity-QA` save folder. Success requires `QA RESULT: 0 failure(s)`. The runner builds first and aborts on compilation failure before copying the fresh assembly. `tools/ReleaseQa.cs` is compiled only in Debug and `tools/` is excluded from exports.

### Bot playtests

```powershell
& tools/run-bot.ps1 -Godot $Godot -Runs 5              # mortal runs, printed timeline and summary
& tools/run-bot.ps1 -Godot $Godot -Immortal -MaxSeconds 1800
& tools/run-bot.ps1 -Godot $Godot -Immortal -Windowed -Shots "60,180,300" -ShotDir C:/Temp
```

`tools/survival_bot.gd` plays whole runs (kiting, aiming with the stick, collecting pickups, using all three abilities) in an isolated copy of the project, so it never touches real saves. It logs unlocks, drops, boss arrivals and fight lengths, and why each run ended. It aims perfectly but dodges worse than a good player, so treat its survival times as a floor.

`tools/playtest.tscn` captures a single screen (`GRIN_SCENE`, `GRIN_VIEW=pause`, `GRIN_SHOT`), and `tools/gallery.tscn` lays out every enemy, pickup and boss for an art review. Automated play does not establish subjective fun or physical controller compatibility.

Godot MCP is supported by `tools/godot-mcp-smoke.mjs`; pass the installed MCP server entry point, the .NET executable, and project path. This avoids accidentally launching the standard non-C# engine.

## Saves

Windows: `%APPDATA%/Godot/app_userdata/Grinfinity/`.

`highscore.cfg`, `leaderboard.cfg`, `profile.cfg`, and `settings.cfg` use atomic replacement and keep the previous `.bak`. Missing or unreadable primary files can recover from that backup; invalid field types fall back safely. Old Endless Orbit records migrate forward. There is no active-run resume.

To reset, close the game and rename the entire save directory as a backup. A fresh directory is created on next launch. Renaming only one primary file can restore its `.bak`, so move the pair if resetting an individual record.

## Assets and rights

Main-menu art remains part of the original project. The new gameplay SVG assets live in `art/cosmic/`; `tools/make_vector_art.py` generates the principal character and icon set. New synthesized audio is generated by `tools/make_audio.py`, without samples. Lilita One uses SIL OFL; see `fonts/OFL-LilitaOne.txt`. Godot and bundled runtime notices are included in the release package. All rights to original project material remain with its owner unless otherwise stated. Historical roadmap documents are not the current feature list.
