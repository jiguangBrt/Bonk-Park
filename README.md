# Bonk Park

A 2D top-down pixel art style chase game - guiding the firefly Lim to evade the bat's pursuit, causing it to collide with the obstacles in the park, thereby obtaining a longer survival time.

## Docs
- [Game Concept Document v2.0](Docs/GameConceptDocumentV2.0.md) — current design
- [Game Concept Document v1.0](Docs/GameConceptDocumentV1.0.md) — earlier design, superseded but kept for context
- [Development Plan](Docs/DevelopmentPlan.md)
- [Report Outline](Docs/Report-Outline.md)
- [Peer playtest log (Issue #23)](https://github.com/jiguangBrt/Bonk-Park/issues/23)
- [Archived v0.1 concept](Docs/Archive/GameConceptDocumentV0.1.md)

## In-class activities
- [2D Game Improvement Assignment](https://github.com/jiguangBrt/2D_Game_Improvement_Assignment)
- [Understanding Unity Assignment](https://github.com/jiguangBrt/Understanding_Unity_Assignment)

## The game

Every summer at the end of the season, fireflies need to store their light in grass seeds in order to rejuvenate themselves the following year. But you, as Lumi, because you are too lively, used too much light during the summer and couldn't complete the storage ritual. 

There are bats roaming in the park, so you decided to secure more time for other fireflies to complete the awareness process. Lumie will automatically accelerate in the direction of the mouse pointer; the player observes the chasing bats, draws them closer to a tree or a rock, then brakes at the last moment and makes a sharp turn, causing the bats to hit an obstacle and make a "bang" sound. 
If the "bonk" mechanism can last longer, more of your friends can be saved.

## Controls

| Input | Action |
|---|---|
| Mouse movement | Lumi auto-accelerates toward cursor |
| Hold Left Click | Emergency brake — used to make sharp turns |
| Release Left Click | Re-launches Lumi at the current heading |

## How to run (Unity Editor)

1. Open the project in Unity **2022.3.62f3c1** via Unity Hub.
2. Open scene `Assets/BonkPark/_Scenes/MainScene.unity`.
3. Press Play.

(No standalone build is wired up yet — build settings have no scenes registered.)

## Repository layout

```
Bonk-Park/
├── README.md
├── LICENSE
├── Assets/
│   ├── BonkPark/             — all original game content lives here
│   │   ├── _Scenes/          — MainScene.unity (more added per arena)
│   │   ├── Scripts/          — Player, BatAI, Bonkable, CameraFit, LetterboxCamera, Park, etc.
│   │   ├── Prefabs/          — Lumi, Bat, Rock, and obstacles added later
│   │   ├── Art/              — Pixel sprites, textures
│   │   └── Animations/       — Bat bonk reaction, Lumi flicker (planned)
│   ├── Settings/             — Engine-level global config (URP, input)
│   ├── Editor/               — Editor-only tooling, excluded from build
│   ├── ThirdParty/           — Any purchased / external packages (none bundled yet)
│   └── TextMesh Pro/         — Unity package import, untouched
├── Docs/                     — Design docs and process artefacts
│   ├── GameConceptDocumentV2.0.md
│   ├── GameConceptDocumentV1.0.md
│   ├── DevelopmentPlan.md
│   ├── Report-Outline.md
│   └── Archive/              — Superseded docs (v0.1 brief, early screenshots)
├── ProjectSettings/          — Unity project settings (committed)
├── Packages/                 — Unity package manifest + lockfile
└── .github/                  — Issue / PR templates, workflow config
```

`Assets/BonkPark/Scripts/` is currently flat — `Player/`, `AI/`, and `Management/` subfolders will be split out once the script count grows. Audio and UI folders will be added when those systems land (see [v2.0 doc](Docs/GameConceptDocumentV2.0.md)).

## Credits

- All gameplay code and art are original to this project.
- No third-party assets are bundled in the current build.
- Engine: Unity 2022 LTS, URP 2D Renderer.
- Licensed under the [MIT License](LICENSE).
