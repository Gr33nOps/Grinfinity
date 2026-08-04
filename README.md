# Grinfinity

A top-down twin-stick survival shooter built with **Godot 4.4** and **C#**.

You are a small, cheerful planet with a gun. Your gravity is why they come.

Bodies do not chase you — they *fall* toward you, gain momentum, overshoot and
clump. Every one you destroy sheds debris your gravity drags back in, and
absorbing it makes you heavier. Mass is the risk dial and the score multiplier at
the same time: heavy means a wider pull, visible rings, moons that orbit and
shoot for you, and a fatter multiplier — paid for with a bigger hitbox, slower
movement and a longer dash cooldown. Spend it with a nova, or ride it.

## Controls

| Action | Keyboard / Mouse | Gamepad |
|--------|------------------|---------|
| Move | `W` `A` `S` `D` | Left stick |
| Aim | Mouse | Right stick |
| Shoot | Left mouse / `Space` | Right trigger |
| Dash | `Shift` | B |
| Rapid fire | `E` | X |
| Nova (spends mass) | `R` | Y |
| Pause | `Esc` | Start |
| Toggle fullscreen | `F11` | — |

Aiming switches automatically between mouse and right stick — whichever you used last.
Every keyboard binding above can be changed in **Settings → Controls**.

## Features

- Five modes, chosen before an orbit: **Endless Orbit** (survive as long as
  you can), **Flyby** (60 seconds, maximum score), **Daily Alignment** (a
  fixed seed shared by everyone that day, one attempt), **Convergence**
  (three bosses back to back, no trash to hide behind) and **Glass Planet**
  (one hit kills you, your own shots hit five times as hard)
- Easy / Normal / Hard difficulty, scaling spawn rate, body speed and contact
  radius — never player damage
- A seeded RNG every gameplay roll draws from, so Daily Alignment reproduces
  the same orbit on every machine
- Gravity, not pathfinding: bodies orbit, overshoot, clump and slingshot
- Three weapons, chosen at orbit start: Comet, Debris Cannon, Ion Lance
- Seven body kinds that each teach something — splitting, armoured, orbiting,
  detonating — plus **The Coil**, a boss of spinning rings with one safe gap
- Mass as a single risk-and-reward dial — rings, moons, venting and a live
  score multiplier all read from it
- Score, survival timer, kill count and streak counter, saved as personal
  bests — per mode, alongside one "best of any mode" figure the menu shows
- Stardust, earned from time, kills and streaks, carried across every orbit
- Three soft-capped permanent upgrades bought with stardust: Thrust, Coolant, Ballast
- Nine achievements, tracked live and persisted
- A local top-10 leaderboard per mode: score, time, kills, weapon, world and date
- A lifetime stats screen: orbits, kills, time played, heaviest mass, favourite weapon
- Death recap: score, time, kills, best streak, mass at death and moons held
- Bodies unlock as an orbit goes on:
  - **Drifter** — the baseline, falls straight in
  - **Shard** — small and fast, arrives in packs (0:18)
  - **Planetoid** — large and slow, takes four hits (0:40)
  - **Fracture** — breaks into three Splinters when killed (1:02)
  - **Bulwark** — armoured across its leading face; flank it (1:25)
  - **Satellite** — holds a ring at fixed range and shoots inward (1:48)
  - **Flare** — detonates lethally on death; kill it at a distance (2:10)
  - **The Coil** — spinning rings with one safe gap, at 3:00
  - **The Brood** — spawns Shards continuously until it dies, once dealt with
  - **The Black Hole** — a rival gravity well with health, the thematic
	centrepiece, once dealt with
- Power-ups (Shield, Freeze, Magnet, Nuke, Overcharge), a relic rolled per
  orbit, 20-second arena events, gravity wells and comet flybys
- Twelve named, unlockable worlds — chosen before an orbit and held for the
  whole of it
- Ramping body speed and spawn rate
- Random player skins and enemy variants
- Dash and rapid-fire abilities
- Hitstop, trauma-based screen shake, bullet trails, muzzle flash and per-enemy
  death bursts
- Procedural deep-space background: a drifting nebula wash under three parallax
  star layers, drawn entirely in a shader
- Music that swells with how dangerous the run has become
- Pause menu and game over flow
- Settings: master / music / SFX volume, screen shake intensity (0 = off),
  fullscreen or windowed at a chosen resolution, VSync, an FPS cap, UI scale,
  and key rebinding
- Accessibility: a colourblind-safe body palette, high-contrast sprite
  outlines, hold-or-toggle rapid fire, an Assist Mode that gently slows
  bodies, a damage-numbers toggle, and gamepad aim assist
- Localisation-ready: every dynamic string and data-table description routes
  through a translation table (`translations/strings.csv`) — adding a
  language is a data change, not a code change
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

| Variable | Effect |
|---|---|
| `GRIN_SCENE` | Scene to load (default `res://scenes/game.tscn`) |
| `GRIN_SHOT` | Where to write the PNG |
| `GRIN_RUN` | Seconds to play for |
| `GRIN_WEAPON` | Loadout index: 0 Comet, 1 Debris Cannon, 2 Ion Lance |
| `GRIN_MODE` | Mode index: 0 Endless Orbit, 1 Flyby, 2 Daily Alignment, 3 Convergence, 4 Glass Planet |
| `GRIN_DIFFICULTY` | Difficulty index: 0 Easy, 1 Normal, 2 Hard |
| `GRIN_BOSS` | Seconds before The Coil arrives |
| `GRIN_MORTAL` | Leave contact damage on, to exercise death and the recap |
| `GRIN_PACIFIST` | Hold fire, so bodies pile to the spawn cap |

Every run also prints frame-time statistics — average, worst, and how many frames
went over the 16.67 ms budget — which is how the object-pooling question in
`ROADMAP.md` gets answered with a number instead of a guess.

`art_source/` holds the full-canvas originals of the character sprites. The
files under `sprites/` are cropped to their artwork bounds — symmetrically about
the original canvas centre, so every node's position, scale and flip stays valid.
Edit the masters, re-crop, and drop the result into `sprites/`.

## Saved data

Stored under Godot's user data directory (`%APPDATA%\Godot\app_userdata\Grinfinity` on Windows):

- `highscore.cfg` — best survival time, kills, streak and score, per mode and
  overall
- `leaderboard.cfg` — the local top-10, per mode
- `profile.cfg` — stardust, lifetime stats, unlocked worlds and achievements,
  upgrade levels, and the day Daily Alignment was last played
- `settings.cfg` — volume, fullscreen, key bindings, and the last weapon,
  world, mode and difficulty picked

## License

All rights reserved unless otherwise noted.
