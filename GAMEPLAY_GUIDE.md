# Gameplay guide

One endless run. Enemies keep coming, the arena gets harder the longer you last, and the run ends the first time something touches the planet without a shield. Your survival time is the record; score is kept alongside it.

Every tuning number mentioned here lives in `scripts/Balance.cs`.

## The arena

The playable area is 5220 × 3220, about three screens each way, surrounded by a band of plain dark space the camera can see into but nothing can enter. The camera follows the planet with a small dead zone, leans a little toward where you aim, and stops at the edge of the world. During a boss fight it also leans a little toward the boss. The lit arena fades into the dark at its edge; that fade is where the planet stops.

Enemies enter just outside the screen and never closer than 760 units to the planet. If you are backed into a corner, they come in from the visible edge of the arena instead, so corners are not safe spots. An enemy left more than 2500 units behind is brought back in near the screen.

## Life and the shield

There is no health bar. One hit ends the run.

A **shield** blocks exactly one hit. You never start with one; it drops like an upgrade. You can hold one at a time. When it breaks, the planet blinks for 1 second and cannot be hit.

## Abilities

Each unlocks automatically at the same time in every run and has its own cooldown. There is no energy meter.

| Ability | Unlocks | Cooldown | Keys | What it does |
|---|---|---|---|---|
| Dash | 0:40 | 2.2 s | Shift / B | Zooms 340 units in your move direction (or aim direction when standing still). While moving you cannot be hurt, and every normal enemy the path crosses is destroyed. Then the planet blinks for 0.4 s: still safe, but no longer destroying anything. Get clear before the blink ends. A boss takes 3.5% of its health, once per dash. |
| Overdrive | 2:20 | 30 s | E / X | 6 seconds of your current gun going wild: 2.5× fire rate, double damage, +1 pierce, bigger and faster shots. It keeps your spread, pierce and weapon. |
| Nova | 5:10 | 45 s | R / Y | A shockwave out to 720 units. Normal enemies it reaches are destroyed, enemy shots in it are wiped, and a boss takes 10% of its health. |

The Accessibility menu can switch Overdrive to hold-to-use.

## Upgrades and drops

There is no upgrade menu. Some kills drop a glowing pickup; touch it and it applies at once. A drop comes due every 38–58 seconds at the start (the gap grows by 10% per minute survived) and falls from your next kill, but only if you have also been fighting since the last one. Pickups last 16 seconds and slide to you when you are close.

A drop is only ever something that would help right now: nothing already maxed, nothing for an ability you have not unlocked, no second weapon, no second shield. A shield cannot drop more than once every 75 seconds.

| Pickup | Levels | Effect |
|---|---|---|
| Faster Shots | 4 | 14% less time between shots per level |
| Piercing Shots | 3 | Shots pass through one more enemy per level |
| Spread Shot | 2 | One extra angled shot (alternating sides), then two |
| Longer Dash | 3 | +14% dash distance and +0.08 s blink per level |
| Overdrive Boost | 3 | +1.5 s Overdrive and 15% faster Overdrive fire per level |
| Bigger Nova | 3 | +18% Nova radius per level |
| Debris Cannon | 1 | Replaces the Comet: six short-range pellets. Not before 2:30 |
| Ion Lance | 1 | Replaces the Comet: slow, heavy shots that pierce eight enemies. Not before 2:30 |
| Shield | 1 held | Blocks one hit |

The two weapon swaps rule each other out. Rings appear around the planet at 4, 9 and 15 upgrade levels.

## Enemies

| Enemy | Joins | HP | Behaviour |
|---|---|---|---|
| Drifter | 0:00 | 1 | Falls toward the planet. Arrives in twos from 0:20, threes from 1:00. |
| Shard | 0:30 | 1 | Fast, in packs. |
| Planetoid | 1:15 | 4 | Big and slow. |
| Fracture | 2:00 | 2 | Splits into three Splinters when destroyed. |
| Satellite | 3:45 | 2 | Circles at range and shoots at you. |
| Bulwark | 5:00 | 3 | Armoured front; shots from the front bounce off. Dash and Nova go through the armour. |
| Flare | 7:00 | 2 | Explodes when destroyed. The dashed ring around it shows the blast. |

Spawn rate, the number alive at once and enemy speed all ramp with time (see the tables in `Balance.cs`). Speed keeps creeping up after 25 minutes. From 1:50, tight packs rush in from one direction about every 26 seconds. By 18 minutes the mix is mostly the dangerous kinds.

Comets (from 4:10) cross the screen along a dashed red line that shows for 1.35 s first. Gravity wells (from 7:50) pull you toward a lethal core; dash out. Solar Wind and Heavy Weather events start around 8:40. None of these run during a boss.

## Bosses

Bosses arrive on a clock, with a warning banner and a marker where they will land. When one dies its shots vanish, and three strong upgrades fly out of the wreck.

| Round 1 | Arrives | HP | Fight |
|---|---|---|---|
| The Coil | 2:50 | 360 | Rings of shots with a gap. Slip through the gap or dash. |
| The Brood | 5:50 | 500 | Keeps spawning Shards. Keep shooting the big one. |
| The Black Hole | 8:50 | 1000 | Pulls you, enemies and your shots toward its core, and throws shots back. |

After the Black Hole the run carries on, and the bosses come round again every 2.5 minutes: The Coil at 11:20, The Brood at 13:50, The Black Hole at 16:20, and so on forever. Each round the bosses get 30% more health and attack a little faster. More of the difficulty comes from company, though: the first time round each boss fights alone, and from round two ordinary enemies keep arriving during the fight (35% of the normal rate in round 2, 55% in round 3, up to 95%). There is always at least 60 seconds between one boss dying and the next arriving.

## Score

10 points per second survived, 25 per kill plus 3 for each link in your current combo (up to 25 links). A combo breaks after 2.5 seconds without a kill. Beating a boss is worth 5,000 × its round.

The leaderboard ranks runs by survival time, with score breaking ties.
