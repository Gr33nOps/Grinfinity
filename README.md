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
| Rapid fire | `Q` | X |
| Pause | `Esc` | Start |
| Toggle fullscreen | `F11` | — |

Aiming switches automatically between mouse and right stick — whichever you used last.

## Features

- Survival timer + best time save
- Ramping enemy speed and spawn rate
- Random player skins and enemy variants
- Dash and rapid-fire abilities
- Pause menu and game over flow
- Settings screen: master / music / SFX volume, fullscreen
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
scenes/       Game, menu, settings, credits, player, enemies, UI
scripts/      C# gameplay systems
sprites/      Art and UI textures (trimmed for runtime)
art_source/   Untrimmed original art masters, excluded from the Godot import
sounds/       Music and SFX
fonts/        UI font
```

`art_source/` holds the full-canvas originals of the character sprites. The
files under `sprites/` are cropped to their artwork bounds — symmetrically about
the original canvas centre, so every node's position, scale and flip stays valid.
Edit the masters, re-crop, and drop the result into `sprites/`.

## Saved data

Stored under Godot's user data directory (`%APPDATA%\Godot\app_userdata\Grinfinity` on Windows):

- `highscore.cfg` — best survival time
- `settings.cfg` — volume and fullscreen preferences

## License

All rights reserved unless otherwise noted.
