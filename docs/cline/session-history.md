# Session history (migrated from GitHub Copilot)

Summary of the previous agent work on this repository, condensed from the 11 checkpoint
summaries GitHub Copilot wrote while the session was compacted. Raw material:

- `C:\Users\lizhe\.copilot\session-state\a0ee708b-00fc-4157-80f1-198eb5f4660a\checkpoints\*.md`
  (per-checkpoint overview, history, important files, next steps; `index.md` lists them)
- `...\events.jsonl` — full 81 MB event log, too large to read wholesale; grep it for commit
  hashes or file names if a detail is missing here.
- `...\workspace.yaml` — session metadata (workspace `D:\develop\src\RTSCamera`, branch
  `master`, repository `lzh-mb-mod/RTSCamera`, created 2026-06-22, last updated 2026-08-22).
- Copilot's repository-scoped "memory" was stored server-side, not in this repository. The
  only known entry was the build command (`dotnet build source\RTSCamera.sln --no-restore`),
  which is now captured in `.clinerules\10-build-and-validation.md`.

The session covered roughly 2026-06 to 2026-10 and was mostly release maintenance plus
compatibility audits of every Harmony patch against Bannerlord v1.4.7, v1.4.8, v1.5.0, and
v1.5.1.

## 1. Elevated camera releases

- Added menu options for elevated-camera rise delay/duration/off-duration and translated the
  remaining Chinese comments in `src\Utilities\Utility.cs` to English.
- Wired `ElevatedCameraSubView` timing through `Utility.CameraEaseSolver`; reverted once when
  the user reported broken camera behavior, then reapplied after the user identified the
  solver as the cause.
- Renamed option strings and IDs (`elevated_camera_rise_delay`, `elevated_camera_rise_duration`)
  across code and all locale files.
- Fixed order preview/formation marker behavior; released v5.4.8 on `master` and v5.3.31 on
  `v1.3.15`, cherry-picking selected fixes.

## 2. Cross-branch releases and docs

- Maintained `master`, `v1.3.15`, and occasionally `v1.2.12`; used temporary git worktrees to
  edit branches without disturbing the main checkout.
- Handled MissionLibrary submodule pointers explicitly (commit submodule first, then parent).
- Shipped fixes for detached-order queues, stealth free-camera charging, siege-tower
  middle-click selection, formation drag cancel, naval orders, and formation markers.

## 3. Releases and Copilot setup

- Created `.github\copilot-instructions.md` from actual repository files, then
  `.github\scripts\Decompile-Bannerlord.ps1` with automatic game-version detection.
- Released v5.4.12 / v5.3.34 and synchronized `MountBladeComCnDescription.txt`; the
  cherry-pick of that file conflicted and was resolved manually in the merged section.
- Lesson: changelog entries must cover every unreleased gameplay commit, and unreleased
  headings are appended to rather than recreated.

## 4. Decompile tooling and memory-leak investigation

- Automated decompilation into versioned folders (`D:\develop\src\Bannerlord.Binaries\<ver>`).
- Investigated long-session/battle-to-battle memory growth: Gauntlet `Movie` instances were
  leaked. Fixed in `FlyCameraMissionView`, `RTSCameraSelectCharacterView`, and MissionLibrary
  `MissionMenuViewBase`; added temporary memory-sampling scripts (never committed).
- User rejected removal of a commented-out `bodyMaterial` line in `CommandQueuePreview.cs`.

## 5. Auditing Harmony transpilers

- Inventoried all active transpilers and compared IL assumptions with v1.4.7 decompiled code.
- At that point the tree also carried unrelated modified files (language data, order troop
  placer, queue preview) and untracked memory scripts — a reminder to check `git status`
  before any audit-related commit.

## 6. Bannerlord v1.5.0 compatibility

- Volley toggle/gating, defensive-hold fallback, forced-control selection fix, Mission UI
  movie cleanup, formation-color teardown, formation-shape material stability.
- Added optional NavalDLC patch `Patch_MissionShip.OnUnitAttached` prefix to stop the native
  rally-point overwrite during naval raids when a ship is being abandoned, and registered it
  in `CommandSystemSubModule`.
- Built clean and ran an isolated Harmony validation of ~61 patch groups.

## 7. Naval raid order fixes

- Multiple rounds of fixes so land movement orders survive ship abandonment and boarding,
  plus safer ladder-queue restoration (`Patch_LadderQueueManager` with
  `SetIsLadderQueueUsing(false)` before removal).

## 8. v1.5 releases and agent interaction cleanup

- Pushed `Support Bannerlord v1.5.0` on `beta`, version bumps on `master` and `v1.3.15`, and
  the matching MissionLibrary commit.
- Replaced a detachment-count heuristic with a peaceful/combat mission condition around
  `HandleStopUsingAction` in MissionLibrary `Utility.cs`.
- Audited whether the patched `MissionOrderVM` can block order UI closure during mission
  finalization.

## 9. Full patch-by-patch audit against v1.5.1

- Removed the no-op `Patch_DeploymentMissionController`; retained patches that still fix
  free-camera/controller differences.
- Restored native v1.5.1 behavior where earlier patches had drifted: order UI finalization
  close, `Patch_PassageUsePoint` null handling, `Patch_RangedSiegeWeaponView` camera
  rotation with cached reflection, `Patch_BattleEndLogic` guards, formation marker alpha and
  distance init, `Patch_MissionOrderVM` troop/role validation.
- Reworked Naval patches into narrow transpilers that preserve the native body
  (`Patch_MissionShipControlView`, `Patch_AgentNavalComponent`), kept broader prefixes only
  where necessary (`Patch_MissionShip`, `Patch_NavalTeamSideSpawnContext`).

## 10. Audit fixes and cross-branch plan

- Fixed a global `SetOrderWithPosition` transpiler issue that changed facing behavior for
  AI orders; fixed null/dictionary/lifecycle issues in CommandSystem; confirmed 15 active
  CommandSystem registrations.
- Declined speculative changes to square-formation `ReapplyFormOrder` behavior at the user's
  request.
- Created isolated clean worktrees for `master` and `v1.3.15` and verified the relevant APIs
  exist in v1.4.8 / v1.3.15.

## 11. Master backport validation

- `beta` audit committed; master backport validated against pipeline-downloaded v1.4.8
  assemblies including NavalDLC (temporary `pipeline-dlls` branch and Workflow artifact
  upload were used to fetch them).
- Per-formation skip state replaced a global `TicksToSkip` counter in `CommandQueueLogic`;
  circular-formation preferences scoped to player formations; Naval marker reflection
  replaced with compiled delegates; CommandSystem `Patch_MissionShip` gated on NavalDLC.
- MissionLibrary was committed before the parent pointer (`c4f1ace` on its own `master`).

## Where things stand now

The release work continued after the last checkpoint: v5.4.16 (`master`, Bannerlord
v1.4.5–v1.4.8), v5.5.0 (`beta`, Bannerlord v1.5.1), and v5.3.38 (`v1.3.15`) are tagged, and
the trees are clean. Only two Workshop text commits remain unpushed (`master` `315ec7f`,
`v1.3.15` `176f0e1`). See `docs/cline/project-state.md` for verified details.

Open threads worth remembering:

- The reusable Harmony patch-application harness only exists as a stale session artifact and
  would have to be recreated for another full-patch validation pass.
- `Patch_MissionShip` still suppresses native `UpdateController` on RTS paths and
  `Patch_NavalTeamSideSpawnContext` still copies a private native deployment method; both were
  accepted as intentional but remain drift risks when Bannerlord changes.
- NavalDLC detection checks module presence, not a supported version range.
