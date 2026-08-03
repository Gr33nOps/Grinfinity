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
- ~~No object pooling — bullets, enemies and debris all instantiate per spawn.~~
  **Measured, and it is not a problem yet.** A 150-second orbit under combat
  load: 152,290 frames, **1.00 ms average, 10.38 ms worst, zero frames over the
  16.67 ms budget**, peaking at 77 live bodies and motes. A second run held at
  the 90-body spawn cap with no shooting saw one 21 ms outlier and nothing else.
  Roughly 17x headroom. Re-measure with `tools/dev_capture.tscn` before spending
  a day on pools — the numbers print at the end of every capture run.
- ~~Enemy behaviour is a single `_PhysicsProcess` branch~~ — split into
  `BodyBehaviour`, one stateless strategy per kind (M3).
- ~~No `RunState` owner~~ — built in M2; it owns time, kills, streak, mass,
  moons and score, and raises signals rather than touching labels.
- **The nine face sprites are still `enemy N.png` on disk.** The classes, scenes
  and groups are all `Body` now; the texture filenames were left alone because
  renaming them moves texture UIDs for no behavioural gain. Rename them when the
  distinct silhouettes in `ASSETS.md` replace them anyway.

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

- [x] **Live-tune every existing `[Export]`** — unlock times, pack sizes, tank
      health, speed multipliers. First committed pass: the ramp tops out around
      2:00, swarmers at 0:18, tanks at 0:40, dash shortened and made punchier.
- [x] **Hitstop** — `GameManager.Hitstop()` drops `Engine.TimeScale` for 45 ms on
      a light kill, 85 ms on a tank, 140 ms on death. Measured against wall-clock
      time, cancelled by pause and by the game-over transition.
- [x] **Screen shake** — trauma-based, on `GameCamera`. Ships with its
      **Settings → Screen Shake** slider (0 = OFF), persisted in `settings.cfg`.
- [x] **Hit feedback** — flash and sprite punch on every survivable hit, longer
      the closer to death; knockback scaled per kind; per-kind death bursts
      (a swarmer pops, a tank detonates).
- [x] **Muzzle flash, spark, bullet trail** — all in-engine: a randomised
      two-part flash at the barrel, a pale spark on chip hits, and a tapering
      `Line2D` trail behind every bullet.
- [x] **Kill-chain banner that pops** — scale punch and colour flash on every
      kill, much bigger at 5 / 10 / 25 / 50 / 100.
- [x] **Deep space look** — done procedurally in `scenes/space.gdshader`: a slow
      nebula wash plus three parallax star layers. No PNGs, resolution
      independent, and it removes four items from `ASSETS.md`.
- [x] **Audio layering** — light and heavy kills are pitched and levelled apart,
      every kill gets jitter so a streak doesn't fatigue, and streak milestones
      fire a rising sting. **Still on placeholders**: `kill_small.ogg`,
      `kill_big.ogg`, `hit_chip.ogg` and the three `streak_*.ogg` stings do not
      exist yet, so one sample is being re-pitched to stand in for all of them.
- [x] **Music intensity layer** — `MusicManager` reads danger from run time and
      arena crowding and swells the mix. It picks up `sounds/music_intense.ogg`
      automatically **once that stem exists**; until then it drives the base
      track's level only.

**Done when:** killing one body feels good with the sound off, and great with it on.

**Status:** mechanically done. The remaining gap is audio *content*, not audio
*systems* — see the two notes above.

---

### M2 — Gravity becomes the mechanic

- [x] **`RunState`** — one owner for mass, kills, streak, time, moons and score,
      raising signals rather than touching labels. `ScoreManager` is now
      persistent records only, and `UIManager` owns every HUD element.
- [x] **Pull replaces chase**, scaled by mass — bodies accelerate toward the
      world under a softened falloff, gain momentum, overshoot and clump. Each
      one is launched with a sideways nudge at birth, without which they fall
      dead straight and never orbit. A minimum closing speed is enforced only
      beyond 700 px, so nothing strands while near-range orbits survive.
- [x] **Debris motes** — shed per kind (a swarmer leaves one, a tank six), flung
      out, caught by the world's gravity, and reeled in on a ramping deadline so
      they orbit before they are absorbed.
- [x] **Mass affects** body scale, move speed, dash cooldown, score multiplier
      and spawn interval.
- [x] **Rings** — drawn in-engine on the world itself, far half behind the
      sprite and near half in front, with a brightness sweep so a solid band
      still reads as spinning. Detached from the world's transform, or they
      would swing with the aim.
- [x] **Moons** — gained at mass thresholds; they orbit, fire at the nearest
      body on their own cadence, and body-block one hit. Blocking drops mass
      below the threshold that granted the moon, so the save costs the risk that
      earned it and cannot immediately repeat.
- [x] **Venting** — dash vents a little, nova vents a lot. Nova is the mass
      cash-out: a radial wipe whose kills score but shed no debris, or it would
      refund itself. Bound to `R`, rebindable like everything else.
- [x] **Score rework** — accumulated as it is earned at the live multiplier,
      rather than computed at the end from average mass, so the multiplier on the
      HUD is literally true. Streak links pay extra up to a cap. Save is at v3
      and v2 files migrate.

**Done when:** a player can explain, unprompted, why they chose to stay light or
go heavy — and wants a moon.

**Status:** built and running. The numbers are a considered first pass, not a
playtested one — `MaxMass`, the ring and moon thresholds, `HeavyPullMultiplier`
and `Drag` are the four knobs most likely to want moving after real hands are on
it, and all four are `[Export]`.

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
| **Drifter** ✅ | Falls straight in | Baseline |
| **Shard** ✅ | Fast, arrives in packs | Crowd control |
| **Planetoid** ✅ | Slow, four hits | Target priority |
| **Fracture** ✅ | Dies into three Splinters | Don't kill it point-blank |
| **Satellite** ✅ | Holds a fixed orbit radius, fires inward | Punishes lazy aim |
| **Flare** ✅ | Detonates in a radius on death | Spacing |
| **Bulwark** ✅ | Armoured front arc | Flanking |
| **Pulsar** | Telegraphed beam from off-screen | Never stand still |

**First boss — The Coil.** Spinning projectile rings with safe gaps; teaches dash
timing. Appears around 3:00 in Endless Orbit.

- [x] **Weapon system + first three weapons**, chosen at orbit start. Weapons are
      data rather than subclasses, so the Mass Driver and Solar Flare are a table
      entry plus whatever field they genuinely need. Range is expressed as shot
      lifetime, which is what makes the Debris Cannon short-ranged without making
      it slow. Chosen between menu and game, never between death and restart.
- [x] **Five new body kinds** — Fracture, its Splinters, Satellite, Flare and
      Bulwark. Only Pulsar is left.
- [x] **Enemy behaviour refactor** — `BodyBehaviour`, one stateless strategy per
      kind covering stats, steering, damage rules and death rules. `EnemyKind`
      became `BodyKind` with the design names.
- [x] **The Coil** — rings of slow shots with one walking safe gap, faster as it
      is wounded, drifting so the gap is never twice in the same place. Trash
      spawning stands down for the fight. Slow-mo on the kill, out of the same
      machinery as hitstop. **The sting is a placeholder**: `music_boss.ogg` and
      `boss_defeat.ogg` do not exist yet.

**Done when:** two orbits with different weapons play noticeably differently.

**Status:** done. Still on placeholder audio, and the eight kinds still share
nine face sprites — `BodyMark` draws the distinguishing shapes (plate arcs, fins,
seams, a pulsing fuse) in-engine until the silhouettes in `ASSETS.md` exist.

---

### M4 — Orbits that feel unique

*In-orbit choices, not just execution.*

- [x] **Power-ups** — Shield, Freeze, Magnet, Nuke, Overcharge. Every timed one
      is a float of remaining seconds on `RunState`; the HUD counts them down
      rather than showing a static icon, since the question a pickup creates is
      "how long have I got".
- [x] **Relics** — Long Shot, Greedy Dash, Deep Well, Rich Seam. One rolled at
      `_Ready` and announced by name and effect. No "None" entry — every orbit
      gets one, or the roll becomes a thing to be unlucky about.
- [x] **Arena events** — Solar Wind, Thrusters Out, Heavy Weather, Inversion —
      your gravity pushes instead of pulling, the single most on-theme modifier
      available. `EventDirector` leaves the opening minute alone and stands
      down during boss fights.
- [x] **Gravity wells** — `GravityWell`, a rival source of gravity that pulls
      bodies, debris and the world's own shots (never hostile ones). Touching
      the core is lethal; the pull on the player is weak enough that a single
      dash always escapes it, because dash sets velocity outright rather than
      blending toward it.
- [x] **Comet flybys** — crosses the arena edge-to-edge on a straight arc and
      destroys whatever it touches, bodies included. Scores nothing for the
      player, or loitering near the crossing would out-earn fighting.
- [x] **Boss 2 — The Brood.** Chases slowly and spawns a Shard continuously for
      as long as it lives — no gap to read, no telegraph to time; the only way
      to stop the flood is keeping damage on it while staying alive among what
      it has already spawned.
- [x] **Boss 3 — The Black Hole.** A rival gravity well with health: bodies fall
      toward *it*, the world's own shots bend toward it, and the motes needed
      for mass get dragged in before they can be reached. Wounded means
      hungrier, not weaker — its pull strengthens as its health drops. Built
      last, on top of a shared `Boss` base the other two were refactored onto.

**Done when:** a player describes an orbit by what happened in it, not just how
long it lasted.

**Status:** done. `Boss` is a new abstract base (health, `TakeDamage`, the hit
flash, the two signals `GameManager` listens for) that `BossCoil`, `BossBrood`
and `BossBlackHole` all sit on; `GameManager.NextBossIndex` sequences the three
and is itself `[Export]`, so any single boss can be tuned in isolation without
first beating the ones before it. Still on placeholder audio throughout — see M1.

---

### M5 — Meta and retention

- [x] **Stardust** — currency from time, kills and streaks. `PlayerProfile`
      owns it (and lifetime stats, unlocks and upgrade levels) the way
      `ScoreManager` owns records: one static store, one save file. Awarded
      once, in `GameManager.TriggerGameOver`, alongside the existing
      `ScoreManager.SaveRun` call.
- [x] **Worlds** — the existing 12 skins are now named, unlockable worlds with a
      line of flavour each: Embertide (free), then Driftlight, Palefrost,
      Cinderbloom, Hollowmere, Duskwarden, Verdant Halo, Ashen Coil, Glasswake,
      Moltencrown, Voidkin, Starforged — gated by orbit count, lifetime kills,
      best score, best time, heaviest mass and lifetime playtime. Chosen on the
      weapon-select screen, which now has a one-at-a-time world carousel below
      the weapon cards rather than a second full screen. **Behaviour change**:
      the player sprite no longer randomly cycles through all 12 skins every
      few seconds — that placeholder made sense when skins were purely
      cosmetic, but undercuts a world as a chosen identity. It now shows
      whichever world was picked, for the whole orbit.
- [x] **Soft-capped permanent upgrades** — Thrust (move speed), Coolant (dash
      cooldown), Ballast (starting mass). Five levels each, quadratically
      pricier per level, capped at +15% / -17.5% / +30 flat mass — genuinely
      felt, never enough to trivialise an orbit. Bought on a new Upgrades
      screen off the main menu; applied once, at orbit start, folded into the
      base stat the mass factor already multiplies on top of.
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
- [ ] ~~**Object pooling** for bullets, bodies and debris if frame time suffers~~
      — measured in M3 and it does not. See "Known debt" above; re-measure
      rather than assuming.
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
2. ~~Kill the phantom `Q` input (vJoy) so playtesting is honest.~~ Done — rapid
   fire defaults to `E`, and a `settings.cfg` version bump drops the stale `Q`
   bind instead of restoring it. The HUD ability icons bake their key name into
   the art, so they are now cropped and the key is drawn live from the input map
   — which also fixes rebinding silently lying about itself.
3. ~~**M1**: tune the existing numbers → hitstop → screen shake → parallax space.~~
   Done — and **M2, M3 and M4 with it**. Every milestone through "orbits that
   feel unique" is built. Next is **M5: meta and retention** — stardust, named
   worlds, soft-capped upgrades, achievements, a local leaderboard.

### The two things code cannot finish

1. **Audio.** Every consumer is wired and will pick its file up on drop-in:
   `kill_small`, `kill_big`, `hit_chip`, the three `streak_*` stings,
   `music_intense` (same tempo and key as `background_music.ogg`), `music_boss`
   and `boss_defeat`. This is the single biggest perceived-quality jump left.
2. **A playtest.** M1's "feels good with the sound off", M2's "can explain why
   they went heavy", and whether The Coil's gap is fair are all things only hands
   on the game can answer. `MaxMass`, the ring and moon thresholds,
   `HeavyPullMultiplier`, `Drag` and `BossCoil.RingInterval` are the knobs most
   likely to want moving, and all of them are `[Export]`.

### Recording the M1 audio

The one thing M1 cannot finish in code. In priority order:
`kill_small.ogg`, `kill_big.ogg`, then `music_intense.ogg` (same tempo and key as
`background_music.ogg`), then `hit_chip.ogg` and the three streak stings. Every
system that consumes them is already wired and will pick them up on drop-in.
