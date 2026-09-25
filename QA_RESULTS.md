# Arcade balance and circular-shield verification — 2026-09-24

Windows x64 candidate **1.0.0-rc4**, Godot 4.7.1 .NET, RTX 3070.

- Fresh debug build and integration suite: **98 passes, zero failures**, `builds/qa/rc4-qa.log`. Five original balance regressions failed before implementation (`rc4-red.log`); three offer/skip regressions then failed before their fixes (`rc4-offers-red.log`). Tests cover delayed spread ranks, actual 2/3 projectile counts, starter cadence, guaranteed standard options, at most one weapon swap per offer, and skipping without changing the build or leaving simulation paused.
- A 150-second rendered automated run deliberately selected only standard upgrades and skipped weapon/spread offers. It reached wave 11, 158 kills, score 35,715 and 142.07 simulation seconds. The final state was starter weapon 0, spread level 0, one boss defeated (`rc4-focused-final.log`). Average frame interval 16.67 ms, worst 41.63 ms, peak 590 nodes. Invulnerability was enabled: this proves functional progression without spread, not ordinary human survivability or subjective enjoyment.
- Inspected rendered menu without the tagline, circular shield gameplay, and opening upgrade screen with the skip button (`rc4-menu.png`, `rc4-shield.png`, `rc4-boost.png`). In-game/menu/credits strings and cover-generation source no longer contain the removed tagline or old spread name.
- Godot MCP launched and stopped real menu and game scenes; all debug/final error arrays were empty (`rc4-mcp.log`).
- Fresh staged compilation, import and release export completed (`rc4-export.log`). The first export attempt hit system memory pressure; retry used reduced compiler concurrency after playtest completion. Shutting down the build server released the console wrapper after export. No user applications were closed.
- ZIP CRC passed: 195 files, 73,755,563 bytes. SHA-256 `12a5bbb2e6f74661d68667654105ac3a4ee34c04dea3084657d5145d518fe0fb`.
- Extracted package default-menu startup exited 0 (`rc4-extracted-menu.log`). Export templates reject command-line scene overrides, so that unsupported invocation was discarded. The rendered standalone build was opened and Enter started gameplay; resource loading confirms arena assets (`rc4-standalone.log`). The game was left open.
- QA staging now stays under the ignored builds directory, and C# compilation excludes copied build/test projects. The QA runner builds first and refuses stale assemblies after compilation failure. An earlier temporary playtest copy lost required files and was replaced before the final successful run.
- Source whitespace check passed. No physical-controller, low-end hardware, or human difficulty validation claimed. Nothing was published.

Full current catalog: GAMEPLAY_GUIDE.md. Earlier records below are historical.

---

# Art and progression verification — 2026-09-24

Windows x64 candidate **1.0.0-rc3**, Godot 4.7.1 .NET, RTX 3070 / Vulkan Mobile renderer.

- Debug build: zero warnings and errors. Integration suite: **89 passes, zero failures** (`builds/qa/rc3-qa.log`). The five original progression/art regressions failed before implementation (`rc3-red.log`). Added coverage includes milestone order/idempotence, live catalog/drop cuts, stable difficulty within a wave, bounded late-run speed, boss wave gates, rotating planet art and early offer prerequisites.
- Godot MCP launched the real menu and arena with the .NET engine; both debug and final error arrays were empty (`rc3-mcp.log`).
- Inspected rendered menu at 1280×720 and 1024×768, first boost choice, and accelerated black-hole encounter. The character face uses closed smooth shapes, simplified eyes and peripheral crater detail across all twelve palettes.
- A 200-second invulnerable automated-input run reached wave 11, 170 kills and score 37,499, continuing past the first boss. Simulation time 191.39 seconds; average frame interval 16.71 ms, worst 166.58 ms, peak 803 nodes (`rc3-long-run.log`). This is functional/performance evidence, not human survival or subjective fun. Invulnerability lets enemies overlap the player, so that capture is not representative of normal collision recovery. The later removal of the redundant player flip was covered by the final suite and shorter captures.
- Fresh staging compilation/import/export completed, with no warning/error entries beyond build totals (`rc3-export.log`). Superseded gameplay, pause and results scene visuals were removed; unused old sprites are excluded from the export.
- ZIP CRC passed for all 194 files, 73,748,757 bytes. SHA-256: `6b3ac74541157c5358306ea1ca6e9ec0c09c4860e0f1ec448ba5894dfb85f0a1`.
- Extracted package headless menu startup exited 0 and loaded the new art (`rc3-extracted.log`). The rendered standalone build was opened and its resource log records menu, cosmetics, and gameplay loading (`rc3-standalone.log`). The user interacted with it; no automated standalone retry claim is made and it was left open.
- Source whitespace check passed. No physical-controller or low-end hardware validation claimed. Nothing was published.

The main-menu preservation rule was superseded by the user's explicit redesign request. The historical rc2/rc1 records below describe earlier builds.

---

# Visual-overhaul verification — 2026-09-24

Windows x64 candidate **1.0.0-rc2**, Godot 4.7.1 .NET, RTX 3070 / Vulkan Mobile renderer.

- Debug compilation: zero warnings or errors.
- Integration suite: **74 passes, zero failures** (`builds/qa/overhaul-final-qa.log`). Includes untimed boost pause, suppressed pickup announcements, exclusive pause layering, restored frozen choice and visible focus after resume, and resumption after one choice. Four overlap/state checks failed before implementation (`ui-overhaul-red.log`).
- Godot MCP launched/stopped the real menu and game using the .NET engine. Both debug error arrays were empty (`overhaul-mcp.log`).
- Fresh staging build/import/release export completed successfully (`overhaul-export.log`).
- Main-menu scene hash remains `af7b48985cb911d51ce0d0245e079c6a4e9988ad50939aad2cedf26f155fdd2d`, matching the pre-overhaul snapshot.
- Rendered and inspected 1280×720 boost, pause, settings, controls, accessibility, credits and results screens; 1024×768 pause settings; 1600×900 fights against all three bosses. Boss timing was accelerated for inspection.
- A 90-second automated input run with 130% HUD text reached wave 9, 104 kills and score 21,472 (84.73 simulation seconds after impact slowdowns). Average frame interval 16.68 ms at the 60 FPS cap; worst 65.97 ms; peak 348 nodes. It uses invulnerability for repeatable sustained rendering and is not a human survival claim.
- The original menu and the new gameplay asset set load in the standalone export. Enter started gameplay; the log subsequently records results and a second gameplay load (`overhaul-standalone.log`). User input was detected during the retry check, so no claim is made that the retry was caused by automated input; the game was left open.
- ZIP CRC validation passed for all 194 files (74,302,478 bytes). The ZIP was extracted to a separate temporary directory and its headless startup exited 0 (`overhaul-extracted.log`).
- Source whitespace check passed.

Screenshots and detailed logs are in `builds/qa/overhaul-*`. The rendered checks identified and corrected excessive announcement width, focus shading, pause/settings layout, and the blaster/muzzle-flash placement.

No physical-controller or low-end-PC test is claimed. Automated play demonstrates operation and performance on this machine; subjective difficulty and enjoyment still benefit from human feedback. Leaderboards remain local. Nothing was published.

The rc1 record below describes the prior release and is retained as history; rc2 results above supersede its visual/UI claims.

---

# Release verification - 2026-09-23

Windows x64 release candidate 1.0.0-rc1. Engine: Godot 4.7.1 .NET. Runtime: .NET 8.0.28. GPU: NVIDIA RTX 3070, Vulkan Mobile renderer.

## Executed checks

| Check | Evidence / result |
|---|---|
| Debug compilation | 0 warnings, 0 errors |
| Fresh staging copy import | `builds/qa/clean-import.log`, clean import without an existing .godot cache |
| Integration suite | **68 passing checks**, `QA RESULT: 0 failure(s)`, `builds/qa/regression.log` |
| Godot MCP | Real menu and game launches, debug-output reads, stops; both error arrays empty in `builds/qa/mcp-final.log` |
| Release export | Exit 0, `builds/qa/export-final.log`; exported from a clean staging project with --quit |
| Standalone menu | Rendered outside the editor, exit 0, `builds/qa/standalone-menu.log` |
| Standalone interaction | Keyboard Enter started a run; normal collision reached results; Enter started another run. Verbose resource-load log confirms both game scene loads and results: `builds/qa/standalone-journey.log` |
| ZIP integrity | CRC check passed; executable, PCK and complete self-contained runtime present, 194 files |
| Fresh ZIP extraction | Rendered launch from E:/Temp/Grinfinity-package-validation-rc1; exit 0, `builds/qa/extracted-package.log` |
| Source whitespace | git diff --check passed |

The integration suite exercises invalid records/names, ordered ties across leaderboard reload, top-ten limits, safe saved-value defaults, settings round-trip, score backup recovery, free upgrades, prerequisite gating, starting shield, pickup activation/expiry, weapon equipping, real spread-projectile count, upgrade caps, HUD/control existence, diagonal movement, duplicate-choice rejection, focus pause, safe boss arrival, each boss's single defeat/bonus, simultaneous-hit protection, sustained contact after protection expires, and three full menu/play/pause/resume/death/results/retry journeys. It also loads settings, controls, accessibility, leaderboard, stats and credits. QA uses separate save files; it does not clear the owner's records.

## Rendered playtests and inspected captures

- Opening wave: six kills and first free choice at about 8.8 survival seconds.
- 90-second input-driven run: reached wave nine; inspected rings, moons, HUD and upgrades.
- 225-second run: 216.3 survival seconds after hitstop, wave ten, 161 kills, score 35,980. Peak 253 scene nodes, average frame interval 16.67 ms, worst 31.95 ms at a 60 FPS cap. This bot can become inefficient against armoured enemies; it is not evidence of ideal human play or late-game completion.
- 90-second uncapped run on the RTX 3070: average 0.68 ms frame interval, six intervals over 16.67 ms, worst 275.21 ms. Startup/compilation outliers are included after the initial 60 frames. This is a light-load measurement, not a minimum hardware guarantee.
- All three bosses ran with actual attacks and shooting. Their timings were accelerated for repeatable capture; the integration suite separately checks defeat and reward handling.
- Mortal, non-shooting run lost its shield and reached the score/leaderboard results naturally.
- 1280x720 gameplay/choices, 1024x768 menus/stats/pause settings/results, and 2560x1080 Coil encounter with 130% HUD text were rendered and visually inspected.
- Corrected the pause settings' tiny default font after inspecting the capture. Final capture: `builds/qa/options-final.png`.
- Store cover is rendered in Godot from the existing planet art and game font. Store screenshots are real captures; `builds/store/screenshots.txt` documents automation conditions.

## Issues found and resolved in the final pass

- A shield/dash overlap could remain safe forever because BodyEntered did not fire again. The new sustained-contact regression failed before the fix (`contact-red.log`) and passes now. Existing contacts are rechecked after protection expires.
- The exported menu referenced old excluded font/audio resources after the open editor rewrote that scene. Corrected the disk references and rebuilt from a fresh staging copy. The initial failed standalone log was replaced by the successful final menu test. Keep the editor closed or synchronized when building.
- Explicit --quit is used with command-line export to avoid a headless exporter lingering after packaging.
- The release engine disallows command-line scene-path overrides. Standalone gameplay was therefore entered using the actual menu and keyboard, rather than claiming a forced scene launch passed.

## Limits stated explicitly

Only the single endless mode is in scope. No global online service exists; the local board is fully offline. No physical controller or low-end machine was available for validation. No claim of universally enjoyable balance or exhaustive long-session leak detection is made. Audio resources loaded and playback paths ran; a listening test on the owner's preferred output remains useful.

Windows Computer Use screenshot capture failed with `SetIsBorderRequired: No such interface supported (0x80004002)`. Its keyboard/accessibility path still allowed direct release-menu and retry input; verbose engine scene-load records provide the transition evidence. Visual inspection used the in-engine rendered PNG captures. The verbose release run reports one Vulkan loader registry warning and a skipped system certificate; normal standalone logs contain no warnings or errors, and no gameplay script errors were observed.

No public upload, signing or Steam integration was performed. See RELEASE.md for publishing steps and exact external dependencies.
