# Grinfinity — Asset List

Everything to draw, model or record, ordered by the milestones in
[ROADMAP.md](ROADMAP.md). Specs are derived from how the existing art is
actually used in-engine, not guessed.

---

## 1. Style guide

Lock these down so assets made months apart still match.

### Look

- **Flat vector.** Solid fills, thick dark outline, no gradients or bevels.
- **Round silhouettes.** Everything is a planet, a mote or a ring. If it has
  corners, it should be UI.
- **Faces carry the tone.** The player grins. Enemies scowl. That contrast *is*
  the game's personality — keep it on every new character.
- **No glow baked into art.** Post-processing is off project-wide. If something
  needs to look bright, draw it bright.

### Palette

| Role | Hex | Notes |
|---|---|---|
| Background | `#100315` | Project clear colour, near-black purple |
| Starfield dots | `#3A1F4D` – `#6B3A8A` | Low contrast, never competes |
| Player warm | `#F5C842` → `#E89B3C` | Yellow body, orange shading |
| Enemy hostile | `#E85A72` → `#B03A52` | Pink body, deep red shading |
| Outline | `#1A0A22` | Near-black, ~6–8 px at native size |
| UI text | `#FFFFFF` | |
| UI accent / focus | `#FFB859` | Hover, focus, streak text |
| Danger telegraph | `#FF4D4D` | Reserved — only for "about to hurt you" |

Enemy tints are applied **in code** (`Enemy.Configure`) — swarmers get a green
cast, tanks a blue one. Draw enemies in the neutral pink and let code tint them,
so one sprite serves every kind.

### Font

`Bubblegum.ttf` for everything. Headers are **arc text baked into a PNG**
(existing pattern: `title.png`, `GAME OVER.png`, `paused.png`).

---

## 2. Export rules

These come from fixing the existing art — please follow them for anything new.

1. **Crop to content.** The original sprites were 800×600 canvases holding ~230 px
   of art, costing 8× the memory for nothing.
2. **Keep the crop box identical across a family.** All 12 player skins share one
   box; all 9 enemies share another. Runtime texture swapping shifts the art if
   they differ.
3. **Centre the crop box on the canvas centre** if the sprite is ever flipped or
   swapped at runtime. Off-centre crops make flips jump.
4. **Export at ~2× on-screen size.** No larger.
5. **PNG-24 with alpha.** No interlacing.
6. **Masters go in `art_source/`** (excluded from import), game-ready in `sprites/`.
7. **Name new files `snake_case.png`.** Existing files use spaces; leave them,
   but don't add more.

### On-screen sizes (1920×1080 base)

| Asset | Texture now | On screen | Target for new art |
|---|---|---|---|
| Player skin | 320×234 | ~175×128 | **320×240** |
| Enemy | 238×210 | ~112×98 (tank ~180×157) | **256×224** |
| HUD ability icon | 226×364 | ~68×109 | **160×256** |
| Crosshair | 49×50 | 49×50 | **96×96** |
| Big button | 642×306 | 420×200 | **640×306** |
| Arc header | 964×574 | ~680×400 | **960×580** |

---

## 3. Structural decision to make first

**The gun is currently baked into each player skin.**

All 12 `player N.png` files contain the body *and* the bazooka. That's fine with
one weapon. M3 adds five — which at 12 skins × 5 weapons is **60 sprites**.

**Recommendation: split the gun onto its own sprite layer.** A `Weapon` Sprite2D
child of the player, rotating with aim, drawn above the body. Then M3 costs
**5 gun sprites**, not 60, and skins and weapons stay independent forever.

This means redrawing the 12 skins **without** the gun — a one-off cost of 12
edits that saves 48 sprites and every future weapon. Do it before M3 starts.

- [ ] Redraw 12 player skins, body only, no gun
- [ ] Draw the existing bazooka as a standalone sprite (`weapon_pea.png`)
- [ ] Add a `Weapon` Sprite2D to `player.tscn`, pivot at the grip

---

## 4. Assets by milestone

### M1 — Feel

**Art (6)** — all six now have a working in-engine stand-in, so these are
upgrades rather than blockers.
- [ ] `muzzle_flash.png` — 96×96, 1 frame, bright yellow-white starburst
      *(currently two `Polygon2D` shapes under `shootyPart/MuzzleFlash`)*
- [ ] `bullet_pea.png` — 32×32, replaces the current `Polygon2D`
- [ ] `bullet_trail.png` — 64×16, soft tapered streak
      *(currently a `Line2D` with a width curve and gradient — arguably better,
      since it follows the real flight path; only swap if art beats it)*
- [ ] `hit_spark.png` — 48×48, small radial burst for non-fatal hits
      *(currently a small pale `CPUParticles2D` burst)*
- [ ] `mote.png` — 16×16, single round particle for death bursts
- [ ] `mote_square.png` — 12×12, chunky alt so bursts mix shapes

**Deep space look — cut, done in engine.** ~~4 sprites~~ **0**.
`scenes/space.gdshader` draws the nebula wash and all three parallax star layers
procedurally. It costs no VRAM, holds up at any resolution (which matters,
because the stretch aspect is `expand`), and never tiles. Tune it through the
shader's uniforms — `nebula_strength`, `star_cell`, `star_scroll`,
`star_radius`, `star_brightness` — rather than by redrawing anything.

~~`stars_far.png` / `stars_mid.png` / `stars_near.png` / `nebula_wash.png`~~

**Audio (9)**
- [ ] `kill_small.ogg` — light pop, for chasers/swarmers
- [ ] `kill_big.ogg` — heavier thud, for tanks
- [ ] `hit_chip.ogg` — non-fatal hit on armoured enemies
- [ ] `streak_5.ogg` / `streak_10.ogg` / `streak_25.ogg` — rising milestone stings
- [ ] `dash_whoosh.ogg`
- [ ] `rapidfire_start.ogg`
- [ ] `music_intense.ogg` — a second layer that fades in over the existing track.
      **Must be the same tempo and key** as `background_music.ogg` so it can
      crossfade cleanly.

---

### M2 — Gravity spine

**Art (4)** — debris ships as a `Polygon2D` chunk, so these are upgrades.
- [ ] `debris_a.png` / `debris_b.png` / `debris_c.png` — 24×24, three chunk shapes
      *(one shared polygon today, tinted by whatever body shed it)*
- [ ] `absorb_flash.png` — 64×64, brief ring on absorbing debris
- [ ] `pull_ring.png` — 512×512, faint hollow circle marking your pull radius,
      scaled by mass in code
- ~~`mass_ring_segment.png`~~ **Cut** — `draw_arc()` worked, as suspected.

**Rings and moons** — built, and mostly without art.
- ~~`world_ring_1/2/3.png`~~ **Cut.** `WorldRings` draws all three tiers in
  `_Draw`, split into a far half behind the sprite and a near half in front so
  they read as rings around a body rather than circles painted on one. Tune via
  `InnerRadius`, `RadiusStep`, `Flatten`, `Tilt` and `SweepSpeed`.
- Moons are placeholder `Polygon2D` discs in `scenes/moon.tscn`. These are the
  ones actually worth drawing:
- [ ] `moon_small.png` — 64×64, orbits the player at low mass tiers
- [ ] `moon_large.png` — 96×96, higher tier
- [ ] `moon_break.png` — 96×96, cracked variant for the frame it detaches
- [ ] `moon_shot.png` — 24×24, the moon's own projectile
      *(moons currently fire the standard bullet)*

**Audio (4)**
- [ ] `debris_absorb.ogg` — very short tick, pitched up by streak in code
- [ ] `mass_vent.ogg` — release/whoosh when spending mass
- [ ] `nova.ogg` — big discharge
- [ ] `mass_hum_loop.ogg` — low drone whose volume rises with mass. Cheap, and it
      makes the risk dial *audible*.

---

### M3 — Arsenal and bestiary

**Art (28)**

Weapons — assuming the split above:
- [ ] 5 × gun sprites, 200×80: `weapon_comet`, `weapon_debris_cannon`,
      `weapon_ion_lance`, `weapon_mass_driver`, `weapon_solar_flare`
- [ ] 5 × weapon select icons, 128×128, flat silhouette on a disc
- [ ] 5 × projectile sprites, 32×32 (Ion Lance is 96×24, Mass Driver is 64×64)
- [ ] `flare_gauge.png` — 256×32, fills as Solar Flare charges

Bodies — new kinds need **distinct silhouettes**, not just recolours:
- [ ] `body_fracture.png` — visibly segmented, reads as "will break apart"
- [ ] `body_satellite.png` — directional fin so its facing is legible
- [ ] `body_flare.png` — bloated, lit crack pattern
- [ ] `body_bulwark.png` — plated front arc, obviously flankable
- [ ] `body_pulsar.png` — single big eye, long thin body
- [ ] `body_fracture_mini.png` — 128×112, the split children

Boss 1 — The Coil:
- [ ] `boss_coil_core.png` — 512×512
- [ ] `boss_coil_ring.png` — 768×768, the rotating projectile ring
- [ ] `boss_coil_hurt.png` — damaged/cracked variant
- [ ] `telegraph_line.png` — 512×32, Pulsar beam warning
- [ ] `telegraph_ring.png` — 512×512, incoming-ring warning
- [ ] `boss_healthbar_frame.png` — 1200×64

**Audio (11)**
- [ ] 5 × weapon fire sounds — each must be identifiable blind
- [ ] `fracture_split.ogg`, `flare_boom.ogg`, `bulwark_deflect.ogg`
- [ ] `pulsar_charge.ogg` — telegraph
- [ ] `music_boss.ogg` — full track
- [ ] `boss_defeat.ogg` — payoff sting

---

### M4 — Runs that differ

**Art (26)**
- [ ] 5 × power-up world pickups, 96×96: shield, freeze, magnet, nuke, damage
- [ ] 5 × matching HUD icons, 64×64
- [ ] `shield_bubble.png` — 256×256, semi-transparent sphere over the player
- [ ] `freeze_overlay.png` — 256×256, frost applied to frozen enemies
- [ ] 10 × relic icons, 128×128
- [ ] `event_banner.png` — 1200×140, frame for arena event announcements
- [ ] `gravity_well.png` — 384×384 (pair with a swirl shader)
- [ ] `comet_flyby.png` — 256×96, bright head with a long tail, for the comet
      that crosses the arena and hurts everything in its path
- [ ] Boss 2 The Brood: `boss_brood.png` 640×640 + `boss_brood_hurt.png`
- [ ] Boss 3 The Black Hole: `boss_blackhole.png` 512×512 + accretion ring
      768×768. Mostly shader work — keep the sprite simple.

**Audio (10)**
- [ ] 5 × power-up pickup sounds, each distinct
- [ ] `event_announce.ogg`
- [ ] `nuke_blast.ogg`
- [ ] `freeze_hit.ogg`
- [ ] `comet_pass.ogg` — doppler whoosh
- [ ] `music_boss_brood.ogg`, `music_boss_blackhole.ogg`

---

### M5 — Meta and retention

**Art (~40, mostly small)**
- [ ] `world_card_frame.png` — 400×560. The 12 skins become unlockable **worlds**;
      you only need the **frame**, reusing existing skin art inside it.
- [ ] `world_card_locked.png` — same size, silhouette state
- [ ] `stardust_icon.png` 64×64 + `stardust_mote.png` 16×16
- [ ] 20 × achievement icons, 128×128
- [ ] `achievement_toast.png` — 640×140 popup frame
- [ ] `leaderboard_row.png` — 900×80
- [ ] 6 × stats screen icons, 64×64
- [ ] `upgrade_node.png` 128×128 + locked/purchased variants

**Audio (4)**
- [ ] `unlock_fanfare.ogg`
- [ ] `stardust_pickup.ogg`
- [ ] `achievement_pop.ogg`
- [ ] `purchase.ogg`

---

### M6 — Modes

**Art (11)**
- [ ] 5 × mode select cards, 480×640: Endless Orbit, Flyby, Daily Alignment,
      Convergence, Glass Planet
- [ ] 3 × difficulty icons, 96×96
- [ ] `daily_badge.png` — 128×128, with a completed-today state
- [ ] `mode_locked.png`

**Audio (1)**
- [ ] `mode_confirm.ogg`

---

### M7 — Ship

**Art**
- [x] **`icon.png` is square.** Was 312×233; now 512×512, padded around the art
      and scaled once so the aspect ratio is exact. **It is an upscale of the
      existing raster**, so it is slightly soft — invisible at the 32–256 px an
      app icon is actually seen at, but re-export from the vector master at
      512×512 when convenient and this becomes crisp for free.
- [ ] Steam capsules: 616×353 (small), 460×215 (header), 1920×620 (hero),
      374×448 (vertical), 3840×1240 (page background)
- [ ] itch.io cover: 630×500
- [ ] 6–8 screenshots at 1920×1080 — action shots, not menus
- [ ] Trailer, 30–60 s: hook in 3 seconds, gameplay only, ends on the title
- [ ] Colourblind-safe enemy palette variant (deuteranopia-safe: shift enemy
      pink toward blue so it doesn't collide with the player's yellow)
- [ ] High-contrast outline variant for accessibility mode

**Audio**
- [ ] `menu_music.ogg` — a calmer loop so the menu isn't running combat music
- [ ] Trailer audio mix

---

## 5. What *not* to make

Save yourself the work — these are better done in engine:

- **Screen shake, hitstop, slow-mo** — code, not art
- **Glow / bloom** — removed project-wide; don't bake fake glow into sprites
- **Explosion sprite sheets** — `CPUParticles2D` already handles bursts; you only
  need the small mote textures listed in M1
- **Starfield motion** — the existing scrolling shader handles the movement; you
  only need to supply the three parallax layers, not animate them
- **Button states** — you already have normal/hover/pressed for all five button
  sets; new buttons should be **flat text in Bubblegum** (that's what Settings,
  Credits and Controls already use) rather than more painted buttons
- **Mass meter** — try `draw_arc()` before drawing a texture

---

## 6. Totals

| Milestone | Art | Audio | Notes |
|---|---|---|---|
| Structural | 13 | — | Degunned skins + first weapon sprite |
| M1 | 10 | 9 | Particles, stings, and the parallax space layers |
| M2 | 13 | 4 | Rings and moons; one item may be skippable (in-engine arc) |
| M3 | 28 | 11 | Biggest art milestone — a boss's worth of detail |
| M4 | 27 | 11 | Bosses are shader-heavy, sprites stay simple |
| M5 | ~40 | 4 | Mostly 128×128 icons; fast to batch |
| M6 | 11 | 1 | Mode cards can reuse gameplay screenshots |
| M7 | Store set | 2 | Marketing, not gameplay |

**Rough total: ~142 art assets, ~42 audio.** Front-loaded work stays small — M1
and M2 together are 23 sprites and 13 sounds, and 4 of those sprites are the
parallax layers that transform how the whole game looks.

---

## 7. Order to work in

1. ~~**The parallax space layers** (4 sprites).~~ Done in a shader instead — see
   M1 above. The game no longer looks like a black box.
2. **M1 audio** (9 sounds) — now the single biggest perceived-quality jump
   available, and the only M1 item still outstanding. Every consumer is wired.
3. **Degun the 12 skins + draw one gun.** Unblocks all of M3 and costs a day.
4. **M1 particles** (6 sprites) — hitstop and shake are in and running on
   in-engine stand-ins, so these are polish now, not blockers.
5. ~~**Square `icon.png`**~~ — done, though it wants a crisp re-export.
6. Then follow the milestones in order.

Batch by type, not by milestone, once you're past M2: drawing 20 icons in one
sitting is far faster than 20 icons spread across four weeks.
