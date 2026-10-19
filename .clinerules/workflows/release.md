---
description: Release pass for one maintained branch (changelog, version bump, build, tag)
---

# Release pass

Run a complete release pass for **one** branch of RTSCamera, following
`.clinerules/40-release-workflow.md`.

First ask the user (or confirm from their message) which line is being released:

| Branch    | Bannerlord target   | Mod version line |
|-----------|---------------------|------------------|
| `master`  | v1.4.5 – v1.4.8     | `v5.4.x`         |
| `beta`    | v1.5.1 (+ NavalDLC) | `v5.5.x`         |
| `v1.3.15` | v1.3.15             | `v5.3.x`         |

## Steps

1. **Confirm state**
   - `git status --short --branch` and `git worktree list`.
   - If the branch is checked out in another worktree, run the commands there with
     `git -C "<worktree path>"` instead of switching branches.
   - After any branch switch: `git submodule update`.

2. **Determine the version**
   - Read `.github\resources\config.env` for the current `MOD_VERSION` / `GAME_VERSION`.
   - `git tag --list "release-v*"` and `git log --oneline <previous-tag>..HEAD` to list the
     unreleased work.
   - Ask the user for the next version number if it is not obvious (patch vs. minor).

3. **Write the changelogs**
   - `CHANGELOG.md` and `CHANGELOG-zh-CN.md`: append to an existing unreleased heading if
     present, otherwise add a new heading at the top. Cover every user-visible change since
     the previous tag on this branch.
   - `MountBladeComCnDescription.txt`: mirror the notes with merged headings such as
     `v5.4.x / v5.3.x` when the same notes apply to both lines; prefix branch-only lines
     inline, for example `(v5.4.x)`.
   - Do not include changes that only exist on another branch.

4. **Bump the versions**
   - `.github\resources\config.env`: `GAME_VERSION`, `GAME_BRANCH`, `MOD_VERSION`.
   - `source\RTSCamera\Modules\RTSCamera\SubModule.xml`.
   - `source\RTSCamera.CommandSystem\Modules\RTSCamera.CommandSystem\SubModule.xml`.

5. **Build**
   - `dotnet build source\RTSCamera.sln --no-restore` and report the result verbatim.

6. **Commit the release**
   - `git status --short`, then stage only the release files.
   - Exclude `source/library/mission-library` pointer changes and any scratch artifacts.
   - Message: `Update changelog and mod version for vX.Y.Z`.

7. **Publish (only if the user asks)**
   - Push the branch, then create and push `release-vX.Y.Z-for-bannerlord-vA.B.C`.

Report back with: branch, version, tag name, files committed, build result, and anything the
user still has to verify in game.
