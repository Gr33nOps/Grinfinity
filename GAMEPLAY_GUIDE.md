# Grinfinity gameplay reference — rc4

There are **7 selectable upgrade types**, **3 automatic ability unlocks**, **3 pickup types**, **8 enemy types** (including the splitter child), and **3 bosses**. No upgrade costs currency. Everything affecting combat resets on a new run; unlocked planet colours are cosmetic.

## How upgrade offers work

Clear every enemy in a wave. The game pauses and presents up to three distinct eligible upgrades. Pick one, or choose **KEEP CURRENT BUILD** to skip. There is no timer. Automatically earned abilities do not consume your choice.

The table lists the earliest **completed** wave for each level. You must own the preceding level; maxed upgrades and upgrades for locked abilities are excluded. An available standard stat/ability upgrade always gets a slot. At most one weapon replacement appears in an offer. New spread ranks are shown on their milestone wave if the previous rank is owned; a skipped rank stays eligible later. Newly introduced weapons also get an introduction slot. Remaining slots are random among eligible choices.

Cannon and Ion Lance are alternative replacements for the starter gun: choosing either removes further weapon replacements for that run. Existing firing-speed, piercing and spread upgrades continue working on the chosen weapon. You can skip every weapon offer and keep the starter.

| Selectable upgrade | Earliest wave cleared by level | Levels | Effect |
|---|---|---|---|
| Faster Shots | 1 / 4 / 7 | 3 | Each level multiplies firing delay by 0.82: 18% less delay. |
| Piercing Shots | 1 | 1 | Bullets penetrate two additional ordinary enemies; front armour still deflects head-on hits. |
| Quicker Dash | 1 / 4 / 7 | 3 | Each level multiplies dash cooldown by 0.87: 13% less cooldown. Requires dash. |
| Debris Cannon | 4 | 1 | Replace the gun with six short-range pellets, firing every 0.52 seconds before upgrades. |
| Spread Shot | 5 / 10 | 2 | One additional side bullet per level: starter gun fires 2, then 3. Its centre shot stays aimed; the first side bullet alternates left/right. |
| Ion Lance | 6 | 1 | Replace the gun with slower, fast-travelling shots dealing 2 damage and penetrating 8 additional ordinary enemies. Base interval 0.78 seconds. |
| Bigger Nova | 6 / 9 / 12 | 3 | Add 22% of base nova radius per level. Requires nova. |

The starter Comet fires every **0.18 seconds**, up from 0.22 seconds in rc3, without needing an upgrade. Spread no longer grants an immediate three-bullet opening volley or reaches five bullets with the starter.

## Automatically earned abilities

| Ability | Earned after clearing | Function |
|---|---|---|
| Dash | Wave 1 | Brief protected burst in movement direction, or aim direction when stationary. Shift / B. Spends 4 mass when available; insufficient mass does not prevent dashing. |
| Rapid Fire | Wave 3 | Fire much faster for 3.5 seconds, followed by a 7-second cooldown. E / X. |
| Nova | Wave 5 | Spend 35 mass to destroy ordinary enemies in a radius. R / Y. It does not damage bosses or refund debris. |

## Pickups

- **Shield:** circular bubble blocking the next hit, followed by a short recovery window. Each run starts with one. Duplicate shields become overcharge drops while protected.
- **Overcharge:** +1 bullet damage for 8 seconds; another pickup refreshes its duration.
- **Nuke:** instantly destroys ordinary enemies in a large radius, not bosses. Available from wave 5; earlier rolls become overcharge.

At most two pickups remain on screen. A normal kill has a 5% base drop chance; Planetoid, Bulwark and Flare kills add 14 percentage points. Before ownership/wave substitutions, type weights are shield 50%, overcharge 35%, nuke 15%. Magnet and Freeze remain removed from live drops.

## Enemies

HP below means ordinary one-damage hits, before overcharge, weapon damage or armour.

| Enemy | First wave | HP | Behaviour |
|---|---|---|---|
| Drifter | 1 | 1 | Basic enemy pulled toward the player. |
| Shard | 3 | 1 | Fast swarmer arriving in small groups. |
| Planetoid | 4 | 4 | Large, slow, heavy enemy; resists knockback. |
| Fracture | 6 | 2 | Splits into three smaller enemies when destroyed. |
| Splinter | From Fracture, wave 6 onward | 1 | Small, fast child launched outward before pursuing. |
| Satellite | 8 | 2 | Circles at range and fires at you roughly every 2.1 seconds. |
| Bulwark | 10 | 3 | Front armour rejects head-on shots; attack its sides/rear. |
| Flare | 12 | 2 | Explodes when destroyed; the blast can hit the player and nearby enemies. Keep distance. |

Regular spawns cap at 18 live enemies; split children have an overall 24-body ceiling. Waves grow from 6 enemies to a budget cap of 28. Speed and spawn pressure advance with wave progress, never with time spent struggling in the same wave.

## Bosses

Bosses arrive after the stated wave is cleared and the choice/break ends. Regular wave spawning stops during the fight. Later waves resume after victory.

| Boss | After clearing | HP | Attacks and response |
|---|---|---|---|
| The Coil | Wave 6 | 140 | Drifts and fires circular bullet patterns with a rotating safe gap. Move toward the gap; dash through danger. Ring attacks accelerate as its health falls. |
| The Brood | Wave 12 | 220 | Slowly follows the player and spawns fast Shards, capped at 12 live ordinary enemies. Spawns accelerate as health falls. Manage the swarm while focusing the boss. |
| The Black Hole | Wave 18 | 300 | Pulls the player, debris and friendly shots toward its core, and fires back at the player. Pull strength and attack frequency increase as it loses health. Keep away from the core and use dash to escape. |

All three are existing distinct encounters; this pass rebalances upgrades rather than adding extra bosses. Endless waves continue after the third boss. Comets, gravity wells and temporary arena events are separate hazards, not additional enemy or boss types.
