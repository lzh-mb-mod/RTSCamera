# Project state (verified 2026-10-18)

Snapshot produced while migrating from GitHub Copilot to Cline. Re-run the commands below
before trusting this file if the user has pushed, merged, or switched branches since.

## Repository

- Local root: `D:\develop\src\RTSCamera`
- Remote: `lzh-mb-mod/RTSCamera` (`origin`)
- Submodule: `source/library/mission-library` at `417d4098` (matches its
  `remotes/origin/HEAD`)
- Working tree: clean; the Cline migration (`.clinerules/`, `docs/cline/`, and the note in
  `.github/copilot-instructions.md`) is committed on `master`

## Branches

| Branch      | HEAD      | Subject                                        | vs. origin        |
|-------------|-----------|------------------------------------------------|-------------------|
| `master`    | `315ec7f` | Update Steam Workshop for Bannerlord v1.4.8    | ahead 1 (unpushed)|
| `beta`      | `721d3b0` | Document game version compatibility            | in sync           |
| `v1.3.15`   | `176f0e1` | Update v1.4 Workshop links                     | ahead 1 (unpushed)|

`v1.3.15` and `beta` are checked out in worktrees, so `git checkout` to either branch fails
with "already used by worktree". Use `git -C "<worktree>"` or remove the worktree first.

## Worktrees

```
D:/develop/src/RTSCamera                                                    315ec7f [master]
C:/Users/lizhe/.copilot/session-state/a0ee708b-.../files/worktrees/beta      721d3b0 [beta]
C:/Users/lizhe/.copilot/session-state/a0ee708b-.../files/worktrees/v1.3.15   176f0e1 [v1.3.15]
```

Both linked worktrees are clean. They live inside the Copilot session directory, which may
be cleaned up at any time. If that happens, repair with `git worktree prune` and, if the
checkouts are still wanted, recreate them somewhere stable:

```powershell
git worktree move "C:/Users/lizhe/.copilot/session-state/a0ee708b-00fc-4157-80f1-198eb5f4660a/files/worktrees/beta" "D:/develop/src/RTSCamera-worktrees/beta"
git worktree move "C:/Users/lizhe/.copilot/session-state/a0ee708b-00fc-4157-80f1-198eb5f4660a/files/worktrees/v1.3.15" "D:/develop/src/RTSCamera-worktrees/v1.3.15"
```

## Versions and tags

`.github\resources\config.env` on `master`:

```
GAME_VERSION=v1.4.8
GAME_BRANCH=master
MOD_VERSION=v5.4.16
```

Latest release tag per line:

- `release-v5.4.16-for-bannerlord-v1.4.8` (`master`, Bannerlord v1.4.5–v1.4.8)
- `release-v5.5.0-for-bannerlord-v1.5.1` (`beta`, Bannerlord/NavalDLC v1.5.1)
- `release-v5.3.38-for-bannerlord-v1.3.15` (`v1.3.15`)

## Reference material on disk

- Decompiled game sources: `D:\develop\src\Bannerlord.Binaries\<version>` with `v1.2.12`,
  `v1.3.4`, `v1.3.5`, `v1.3.15`, `v1.4.5`, `v1.4.6`, `v1.4.7`, `v1.4.8`, `v1.5.0`, `v1.5.1`,
  `v1.5.5` plus `ThirdPartyMods`.
- Decompiler script: `.github\scripts\Decompile-Bannerlord.ps1`.
- In-game manual regression list: `manual_test_cases.md`.
- Technical notes: `docs\*.md` (formation behavior, mission initialization, AI input).

## Legacy Copilot session artifacts (not migrated)

Located under
`C:\Users\lizhe\.copilot\session-state\a0ee708b-00fc-4157-80f1-198eb5f4660a\files\`:

- `PatchValidation`, `PatchValidationMaster`, `PatchValidationV13` — throwaway .NET 6
  Harmony-validation harnesses. Their hard-coded paths point at a now-removed
  `worktrees\master` folder, so they are stale; recreate instead of reusing.
- `Bannerlord-v1.4.8`, `Bannerlord-v1.4.8-binaries.zip` — v1.4.8 game assemblies pulled
  through the CI pipeline for validation.
- `*.il` dumps (`MissionState.il`, `OrderController.il`, `ShipOrder.il`, ...) — raw IL for
  patch research.
- `beta-patch-audit*.patch` — v1.5.1 audit diff.
- `worktrees\beta`, `worktrees\v1.3.15` — the live worktrees listed above.

None of these belong in the repository. Delete them once a validation harness has been
recreated outside the Copilot state folder.

## Re-verification commands

```powershell
git worktree list
git status --short --branch
git submodule status
git log --oneline -5 master
git log --oneline -5 beta
git log --oneline -5 v1.3.15
git tag --list "release-v*" --sort=-v:refname | Select-Object -First 10
```
