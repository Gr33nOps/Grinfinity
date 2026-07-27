# Grinfinity

A top-down twin-stick survival shooter built with **Godot 4.4** and **C#**.

Survive as long as you can while enemies spawn faster and get quicker over time. Aim with the mouse, move with WASD, and use dash / rapid-fire abilities to stay alive.

## Controls

| Action | Input |
|--------|--------|
| Move | `W` `A` `S` `D` |
| Shoot | Left mouse / `Space` |
| Dash | `Shift` |
| Rapid fire | `Q` |
| Pause | `Esc` |

## Features

- Survival timer + best time save
- Ramping enemy speed and spawn rate
- Random player skins and enemy variants
- Dash and rapid-fire abilities
- Pause menu and game over flow
- Background music and SFX

## Requirements

- [Godot 4.4](https://godotengine.org/download) with **.NET** / C# support
- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Run locally

1. Clone this repository
2. Open the project folder in Godot 4.4 (.NET)
3. Wait for the C# solution to build
4. Press **Play** (main scene is the menu)

```bash
git clone https://github.com/Gr33nOps/Grinfinity.git
```

## Project layout

```
scenes/     Game, menu, player, enemies, UI
scripts/    C# gameplay systems
sprites/    Art and UI textures
sounds/     Music and SFX
fonts/      UI font
```

## License

All rights reserved unless otherwise noted.
