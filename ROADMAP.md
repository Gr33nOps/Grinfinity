# Grinfinity — Design & Development Roadmap

The plan we build to, start to finish. Supersedes `LAUNCH_TODO.md` (folded in
here; the original is in git history).

Written against the actual codebase: Godot 4.4 / C#, `GameManager` +
`EnemySpawner` + `PlayerAbilities` + `ScoreManager` + `GameSettings`.

---

## 1. What this game is

> **You are a small, cheerful planet with a gun. Your gravity is why they come.**

That one sentence should decide every argument from here on. It is not a
re-skin — it is a mechanic waiting to be built. Right now enemies "chase" the
player because a script points them at it. The moment they are *pulled* instead,
the game has an identity nothing else in the genre has.

### Pillars

| Pillar | Meaning | What it rules out |
|---|---|---|
| **Gravity is the fantasy** | Everything orbits, falls, slingshots, clumps | Enemies that ignore physics; a static arena |
| **Cheerful violence** | Bright, round, grinning; carnage without grimness | Gore, horror framing, muddy palettes |
| **One more run** | 2–6 minute runs, instant restart, visible progress | Long levels, cutscenes, save-scumming |
| **Readable chaos** | 100 things on screen, still legible at a glance | Visual noise that hides threats |

### Session shape

Target run: **2–6 minutes**. Restart in **under 2 seconds** from death. A play
session is 20–40 minutes of repeated runs.

---

## 2. Where we stand today

Honest inventory so nothing gets rebuilt.

**Done and working**
- Core loop: move, aim, shoot, dash, rapid fire, survive, die, restart
- Pause, game over, scene transitions, settings, credits, key rebinding
- Kill count + streak tracking, death recap with new-record callout
- Three enemy kinds (Chaser / Swarmer / Tank) unlocking over a run
- Bullets deal damage; tanks take four hits with a hit flash
- Ramping speed and spawn rate
- Versioned save (`highscore.cfg` v2), settings save, audio buses, gamepad
- Windows + Linux export presets, CI that fails on import errors

**Deliberately not built yet** — everything below.

**Known debt to watch**
- Bullets and enemies are instantiated per spawn — no pooling yet
- Enemy behaviour is one `_PhysicsProcess` branch; will need a state/strategy
  split once there are 6+ kinds
- No object for "the run" — run state lives across `GameManager` and
  `ScoreManager`. A `RunState` owner should appear in M2.

---

## 3. The gravity spine

The single most important system to build. Everything in M3+ hangs off it.

### 3.1 Mass

The player has **Mass**. It starts small and grows as you absorb debris from
kills.

| Higher mass | Lower mass |
|---|---|
| Larger pull radius → more enemies converge | Fewer enemies, calmer screen |
| Higher score multiplier | Lower multiplier |
| Larger hitbox | Small, nimble hitbox |
| Slower movement, longer dash cooldown | Fast, twitchy |

Mass is **the risk dial the player controls**, and it is the same dial as the
score multiplier. That is the whole game in one number.

### 3.2 Venting

Abilities **spend Mass** rather than sitting on pure cooldowns. Dash vents a
little; a nova vents a lot. So the loop becomes:

> pull enemies in → kill → absorb → get heavier and more dangerous to yourself →
> choose when to cash mass out as power → repeat

This gives every run an arc the player authors, instead of a difficulty curve
that just happens to them.

### 3.3 Pull, not chase

Replace the beeline in `Enemy._PhysicsProcess` with an acceleration toward the
player scaled by mass and distance. Enemies gain momentum, overshoot, and swing
back — free visual interest, and it makes dashing *through* a cluster feel great.

Keep a hard "minimum approach speed" so nothing stalls out of reach.

### 3.4 Emergent toys this unlocks

- **Clumping** — heavy mass bunches enemies, rewarding a single well-placed shot
- **Slingshot dash** — dash past a cluster and drag it off course
- **Gravity wells** — arena hazards that fight your pull
- **Rival wells** — the Black Hole boss (see M4)

---

## 4. Milestones

Ordered so each one leaves a better game than the last. Do not skip ahead —
M1 exists because tuning an unfelt game wastes the work in M2+.

---

### M1 — Make it feel good *(before adding anything)*

**Goal:** the current game, but every action is satisfying.

- [ ] **Playtest and tune existing numbers.** Swarmer/tank unlock times, tank
      health, pack size, speed multipliers are all *guesses* and all `[Export]`s.
      Tune them live in the inspector, then commit the values.
- [ ] **Hitstop** — 40–60 ms freeze on kill. Single cheapest feel upgrade.
- [ ] **Screen shake** — short, small, on kill; larger on player death. Must have
      an intensity slider (0 = off) from day one.
- [ ] **Hit feedback** — enemy flash (done for tanks, extend to all), knockback
      on non-fatal hits, punchier death particles.
- [ ] **Muzzle flash + shell/spark on fire**, bullet trail.
- [ ] **Kill chain banner** — the streak counter should *pop* and scale, not just
      change text.
- [ ] **Audio layering** — separate kill / big-kill / streak-milestone sounds;
      pitch variation so repetition doesn't fatigue.
- [ ] **Music intensity** — second music layer that fades in above a danger
      threshold.

**Done when:** killing one enemy feels good with the sound off, and feels great
with it on.

---

### M2 — The gravity spine

**Goal:** the fantasy becomes the mechanic.

- [ ] **`RunState`** — one owner for mass, kills, streak, time, modifiers.
      Extract from `GameManager`/`ScoreManager`.
- [ ] **Mass meter** on the HUD — a ring around the player, not a bar in a corner.
- [ ] **Debris pickups** — kills drop motes; motes are pulled in by *your* gravity
      (self-demonstrating mechanic), absorbed on contact, grant mass.
- [ ] **Pull replaces chase** in `Enemy._PhysicsProcess`, scaled by mass.
- [ ] **Mass affects** hitbox scale, move speed, score multiplier, spawn rate.
- [ ] **Venting** — dash and nova consume mass.
- [ ] **Score rework** — score = f(time, kills, streak, average mass). Bump save
      to v3.

**Done when:** a player can explain, unprompted, why they chose to stay light or
go heavy.

---

### M3 — Arsenal and bestiary

**Goal:** runs differ by *how* you fight.

**Weapons** — pick one at run start; 3 at first, 5 later.

| Weapon | Fantasy | Tradeoff |
|---|---|---|
| Pea Shooter | Current gun, reliable | Baseline |
| Scatter | Shotgun cone, deletes swarms | Useless at range |
| Lance | Piercing line through a whole clump | Slow fire, needs lining up |
| Heavy | One big slow shot, huge knockback | Punishing to miss |
| Overheat | Hold to charge; overheats and locks | High skill ceiling |

**Enemies** — extend `EnemyKind` and split behaviour out of one method.

| Enemy | Behaviour | Teaches |
|---|---|---|
| Chaser ✅ | Straight in | Baseline |
| Swarmer ✅ | Fast, packs | Crowd control |
| Tank ✅ | Slow, 4 HP | Target priority |
| **Splitter** | Dies into 3 minis | Don't kill it point-blank |
| **Orbiter** | Circles at fixed radius, fires inward | Punishes lazy aim |
| **Exploder** | Detonates in a radius on death | Spacing |
| **Shielded** | Armoured front arc | Positioning / flanking |
| **Sniper** | Telegraphed laser from off-screen | Don't stand still |

**First boss — The Coil.** Spins projectile rings with safe gaps; teaches dash
timing. Appears at 3:00 in Classic.

- [ ] Weapon system + 3 weapons, selectable at run start
- [ ] 4 new enemy kinds
- [ ] Enemy behaviour refactor (strategy per kind, not one `if` ladder)
- [ ] The Coil boss + boss music sting + slow-mo on kill

**Done when:** two runs with different weapons play noticeably differently.

---

### M4 — Runs that differ

**Goal:** in-run choices, not just execution.

- [ ] **Power-up pickups** — shield, freeze, magnet, nuke, damage boost. Short,
      loud, frequent.
- [ ] **Relics** — one random passive per run (piercing shots, vampiric dash,
      slow aura, double debris). Roguelike spice without a meta tree.
- [ ] **Arena events** — 20-second modifiers announced on screen: *double speed*,
      *no dash*, *giant bullets*, *inverted gravity*.
- [ ] **Gravity wells** — map hazards that pull bullets and enemies; dash escapes.
- [ ] **Boss 2 — Swarm Queen** (spawns swarmers; DPS + movement check)
- [ ] **Boss 3 — The Black Hole** — a rival gravity well that steals your pull
      and your bullets. The thematic centrepiece; build it last and build it well.

**Done when:** a player can describe a run by what happened in it, not just how
long it lasted.

---

### M5 — Meta and retention

**Goal:** a reason to close the game *and come back*.

- [ ] **Stardust** — currency from time, kills and streaks
- [ ] **Unlocks** — the 12 player skins become *pilots* with names and bios,
      unlocked by challenges. The art already exists; this is nearly free content.
- [ ] **Light permanent upgrades** — soft-capped: move speed, dash cooldown,
      starting mass. Must never trivialise a run.
- [ ] **Achievements** — Survive 5:00, 100 kills, x25 streak, no-hit minute,
      beat each boss, max mass.
- [ ] **Local top-10 leaderboard** — per mode, with date and weapon used.
- [ ] **Stats screen** — lifetime kills, runs, favourite weapon, time played.

**Done when:** there is a visible reason to start run #20.

---

### M6 — Modes

**Goal:** different reasons to play, same core.

| Mode | Shape | Why |
|---|---|---|
| **Classic** ✅ | Endless survival | The default |
| **Hot Minute** | 60 seconds, max score | Perfect for "one more" |
| **Daily Seed** | Fixed seed, one attempt, shared leaderboard | Reason to return daily |
| **Boss Rush** | Three bosses, no trash | Showcases M3/M4 work |
| **Glass Cannon** | One-hit death, huge damage | Ranked, for the skilled |

- [ ] Mode select on the main menu
- [ ] Seeded RNG (`RandomNumberGenerator` with explicit seed, not `GD.Rand*`)
- [ ] Per-mode high scores in the save
- [ ] **Difficulty select** — Easy / Normal / Hard affecting spawn rate, enemy
      speed and contact radius (not player damage)

**Done when:** Daily Seed produces identical runs across two machines.

---

### M7 — Ship it

- [ ] **Options completion** — resolution, vsync, FPS cap, shake intensity, UI
      scale, damage numbers toggle, gamepad aim assist
- [ ] **Accessibility** — colourblind-safe enemy palette, high-contrast outline
      mode, hold-vs-toggle rapid fire, assist mode (slower enemies), shake off
- [ ] **Localisation-ready** — all UI strings through a translation table
- [ ] **Object pooling** for bullets, enemies and debris if frame time suffers
- [ ] **Steamworks** — overlay, achievements sync, cloud saves
- [ ] **Store assets** — trailer, 6–8 screenshots, capsule art, description
- [ ] **QA pass** — 1080p/1440p/ultrawide, gamepad-only run, keyboard-only run,
      fresh-install run with no save file
- [ ] **itch.io build** as a soft launch before Steam

**Done when:** someone who has never seen the game can install, play and quit
without confusion.

---

## 5. Standing rules

Things that apply to every milestone, not one of them.

1. **Everything tunable is `[Export]`.** No gameplay constant gets buried.
2. **Every new toggle ships with its options entry.** Screen shake without a
   slider is a bug.
3. **Readability beats spectacle.** If a new effect hides a threat, it's wrong.
4. **New save fields bump the version and migrate.** `ScoreManager` already
   models this; keep it.
5. **Runtime spawns go through `GameManager.Spawn()`** so they stay pausable.
6. **Test a fresh install** — no save file, no settings — before every release.
7. **CI must stay green.** A red import is a broken game, not a warning.

---

## 6. Explicitly out of scope

Say no now, save the argument later.

- Online multiplayer / co-op
- Open world, levels, or a campaign
- Deep RPG inventory or crafting
- Story, dialogue, cutscenes
- Mobile touch controls (revisit only if Android becomes a real target)
- Procedurally generated art

---

## 7. What to watch

If we ever want to know whether a change worked:

- **Median run length** — should sit at 2–6 min. Under 90s is punishing; over
  10 min is boring.
- **Restart rate** — % of deaths followed by an immediate restart. This is the
  "one more run" pillar, measured.
- **Weapon spread** — if one weapon takes >50% of picks, it's overtuned.
- **Deaths by enemy type** — reveals which enemies are unreadable rather than
  hard.
- **Mass at death** — tells us whether the risk dial is being used at all.

---

## 8. Immediate next three

1. Merge the `audit-fixes` PR and confirm the first CI run is green.
2. Kill the phantom `Q` input on the dev machine (vJoy) so playtesting is honest.
3. **M1**: tune the existing numbers, then hitstop and screen shake.
