# Gameplay guide

One endless run. Enemies keep coming, the arena gets harder the longer you last, and the run ends the first time something touches the planet with no moon left to save it. Your survival time is the only result. There is no score.

Every tuning number mentioned here lives in `scripts/Balance.cs`.

## The arena

The playable area is 5220 × 3220, about three screens each way, surrounded by a band of plain dark space the camera can see into but nothing can enter. The camera follows the planet with a small dead zone, leans a little toward where you aim, and stops at the edge of the world. During a boss fight it also leans a little toward the boss. The lit arena fades into the dark at its edge; that fade is where the planet stops.

Enemies enter just outside the screen and never closer than 760 units to the planet. If you are backed into a corner, they come in from the visible edge of the arena instead, so corners are not safe spots. An enemy left more than 2500 units behind is brought back in near the screen.

Enemies keep a little personal space: any two that overlap are eased apart, the smaller giving way more, so a crowd arrives as a spread-out pack you can read instead of one heap of faces. It never slows them down or changes where they are heading.

## Life and moons

There is no health bar. One hit ends the run, unless a moon takes it.

**Moons** are your shields. Each one orbits the planet, and each one blocks exactly one hit. You never start with one; they drop as pickups, and you can have up to **three** in orbit at once. When a hit lands, one moon bursts instead of you, the planet blinks for 1 second and cannot be hit, and an enemy that caused it is destroyed too and counts as a kill (bosses are too big to pop). You can see how many are left just by looking at your planet.

## Abilities

All three are unlocked from the first second but start the run charging, so the first Dash comes at 0:02, Overdrive at 0:26 and Nova at 0:40. Each has its own cooldown. There is no energy meter. (Setting the unlock times in `Balance.cs` above zero staggers them again.)

| Ability | Cooldown | Keys | What it does |
|---|---|---|---|
| Dash | 2.0 s | Shift / B | Zooms 340 units in your move direction (or aim direction when standing still). While moving you cannot be hurt, and every normal enemy the path crosses is destroyed. Then the planet blinks for 0.4 s: still safe, but no longer destroying anything. Get clear before the blink ends. A boss takes 3.5% of its health, once per dash. |
| Overdrive | 26 s | E / X | 6 seconds of your current gun going wild: 2.5× fire rate, double damage, +1 pierce, bigger and faster shots. It keeps your spread and pierce. |
| Nova | 40 s | Q / Y | A shockwave out to 720 units. Normal enemies it reaches are destroyed, enemy shots in it are wiped, and a boss takes 10% of its health. |

The Accessibility menu can switch Overdrive to hold-to-use.

## CORE and upgrades

Every kill fills the **CORE** bar along the bottom of the screen, and tougher enemies fill it faster (a Drifter gives 1, a Planetoid 3). When it is full, it turns orange and reads **UPGRADE READY** with the button to press, right inside the bar. Open the upgrade screen whenever you like with **Tab** (Back/View on a controller) or by clicking the bar, and spend a full bar on one rank. Full bars are **saved**, up to three: the three pips at the right end of the bar count them, and the bar says "2 UPGRADES READY" and so on. So you can keep fighting and spend them later, and three saved bars buy three upgrades in one visit (the screen stays open until you have spent them). Only with three saved does the bar wait at full. Each kill makes the front edge of the bar flare with a few sparks, so you can see it being fed without anything crossing the screen. The upgrade screen shows a little looping picture of whichever upgrade you point at. Once every upgrade is bought, the bar turns purple and becomes **OVERCHARGE**: kills keep filling it, and each full bar recharges all three abilities at once.

The first bar needs 35 CORE and each one after needs 42% more, so upgrades come quickly early and slow down as the build gets strong.

The whole game is paused while the upgrade screen is open: enemies, shots, cooldowns and the survival clock. If you buy something, the planet blinks safely for 1.4 seconds when you return, so you are never killed the instant you come back. The blink only protects; it destroys nothing. Opening the screen and buying nothing gives no blink.

| Branch | Upgrade | Ranks | Effect |
|---|---|---|---|
| Gun | Faster Shots | 2 | 23% less time between shots per rank |
| Gun | Spread Shot | 2 | One extra angled shot (alternating sides), then two |
| Gun | Piercing Shots | 2 | Shots pass through 1, then 3 extra enemies |
| Dash | Dash Reach | 2 | +21% dash distance per rank |
| Dash | Dash Blink | 2 | +0.12 s safe blink after a dash per rank |
| Overdrive | Overdrive Power | 2 | Overdrive fires 17.5% faster per rank |
| Overdrive | Overdrive Time | 2 | +1.5 s Overdrive per rank |
| Nova | Bigger Nova | 2 | +22.5% Nova radius per rank |
| Nova | Nova Power | 2 | +3% boss damage and 15% shorter cooldown per rank |

The upgrade screen is drawn as a tree growing out of the CORE: each branch runs bottom to top in the order above, and every node needs one rank in the node below it before it opens. The Dash, Overdrive and Nova branches stay locked until their ability is online. The yellow ring around the planet is a badge, not an upgrade: your planet wears it, on the menu and in game, once you have a run on the leaderboard. It does nothing in play.

## Pickups

Enemies drop only three things, and only when they would help:

| Pickup | Does | Never drops when |
|---|---|---|
| Moon | One more moon in orbit; each blocks one lethal hit. Up to three. | Three already in orbit, one is already lying in the arena, or one dropped in the last 75 s |
| CORE Burst | One whole upgrade saved at once, however full the bar was; your progress toward the next is kept | Three upgrades are already saved |
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
| Bulwark | 4:00 | 3 | Armoured front: most shots from the front bounce off, though every fifth chips through. Hit it from the side or back, or Dash or Nova through the armour. |
| Flare | 5:30 | 2 | Explodes when destroyed. The dashed ring around it shows the blast, which lands 0.3 s after the flash, so there is just time to step or dash out. |

Spawn rate, the number alive at once and enemy speed all ramp with time (see the tables in `Balance.cs`). Speed keeps creeping up after 20 minutes. By 13 minutes the mix is mostly the dangerous kinds.

From 0:45, about every 20 seconds, a **rush** arrives in one of five shapes, never the same one twice running, and a little bigger as the run goes on:

- **Pack:** a tight group from one direction.
- **Pincer** (from 1:00): two groups from opposite sides at once.
- **Ring** (from 1:30): enemies all the way round you, just off screen, closing in. From 1:20 some of them are Fractures.
- **Wall:** a line of Shards sweeping in across one side.
- **Escort** (from 1:30): a Planetoid (later sometimes a Bulwark) with a guard of Shards.

Comets (from 2:50) cross the screen in a straight line: a bright blue teardrop head with a long icy tail. For 1.35 s before it flies, its lane is shown as a red band exactly as wide as what it hits, with arrows running along it the way it will go. Gravity wells (from 5:30) pull you toward a lethal core; dash out. Solar Wind and Heavy Weather events start around 6:20. None of these run during a first-round boss fight or a boss warning; from round two they carry on regardless.

## Bosses

Your shots are cream, and hot gold during Overdrive. Every enemy shot (the Coil's rings, the Black Hole's throws) looks the same: a round hot-pink orb with a dark rim and a pale centre, drawn exactly the size of what it hits with, trailing a short fading tail. It swells in when fired and shrinks away at the end of its range, harmless from the moment it starts to shrink.

Bosses arrive on a clock, with a warning banner and a marker where they will land. Ordinary enemies never stop for them: a boss is fought with the arena as busy as ever. When one dies all enemy shots vanish, and three strong upgrades fly out of the wreck. Each boss is bigger than the last.

| Round 1 | Arrives | HP | Fight |
|---|---|---|---|
| The Coil | 2:05 | 340 | Rings of shots with a gap. Slip through the gap or dash. |
| The Brood | 4:25 | 1100 | Chases you, throws out Shards two or three at a time, and every few seconds stops, glows and lunges at where you are. Sidestep or dash the lunge. |
| The Black Hole | 6:45 | 1100 | Pulls you, enemies and your shots toward its core, and throws shots back. |

The first time round, each boss waits for the one before to die, with at least 45 seconds of breathing room after it. Beating the first Black Hole changes that: from then on bosses keep to the clock whether or not the last one is dead, so a boss you cannot finish in time is still there when the next arrives. The gaps also shrink each time, from 2 minutes 10 seconds by 10 seconds a boss down to 1 minute: The Coil at 8:55, The Brood at 10:55, The Black Hole at 12:45, then 14:25, 15:55, 17:15, 18:25 and every minute after 19:25. Each round the bosses get 70% more health and attack a little faster. Up to five can be on the field at once, each with its own health bar at the top of the screen. Far enough in, they will outpace anyone.

## Records

How long you survived is the only result. The HUD shows it top right, the game over screen shows it big, and the leaderboard ranks runs by it and nothing else. The game over screen shows just that time and your best.

The combo counter under the time counts kills in a row, and breaks after 2.5 seconds without one. It is just for show.

## My Planet

The **My Planet** screen shows the planet you wear, big, with the ring if you have earned it. The arrows browse all twelve planets: landing on an unlocked one wears it straight away and shows its name, and a locked one shows as a dark shape with its unlock rule underneath. Planets are only looks and never change how you play.

| Planet | Unlocked by |
|---|---|
| Easewind | Yours from the start |
| Stillwater | Play 3 runs |
| Hearthglow | Play 6 runs |
| Hushmere | Pop 100 enemies in total |
| Anchorlight | Pop 300 enemies in total |
| Gracebloom | Survive 2 minutes in one run |
| Wishfall | Survive 3 minutes in one run |
| Boldcrest | Survive 4 minutes in one run |
| Laurelcrown | Survive 5 minutes in one run |
| Sunburst | Max out every upgrade in one run |
| Sparkrush | Play for 1 hour in total |
| Heartsong | Play 20 runs, and survive 10 minutes in one |

The same screen lists your lifetime numbers, and below them a card with all nine achievements, the earned ones ticked:

| Achievement | How |
|---|---|
| Long Orbit | Survive 5 minutes |
| Century | Pop 100 enemies in one run |
| Unbroken | Build a 25 combo |
| Untouched | Go 60 seconds without getting hit |
| Thread the Gap | Beat The Coil |
| Stem the Tide | Beat The Brood |
| Escape Velocity | Beat The Black Hole |
| Fully Loaded | Max out every upgrade in one run |
| Deep Space | Survive 10 minutes |

## On-screen buttons

Hints only show the controls you are using. Touch the keyboard or mouse and they say SHIFT, E, Q, TAB; touch a controller and they say B, X, Y, BACK. Plugging a controller in switches to controller hints. For the first 5 seconds of a run a small card under the planet shows pictures of the keys (or buttons) to move, aim, shoot and use each ability, then fades away.

## Faces

Only your planet is happy. Each of the twelve planets wears one positive emotion, the gentlest on the planet you start with and the strongest on the hardest to earn, with a face, colours and markings to match: Easewind is relieved (mint, with a breeze), Stillwater calm (pale blue, still ripples), Hearthglow content (warm peach bands), Hushmere peaceful (lavender, soft clouds), Anchorlight trusting (teal, a sturdy band), Gracebloom grateful (pink, little blossoms), Wishfall hopeful (dawn gold, a shooting star), Boldcrest confident (strong blue, a bold sash), Laurelcrown proud (purple, a golden laurel wreath), Sunburst joyful (sunny yellow, confetti), Sparkrush excited (coral, lightning and sparkles) and Heartsong loving (rose, little hearts). Every enemy wears one negative emotion, and the more dangerous the enemy, the more intense its emotion: Drifters are sad, lonely or anxious (their rock colour tells you which), Splinters doubt themselves, Shards are irritable, Fractures are frustrated, Bulwarks are ashamed, Planetoids are resentful and Flares are angry. The bosses carry the heaviest: the Coil is disgusted, the Brood grieves and the Black Hole is hopeless. Each emotion comes in a few expressions picked at random, so a crowd never looks cloned, and no enemy ever looks happy.

| Enemy | Emotion | Expressions |
|---|---|---|
| Drifter, rose rock | Sadness | forlorn, teary, sniffly, moping |
| Drifter, stone rock | Loneliness | longing, wistful, weary, forsaken |
| Drifter, clay rock | Anxiety | jittery, fretting, uneasy, tense |
| Splinter | Self-doubt | unsure, timid, shrinking |
| Shard | Irritability | huffy, twitchy, snappy, scowling (on three four-cornered shapes: a tall kite, a leaning blade, a sideways arrow) |
| Fracture | Frustration | fed up, exasperated, strained |
| Bulwark | Shame | hiding, cringing, ashamed |
| Planetoid | Resentment | grudging, bitter, brooding |
| Flare | Anger | furious, seething, roaring |
| The Coil | Disgust | disgusted, then revolted below half health |
| The Brood | Grief | mourning, then wailing below half health |
| The Black Hole | Hopelessness | empty, then despairing below half health |

Drifters come on three rock shapes in three colours mixed at random, and the colour gives the emotion. Splinters are smooth; only the Fracture they break from is cracked. They blink, look scared while they are close to your planet, and look shocked for a moment when one next to them pops. Each panics in its own way (a forlorn one dreads, a teary one bawls, a sniffly one screams, a moping one grimaces; a longing one pleads, a wistful one trembles, a weary one jolts awake, a forsaken one breaks down; a jittery one gets spiral eyes, a fretting one panics, an uneasy one bolts, a tense one freezes), at its own distance from you, and about one in five never panics at all. When a neighbour pops it shows one of four shocked faces (gasping, jaw-dropped, a double take with one eye popping, flinching). That is all looks: every enemy of a kind has the same size, speed and toughness. Each boss is bigger than the last: the Brood is 1.3 times the size of the Coil and the Black Hole 1.6 times.

When an enemy is destroyed it pops in its own colour only: a flash, a ring and a few little stars, all shades of the thing that died (a Drifter in its own rock's colour), so a whole crowd popping at once stays clean and easy to read.
