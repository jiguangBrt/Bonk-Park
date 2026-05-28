# Issue #32 — Restructure docs folder and update README

> **Temporary handoff document.** Not for commit. Delete or gitignore after the new agent finishes the task.

Branch: `32-docs/restructure-docs-folder-and-update-readme` (already checked out).

## Context

The repo root has accumulated docs that don't match the current state of the game:

- [README.md](README.md) is 7 lines and only links to two in-class activity repos. The rubric explicitly requires a README that explains the game, controls, how to run, and credits — this is the biggest visible gap for the marker.
- [GameConceptDocument.md](GameConceptDocument.md) sits at repo root. The doc plan inside it ([GameConceptDocument.md:239-243](GameConceptDocument.md#L239-L243)) calls for a `Docs/` folder that doesn't exist.
- `prototype/brief.md` describes an abandoned earlier direction ("magic friction shoes" firefly chase), not the current Lumi/bat chase. It misleads anyone reading the repo cold.
- There is no rolling development plan or report skeleton, both of which the rubric values as evidence of process and reflection.

The Issue defines acceptance criteria precisely. This plan executes them with two user-confirmed details:
1. `DevelopmentPlan.md` uses the format **"By Session N, I will ... Evidence: ..."** (Session = one class/day); first entry retrospectively covers past work, future commitment slot is left as a `TODO` placeholder.
2. `prototype/` becomes `Docs/Archive/GameConceptDocumentV0.1.md` + the two PNGs alongside it; old `prototype/` folder is removed.

Intended outcome: a reviewer landing on the repo can, within 60 seconds, understand what the game is, how to run it, what's in `Docs/`, and where to find evidence of the design pivot.

## Files Created / Moved / Deleted

| Action | Path | Note |
|---|---|---|
| `git mv` | `GameConceptDocument.md` → `Docs/GameConceptDocument.md` | Preserves history per Issue task list |
| `git mv` | `prototype/brief.md` → `Docs/Archive/GameConceptDocumentV0.1.md` | Add a one-line header "Abandoned v0.1 direction; see [../GameConceptDocument.md](../GameConceptDocument.md) for the current design." Body unchanged. |
| `git mv` | `prototype/description.png` → `Docs/Archive/description.png` | |
| `git mv` | `prototype/fake Screenshot.png` → `Docs/Archive/fake Screenshot.png` | Quote the space when running `git mv`. |
| Delete | `prototype/` folder | After the three moves above, the folder is empty; remove it. |
| Create | `Docs/DevelopmentPlan.md` | See structure below. |
| Create | `Docs/Report-Outline.md` | See structure below. |
| Rewrite | `README.md` | See structure below. |

Use `git mv` (not Edit/Write) for all moves so the rename is tracked. Don't touch any `.meta` files — the moved files are docs/PNG outside `Assets/`, so they have no `.meta`.

## `Docs/DevelopmentPlan.md` structure

```markdown
# Development Plan — Bonk Park

Rolling commitment log. One line per Session (= one class/day) in the format:

> **By Session N, I will <do thing>. Evidence: <link to PR / commit / Issue / doc>.**

Past work is summarised once at the top so the timeline reads complete.

## Past work (Sessions 1 – <last>)

One short paragraph summarising the journey so far:
- Project init + 3D→2D template migration (PRs #4, #7)
- Game concept document landed (#14)
- Park scene + mouse-driven Lumi + letterbox camera (#16)
- Bat enemy with reaction-buffer chase (#20, #22)
- Bonk-reaction rock obstacle (#28)
- Letterbox guard + bat unstick fixes (#26, #29)
- Lifecycle refactor (#31)
- First two class playtests logged in #23

## Upcoming commitments

- [ ] **By Session <TODO>, I will <TODO>. Evidence: <TODO>.**
```

Keep the file deliberately short. Future sessions append one bullet each.

## `Docs/Report-Outline.md` structure

Skeleton matching the rubric's report criteria:

```markdown
# Final Report — Outline

Living outline. Each section gets a paragraph added whenever a notable decision lands.

## 1. Game overview
## 2. Design decisions
## 3. Technical decisions
## 4. Testing and what changed because of it
## 5. Problems and limitations
## 6. Reflection: concept → final
## 7. Credits and references
```

Each heading sits empty for now; the Issue task list says "append a paragraph each time a notable design decision lands."

## `README.md` rewrite structure

Order matches the Issue's acceptance criteria (link block first, then game, controls, run, credits):

```markdown
# Bonk Park

One-sentence pitch: 2D top-down pixel-art chase — guide firefly Lumi to bait bats into park obstacles.

## Docs
- [Game Concept Document](Docs/GameConceptDocument.md)
- [Development Plan](Docs/DevelopmentPlan.md)
- [Report Outline](Docs/Report-Outline.md)
- [Peer playtest log (Issue #23)](https://github.com/jiguangBrt/Bonk-Park/issues/23)
- [Archived v0.1 concept](Docs/Archive/GameConceptDocumentV0.1.md)

## In-class activities
- [2D Game Improvement Assignment](https://github.com/jiguangBrt/2D_Game_Improvement_Assignment)
- [Understanding Unity Assignment](https://github.com/jiguangBrt/Understanding_Unity_Assignment)

## The game
Short paragraph from GameConceptDocument's "One-Sentence Idea" + "Moment-to-Moment Gameplay" (3–5 sentences max).

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

## Credits
- All gameplay code and art are original to this project.
- No third-party assets are bundled in the current build.
- Engine: Unity 2022 LTS, URP 2D Renderer.
- Licensed under the [MIT License](LICENSE).
```

Keep voice consistent with how Brt writes the existing PR bodies — terse, factual, no marketing fluff. **Do not leave any AI signatures** in commit messages, file headers, or content (see `CLAUDE.md` rule).

## Verification

After execution, all checks are read-only:

1. **Acceptance criteria walk** — verify each Issue #32 checkbox:
   - `ls Docs/` shows `GameConceptDocument.md`, `DevelopmentPlan.md`, `Report-Outline.md`, `Archive/`.
   - `ls Docs/Archive/` shows the renamed v0.1 doc + 2 PNGs.
   - `git log --follow Docs/GameConceptDocument.md` shows the pre-move history.
   - `git status` shows no stray files in repo root from the old `prototype/`.
2. **Link sanity** — open the new `README.md` in VSCode preview, click every link, confirm none 404 locally. The Issue #23 link goes to GitHub (external).
3. **Old-path scan** — `Grep -r "GameConceptDocument" --glob "*.md"` should match only files inside `Docs/` and the new README link. Confirm no link points at the old root path.
4. **`prototype/` gone** — `Test-Path prototype` returns false.
5. **CLAUDE.md sanity** — the local `CLAUDE.md` references `agile-workflow-sop.md` (not in the repo) and lives outside this Issue's scope. Do not touch it in this PR.

After verification, Brt drives the commit / PR (per `CLAUDE.md` no-AI-traces rule). A reasonable Conventional Commit subject: `docs: restructure docs folder and rewrite README`. Brt can run `/agile-commit` and `/agile-pr` to draft those.

## Out of scope (don't do here)

- Filling in real Session entries beyond the past-work paragraph + one TODO placeholder.
- Writing actual Report body content.
- Touching `CLAUDE.md`, `.gitignore`, or any `Assets/` files.
- Closing Sprint 1 or moving #17/#18 — separate housekeeping.
- **Do not commit this `PLAN-issue32.md` file** — it's a handoff doc, delete after use.
