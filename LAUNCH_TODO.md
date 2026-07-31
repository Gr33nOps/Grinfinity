# Grinfinity — Launch TODO & Game Ideas

Verified against the current project (Godot 4.4 / C#).  
**Only unfinished work is listed.** Already-done systems (pause, game over, best-time save, dash, rapid-fire, scene transitions, HUD, random skins, spawn ramp, music/SFX basics) are omitted.

---

## A. Ship blockers (Steam / itch)

Do these before public launch.

- [ ] **Settings menu** — master / music / SFX volume, fullscreen↔windowed, resolution, mouse sensitivity
- [ ] **Key rebinding** — remappable move, shoot, dash, rapid-fire, pause
- [ ] **Audio buses** — separate Music / SFX buses wired to volume sliders (music + SFX already exist; channels do not)
- [ ] **Quit confirmation** — dialog before exit from main menu
- [ ] **Credits screen** — you, tools (Godot), fonts/assets licenses
- [ ] **Main menu buttons** — add Settings + Credits (Play / Quit already exist)
- [ ] **Tutorial / first-run tips** — short overlay: WASD, mouse shoot, Shift dash, Q rapid-fire
- [ ] **Controller / gamepad support** — move stick, aim stick (or aim-assist), shoot/dash/RF buttons; Steam Deck friendly
- [ ] **Export presets** — Windows build at minimum; Linux optional for Steam Deck
- [ ] **Store packaging** — trailer/GIF, 4–8 screenshots, itch zip / Steam depot smoke test

---

## B. Gameplay depth (make runs feel different)

- [ ] **Kill scoring + combos** — score from kills/streaks, not only survival time
- [ ] **Death recap** — time survived, kills, best combo, new best yes/no
- [ ] **Enemy AI types** (not just textures)
  - Chaser (current)
  - Shooter (keeps distance, fires)
  - Tank (slow, high contact danger)
  - Swarmer (small, fast, packs)
  - Exploder (dies in a burst radius)
- [ ] **Boss every N minutes** — one arena fight that changes pacing
- [ ] **Pickup power-ups** — shield, freeze, magnet, nuke, temporary damage boost
- [ ] **Weapon variety / loadouts** — 3–5 guns with tradeoffs (pierce, spread, heavy slow shot)
- [ ] **Difficulty select** — Easy / Normal / Hard (spawn rate, enemy speed, contact radius)
- [ ] **Second mode** — e.g. Daily Seed or 3-minute score attack (Classic survival can stay default)

---

## C. Meta & retention

- [ ] **Run currency** — earn from time/kills; spend between runs
- [ ] **Unlocks** — skins, weapons, starting perks
- [ ] **Light permanent upgrades** — soft-capped (move speed, dash CD, etc.)
- [ ] **Achievements** — Survive 5:00, 100 kills, no-hit minute, beat a boss, etc.
- [ ] **Local top-10 leaderboard** — menu stubs exist (`TopScoresList`) but are unwired
- [ ] **Online leaderboard** — optional later (Steam / custom); not required for v1

---

## D. Feel & polish

- [ ] **Screen shake / hit punch** — short camera shake on kill / player hurt (toggle in settings)
- [ ] **Stronger hit feedback** — flash, knockback, better death particles
- [ ] **Music intensity layers** — swell as danger rises (optional but strong)
- [ ] **Accessibility** — shake toggle, UI scale, colorblind-safe damage colors
- [ ] **Steamworks** (Steam only) — overlay, achievements sync, cloud save optional

---

## Suggested build order

1. Settings + audio buses + quit confirm + credits  
2. Kill score + death recap + tutorial tips  
3. 2–3 new enemy types + pickups  
4. One boss + difficulty select  
5. 3 weapons + unlock currency  
6. Gamepad + export + store page assets  

---

## Game ideas that fit Grinfinity

Ideas that match the neon twin-stick, short-run, high-score fantasy — not a different genre.

### Core twists
1. **Gravity wells** — rare map hazards that pull bullets/enemies; dash can escape
2. **Mirror clones** — every 60s spawn a ghost of your last path that enemies can “see”
3. **Overheat gun** — hold shoot to charge; overheat forces a short reload (skill ceiling)
4. **Faction colors** — purple vs orange enemies; killing the “marked” color gives bonus score
5. **Arena events** — timed modifiers: “double speed”, “no dash”, “giant bullets” for 20s

### Modes (lightweight)
6. **Classic** — current endless survival  
7. **Hot Minute** — 60s max score attack  
8. **Boss Rush** — three bosses, no trash mobs between  
9. **Glass Cannon** — huge damage, one-hit death, ranked leaderboard  

### Unlocks / fantasy
10. **Skin lore cards** — each of the 12 player skins is a “pilot”; unlock bios via challenges  
11. **Contract board** — optional objectives mid-run (“kill 20 swarmers”, “dash 5 times”) for bonus currency  
12. **Relic drops** — one random relic per run (piercing, vamp-dash, slow aura) — roguelike spice without a full meta tree  

### Enemies / bosses that fit
13. **Splitter** — dies into 3 mini chasers  
14. **Sniper** — telegraph laser, punish standing still  
15. **The Coil (boss)** — spins projectile rings; safe gaps teach dash timing  
16. **The Swarm Queen** — spawns swarmers; DPS check + movement check  

### Juice that sells the trailer
17. **Kill chain banner** — “x10 STREAK” in Bubblegum font  
18. **Slow-mo on boss kill** — 0.5s freeze-frame  
19. **Crosshair evolves** — shape/color changes during rapid-fire  

---

## Out of scope for v1 (skip unless you love them)

- Full online multiplayer  
- Huge open map / levels campaign  
- Deep RPG inventory  
- Mobile touch controls (unless you specifically want Android later)  

---

*Last verified against current `scripts/`, `scenes/`, and `project.godot`.*
