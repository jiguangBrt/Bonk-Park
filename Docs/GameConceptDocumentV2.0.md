# BONK-PARK — Game Concept Document (v2.0)

> Superseded by [v3.0](GameConceptDocumentV3.0.md). Kept for context.

Supersedes [v1.0](GameConceptDocumentV1.0.md). The story, scoring, and core loop are unchanged; the AI model and player control have been reworked in light of what was actually built. Sections present in v1.0 but omitted here (difficulty curve, art direction, five-week plan, Unity technical plan, repository structure) are still planned and live in v1.0 or in the README.

## Game Title

**BONK-PARK**

## One-Sentence Idea

A 2D pixel-art top-down chase game where the player, an firefly named **Lumi**, lures nocturnal predators into park obstacles to "bonk" them out, buying time for the next generation of fireflies to complete their light-passing ritual.

## Format

2D top-down, pixel art, mouse-driven controls, single-run survival structure with a reincarnation-based replay narrative. Target play length: 1–3 minutes per run; meant to be replayed.

---

## Story & Narrative

Late summer in a city park. Tonight is the last night before the season ends. Every year, the firefly colony performs the **light-passing ritual** — young fireflies fly to the moonlight meadow and seal their light into grass seeds. Next summer, those seeds germinate into new fireflies. This is the only way the colony continues.

Lumi is the most active firefly of the summer. It flew too far, played too long, burned too much. Even if it sealed every last spark it had left into a seed, that light could not survive until next year's germination. **Its light cannot be passed on.**

But Lumi knows every tree, every lamppost, every wind chime in the park better than any other firefly. And it remembers, from last summer, watching an older firefly do exactly what it now intends to do.

Tonight, Lumi stays behind as bait. The others fly to the meadow.

Every run, the player controls a new Lumi — a young firefly who survived the previous summer because someone else stayed behind. **The role is inherited. The name is a title, not a name.** This is why the player can restart after death: each restart is next year.

### Opening Sequence (planned, ≤ 20 words total)

> Screen 1: A small glowing dot watches a brighter light flicker and fade.
> Screen 2: One year passes. The dot is now brighter.
> Screen 3: *"Tonight, it's your turn."*

### Death Screen (planned)

> *Lumi's light went out.*
> *X sparks made it to the meadow.*
> *Next summer, they will remember.*

---

## Moment-to-Moment Gameplay

1. **Observe** the predator's heading and how its steering trails the cursor.
2. **Steer** Lumi by moving the mouse — the firefly auto-accelerates toward the cursor, slowing as it arrives.
3. **Bait** the predator toward a hard obstacle by skimming past it.
4. **Brake** with left click at the last possible moment to make a sharp turn (planned — see Controls).
5. **Witness** the predator slam into the obstacle — camera shake, a brief stun, and a velocity-preserving bounce that knocks it clear of the wall.
6. **Recover**: pick the next target while the predator slides through its stun.
7. **Survive**. Every 15 seconds saves another spark (planned).

The loop is **see the chase coming → set up the trap → pull the trigger**. The tension is constant; the release is the bonk.

---

## Core Mechanics

### Controls

| Input | Action | Status |
|---|---|---|
| **Mouse movement** | Lumi auto-accelerates toward cursor. Speed falls off as Lumi approaches and as heading-vs-desired alignment drops, so off-axis cursor swings naturally slow the firefly. Turn rate is inversely proportional to current speed: fast = wide turns, slow = sharp turns. | Implemented |
| **Cursor close to Lumi** | Lumi enters a "parked" state and decelerates to a stop in place. Cursor moving back out past a resume radius re-engages flight. Hysteresis prevents jitter. | Implemented |
| **Hold Left Click** | Emergency brake: Lumi decelerates rapidly while turning logic continues at the brake-reduced speed, so the brake becomes the means of sharp-turning at the cost of energy. | Planned |
| **Release Left Click** | Lumi launches forward at its current facing direction with whatever speed remains. | Planned |

**Core gameplay tension** (with brake): large-angle turns require holding the brake longer → more energy drained → natural stall as the cost of overcorrection. Without the brake (current build), the cursor-distance falloff and alignment-driven speed loss already supply most of the speed-vs-precision trade — adding the brake on top of that is the planned next layer of control.

### Light = Energy (planned)

The energy bar **is** Lumi's glow. No separate HUD bar — the player reads the resource directly from the character's halo size and color.

| Mechanic | Value | Visual |
|---|---|---|
| Maximum energy | 100 | Warm gold glow, large halo, particle trail |
| Brake drain rate | 40 / sec | Halo contracts during brake |
| Natural regen rate | 8 / sec | Halo slowly reforms during steady flight |
| Bonk reward | +30 | Halo flares back up |
| Zero-energy penalty | Brake at 30% effectiveness, control sluggish | Halo dim, colour fades to orange-red, slight flicker |

### Bonk Detection and Reaction

When a predator collides with a `Bonkable` obstacle, the bat reflects its pre-impact velocity off the contact normal with a retention factor (~0.3), then enters a fixed-duration stun. During the stun the bat slides along the reflected heading and decelerates so it ends the stun pointing away from the wall — this is what breaks the "stuck against the wall, re-bonk on resume" loop that the v1.0 design produced in testing (Issue #27).

Effects: camera shake, animator `Bonk` trigger, no AI input for the stun duration. The "BONK!" comic text, dedicated SFX, and white flash listed in v1.0 are still planned.

`Bonkable.StunDuration` is per-obstacle, so heavier props can be tuned to harder hits later.

### Death

One-hit kill. Any predator touching Lumi ends the run. Implementation freezes Lumi for 0.3 s and reloads the active scene. The freeze-frame + camera zoom-in and the standalone death screen from v1.0 are planned but not built.

### Spark Count — Primary Score (planned)

Every 15 seconds survived = +1 spark saved. Displayed quietly in the corner of the HUD. Bonk count is a secondary stat shown only on the death screen.

---

## AI Design

The v1.0 plan used a ring buffer of past **positions**: each AI steered toward where the player was 0.25–0.5 s ago. In testing this produced a known failure: the bat would hit an obstacle, get stuck on its far side, and oscillate against the wall because its delayed target was always on the wrong side. The system has been reworked.

### Reaction Delay on Steering, Not Position

Each AI still keeps a ring buffer, but it now stores the **instantaneous desired direction toward the player**, not the player's raw position. On each tick the AI reads the entry from `reactionDelay` seconds ago and treats that as its current intent. The result feels the same to the player — the bat lags behind feints — but the AI's target is now a direction vector, not a stale point in space, so it can't pin itself behind geometry the player has already left.

### Context Steering Layer

The delayed desired direction is the *interest* signal for a context-steering pass. The AI samples N evenly-spaced directions (default 16), scores each by:

- **Interest**: dot product with the delayed desired direction, plus a small heading-momentum bonus that prevents flip-flopping between two adjacent slots.
- **Danger**: short raycast along the slot; obstacles closer than the lookahead distance push the score down with an exponential falloff.

The highest-scoring slot becomes the new desired direction for the motion step. Tuned correctly, the bat still bonks (because interest beats danger when the player is the bait), but it stops *pinning itself* on obstacles in non-bait situations. This is the unstick behaviour from Issue #27.

### Motion

Once the desired direction is chosen, motion uses the same speed-by-alignment + speed-dependent turn-rate model as the player, plus an asymmetric acceleration/auto-brake ramp so sharp turns produce visible drift. After a bonk, the stun-slide branch overrides this entirely (see Bonk Detection).

### Predator Types

| AI | Role | Buffer | Status |
|---|---|---|---|
| **Bat** | Direct chase via echolocation | 0.1 s steering delay (was 0.4 s position delay in v1.0; the steering-direction model needed less lag to feel right) | Implemented |
| **Owl** | Predictive interceptor — extrapolates 0.5–1.0 s ahead of the player's velocity rather than chasing | 0.25 s steering delay | Planned |

A third "Herder" archetype was scoped out in v1.0 and remains out of scope.

---

## Obstacles (the Bonk Surfaces)

| Obstacle | Bonk Value | Status |
|---|---|---|
| Rock cluster | Medium | Implemented |
| Old tree trunk | High | Planned |
| Lamppost | High | Planned |
| Wind chime | High | Planned |
| Park bench | Medium | Planned |
| Stone marker | Medium | Planned |
| Pebble cluster | Low (decorative slowdown for predators; Lumi flies over) | Planned |

---

## Smallest Playable Version (MVP) — landed

The MVP target from v1.0 is now playable in `MainScene`:

- Mouse moves Lumi; left-click brake still pending.
- One Bat with steering-direction delay + context steering + bonk reflection.
- Rock prefab as the first `Bonkable`.
- Bat touching Lumi triggers death and scene reload.
- Camera shake + animator trigger on bonk; comic text and SFX still planned.

The core chase-and-bonk loop produces real tension during 30-second tests, so the central idea is proven and the rest is layering.

---

## Biggest Risk

V1.0 named the brake-to-turn control as the biggest unknown. Since the brake is not yet built, the *current* biggest risk has shifted:

**The bat's context-steering weights need playtesting under more varied geometry.**

The unstick fix from #27 was validated against the existing rock layout. As more obstacle types and tighter arrangements are added, the `dangerWeight` / `dangerFalloff` / `headingMomentumBias` parameters may need re-tuning to keep the bat aggressive enough to feel threatening without re-introducing wall-pinning. All three values are exposed as serialized fields specifically so they can be tuned live during playtests.

Brake-to-turn remains a secondary unknown — it will become the headline risk once it lands.

---

## One Visible Task Before Next Session

Add the left-click brake mechanic to the player controller (or, if a playtest reveals that the brake is no longer needed given the current cursor-distance and alignment falloffs, document that decision and remove it from the design).

---

## Scope Sorting (MoSCoW)

### Must Have — Without these, the game does not exist

| Feature | Why it is essential | Status |
|---|---|---|
| Mouse-driven movement | The entire control identity depends on this | Implemented |
| Bat AI (steering-delay + context steering) | The chase has no character without buffered, obstacle-aware responses | Implemented |
| Bonk reflection + stun | The game is literally named after this mechanic | Implemented |
| Left-click brake-to-turn | Defining the control identity past the cursor follow | Planned |
| Light = energy visual link | Narrative-mechanic core; defines the game's identity | Planned |
| One-hit-kill death | Closes the game loop | Implemented |
| Death screen (X sparks made it to the meadow) | Closes the narrative loop | Planned |
| Spark count (every 15s = +1) | The player's primary scoring narrative | Planned |
| One greybox arena with 4+ obstacles | Bonk needs surfaces; the chase needs space | Partial — one rock so far |
| Main menu + opening + death screen | Assessment requires a complete game flow | Planned |
| Basic audio (Bonk, brake, death, BGM) | Assessment lists audio as a required system | Planned |
| Camera shake + freeze frame on Bonk/death | Without juice the core mechanic falls flat | Shake implemented; freeze frame planned |
| GitHub repo with steady commits from Week 1 | Assessment: "A last minute upload is not the same as steady development" | On track |
| README explaining game, controls, story, credits | Assessment requirement | Implemented |

### Should Have — Strongly expected for a high mark

| Feature | Why it matters |
|---|---|
| Owl AI (predictive Interceptor) | Adds depth; shows AI variety on top of one shared framework |
| Difficulty curve over time | Assessment values progression and pacing evidence |
| Glow halo dynamically scaling with energy | The "light = energy" link only works if the player sees it |
| Pixel art replacing placeholder shapes | Visual polish for demo and presentation |
| Pause screen with unpause + return to menu | Covered in class; expected by assessor |
| Responsive UI (Canvas Scaler + anchors) | Taught in class; technical-competence signal |
| Looping ambient night BGM | Greatly improves presentation atmosphere |
| Playtest notes documented in repo | Assessment: "evidence of testing and what you changed because of it" |
| Credits screen listing all external assets | Assessment: legal / ethical awareness |
| Predators distinguished by silhouette + motion (not colour alone) | Assessment: accessibility awareness |
| Opening pixel sequence | Defines tone before gameplay starts |

### Could Have — Nice extras if time allows

| Feature | Benefit |
|---|---|
| Lumi trail particles scaling with speed | Visual richness; speed feedback |
| "+1 spark" mini-animation every 15s | Reinforces narrative cadence |
| Multiple obstacle types with unique bonk effects | Lamppost flicker, wind-chime ripple add personality |
| Ambient dynamic sound (distant chimes, owl hoot) | Atmosphere boost |
| Custom cursor (glowing reticle) | Class-covered; minor polish |
| Low-energy red-flash warning | Accessibility + tension |
| Death-screen run statistics (best survival, total bonks across runs) | Replay motivation |
| Static high score via PlayerPrefs | Class-covered; adds run-to-run progression |

### Cut First — Remove immediately if behind schedule

| Feature | Why it can go |
|---|---|
| Herder AI (third predator type) | Already scoped out due to complexity |
| "Light = vision radius" mechanic | Interesting but out of scope for a vertical slice |
| Multiple arenas | One polished arena is a stronger slice than two unfinished ones |
| Story cutscenes beyond the opening | Death screen + opening already carry the narrative |
| Persistent meta-progression (unlocks, upgrades) | Roguelike scope creep; not required by assessment |
| Online leaderboards | Out of scope |
| Controller / gamepad support | Mouse is core; alternative input is later |
| Multiple Lumi skins | Pure cosmetic; zero impact on assessment |
| Dynamic weather / day-night cycle | Single setting is intentional and sufficient |

---

## Accessibility, Legal, and Ethical Considerations

- **Accessibility**: predators distinguished by silhouette AND motion pattern, not colour alone. Bonk feedback is multimodal (visual flash + camera shake + audio). Critical HUD signals (sparks, low-energy warning) use shape and motion in addition to colour.
- **Legal**: all assets either self-made, public-domain, or licensed under Creative Commons with proper attribution in both the credits screen and the GitHub README.
- **Ethical**: the narrative engages with mortality and sacrifice, but frames them as part of a hopeful cycle of renewal rather than as bleak or hopeless content. Appropriate for the 16–18 target audience and avoids gratuitous distress.
- **Security**: no online features, no data collection, no user accounts — there is no security surface beyond standard Unity build hygiene.
