# BONK-PARK — Game Concept Document

## Game Title

**BONK-PARK**

## One-Sentence Idea

A 2D pixel-art top-down chase game where the player, an aging firefly named **Lumi**, lures nocturnal predators into park obstacles to "bonk" them out, buying time for the next generation of fireflies to complete their light-passing ritual.

## Format

2D top-down, pixel art, mouse-driven controls, single-run survival structure with a reincarnation-based replay narrative. Target play length: 1–3 minutes per run; meant to be replayed.

---

## Story & Narrative

Late summer in a city park. Tonight is the last night before the season ends. Every year, the firefly colony performs the **light-passing ritual** — young fireflies fly to the moonlight meadow and seal their light into grass seeds. Next summer, those seeds germinate into new fireflies. This is the only way the colony continues.

Lumi is the most active firefly of the summer. It flew too far, played too long, burned too much. Even if it sealed every last spark it had left into a seed, that light could not survive until next year's germination. **Its light cannot be passed on.**

But Lumi knows every tree, every lamppost, every wind chime in the park better than any other firefly. And it remembers, from last summer, watching an older firefly do exactly what it now intends to do.

Tonight, Lumi stays behind as bait. The others fly to the meadow.

Every run, the player controls a new Lumi — a young firefly who survived the previous summer because someone else stayed behind. **The role is inherited. The name is a title, not a name.** This is why the player can restart after death: each restart is next year.

### Opening Sequence (2–3 pixel screens, ≤ 20 words total)

> Screen 1: A small glowing dot watches a brighter light flicker and fade.
> Screen 2: One year passes. The dot is now brighter.
> Screen 3: *"Tonight, it's your turn."*

### Death Screen

> *Lumi's light went out.*
> *X sparks made it to the meadow.*
> *Next summer, they will remember.*

---

## Moment-to-Moment Gameplay

1. **Observe** the predator's position, heading, and reaction lag.
2. **Steer** Lumi by moving the mouse — the firefly auto-accelerates toward the cursor.
3. **Bait** the predator toward a hard obstacle by flying close to it.
4. **Brake** with left click at the last possible moment to make a sharp turn — light drains as Lumi pivots hard.
5. **Witness** the predator slam into the obstacle — comic-style "BONK!" text, camera shake, sound effect.
6. **Recover** energy from the Bonk reward; the predator stuns for a moment; pick the next target.
7. **Survive** as long as possible. Every 15 seconds saves another spark.

The loop is **see the chase coming → set up the trap → pull the trigger**. The tension is constant; the release is the Bonk.

---

## Core Mechanics

### Controls

| Input | Action |
|---|---|
| **Mouse movement** | Lumi auto-accelerates toward cursor. Turn rate is inversely proportional to current speed: fast = wide turns, slow = sharp turns. |
| **Hold Left Click** | Emergency brake. Lumi decelerates rapidly. Turning logic still applies, but at the brake-reduced speed (so the brake becomes the means of sharp-turning). |
| **Release Left Click** | Lumi launches forward at its current facing direction with whatever speed remains. |
| **Zero speed** | Lumi can rotate freely in place. Releasing brake launches from zero. |

**Core gameplay tension**: large-angle turns require holding the brake longer → more energy drained → natural stall as the cost of overcorrection. The player must learn when a turn is "worth it".

### Light = Energy

The energy bar **is** Lumi's glow. No separate HUD bar — the player reads the resource directly from the character's halo size and color.

| Mechanic | Value | Visual |
|---|---|---|
| Maximum energy | 100 | Warm gold glow, large halo, particle trail |
| Brake drain rate | 40 / sec | Halo contracts during brake |
| Natural regen rate | 8 / sec | Halo slowly reforms during steady flight |
| Bonk reward | +30 | Halo flares back up — hope rekindles |
| Zero-energy penalty | Brake at 30% effectiveness, control sluggish | Halo dim, color fades to orange-red, slight flicker |

### Bonk Detection

When a predator's velocity exceeds a threshold AND its collision angle with an obstacle exceeds a threshold → Bonk triggered.

Effects: predator stunned 1.5–2.5s, score +1, "BONK!" comic text, camera shake, dedicated sound effect, white flash.

### Death

One-hit kill. Any predator touching Lumi ends the run. Triggers 0.3s freeze frame + camera zoom-in on the contact point before transitioning to the death screen.

### Spark Count (Primary Score)

Every 15 seconds survived = +1 spark saved. Displayed quietly in the corner of the HUD. Bonk count is a secondary stat shown only on the death screen.

---

## AI Design

### Reaction Buffer System

Each AI reads the player's position/velocity from a **delayed snapshot** (0.25–0.5s ago) stored in a ring buffer. This creates natural-feeling responses to player feints, makes the chase feel "alive", and rewards deceptive movement.

### Predator Types

| AI | Role | Max Speed | Accel | Turn Rate | Buffer | Behavior |
|---|---|---|---|---|---|---|
| **Bat** | Hound (direct chase via echolocation) | 13 | 12 | 120°/s | 0.4s | Steers toward buffered position |
| **Owl** | Interceptor (predictive dive) | 11 | 18 | 200°/s | 0.25s | Extrapolates 0.5–1.0s ahead of buffered velocity |

Player baseline: max speed 12, accel 15, turn rate 360°/s at low speed → 90°/s at high speed.

**Note**: a third "Herder" AI was scoped out as too complex for the time budget.

### Difficulty Curve

| Time | Event |
|---|---|
| 0s | 1× Bat, buffer 0.5s |
| 20s | Bat buffer → 0.4s |
| 40s | Add 1× Owl |
| 60s | Bat max speed +1, buffer → 0.3s |
| 80s+ | Both AI max speed +0.5 every 20s |

0–40s = learning zone. 40–60s = challenge zone. 60s+ = survival zone.

---

## Setting & Art Direction

**Environment**: late-summer city park, night. Dark base with dynamic point-light from Lumi. Distant ambient sources (lamppost glow, moonlight) hint at depth.

**Obstacles** (the Bonk surfaces):

| Obstacle | Bonk Value | Visual Effect |
|---|---|---|
| Old tree trunk | High | Bark shudders, leaves shake, distant bird calls |
| Lamppost | High | Bulb flickers, briefly extinguishes, then relights |
| Wind chime (Owl-only) | High | Chime sound + light ripple |
| Park bench | Medium | Wood splinters effect |
| Stone marker | Medium | Sparks + echo |
| Pebble cluster | Low (decorative slowdown for predators only — Lumi flies over) | Small dust puff |

**Palette**:
- Lumi: warm gold → fading orange-red as light depletes
- Bat: dark grey silhouette with subtle red eye-glint
- Owl: pale tan, large but quiet
- Park: muted greens, browns, navy night sky
- Bonk flash: high-contrast white

---

## Smallest Playable Version (MVP)

One scene, one Bat AI, one obstacle, four core scripts, all placeholder shapes:

```
[dark background] — [Lumi (glowing yellow dot)] — [tree pillar (brown square)] — [Bat (red circle)]
```

**Behavior to validate**:
- Mouse moves the dot; left click brakes; releasing relaunches.
- Bat reads Lumi's position from 0.4s ago and steers toward it.
- Bat collides with the tree pillar above a speed threshold → stuns 2s → "BONK!" text appears → score increments.
- Bat touches Lumi → game ends.

If this loop produces real tension during a 30-second test, the core idea is proven. **No art, no audio, no menus required at this stage.**

---

## Vertical Slice — Five-Week Plan

| Week | Goal | Deliverable |
|---|---|---|
| 1 | Core movement + one Bat | Mouse-driven Lumi, brake mechanic, one Bat with reaction buffer, one obstacle, working Bonk detection (placeholder shapes only) |
| 2 | Energy + spark system | Light-as-energy visual link, energy drain/regen, Bonk reward, spark counter (+1 per 15s), death detection, basic HUD |
| 3 | Greybox arena + Owl | Full 40×30 unit arena with 4–6 obstacles, Owl AI with prediction, difficulty timer triggering AI changes over time |
| 4 | Art + audio + UI | Pixel art for Lumi/Bat/Owl/obstacles, glow effect, ambient BGM, SFX (bonk, brake, death, low-energy), main menu, pause, death screen, opening sequence |
| 5 | Test, polish, submit | Two rounds of external playtesting, parameter tuning from feedback, bug fixes, Windows + WebGL builds, written report |

**Final deliverable**: one polished arena, complete game loop (menu → opening → play → death → menu), 1–3 minutes per run, infinitely replayable.

---

## Unity Technical Plan

### Player Control

| Script | Purpose |
|---|---|
| `PlayerController.cs` | Reads mouse position, calculates target direction, applies acceleration and speed-dependent turn rate |
| `BrakeSystem.cs` | Handles left-click hold/release, drains energy, modifies turn behavior during brake |
| `LightSystem.cs` | Tracks current energy, drives glow halo size + color, applies low-energy control penalty |

### AI

| Script | Purpose |
|---|---|
| `ReactionBuffer.cs` | Records player position + velocity into a ring buffer; provides delayed-snapshot lookups |
| `BatAI.cs` | Reads buffered position, steers toward it with limited turn rate |
| `OwlAI.cs` | Reads buffered velocity, extrapolates predicted position, steers toward it |
| `BonkSystem.cs` | `OnCollisionEnter2D`: checks velocity + angle thresholds, applies stun, triggers feedback |
| `DifficultyManager.cs` | Time-based scheduler; modifies AI parameters and spawns new AI at defined intervals |

### Game Management

| Script | Purpose |
|---|---|
| `GameManager.cs` | Tracks survival time, spark count, Bonk count; manages run start/end and restart |
| `SparkCounter.cs` | Increments spark count every 15s; displays in HUD; handles death-screen reporting |
| `LevelLoader.cs` | Scene transitions: Menu ↔ Opening ↔ Arena ↔ DeathScreen |

### Feedback Systems

| Category | Implementation |
|---|---|
| **UI** | `UIManager.cs` — main menu, HUD (sparks + survival timer), pause, death screen using Canvas + UGUI; Canvas Scaler set to "Scale With Screen Size" |
| **Audio** | `AudioManager.cs` — looping ambient night BGM, one-shot SFX (Bonk, brake, death, low-energy warning, opening chime) |
| **Visual** | Lumi glow via Unity 2D lights or layered transparent sprites; particle systems for trail, Bonk impact, brake puff; camera shake on Bonk and death |

---

## GitHub Repository Plan

```
bonk-park/
├── README.md                — Game description, controls, story, how to run, credits
├── Assets/
│   ├── _Scenes/             — MainMenu, Opening, Arena, DeathScreen
│   ├── Scripts/
│   │   ├── Player/          — PlayerController, BrakeSystem, LightSystem
│   │   ├── AI/              — ReactionBuffer, BatAI, OwlAI, BonkSystem, DifficultyManager
│   │   ├── Management/      — GameManager, SparkCounter, LevelLoader, UIManager, AudioManager
│   │   └── Environment/     — Obstacle behavior, ambient elements
│   ├── Prefabs/             — Lumi, Bat, Owl, Tree, Lamppost, WindChime, Bench, StoneMarker
│   ├── Art/                 — Pixel sprites, glow shaders, particle textures
│   ├── Audio/               — BGM, SFX
│   └── Animations/          — Lumi flicker, Bonk stun, opening sequence
└── Docs/
    ├── GameConceptDocument.md
    ├── PlaytestNotes/       — Dated playtest sessions and what changed because of them
    └── DesignDiary/         — Weekly progress notes
```

**Repository link**: *[to be created in Week 1]*

---

## Biggest Risk

**The "mouse drives the character + brake-to-turn-sharp" control scheme may not feel right.**

Everything else in this project — energy tracking, AI steering, scoring, UI, audio — is well-trodden Unity territory. The single unknown is whether the speed-dependent turn rate combined with the brake-to-sharp-turn model produces the intended tension between "go fast and feel chased" and "slow down to maneuver".

Specific failure modes:

- **Brake-duration confusion**: players may not intuit that "holding longer = sharper turn". They might tap-spam expecting discrete dashes.
- **Input scaling issues**: mouse DPI and screen size affect how fast players can swing the cursor. Turn rate must scale to screen-space mouse delta, not raw pixel delta.
- **Reaction buffer feels wrong**: too short, the Bat is unbeatable; too long, Bonks happen by accident. The correct value is only knowable through testing.
- **"It just isn't fun"**: every chase game lives or dies by feel. Only playtesting reveals this.

**Mitigation**: build the MVP in Week 1 using only placeholder shapes and the four core scripts. Test the chase loop on at least three external playtesters before adding anything else. If brake-to-turn feels wrong, prototype alternative control models (WASD comparison, single-tap dash) while scope is still small enough to pivot. Expose all numeric parameters (turn rate, brake drain, buffer length, AI speeds) as serialized fields and tune live during playtests.

---

## One Visible Task Before Next Session

Build and push the MVP prototype to GitHub:

- One scene
- Lumi as a yellow circle, Bat as a red circle, tree as a brown square
- Mouse-driven movement, brake mechanic, reaction buffer chase, Bonk detection
- Console output of Bonk events for verification

First commit message: `feat: core chase + bonk MVP with one Bat and one obstacle`

---

## Scope Sorting (MoSCoW)

### Must Have — Without these, the game does not exist

| Feature | Why it is essential |
|---|---|
| Mouse-driven movement + brake-to-turn control | The entire control identity depends on this |
| Reaction Buffer AI (at least Bat) | The chase has no character without buffered responses |
| Bonk detection (velocity + angle thresholds) | The game is literally named after this mechanic |
| Light = energy visual link | Narrative-mechanic core; defines the game's identity |
| One-hit-kill death + death screen | Closes the game loop |
| Spark count (every 15s = +1) | The player's primary scoring narrative |
| One greybox arena with 4+ obstacles | Bonk needs surfaces; the chase needs space |
| Main menu + opening + death screen | Assessment requires a complete game flow |
| Basic audio (Bonk, brake, death, BGM) | Assessment lists audio as a required system |
| Camera shake + freeze frame on Bonk/death | Without juice, the core mechanic falls flat |
| GitHub repo with steady commits from Week 1 | Assessment: "A last minute upload is not the same as steady development" |
| README explaining game, controls, story, credits | Assessment requirement |

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
| Predators distinguished by silhouette + motion (not color alone) | Assessment: accessibility awareness |
| Opening pixel sequence | Defines tone before gameplay starts |

### Could Have — Nice extras if time allows in Week 4–5

| Feature | Benefit |
|---|---|
| Lumi trail particles scaling with speed | Visual richness; speed feedback |
| "+1 spark" mini-animation every 15s | Reinforces narrative cadence |
| Multiple obstacle types with unique Bonk effects | Lamppost flicker, wind-chime ripple add personality |
| Ambient dynamic sound (distant chimes, owl hoot) | Atmosphere boost |
| Custom cursor (glowing reticle) | Class-covered; minor polish |
| Low-energy red-flash warning | Accessibility + tension |
| Death-screen run statistics (best survival, total Bonks across runs) | Replay motivation |
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

- **Accessibility**: predators distinguished by silhouette AND motion pattern, not color alone. Bonk feedback is multimodal (visual flash + camera shake + audio). Critical HUD signals (sparks, low-energy warning) use shape and motion in addition to color.
- **Legal**: all assets either self-made, public-domain, or licensed under Creative Commons with proper attribution in both the credits screen and the GitHub README.
- **Ethical**: the narrative engages with mortality and sacrifice, but frames them as part of a hopeful cycle of renewal rather than as bleak or hopeless content. Appropriate for the 16–18 target audience and avoids gratuitous distress.
- **Security**: no online features, no data collection, no user accounts — there is no security surface beyond standard Unity build hygiene.
