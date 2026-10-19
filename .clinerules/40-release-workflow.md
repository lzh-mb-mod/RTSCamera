# Release workflow

## Release lines

| Branch     | Bannerlord target       | Mod version line | Latest tag (as of 2026-10-20)                 |
|------------|-------------------------|------------------|-----------------------------------------------|
| `master`   | v1.4.5 – v1.4.8         | `v5.4.x`         | `release-v5.4.16-for-bannerlord-v1.4.8`       |
| `beta`     | v1.5.5 (+ NavalDLC)     | `v5.5.x`         | `release-v5.5.0-for-bannerlord-v1.5.1`        |
| `v1.3.15`  | v1.3.15                 | `v5.3.x`         | `release-v5.3.38-for-bannerlord-v1.3.15`      |

Nothing is merged between these branches. Fixes are ported individually and validated
against each branch's target game version.

## Files touched by a normal release pass

1. `CHANGELOG.md`
2. `CHANGELOG-zh-CN.md`
3. `MountBladeComCnDescription.txt` (Mount & Blade Chinese site description)
4. `.github\resources\config.env` — `GAME_VERSION`, `GAME_BRANCH`, `MOD_VERSION`
5. `source\RTSCamera\Modules\RTSCamera\SubModule.xml`
6. `source\RTSCamera.CommandSystem\Modules\RTSCamera.CommandSystem\SubModule.xml`
7. `SteamWorkshopReadMe*.txt` when Workshop text/compatibility tags change

## Order of operations

1. Gameplay/docs changes are committed first on the target branch.
2. Version bump + changelog is a dedicated release commit:
   `Update changelog and mod version for vX.Y.Z`
3. Push the branch (only when asked).
4. Create and push the tag `release-vX.Y.Z-for-bannerlord-vA.B.C`.
   `.github\workflows\Build.yml` builds and publishes the GitHub release from the tag.

## Changelog rules

- Collect every user-visible change since the previous release tag on that branch, not just
  the latest commit. `git log --oneline <previous-tag>..HEAD` is the checklist.
- If an unreleased version heading already exists at the top, append to it; open a new
  version only when the release is actually new.
- English and Chinese changelogs must describe the same set of changes with matching
  wording.
- `MountBladeComCnDescription.txt` carries merged headings when notes are shared across
  branches, for example `v5.4.16 / v5.3.38`; branch-only lines get an inline prefix such as
  `(v5.4.16)`.
- Do not list changes that exist only on another branch.

## Pre-release checks

- `git status --short` is clean apart from the files you intend to commit; the
  `mission-library` submodule pointer is not accidentally staged.
- `dotnet build source\RTSCamera.sln --no-restore` succeeds on the branch being released.
- `GAME_VERSION` / `GAME_BRANCH` in `config.env` match the branch, not the other release line.
- Tag name matches the Bannerlord version in `GAME_VERSION`.

## Backports

When a fix applies to several lines, repeat the code change per branch, build per branch,
and run the runtime checks per branch. Use a separate worktree for each branch
(`docs/cline/project-state.md`) and run `git submodule update` after switching. Never
cherry-pick a release/version commit across branches; only port functional commits and then
bump each branch's own version.
