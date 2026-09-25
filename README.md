# GRINFINITY

An arcade space shooter: clear waves, defeat bosses, and chase a high score.

Move, aim and shoot. Clear a wave, choose one free boost while the action pauses, then dive back in. Collect debris to grow rings, orbiting moons and your score multiplier. You begin with one shield; after it breaks, the next unprotected hit ends the orbit. Retry straight from the results screen.

## Play the Windows build

Extract `builds/Grinfinity-1.0.0-rc4-windows-x64.zip` into a folder and launch `Grinfinity.exe`. Keep the `.pck` and `data_Grinfinity_windows_x86_64` folder beside it. No separate .NET installation is required for the exported game.

| Action | Keyboard / mouse | Xbox-style controller |
|---|---|---|
| Move | WASD | Left stick |
| Aim | Mouse | Right stick |
| Shoot | Left mouse | Right trigger |
| Dash, once unlocked | Shift | B |
| Rapid fire, once unlocked | E | X |
| Nova, once unlocked and charged | R | Y |
| Choose a boost | 1 / 2 / 3 or click | D-pad, then A |
| Pause | Escape | Start |

Keys can be rebound. Settings include audio, fullscreen, resolution, VSync, frame cap, HUD text scale, reduced shake, colourblind palette, high-contrast outlines, aim assist and assist speed. Cosmetic planet choices and your leaderboard name live in Stats. Cosmetics have no power advantage.

## What is in this version

The main menu now shares the illustrated gameplay theme. Gameplay now uses an original illustrated asset set: expressive planets, a separate recoiling blaster, distinct enemies and bosses, warm star effects, illustrated pickups and a layered space background. Compact HUD, untimed boost choices, exclusive pause screens and score-first results replace the overlapping interface.

- One endless survival mode, with short waves and a gradually changing enemy mix.
- Distinct chasing, swarming, armoured, splitting, orbiting/shooting and exploding threats.
- Three boss encounters within a continuing endless run, plus telegraphed comets, gravity wells and brief arena events.
- Dash unlocks after wave 1, rapid fire after wave 3, and nova after wave 5. Free between-wave boosts improve shooting, piercing, spread and abilities, with optional weapon choices. Spread Shot adds one angled bullet per level, first available after waves 5 and 10. The starting gun fires every 0.18 seconds. Nothing to buy or grind outside a run.
- Three pickup types (shield, overcharge and nova burst), growing rings and helper moons, impact effects, and original synthesized music and sound effects.
- Local score-ranked top ten, personal bests, lifetime records and cosmetic achievements. Fully offline; no account or network required.

Survival earns points; kills and quick streaks earn more. Absorbing debris raises the visible multiplier from x1 to x3. Bosses add a flat bonus. Upgrade timing, enemy behaviours and boss attacks are listed in [GAMEPLAY_GUIDE.md](GAMEPLAY_GUIDE.md). Release verification is in [RELEASE.md](RELEASE.md).

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

### Rendered playtests

```powershell
$env:GRIN_RUN = '90'
$env:GRIN_AUTO_BUY = '1'
$env:GRIN_SIZE = '1280x720'
$env:GRIN_SHOT = 'C:/Temp/grinfinity.png'
& $Godot --path "builds/qa/isolated-project" res://tools/playtest.tscn
```

The capture harness moves, aims and fires through actual gameplay. By default it is invulnerable so longer runs can be inspected. `GRIN_MORTAL=1` enables real deaths; `GRIN_PACIFIST=1` stops shooting. `GRIN_CAPTURE_BREAK=1` captures the first choice, `GRIN_VIEW=options` captures pause settings, `GRIN_BOSS_INDEX=0/1/2` accelerates a boss, and `GRIN_UNCAPPED=1` measures uncapped rendering. `GRIN_SCENE` can select a menu scene. Clear environment overrides between tests. Automated play does not establish subjective fun or physical controller compatibility.

Godot MCP is supported by `tools/godot-mcp-smoke.mjs`; pass the installed MCP server entry point, the .NET executable, and project path. This avoids accidentally launching the standard non-C# engine.

## Saves

Windows: `%APPDATA%/Godot/app_userdata/Grinfinity/`.

`highscore.cfg`, `leaderboard.cfg`, `profile.cfg`, and `settings.cfg` use atomic replacement and keep the previous `.bak`. Missing or unreadable primary files can recover from that backup; invalid field types fall back safely. Old Endless Orbit records migrate forward. There is no active-run resume.

To reset, close the game and rename the entire save directory as a backup. A fresh directory is created on next launch. Renaming only one primary file can restore its `.bak`, so move the pair if resetting an individual record.

## Assets and rights

Main-menu art remains part of the original project. The new gameplay SVG assets live in `art/cosmic/`; `tools/make_vector_art.py` generates the principal character and icon set. New synthesized audio is generated by `tools/make_audio.py`, without samples. Lilita One uses SIL OFL; see `fonts/OFL-LilitaOne.txt`. Godot and bundled runtime notices are included in the release package. All rights to original project material remain with its owner unless otherwise stated. Historical roadmap documents are not the current feature list.
