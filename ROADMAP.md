# Grinfinity — Design & Development Roadmap

Gravity-first. The plan we build to, start to finish.
Supersedes `LAUNCH_TODO.md` (folded in here; the original is in git history).

Written against the real codebase: Godot 4.4 / C#, `GameManager` +
`EnemySpawner` + `PlayerAbilities` + `ScoreManager` + `GameSettings`.

---

## Core fantasy (non-negotiable)

> **You are a small, cheerful planet with a gun. Your gravity is why they come.**

Everything orbits, falls, slingshots or clumps around you. That single idea
settles every design argument from here on.

### Pillars

| Pillar | Meaning | Rules out |
|---|---|---|
| **Gravity is the fantasy** | Everything is pulled by your mass | Enemies that ignore physics, static arenas |
| **Cheerful violence** | Bright, round, grinning carnage | Gore, horror framing, muddy palettes |
| **One more orbit** | 2–6 min runs, under 2 s restart | Long levels, cutscenes, save-scumming |
| **Readable chaos** | 100 things on screen, still legible | Visual noise that hides threats |

### Session shape

Target orbit: **2–6 minutes**. Restart in **under 2 seconds**. A session is
20–40 minutes of repeated orbits.

### Naming

The fiction stays consistent everywhere — code, UI and docs. You are a **world**.
Enemies are **bodies**. A run is an **orbit**. Currency is **stardust**.
Never introduce a name that could belong to any other genre.

---

## 1. Current state

**Working**
- Core loop: move, aim, shoot, dash, rapid fire, survive, die, restart
- Pause / game over / settings / credits / key rebinding / gamepad
- Kill count + streak + death recap + new-record callout
- Three enemy kinds unlocking mid-orbit
- Ramping speed and spawn rate, versioned save, audio buses
- Windows + Linux exports, CI that fails on import errors

**Known debt**
- No object pooling — bullets, enemies and debris all instantiate per spawn
- Enemy behaviour is a single `_PhysicsProcess` branch; needs a per-kind split
  once there are six or more
- No `RunState` owner — mass, score and modifiers will otherwise smear across
  `GameManager` and `ScoreManager`

---

## 2. The gravity spine

The game's identity. Everything after M1 hangs off this.

### Mass

You start light. Every kill sheds debris, your gravity draws it in, you absorb
it. **Mass is the risk dial and the score multiplier — the same number.**

| Heavy | Light |
|---|---|
| Stronger, wider pull | Weaker pull, calmer screen |
| Higher score multiplier | Lower multiplier |
| Larger hitbox | Small, nimble hitbox |
| Slower movement, longer dash cooldown | Fast and twitchy |
| **Gains moons** (see below) | No moons |

### Venting

Abilities spend Mass rather than sitting on pure cooldowns. Dash vents a little;
nova vents a lot. The loop becomes:

> pull → kill → absorb → grow heavier and more dangerous → **choose** when to
> cash mass out as power → repeat

### Pull, not chase

Replace the beeline in `Enemy._PhysicsProcess` with acceleration toward the
player scaled by mass and distance. Bodies gain momentum, overshoot and swing
back. Keep a hard minimum approach speed so nothing stalls out of reach.

This is the **biggest identity gap today**. Until bodies orbit, overshoot and
clump, this is still a re-skinned arena shooter.

### Moons — the payoff for going heavy

Without a reward, heavy mass is pure downside plus points. So: at mass
thresholds you gain a **moon** that orbits you, body-blocks one hit, and fires
on its own cadence. Lose mass and the moon breaks away.

Moons make the risk dial *desirable* rather than merely scoring, and they are
the most legible possible statement of "I am a planet."

### Rings — diegetic mass display

Rather than a bar in a corner, heavy worlds grow **visible rings**. The player
reads their own risk from their own silhouette. A HUD ring around the player is
the fallback if rings prove unreadable in motion.

### Emergent toys this unlocks

- **Clumping** — heavy mass bunches bodies, rewarding one well-placed shot
- **Slingshot dash** — dash past a cluster and drag it off course
- **Gravity wells** — arena hazards that fight your pull
- **Rival wells** — the Black Hole boss

---

## 3. Milestones

Ordered so each leaves a better game than the last. Do not skip ahead — M1
exists because tuning an unfelt game wastes everything after it.

---

### M1 — Make the current game feel good

*Tune and juice what exists. No new systems.*

- [ ] **Live-tune every existing `[Export]`** — unlock times, pack sizes, tank
      health, speed multipliers. These are all guesses today. Commit the values.
- [ ] **Hitstop** — 40–60 ms freeze on kill. Cheapest feel upgrade available.
- [ ] **Screen shake** — short and small on kill, larger on death. **Ships with
      its intensity slider (0 = off) on day one.**
- [ ] **Hit feedback** — flash on all bodies (tanks already have it), knockback
      on non-fatal hits, punchier death bursts
- [ ] **Muzzle flash, spark, bullet trail**
- [ ] **Kill-chain banner that pops** — scale and punch, not just changing text
- [ ] **Deep space look** — parallax starfield layers plus a slow nebula wash
      behind them. The arena currently reads as a black box; three cheap layers
      make it read as *space*. Highest look-per-effort item on the list.
- [ ] **Audio layering** — distinct light/heavy kill sounds, streak milestone
      stings, pitch variation so repetition doesn't fatigue
- [ ] **Music intensity layer** — a second stem that fades in above a danger
      threshold

**Done when:** killing one body feels good with the sound off, and great with it on.

---

### M2 — Gravity becomes the mechanic

- [ ] **`RunState`** — one owner for mass, kills, streak, time and modifiers.
      Extract from `GameManager` / `ScoreManager`.
- [ ] **Pull replaces chase**, scaled by mass
- [ ] **Debris motes** — shed on death, pulled in by *your* gravity. They should
      **orbit you briefly before being absorbed** — the mechanic demonstrating
      itself, and free satisfaction.
- [ ] **Mass affects** hitbox scale, move speed, dash cooldown, score multiplier,
      spawn rate
- [ ] **Rings** — visible mass on the world itself
- [ ] **Moons** — gained at mass thresholds; orbit, block one hit, fire
      independently; break away when mass drops
- [ ] **Venting** — dash and nova consume mass
- [ ] **Score rework** — `f(time, kills, streak, average mass)`; bump save to v3

**Done when:** a player can explain, unprompted, why they chose to stay light or
go heavy — and wants a moon.

---

### M3 — Arsenal and bestiary

*How you fight starts to differ.*

**Weapons** — one chosen at orbit start. Three first, five later.

| Weapon | Fantasy | Tradeoff |
|---|---|---|
| **Comet** | Baseline shot with a trail | Reliable, unremarkable |
| **Debris Cannon** | Shotgun spray of rock | Deletes swarms, useless at range |
| **Ion Lance** | Piercing beam through a whole clump | Slow, needs lining up |
| **Mass Driver** | One huge slug, enormous knockback | Punishing to miss |
| **Solar Flare** | Hold to charge, overheats and locks | Highest skill ceiling |

**Bodies** — new kinds teach gravity literacy, not just raise numbers. Each needs
a **distinct silhouette**, since they are all tinted variations of one sprite today.

| Body | Behaviour | Teaches |
|---|---|---|
| **Drifter** ✅ | Straight in | Baseline |
| **Shard** ✅ | Fast, arrives in packs | Crowd control |
| **Planetoid** ✅ | Slow, four hits | Target priority |
| **Fracture** | Dies into three smaller bodies | Don't kill it point-blank |
| **Satellite** | Holds a fixed orbit radius, fires inward | Punishes lazy aim |
| **Flare** | Detonates in a radius on death | Spacing |
| **Bulwark** | Armoured front arc | Flanking |
| **Pulsar** | Telegraphed beam from off-screen | Never stand still |

**First boss — The Coil.** Spinning projectile rings with safe gaps; teaches dash
timing. Appears around 3:00 in Endless Orbit.

- [ ] Weapon system + first three weapons, chosen at orbit start
- [ ] Four new body kinds
- [ ] **Enemy behaviour refactor** — strategy per kind, not one `if` ladder
- [ ] The Coil + boss music sting + slow-mo on kill

**Done when:** two orbits with different weapons play noticeably differently.

---

### M4 — Orbits that feel unique

*In-orbit choices, not just execution.*

- [ ] **Power-ups** — short, loud, frequent: shield, freeze, magnet, nuke,
      damage boost
- [ ] **Relics** — one random passive per orbit (piercing, vampiric dash, slow
      aura, double debris). Roguelike spice without a meta tree.
- [ ] **Arena events** — 20-second announced modifiers: *solar wind* (constant
      drift), *no dash*, *giant slugs*, *inverted gravity* (you repel instead of
      pull — the single most on-theme modifier available)
- [ ] **Gravity wells** — hazards that pull bullets and bodies; dash escapes
- [ ] **Comet flybys** — a fast body crosses the arena on a fixed arc, hurting
      anything in its path including enemies. Free spectacle, pure space.
- [ ] **Boss 2 — The Brood.** Spawns shards continuously; a DPS and movement check.
- [ ] **Boss 3 — The Black Hole.** A rival gravity well that steals your pull
      *and* your bullets. The thematic centrepiece — build it last, build it well.

**Done when:** a player describes an orbit by what happened in it, not just how
long it lasted.

---

### M5 — Meta and retention

- [ ] **Stardust** — currency from time, kills and streaks
- [ ] **Worlds** — the existing 12 skins become named, unlockable worlds with a
      line of flavour each, earned by challenges. The art already exists; this is
      the cheapest content in the whole plan.
- [ ] **Soft-capped permanent upgrades** — move speed, dash cooldown, starting
      mass. Must never trivialise an orbit.
- [ ] **Achievements** — survive 5:00, 100 kills, x25 streak, no-hit minute, beat
      each boss, reach max mass, finish an orbit at minimum mass
- [ ] **Local top-10 leaderboard** — per mode, with weapon, world and date
- [ ] **Lifetime stats** — orbits, kills, favourite weapon, time played,
      heaviest mass reached

**Done when:** there is a visible reason to start orbit #20.

---

### M6 — Modes

| Mode | Shape | Why |
|---|---|---|
| **Endless Orbit** ✅ | Endless survival | The default |
| **Flyby** | 60 seconds, max score | Perfect for "one more" |
| **Daily Alignment** | Fixed seed, one attempt | Reason to return daily |
| **Convergence** | Three bosses, no trash | Showcases M3/M4 work |
| **Glass Planet** | One-hit death, huge damage | Ranked, for the skilled |

- [ ] Mode select on the main menu
- [ ] **Seeded RNG** — `RandomNumberGenerator` with an explicit seed, not `GD.Rand*`
- [ ] Per-mode high scores in the save
- [ ] **Difficulty select** — Easy / Normal / Hard, affecting spawn rate, body
      speed and contact radius. Never player damage.

**Done when:** Daily Alignment produces identical orbits across two machines.

---

### M7 — Ship it

- [ ] **Options** — resolution, vsync, FPS cap, shake intensity, UI scale,
      damage numbers toggle, gamepad aim assist
- [ ] **Accessibility** — colourblind-safe body palette, high-contrast outlines,
      hold-vs-toggle rapid fire, assist mode (slower bodies), shake off
- [ ] **Localisation-ready** — all UI strings through a translation table
- [ ] **Object pooling** for bullets, bodies and debris if frame time suffers
- [ ] **Steamworks** — overlay, achievements, cloud saves
- [ ] **Store assets** — trailer, 6–8 screenshots, capsules, description
- [ ] **QA matrix** — 1080p / 1440p / ultrawide, gamepad-only, keyboard-only,
      fresh install with no save
- [ ] **itch.io soft launch** before Steam

**Done when:** someone who has never seen the game can install, play and quit
without confusion.

---

## 4. Standing rules

1. **Everything tunable is `[Export]`.** No gameplay constant gets buried.
2. **Every new toggle ships with its options entry.** Shake without a slider is a bug.
3. **Readability beats spectacle.** If an effect hides a threat, it's wrong.
4. **New save fields bump the version and migrate.** `ScoreManager` models this.
5. **Runtime spawns go through `GameManager.Spawn()`** so they stay pausable.
6. **Test a fresh install** — no save, no settings — before every release.
7. **CI stays green.** A red import is a broken game, not a warning.
8. **Nothing gets a name that could belong to another genre.**

---

## 5. Explicitly out of scope

Online multiplayer or co-op · open world, levels or campaign · deep RPG inventory
or crafting · story, dialogue, cutscenes · mobile touch controls · procedurally
generated art.

Firm. Revisit only if a shipped game earns the right.

---

## 6. What to watch

- **Median orbit length** — target 2–6 min. Under 90 s is punishing; over 10 is boring.
- **Restart rate** — deaths followed by an immediate restart. The "one more orbit"
  pillar, measured.
- **Weapon spread** — one weapon over 50% of picks means it's overtuned.
- **Deaths by body type** — reveals which bodies are *unreadable* rather than hard.
- **Mass at death** — tells us whether the risk dial is being used at all.
- **Moons held at death** — tells us whether going heavy is actually worth it.

---

## 7. Immediate next three

1. Merge the `audit-fixes` PR and confirm the first CI run is green.
2. Kill the phantom `Q` input (vJoy) so playtesting is honest.
3. **M1**: tune the existing numbers → hitstop → screen shake → parallax space.
