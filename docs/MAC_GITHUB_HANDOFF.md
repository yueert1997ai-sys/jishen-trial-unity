# GitHub Backup And Mac Handoff

Repository: https://github.com/yueert1997ai-sys/jishen-trial-unity

Private repository created for the Unity project on 2026-09-06. The existing public `mecha-marco` repository is a different JavaScript browser implementation (v4 player and requestAnimationFrame loop), not this Unity C# checkout. Its files and visibility were not changed. A package.json private flag is an npm publishing setting, not GitHub repository visibility.

## What Is Saved

The Unity Assets (including .meta), Packages, ProjectSettings, current C# changes and prefabs, project documentation, tools and authored art_prototypes are included. Blender source versions are retained. This captures files saved to disk, not unsaved Blender/Unity memory or changes made after the snapshot.

Excluded: Library/Temp/Logs/Builds, nested UnityImportReview copies, Blender numbered autosaves, duplicate export ZIP/unitypackage files, OptiX caches, raw chat backups and unrelated personal utilities/audio. Existing tracked historical validation evidence remains in Git history. This is a private source backup; public asset-release licensing remains a separate check.

No paid plan, Git LFS subscription or paid storage purchase was enabled. Assets are regular Git objects after size checks, not LFS pointer-only files.

## Mac Setup

1. Sign in to GitHub using the owning account or an invited collaborator account. A private URL alone does not grant download access. Other IDEs/platforms must authenticate to GitHub with authorized repository access.
2. Install Git/GitHub Desktop and Unity Hub. Install Unity **6000.3.18f1**, choosing the Editor for the Mac's processor architecture.
3. Clone this repository. The remote default branch is intended to point to the current development branch `codex/industrial-dash-polish`; verify the latest backup commit before editing. Do not clone `mecha-marco` as a substitute.
4. Add the cloned directory in Unity Hub and allow the Library/package caches to rebuild. Open `Assets/Scenes/Demo_Main.unity` and enter Play Mode.
5. Read `docs/CORE_GAME_DESIGN.md`, `docs/JISHEN_GAME_INTEGRATION.md`, then `docs/MANUAL_COMBAT_HANDOFF.md`. New model work may be ahead of previously packaged builds.
6. Use Blender to continue the authored sources when needed. Windows tool launch paths in automation scripts must be adapted for macOS. The Windows EXE is not a macOS executable.

The D-drive storage rule applies to the original Windows host. On macOS use the actual local clone directory; do not invent a D: drive or reuse Windows absolute paths. Scripts and model export tools have not been certified on a physical Mac by this backup operation.

## Daily Workflow

Pull before starting on another computer. Save files, inspect changes, commit and push before switching computers. Do not concurrently edit the same scene/prefab/Blender file on both machines. A Git push uploads committed files only, not unsaved or later edits. Never force-push to solve an ordinary divergence.

This repository backup does not back up Codex sidebar categories or all Codex chat history. Those are application state and are separate from the Unity project.
