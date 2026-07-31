# Grinfinity — Audit & Fix Report

Audited and fixed 2026-07-31 against Godot 4.4.1 stable (mono).
Covers code, scenes, project settings, assets, audio, repo hygiene, and runtime behaviour.

**Status: all 29 findings addressed.** Every fix below was verified by a clean
build, a clean headless import, and a runtime check. Nothing is committed — the
working tree is left for review.

---

## Verification performed

| Check | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `godot --headless --import` from a **deleted** `.godot/` cache | no errors |
| Menu / game / settings / credits / game-over scenes launched | no errors, no warnings |
| Pause behaviour (instrumented) | timer, spawner and enemies all frozen — see below |
| Death → save → transition | high score written, verified twice |
| Legacy save migration | 206.49 s record carried into the new format |
| Visual pass | all five screens captured and inspected |

The pause test is the important one, because P0-2 was invisible from the outside:

```
before-pause:   time=4.00 enemies=2 entities=7 paused=False menuVisible=False
just-paused :   time=4.00 enemies=2 entities=7 paused=True  menuVisible=True
after-5s-pause: time=4.00 enemies=2 entities=7 paused=True  menuVisible=True
resumed     :   time=4.00 enemies=2 entities=7 paused=False menuVisible=False
```

Five real seconds paused, zero simulation advance. Before the fix the timer would
have reached ~9.00 and the enemy count would have climbed.

---

## P0 — Was broken

### P0-1. Stale import cache — background, HUD icons, app icon, and **all enemies** failed to load
`.godot/imported/` had no `.ctex` for `background.png`, `Dash.png`, `RapidFire.png`
or `icon.png`, and `uid_cache.bin` still mapped `enemy.tscn`'s script UID to
`res://scripts/Enemies.cs` — renamed to `Enemy.cs` at some point. `enemy.tscn`
therefore failed to parse, `EnemySpawner` hit `enemyScene == null` and returned:
**the game was running with zero enemies.**

**Fixed.** Cache rebuilt from scratch; import is clean from a cold start. To stop
it recurring, CI now runs `godot --headless --import` and fails on any error
(`.github/workflows/ci.yml`), and the README documents the import-once step.

### P0-2. The pause menu did not pause the game
`game.tscn` set `process_mode = 3` (Always) on the root and `GameManager` set it
again in code, so every child inherited Always: the player kept moving and
shooting, the spawner kept spawning, and the survival timer kept counting behind
the pause overlay.

**Fixed.** The root stays Always (it must still read the pause key), but it now
opts its children back out. `GameManager.AddPausableChild()` sets
`ProcessMode.Pausable` on `ScoreManager`, `EnemySpawner`, `UIManager` and
`PlayerManager`; `player` is Pausable in the scene; and a new pausable `Entities`
container parents every runtime-spawned bullet, enemy and particle burst via
`GameManager.Spawn()`. Verified above.

### P0-3. The player had no collision shape
`player.tscn` had no `CollisionShape2D` at all, so `MoveAndSlide()` resolved
nothing and hit detection was a hand-rolled 100 px distance scan over every enemy,
every physics frame.

**Fixed.** Added a `CollisionShape2D` plus a dedicated `HitBox` `Area2D`
(mask 2) wired to `BodyEntered`. The trigger distance is unchanged (25 px local
× 2 scale = 50 px, plus the enemy's 50 px = the original 100 px), so the game
feels identical. `CheckSpriteCollisions()` is gone, which also removes P1-15.

---

## P1 — Serious logic and UX bugs

| # | Issue | Fix |
|---|---|---|
| P1-4 | Shoot sound played twice per shot | Removed the duplicate `PlayShootSound()` inside `ShootBullet`; `PlayerAbilities` is now the only caller |
| P1-5 | Fade-in raced the scene change; no re-entrancy guard | `ChangeScene` now awaits a `ProcessFrame` after `ChangeSceneToFile`, checks the returned `Error`, and guards with `isTransitioning` |
| P1-6 | Esc during the death transition soft-locked the game | `GameManager` has an `isGameOver` guard on `_Input`; `SceneTransition.ChangeScene` also force-clears `GetTree().Paused` |
| P1-7 | Ability icons drew over the pause menu | `UIManager` is Pausable, so `_Process` stops while paused and leaves `ShowCursor()`'s state alone |
| P1-8 | Hardcoded `/root/game/...` paths; crosshair behind the HUD | All lookups are relative to `GetParent()` with `GetNodeOrNull`; crosshair moved to a `CrosshairLayer` (layer 2) above the HUD |
| P1-9 | Game-over screen spawned a never-freed `ScoreManager` | `GetFormattedHighScore()` / `FormatTime()` are now static; the throwaway node is gone |
| P1-10 | Enemies never despawned | `Enemy.CullIfLost()` frees anything that drifts past a 600 px margin |
| P1-11 | Speed ramp only applied to newly-spawned enemies | `EnemySpawner.CurrentSpeed` is published and read by every living enemy each frame |
| P1-12 | A bullet could hit twice | `hasHit` flag plus a deferred `Monitoring = false` on first contact |
| P1-13 | A `Timer` node allocated per explosion | Uses the `CPUParticles2D.Finished` signal instead (the scene is already `one_shot`) |
| P1-14 | Save file had no format marker or error handling | Migrated to `ConfigFile` with a version key, `Error` checks and `using`; old raw-float saves are migrated automatically |
| P1-15 | O(n) scan + array allocation every physics frame | Removed with P0-3 — collision is now signal-driven |

---

## P2 — Configuration, assets and hygiene

| # | Issue | Fix |
|---|---|---|
| P2-16 | Launched exclusive-fullscreen with no way out | Defaults to maximised; **F11 toggles fullscreen anywhere**, plus a settings toggle that persists |
| P2-17 | Every character sprite was an 800×600 PNG at ~23 % scale | All 23 trimmed; **42.1 MB → 5.8 MB** of VRAM. Texture filter switched to Linear |
| P2-18 | Only the Master audio bus existed | Added `default_bus_layout.tres` with Music and SFX buses; every player reassigned |
| P2-19 | No export presets | Added `export_presets.cfg` (Windows + Linux) and un-ignored it in `.gitignore` |
| P2-20 | Glow only in `game.tscn` | Resolved by removing glow entirely (author's call) — see note below |
| P2-21 | Three dead leaderboard labels in the menu | Removed; replaced with one *wired* `BestTimeLabel` |
| P2-22 | Orphaned `player_name.save` | Archived to `player_name.save.orphan-bak` |
| P2-23 | Stale imports; `GameOver..ogg` typo | `.godot/` rebuilt clean (45 → 44 entries, no orphans); file renamed to `GameOver.ogg` |
| P2-24 | Dead public API | Removed `GetInstance`, `ResetScore`, `ResetHighScore`, `GetHighScore`, `SetDead`, both `GetRapidFireTimeLeft`, and the unused `"bullets"` group |
| P2-25 | No `[Export]` — every tweak needed a recompile | Movement, abilities, bullet and spawner tuning are all exported; scenes preloaded via `[Export] PackedScene` |
| P2-26 | Naming inconsistencies | `UiManager.cs` → `UIManager.cs` (via `git mv`), enemy root → `Enemy`, transition root → `SceneTransition` |
| P2-27 | Input map not launch-ready | `esc` → `pause`; full gamepad map added (sticks, triggers, face buttons, Start) |
| P2-28 | Shader `TIME` drifted over long sessions | Wrapped with `fract()` |
| P2-29 | No CI, thin README | Added `.github/workflows/ci.yml`; README documents controls, the import-once step, layout and save locations |

### Two things found only by looking at the screen

Both were latent bugs the audit's static pass could not have caught, exposed once
`window/stretch/aspect="expand"` made the viewport wider than the 1920 base:

- **The background did not cover the full viewport** — a ~46 px dark strip down
  the right edge. `background.tscn` was a bare 40×40 `TextureRect` relying on the
  root Window to size it. It is now a `CanvasLayer` (layer −100) with a Full Rect
  child, so it tracks the viewport.
- **The game-over labels flew off-screen.** Their bottom-anchored offsets were
  written against the old zero-size root rect, so they only worked by accident.
  Re-anchored properly, along with the buttons and the GAME OVER image.

The menu had the same latent problem: its Controls sat under a `Node2D`, where
anchors resolve against a zero rect and offsets act as absolute coordinates.
Rebuilt under a `CanvasLayer`: the title arc wraps the PLAY/QUIT ball (they are
two halves of one circle and must stay joined), and SETTINGS/CREDITS sit as flat
text buttons in the bottom-left corner.

### Note on the glow

The original inconsistency (glow in `game.tscn` only) is resolved by **removing
bloom entirely** — the author's decision after seeing it applied. The
`WorldEnvironment`, the `WorldFx` autoload and `rendering/viewport/hdr_2d` are
all gone, so nothing anywhere applies post-processing. Sprites render flat.

Worth recording for anyone tempted to reintroduce it: large blocks of white menu
text are brighter than any gameplay sprite, so no single HDR threshold flatters
both. A threshold low enough to bloom the player blows menu text out completely.
Glow here has to be per-scene or not at all.

---

## New files

```
scripts/GameSettings.cs      Autoloaded settings: volumes, fullscreen, persistence
scripts/SettingsMenu.cs      Settings screen
scripts/Credits.cs           Credits screen
scenes/settings.tscn         Settings screen
scenes/credits.tscn          Credits screen
default_bus_layout.tres      Master / Music / SFX buses
export_presets.cfg           Windows + Linux export presets
.github/workflows/ci.yml     Import + build CI
art_source/sprites/          Untrimmed art masters (excluded via .gdignore)
```

## Still open (deliberately out of scope)

These are features from `LAUNCH_TODO.md`, not defects: kill scoring and combos,
death recap, new enemy types, bosses, pickups, weapon variety, difficulty select,
unlocks and currency, leaderboards, screen shake, and music intensity layers.
Key **rebinding** also remains unbuilt — the input map is now rebind-ready
(actions are named by intent, not by key) but there is no UI for it yet.
