# Agent workflow for RTSCamera

This repository is a Mount & Blade II: Bannerlord mod (RTS Camera + Command System +
shared agent component). C# targeting .NET Framework 4.7.2 and .NET 6, Harmony patching,
two packaged modules plus one shared library submodule.

## Read before acting

1. `.clinerules/10-build-and-validation.md` — how to build and how to check work.
2. `.clinerules/40-release-workflow.md` — branch/version model before touching versions,
   changelogs, tags, or Workshop/description text.
3. `docs/cline/project-state.md` — verified branch, tag, worktree, and artifact state.
4. `docs/cline/session-history.md` — what the previous (Copilot) sessions did and why.

Do not re-derive facts already recorded there. Re-check them with `git` only when the
recorded state looks stale (for example after the user pushed, merged, or switched branches).

## Working agreement

- Build before claiming success. Use `dotnet build source\RTSCamera.sln --no-restore`
  unless the user asks for a CI-style MSBuild/Release run. Report warnings and errors
  verbatim instead of paraphrasing them.
- There are no automated tests and no lint configuration. Manual regression scenarios live
  in `manual_test_cases.md`. Gameplay, camera, UI, and Harmony behavior can only be fully
  verified in-game, so state clearly which parts you built and which parts remain untested.
- Investigate before editing a Harmony patch. Search the decompiled reference source under
  `D:\develop\src\Bannerlord.Binaries\<game version>\` for the exact target method and IL.
  Never guess method names, overloads, or control flow.
- Explain the intended patch change before applying broad or behavior-changing edits:
  target type/method, patch kind (prefix/postfix/transpiler/finalizer), what native code
  does, and why the patch must differ. During audits the user expects a one-item-at-a-time
  review, not a batch rewrite.
- Keep changes surgical and consistent with the surrounding style. English comments and
  identifiers. Do not reformat, rename, or refactor unrelated code.
- Do not undo intentional decisions. The user has explicitly kept some non-obvious code
  (intentionally broad patch behavior, commented-out experiments, inherited native logic).
  Ask before "cleaning up" anything that looks odd but predates your change.
- Do not silently change gameplay defaults or option IDs; those are user-facing contracts.

## Git hygiene

- Keep functional commits separate from release commits. Code/docs changes land first;
  the version + changelog commit comes afterward as its own commit.
- Run `git status --short` before staging. `source/library/mission-library` is a submodule
  and frequently shows as modified after branch switches: commit inside the submodule first,
  then stage the parent pointer deliberately. Never include a submodule pointer change in a
  release version/changelog commit unless it is intended.
- After a branch switch in a worktree, run `git submodule update` (the user asked for this
  explicitly and it prevents building against the wrong MissionLibrary revision).
- Do not commit scratch artifacts: memory-sampling CSV/marker files, session-only validation
  projects, `source/package/` build output, or downloaded game binaries.
- Only push, rewrite history, force-push, or create tags when the user asks for it.
  If a rebase/amend would touch already-pushed commits, confirm first.
- Prefer `git worktree` over stashing when work must happen on two branches at once.
  See `docs/cline/project-state.md` for the existing worktrees.

## Cline-specific notes

- Use the `/release` workflow (`.clinerules/workflows/release.md`) for any version bump plus
  changelog pass instead of improvising the file list.
- Rules in `.clinerules/` are loaded into every task. Keep them stable; put volatile facts
  and one-off findings in `docs/cline/` instead.
- Never add AI attribution to commit messages: no `Co-authored-by`, `Generated-by`, or
  similar trailers for Copilot, Cline, or any other assistant, even though older history
  contains `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>`.
- When a task spans branches or versions, say which branch/version each edit targets and
  which ones are still pending.
