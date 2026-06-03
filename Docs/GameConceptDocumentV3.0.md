# BONK-PARK — Game Concept Document (v3.0)

Supersedes [v2.0](GameConceptDocumentV2.0.md). The story, the chase, and the bonk are unchanged. What changed is the resource layer: the left-click brake is cut, the energy bar is reworked from a passive regen meter into a fly-to-survive economy, and scoring is reframed as a narrative beat instead of a number. These changes came directly out of class playtests (see [Why this changed](#why-this-changed)). Sections present in earlier versions but omitted here (difficulty curve, art direction, five-week plan, Unity technical plan, repository structure) are still planned and live in v1.0 or in the README.

## Game Title

**BONK-PARK**

## One-Sentence Idea

A 2D pixel-art top-down chase game where the player, a firefly named **Lumi**, lures nocturnal predators into park obstacles to "bonk" them out, buying time for the next generation of fireflies to complete their light-passing ritual.

## Format

2D top-down, pixel art, mouse-driven controls, single-run survival structure with a reincarnation-based replay narrative. Target play length: 1–3 minutes per run; meant to be replayed.

---

## Story & Narrative

Late summer in a city park. Tonight is the last night before the season ends. Every year, the firefly colony performs the **light-passing ritual** — young fireflies fly to the moonlight meadow and seal their light into grass seeds. Next summer, those seeds germinate into new fireflies. This is the only way the colony continues.

Lumi is the most active firefly of the summer. It flew too far, played too long, burned too much. Even if it sealed every last spark it had left into a seed, that light could not survive until next year's germination. **Its light cannot be passed on.**

But Lumi knows every tree, every lamppost, every bush in the park better than any other firefly. And it remembers, from last summer, watching an older firefly do exactly what it now intends to do.

Tonight, Lumi stays behind as bait. The others fly to the meadow.

Every run, the player controls a new Lumi — a young firefly who survived the previous summer because someone else stayed behind. **The role is inherited. The name is a title, not a name.** This is why the player can restart after death: each restart is next year.

---

## Moment-to-Moment Gameplay

1. **Observe** the predator's heading and how its steering trails the cursor.
2. **Steer** Lumi by moving the mouse — the firefly auto-accelerates toward the cursor, slowing as it arrives. Flying burns light the whole time.
3. **Bait** the predator toward a hard obstacle by skimming past it.
4. **Witness** the predator slam into the obstacle — camera shake, a brief stun, a velocity-preserving bounce that knocks it clear, and a burst of light scattering from the impact.
5. **Refuel** by sweeping Lumi through that scattered light, or by detouring to a glow mote that has drifted into the park.
6. **Dash** with left click to break contact or close a gap, spending a chunk of light for the burst.
7. **Recover**: pick the next target while the predator slides through its stun, and watch the glow so it never runs dry.
8. **Survive**. Every few seconds buys another friend a safe trip to the meadow.

The loop is **see the chase coming → set up the trap → spring it → live off what the trap drops**. The bonk is no longer only the release; it is also how Lumi stays lit. Running away forever is not an option, because flying costs light and only the chase pays it back.

---

## Core Mechanics

### Controls

| Input | Action | Status |
|---|---|---|
| **Mouse movement** | Lumi auto-accelerates toward the cursor. Speed falls off as Lumi approaches and as heading-vs-desired alignment drops, so off-axis cursor swings naturally slow the firefly. Turn rate is inversely proportional to current speed: fast = wide turns, slow = sharp turns. | Implemented |
| **Cursor close to Lumi** | Lumi enters a "parked" state and decelerates to a stop in place. Cursor moving back out past a resume radius re-engages flight. Hysteresis prevents jitter. | Implemented |
| **Left Click** | Dash: a short forward burst at the current heading that spends a fixed chunk of light, with a brief cooldown. Used to break a predator's contact, close a gap to a glow mote, or commit to a tight bait line. Disabled when light is below the dash threshold. | Planned |

The brake-to-turn control from v2.0 is gone — see below.

### Why this changed

Two findings from class playtests drove this version:

1. **The brake felt wrong.** Testers found the hold-to-brake-and-turn control unintuitive, and discovered that the existing mouse model already covers sharp turns: drift the cursor near Lumi to bleed speed, then swing it hard to snap around. The cursor-distance and alignment falloffs were doing the brake's job already, so the brake added a confusing second way to do the same thing. It is cut. Left click is repurposed into a **dash** — an offensive/escape burst rather than a defensive slowdown.

2. **Stalling was a dominant strategy.** Because v2.0 energy regenerated passively during steady flight, a player could orbit a single safe spot, let the bat fail to reach them, and never engage the obstacles at all. The bonk — the whole point of the game — became optional. Energy is therefore reworked so that **simply flying drains it and the only meaningful way to refuel is to engage**: bonk a predator for the light it drops, or break position to collect a drifting glow mote. Sitting still is no longer safe, because sitting still still costs light.

### Light = Energy

The energy bar **is** Lumi's glow. There is no separate HUD bar — the player reads the resource directly from the halo's size, colour, and flicker.

Energy is now a closed economy. It only goes out through play and only comes back through engagement:

**Drains**

| Source | Behaviour |
|---|---|
| Flight | Continuous drain while moving. This is the clock the whole run runs against. |
| Dash | A fixed chunk per dash, on top of the flight drain. |

**Gains**

| Source | Behaviour |
|---|---|
| Bonk light | When a predator bonks an obstacle, light scatters from the impact point. Lumi absorbs it automatically by sweeping through the absorb radius. This is the primary, high-yield refuel and the reason to keep baiting. |
| Glow motes | Pickups that spawn at random points in the park and drift. Flying through one tops Lumi up. This is the safety-net refuel — it keeps a new player alive long enough to learn the bonk, but it is lower-yield and exposes Lumi to the chase to reach it. |

**Low-light penalty (no death from energy)**

Below a threshold, Lumi's flight speed is scaled down a **steepening curve** — the lower the light, the sharper the loss, so the last stretch toward empty leaves Lumi barely able to crawl. The glow nearly goes out and dash is locked. Lumi **never dies from low energy directly.** Instead the death spiral is emergent: a near-empty firefly is too slow to escape, so the bat catches it. The system never pronounces the death — the predator does. This keeps the failure legible (you got caught) rather than arbitrary (a meter hit zero).

| Quantity | Initial value (tune in playtest) | Visual |
|---|---|---|
| Maximum energy | 100 | Warm gold glow, large halo |
| Flight drain | ~5 / sec | Halo slowly shrinks as it burns |
| Dash cost | ~20 per dash | Halo pops then contracts |
| Bonk light dropped | ~30 per bonk | Light bursts from the impact, pulls into Lumi |
| Glow mote value | ~25 each | Halo flares on pickup |
| Low-light threshold | 30% | Below here, speed starts dropping the curve |
| Penalty curve | speed scales with (energy / threshold) squared below the threshold | Glow dims to orange-red, flickers; dash locked |

The existing `LumiEnergy` component already drains passively and drives the glow colour, and exposes `Add()` / `Consume()` hooks. v3.0 builds the economy on top of it: motes and bonk light call `Add()`, dash calls `Consume()`, and the low-light speed scaling is read from `Normalized` by the player controller.

### Bonk Detection and Reaction

When a predator collides with a `Bonkable` obstacle, it reflects its pre-impact velocity off the contact normal with a retention factor (~0.3), then enters a fixed-duration stun. During the stun the predator slides along the reflected heading and decelerates so it ends the stun pointing away from the wall — this is what breaks the "stuck against the wall, re-bonk on resume" loop that the v1.0 design produced in testing (Issue #27).

New in v3.0: the impact also **scatters light** at the contact point, which Lumi absorbs by passing through it. This is what ties the offensive mechanic to survival — every successful trap is also a meal.

Effects: camera shake, animator `Bonk` trigger, light scatter, no AI input for the stun duration. The "BONK!" comic text, dedicated SFX, and white flash are still planned.

`Bonkable.StunDuration` is per-obstacle, so heavier props can be tuned to harder hits later.

### Death

One-hit kill. Any predator touching Lumi ends the run. Implementation freezes Lumi for 0.3 s and reloads the active scene. The freeze-frame + camera zoom-in and the standalone death screen from v1.0 are planned but not built.

Energy does **not** cause death on its own (see the low-light penalty above). The only kill condition is predator contact.

### Companions Saved — Primary Narrative Beat

There is no score number. Every few seconds survived, one more firefly reaches the meadow — shown quietly in the corner as a small running tally ("a friend made it"), not a points counter. The death screen reports only how many friends made it. Bonk count is **not** shown; the bonk is a means, not a metric.

| Quantity | Initial value (tune in playtest) |
|---|---|
| Save cadence | one friend every 6 s (range 5–8 s) |

---

## AI Design

The v1.0 plan used a ring buffer of past **positions**: each AI steered toward where the player was 0.25–0.5 s ago. In testing this produced a known failure: the bat would hit an obstacle, get stuck on its far side, and oscillate against the wall because its delayed target was always on the wrong side. The system was reworked.

### Reaction Delay on Steering, Not Position

Each AI keeps a ring buffer, but it stores the **instantaneous desired direction toward the player**, not the player's raw position. On each tick the AI reads the entry from `reactionDelay` seconds ago and treats that as its current intent. The result feels the same to the player — the bat lags behind feints — but the AI's target is a direction vector, not a stale point in space, so it can't pin itself behind geometry the player has already left.

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
| **Bat** | Direct chase via echolocation | 0.1 s steering delay | Implemented |
| **Owl** | Predictive interceptor — extrapolates 0.5–1.0 s ahead of the player's velocity rather than chasing | 0.25 s steering delay | Planned |

A third "Herder" archetype was scoped out in v1.0 and remains out of scope.

---

## Obstacles (the Bonk Surfaces)

| Obstacle | Bonk Value | Status |
|---|---|---|
| Rock cluster | Medium | Implemented |
| Lamppost | High | Planned (this sprint) |
| Bush | Medium | Planned (this sprint) |
| Old tree trunk | High | Planned |
| Park bench | Medium | Planned |
| Pebble cluster | Low (decorative slowdown for predators; Lumi flies over) | Planned |

The lamppost and bush are the next two surfaces to land — they fill out the garden and give the bait loop more than one wall to work with, which also stress-tests the bat's context-steering weights against tighter geometry.

---

## Smallest Playable Version (MVP) — landed, now extended

The MVP target from v1.0 is playable in `MainScene`:

- Mouse moves Lumi; left-click dash is the next control to land.
- One Bat with steering-direction delay + context steering + bonk reflection.
- Rock prefab as the first `Bonkable`.
- Bat touching Lumi triggers death and scene reload.
- Camera shake + animator trigger on bonk; light scatter, comic text, and SFX still planned.

The core chase-and-bonk loop produces real tension during 30-second tests, so the central idea is proven. v3.0 closes the gap the tests exposed: the loop now has a reason to keep engaging instead of stalling.

---

## Biggest Risk

V2.0 named the bat's context-steering weights as the live risk. With the energy economy reworked, the headline risk is now **pacing the economy**:

Flight drain, glow-mote spawn rate and value, and bonk light yield have to balance so the run stays tense without tipping into either extreme — too generous and stalling creeps back in; too harsh and a player dies before learning to bonk, which reads as unfair. The steepening low-light curve is the most sensitive part: too shallow and there is no real danger to running low, too steep and the death spiral feels like a sudden cliff. Every one of these values is exposed as a serialized field so it can be tuned live during playtests, and the safety-net glow motes exist specifically to widen the survivable band for new players while the bonk loop is being learned.

The bat's context-steering weights remain a secondary risk, now compounded because the lamppost and bush add geometry the weights were never tuned against.

---

## Scope Sorting (MoSCoW)

### Must Have — Without these, the game does not exist

| Feature | Why it is essential | Status |
|---|---|---|
| Mouse-driven movement | The entire control identity depends on this | Implemented |
| Bat AI (steering-delay + context steering) | The chase has no character without buffered, obstacle-aware responses | Implemented |
| Bonk reflection + stun | The game is literally named after this mechanic | Implemented |
| Light = energy fly-to-survive economy | Narrative-mechanic core; it is what forces engagement | Planned |
| Bonk light + glow-mote refuel | The two ways to refill; without them the economy has no input | Planned |
| Left-click dash | The control layer past the cursor follow, and a second energy sink | Planned |
| One-hit-kill death | Closes the game loop | Implemented |
| Death screen (X friends made it to the meadow) | Closes the narrative loop | Planned |
| Companions-saved beat (every few seconds) | The player's primary narrative payoff | Planned |
| One greybox arena with 4+ obstacles | Bonk needs surfaces; the chase needs space | Partial — rock landed, lamppost + bush this sprint |
| Main menu + opening + death screen | Assessment requires a complete game flow | Planned |
| Basic audio (Bonk, dash, death, BGM) | Assessment lists audio as a required system | Planned |
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
| "A friend made it" mini-animation on each save | Reinforces narrative cadence |
| Multiple obstacle types with unique bonk effects | Lamppost flicker, chime ripple add personality |
| Ambient dynamic sound (distant chimes, owl hoot) | Atmosphere boost |
| Custom cursor (glowing reticle) | Class-covered; minor polish |
| Low-energy red-flash warning | Accessibility + tension |
| Death-screen run statistics (best survival across runs) | Replay motivation |
| Static high score via PlayerPrefs | Class-covered; adds run-to-run progression |

### Cut First — Remove immediately if behind schedule

| Feature | Why it can go |
|---|---|
| Left-click brake-to-turn | Cut in v3.0 — testers found it unintuitive and redundant with the mouse model |
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

- **Accessibility**: predators distinguished by silhouette AND motion pattern, not colour alone. Bonk feedback is multimodal (visual flash + camera shake + audio). The low-light state is signalled by halo size and flicker in addition to colour, so the energy warning does not rely on the red shift alone. Critical signals (companions saved, low light) use shape and motion as well as colour.
- **Legal**: all assets either self-made, public-domain, or licensed under Creative Commons with proper attribution in both the credits screen and the GitHub README.
- **Ethical**: the narrative engages with mortality and sacrifice, but frames them as part of a hopeful cycle of renewal rather than as bleak or hopeless content. Appropriate for the 16–18 target audience and avoids gratuitous distress.
- **Security**: no online features, no data collection, no user accounts — there is no security surface beyond standard Unity build hygiene.
