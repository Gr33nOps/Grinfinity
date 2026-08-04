# Simplification Pass — Implementation Plan

Companion to `GRINFINITY_SIMPLIFIED.md`. That document decides *what*; this one
records what the code actually looks like underneath those decisions, measured
rather than estimated, so the cuts can be sequenced without building on top of
something about to be torn out.

Every count below came from reading the tree before any of it was built.

**Status: steps 1–4 are done.** What is left is step 5 — the leaderboard name
prompt and the Stats screen — plus the tuning that only a human at the
keyboard can do. See §9 for what is still open.

---

## 0. Headline findings

Three things materially change how this pass should be sequenced:

**The cuts are cheaper than they look; the additions are more expensive.**
Mode-conditional gameplay logic is 8 call sites total. Difficulty is 2. The
five modes barely touched gameplay — they were mostly a menu. Deleting them is
a small, safe diff.

**The leaderboard needs a feature, not a paring-back.** §5 asks for "player
name, score." `Leaderboard.Entry` has score, time, kills, weapon, world and
date — and **no name field**. The game never asks the player who they are.
Name capture is new work: a prompt, a place to store it, and a decision about
when it's asked.

**Four relic effects are real gameplay, not menu scaffolding.** Cutting
`Relics` as a *system* is right. Deleting Slow Aura, Vampiric Dash, Piercing
and Double Debris as *effects* throws away four tested, working modifiers that
are exactly the shape §2 wants for its upgrade pool. Re-home them, don't
delete them.

---

## 1. Measured blast radius

| System | Files referencing | Gameplay call sites | Notes |
|---|---|---|---|
| `Loadout` | 12 | — | The spine. Everything routes through it. |
| `PlayerProfile` | 13 | — | Survives, in reduced form (§7). |
| `GameMode` | 9 | 8 | 6 in `GameManager`, 1 in `BodySpawner`, rest is menu/save. |
| `Difficulty` | 6 | 2 | `Body` contact radius, `BodySpawner` ramp/cadence. |
| `Relic` | 5 | 6 | All `run.Has(RelicId.X)` guards. See §4. |
| `Worlds` | 7 | — | Cosmetic; §7 keeps them off the pre-run path. |
| `Upgrade` (shop) | 6 | — | Menu-side only. |
| `RunState.Rng` | 18 | 50 | **Do not touch.** See §3. |

Deletable outright once callers are cleaned: `ModeSelect.cs`, `Modes.cs`,
`Difficulties.cs`, `UpgradesMenu.cs`, `UpgradeId.cs`, `Relic.cs`,
`WeaponSelect.cs`, and the scenes `mode_select.tscn`, `weapon_select.tscn`,
`upgrades.tscn`.

`Loadout` collapses from four fields (weapon, world, mode, difficulty) to one
(weapon), and once §2's in-run weapon progression lands it may not need to
survive a scene change at all.

---

## 2. Save data

Four separate files, each independently versioned:

| File | Version | Holds | Fate |
|---|---|---|---|
| `user://settings.cfg` | — | options, plus weapon/world/mode/difficulty mirrors | Drop the four loadout mirrors, keep everything else. |
| `user://profile.cfg` | 1 | stardust, lifetime totals, worlds, achievements, upgrade levels, weapon tally | → v2. Keep totals. Drop upgrade levels. Park worlds/achievements. |
| `user://highscore.cfg` | 4 | per-mode records | → v5. Collapse to the Endless Orbit record; discard the other four. |
| `user://leaderboard.cfg` | 2 | per-mode tables | → v3. Collapse to one table, **add name**. |

Per your call — **keep lifetime stats, drop the rest**:

- **Carried forward:** total orbits, total kills, total time played, heaviest
  mass, best score, longest survival. These feed the Stats screen (§7) and are
  the only things that stay true across a curve retune.
- **Demoted:** stardust. It stops being a balance you spend and becomes a
  lifetime "earned" counter on Stats. In-run stardust is a fresh per-run
  resource starting at zero. Both can't be the same number — this is the
  cleanest split, and it's what makes §7's "zero currency" literally true.
- **Discarded:** permanent upgrade levels (the shop is gone), per-mode records
  for the four cut modes, `daily_day` / `daily_score`.
- **Parked, not deleted:** unlocked worlds and achievements stay in the file,
  unread, until §10 revisits them. Cheaper than a second migration later, and
  it means a player who unlocked nine worlds doesn't silently lose them.

Migration runs once on load, per file, keyed off the existing version int.
Each is a read-old / write-new in place — no separate tool.

---

## 3. Seeded RNG — cut the seed, keep the plumbing

The document files this under "Daily Alignment machinery." It isn't. `RunState.Rng`
replaced `GD.Rand*` across **50 call sites in 18 files**; only 2 `GD.Rand`
calls remain in the entire codebase.

Removing `RunState.Rng` would mean touching every spawn, roll and jitter in the
game to gain nothing — the loop doesn't care where its randomness comes from.

**Cut instead:** `Modes.SeedRun()`, the fixed-seed path, and the
`daily_day`/`daily_score` profile keys. `RunState.Rng` keeps working, seeded
from the clock. One-line change at the seed source, nothing else moves.

---

## 4. Relics — retire the system, keep the effects

The six gameplay hooks are all simple guards:

| Effect | Where | Reuse as |
|---|---|---|
| Slow Aura | `Body.cs:291` | Mass-economy upgrade — it's a pull-radius-adjacent effect |
| Vampiric Dash | `Player.cs:170` | Ability upgrade |
| Piercing | `Player.cs:453` | Weapon upgrade — already the Ion Lance archetype's identity |
| Double Debris | `GameManager.cs:570` | Mass-economy upgrade |

Each is tested and works. Deleting `Relics.Roll()` and the once-per-run
announce is correct; deleting the effects would mean rebuilding four of §2's
upgrade options from scratch in step 3.

**Recommendation:** in step 1, strip the rolling and the announce but leave the
effect code behind a flag that's simply always-off. Step 3 rewires those same
flags to upgrade purchases. Nothing gets written twice.

---

## 5. Difficulty — keep the mechanism, change its driver

Only two hooks:

- `Body.cs:183` — `collisionShape.Scale *= Loadout.DifficultyProfile.ContactRadiusMultiplier`
- `BodySpawner.cs:65` — ramp and spawn cadence multipliers

§3 explicitly wants this mechanism kept, driven by elapsed time instead of a
menu choice. So both survive; only the source of the multiplier changes, from
`Loadout.DifficultyProfile` to a curve function of `RunState.SurvivalTime`.

This is the single highest-leverage piece of the whole pass — it *is* the
curve §3 spends its length describing, and it already has its hooks in the
right places. Everything else is deletion.

Assist Mode (§10) stays a standalone Settings toggle applying its own
body-speed cut, independent of that curve. It currently stacks on top of
Difficulty; it needs unhooking, not deleting.

---

## 6. Build order

Refining §9 with what the measurements changed:

**Step 1 — menu chain.** Delete mode/weapon/world/difficulty select. Menu's
Play goes straight to the arena. Collapse `Loadout` to weapon-only. Strip
relic rolling but leave effect code dormant (§4). Cut the seed source, keep
`RunState.Rng` (§3). Repoint `Difficulty` multipliers at a fixed constant for
now — step 4 replaces the constant with the curve.

*Verify:* Menu → Play → arena → death → recap → Menu, with no orphan scene
references. This is the riskiest cut; validate before continuing.

**Step 2 — save schema.** All four files, migrations as §2. Do it while step 1
has the same code open.

*Verify:* load an existing profile, confirm lifetime totals survive and
nothing throws on the dropped keys.

**Step 3 — wave-break upgrades.** The one genuinely new system. Start with
three options — one weapon, one ability, one mass-economy — wired to the
dormant relic flags where they fit. Validate the *interaction* (no pause,
readable, fast) before growing the pool.

*Note:* the HUD centre is now clear, which is where this prompt belongs.

**Step 4 — the curve.** Replace step 1's fixed difficulty constant with the
real time-driven curve. Most of the playtesting time lives here.

**Step 5 — leaderboard name capture + Stats screen.** Name capture is new
work (§0); Stats is pure display.

---

## 7. Open questions the plan can't answer

These need hands on the game, not a decision here. Listed so they don't get
lost:

1. **Random offers vs. full menu** at wave breaks (§2's own open question).
   Built as random-offer, with two fairness rules bolted on (a mass option
   always present, at least one offer always affordable). Whether that is
   enough structure is a playtest question.
2. ~~**Nuke and Freeze**~~ — **decided: they stay as they are.** Power-ups
   were explicitly kept. If the curve later proves they defuse it, that is a
   tuning problem to solve then, with the curve in front of you.
3. **When is the player's name asked?** First launch, or first time they place
   on the board? The latter is less friction but means the prompt interrupts a
   recap screen. Still open — the name currently defaults to PLAYER and is
   already persisted, so only the prompt is missing.
4. **Arena events during bosses** (§10) — bosses recur far more often now; the
   stand-down rule may starve events of airtime.
5. **Stardust outpaces spending on a long run.** One purchase per break is the
   design, but by the fifth or sixth break every option is affordable and the
   choice stops being about cost. Whether that matters depends on whether the
   time pressure alone carries the decision — which only playing will say.

---

## 9. What is still to do

**Step 5 — the leaderboard name prompt and the Stats screen.** The name is
persisted and every board entry already carries one; there is simply no UI to
set it, so everything reads PLAYER. Stats is pure display and depends on
nothing else.

**The tuning pass.** Wave sizes, the 7-second break, cost curves and the
escalation shape are all first-pass numbers chosen to be reasonable, not
right. §3 of the design doc is explicit that this is where most of the real
playtesting time goes, and none of it has happened yet — a bot can confirm
the systems run, not whether the rhythm is any good.

---

## 8. What this pass does not touch

Unchanged and mode-agnostic, per §1: hitstop, screen shake, hit feedback,
mass/venting/moons/rings, pull-not-chase, the bestiary, all three bosses,
power-ups (pending question 2), arena events, gravity wells, comet flybys,
options, accessibility, localisation.

Audio placeholders remain placeholders. This pass neither fixes nor worsens
that.
