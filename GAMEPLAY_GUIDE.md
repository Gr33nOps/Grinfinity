# Gameplay guide

One endless run. Enemies keep coming, the arena gets harder the longer you last, and the run ends the first time something touches the planet without a shield. Your survival time is the only result. There is no score.

Every tuning number mentioned here lives in `scripts/Balance.cs`.

## The arena

The playable area is 5220 × 3220, about three screens each way, surrounded by a band of plain dark space the camera can see into but nothing can enter. The camera follows the planet with a small dead zone, leans a little toward where you aim, and stops at the edge of the world. During a boss fight it also leans a little toward the boss. The lit arena fades into the dark at its edge; that fade is where the planet stops.

Enemies enter just outside the screen and never closer than 760 units to the planet. If you are backed into a corner, they come in from the visible edge of the arena instead, so corners are not safe spots. An enemy left more than 2500 units behind is brought back in near the screen.

## Life and the shield

There is no health bar. One hit ends the run.

A **shield** blocks exactly one hit. You never start with one; it drops like an upgrade. You can hold one at a time. When it breaks, the planet blinks for 1 second and cannot be hit.

## Abilities

All three are unlocked from the first second but start the run charging, so the first Dash comes at 0:02, Overdrive at 0:26 and Nova at 0:40. Each has its own cooldown. There is no energy meter. (Setting the unlock times in `Balance.cs` above zero staggers them again.)

| Ability | Cooldown | Keys | What it does |
|---|---|---|---|
| Dash | 2.0 s | Shift / B | Zooms 340 units in your move direction (or aim direction when standing still). While moving you cannot be hurt, and every normal enemy the path crosses is destroyed. Then the planet blinks for 0.4 s: still safe, but no longer destroying anything. Get clear before the blink ends. A boss takes 3.5% of its health, once per dash. |
| Overdrive | 26 s | E / X | 6 seconds of your current gun going wild: 2.5× fire rate, double damage, +1 pierce, bigger and faster shots. It keeps your spread and pierce. |
| Nova | 40 s | R / Y | A shockwave out to 720 units. Normal enemies it reaches are destroyed, enemy shots in it are wiped, and a boss takes 10% of its health. |

The Accessibility menu can switch Overdrive to hold-to-use.

## CORE and upgrades

Every kill fills the **CORE** bar, the tall bar in the top-left corner beside your three abilities, and tougher enemies fill it faster (a Drifter gives 1, a Planetoid 3). When it is full, **UPGRADE READY** appears under the abilities. Open the upgrade screen whenever you like with **Tab** (Back/View on a controller) or by clicking the prompt, and spend the full bar on one rank. The bar stops at full rather than saving a second one, so do not sit on it.

The first bar needs 35 CORE and each one after needs 32% more, so upgrades come quickly early and slow down as the build gets strong.

The whole game is paused while the upgrade screen is open: enemies, shots, cooldowns and the survival clock. If you buy something, the planet blinks safely for 1.4 seconds when you return, so you are never killed the instant you come back. The blink only protects; it destroys nothing. Opening the screen and buying nothing gives no blink.

| Branch | Upgrade | Ranks | Effect |
|---|---|---|---|
| Gun | Faster Shots | 3 | 16% less time between shots per rank |
| Gun | Spread Shot | 2 | One extra angled shot (alternating sides), then two |
| Gun | Piercing Shots | 2 | Shots pass through 1, then 3 extra enemies |
| Dash | Dash Reach | 3 | +14% dash distance per rank |
| Dash | Dash Blink | 2 | +0.12 s safe blink after a dash per rank |
| Overdrive | Overdrive Power | 3 | Overdrive fires 12% faster per rank |
| Overdrive | Overdrive Time | 2 | +1.5 s Overdrive per rank |
| Nova | Bigger Nova | 3 | +15% Nova radius per rank |
| Nova | Nova Power | 2 | +3% boss damage and 15% shorter cooldown per rank |

The upgrade screen is drawn as a tree growing out of the CORE: each branch runs bottom to top in the order above, and every node needs one rank in the node below it before it opens. The Dash, Overdrive and Nova branches stay locked until their ability is online. Rings appear around the planet at 5, 11 and 17 ranks, out of 22.

## Pickups

Enemies drop only three things, and only when they would help:

| Pickup | Does | Never drops when |
|---|---|---|
| Shield | Blocks one lethal hit. You can hold one. | You already have one, or one dropped in the last 60 s |
| CORE Burst | Fills the CORE bar at once | The bar is already full |
| Power Cell | Every unlocked ability ready again | Every ability is already ready |

The first pickup comes 18–26 seconds in, then one every 24–36 seconds (a little slower as the run goes on). It falls from your next kill, but only if you have been fighting since the last one. A kill off screen drops its pickup a short way from you instead. Pickups last 22 seconds and slide to you from about half a screen away.

Bosses still drop three upgrade ranks each, on top of what CORE buys. They only give ranks the tree could sell you right now.

## Enemies

| Enemy | Joins | HP | Behaviour |
|---|---|---|---|
| Drifter | 0:00 | 1 | Falls toward the planet. Arrives in twos from 0:08, threes from 0:40. |
| Shard | 0:15 | 1 | Fast, in packs. |
| Planetoid | 0:45 | 4 | Big and slow. |
| Fracture | 1:20 | 2 | Splits into three Splinters when destroyed. |
| Satellite | 2:30 | 2 | Circles at range and shoots at you. |
| Bulwark | 4:00 | 3 | Armoured front: most shots from the front bounce off, though every fifth chips through. Hit it from the side or back, or Dash or Nova through the armour. |
| Flare | 5:30 | 2 | Explodes when destroyed. The dashed ring around it shows the blast. |

Spawn rate, the number alive at once and enemy speed all ramp with time (see the tables in `Balance.cs`). Speed keeps creeping up after 20 minutes. By 13 minutes the mix is mostly the dangerous kinds.

From 0:45, about every 20 seconds, a **rush** arrives in one of five shapes, never the same one twice running, and a little bigger as the run goes on:

- **Pack:** a tight group from one direction.
- **Pincer** (from 1:00): two groups from opposite sides at once.
- **Ring** (from 1:30): enemies all the way round you, just off screen, closing in. From 1:20 some of them are Fractures.
- **Wall:** a line of Shards sweeping in across one side.
- **Escort** (from 1:30): a Planetoid (later sometimes a Bulwark) with a guard of Shards.

Comets (from 2:50) cross the screen along a dashed red line that shows for 1.35 s first. Gravity wells (from 5:30) pull you toward a lethal core; dash out. Solar Wind and Heavy Weather events start around 6:20. None of these run during a boss.

## Bosses

Bosses arrive on a clock, with a warning banner and a marker where they will land. When one dies its shots vanish, and three strong upgrades fly out of the wreck.

| Round 1 | Arrives | HP | Fight |
|---|---|---|---|
| The Coil | 2:05 | 400 | Rings of shots with a gap. Slip through the gap or dash. |
| The Brood | 4:25 | 660 | Keeps spawning Shards. Keep shooting the big one. |
| The Black Hole | 6:45 | 1100 | Pulls you, enemies and your shots toward its core, and throws shots back. |

After the Black Hole the run carries on, and the bosses come round again every 2 minutes 10 seconds: The Coil at 8:55, The Brood at 11:05, The Black Hole at 13:15, and so on forever. Each round the bosses get 70% more health (so a full late-game build still has a fight on its hands) and attack a little faster. More of the difficulty comes from company, though: the first time round each boss fights alone, and from round two ordinary enemies keep arriving during the fight (35% of the normal rate in round 2, 55% in round 3, up to 95%). There is always at least 45 seconds between one boss dying and the next arriving.

## Records

How long you survived is the only result. The HUD shows it top right, the game over screen shows it big, and the leaderboard ranks runs by it and nothing else. Kills and bosses beaten are shown on the game over screen for interest, but they do not add up to anything.

The combo counter under the time counts kills in a row, and breaks after 2.5 seconds without one. It is just for show.

## On-screen buttons

Hints only show the controls you are using. Touch the keyboard or mouse and they say SHIFT, E, R, TAB; touch a controller and they say B, X, Y, BACK. Plugging a controller in switches to controller hints.

## Faces

Only your planet is happy. Every enemy has its own bad mood: Drifters are sad (three different sad faces), Shards angry (two), Planetoids grumpy, Fractures worried and their Splinters scared, Satellites suspicious, Flares furious and Bulwarks stubborn. The Coil sneers, the Brood wails and the Black Hole glares.
