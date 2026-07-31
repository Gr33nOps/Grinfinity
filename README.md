# Grinfinity

A top-down twin-stick survival shooter built with **Godot 4.4** and **C#**.

Survive as long as you can while enemies spawn faster and get quicker over time. Aim with the mouse, move with WASD, and use dash / rapid-fire abilities to stay alive.

## Controls

| Action | Keyboard / Mouse | Gamepad |
|--------|------------------|---------|
| Move | `W` `A` `S` `D` | Left stick |
| Aim | Mouse | Right stick |
| Shoot | Left mouse / `Space` | Right trigger |
| Dash | `Shift` | B |
| Rapid fire | `E` | X |
| Pause | `Esc` | Start |
| Toggle fullscreen | `F11` | — |

Aiming switches automatically between mouse and right stick — whichever you used last.
Every keyboard binding above can be changed in **Settings → Controls**.

## Features

- Survival timer, kill count and streak counter, all saved as personal bests
- Death recap: time survived, kills, best streak, and a new-record callout
- Three enemy types that unlock as a run goes on:
  - **Chaser** — the baseline, walks straight at you
  - **Swarmer** — small and fast, arrives in packs (from 20s)
  - **Tank** — large and slow, takes four hits (from 45s)
- Ramping enemy speed and spawn rate
- Random player skins and enemy variants
- Dash and rapid-fire abilities
- Hitstop, trauma-based screen shake, bullet trails, muzzle flash and per-enemy
  death bursts
- Procedural deep-space background: a drifting nebula wash under three parallax
  star layers, drawn entirely in a shader
- Music that swells with how dangerous the run has become
- Pause menu and game over flow
- Settings: master / music / SFX volume, screen shake intensity (0 = off),
  fullscreen, and key rebinding
- Gamepad support
- Background music and SFX on separate audio buses

## Requirements

- [Godot 4.4](https://godotengine.org/download) with **.NET** / C# support
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Run locally

```bash
git clone https://github.com/Gr33nOps/Grinfinity.git
```

1. Open the project folder in Godot 4.4 (.NET) **once** so it imports the assets.
   The `.godot/` cache is not committed, so a fresh clone has no imported
   textures until the editor (or `godot --headless --import`) has run. Skipping
   this step makes scenes fail to load with
   `referenced non-existent resource` errors.
2. Wait for the C# solution to build.
3. Press **Play** (main scene is the menu).

To import and build without opening the editor:

```bash
godot --headless --path . --import && dotnet build
```

## Project layout

```
scenes/       Game, menu, settings, credits, player, enemies, UI, shaders
scripts/      C# gameplay systems
sprites/      Art and UI textures (trimmed for runtime)
art_source/   Untrimmed original art masters, excluded from the Godot import
sounds/       Music and SFX
fonts/        UI font
tools/        Dev-only helpers, not part of the game
```

### Unattended screenshots

`tools/dev_capture.tscn` runs a scene without a human at the keyboard and writes
a PNG, which is how visual changes get checked. It plays the game for real —
holding fire and sweeping the aim — with contact damage switched off so a run can
reach its busy late minutes.

```bash
GRIN_SCENE=res://scenes/game.tscn GRIN_RUN=60 GRIN_SHOT=/tmp/shot.png godot --path . --windowed res://tools/dev_capture.tscn
```

`art_source/` holds the full-canvas originals of the character sprites. The
files under `sprites/` are cropped to their artwork bounds — symmetrically about
the original canvas centre, so every node's position, scale and flip stays valid.
Edit the masters, re-crop, and drop the result into `sprites/`.

## Saved data

Stored under Godot's user data directory (`%APPDATA%\Godot\app_userdata\Grinfinity` on Windows):

- `highscore.cfg` — best survival time, best kill count, best streak
- `settings.cfg` — volume, fullscreen and key binding preferences

## License

All rights reserved unless otherwise noted.
