# Copilot instructions for RTSCamera

## Build, validation, and release commands

### Prerequisites
- Run `git submodule update --init --recursive` before building. The solution depends on `source\library\mission-library`.
- Local builds expect a Bannerlord install path through MSBuild `GamePath`. The default local path is stored in `source\library\mission-library\source\BasicSharedLibrary\GamePath.props`.

### Local validation
- Quick local build:
  - `dotnet build source\RTSCamera.sln --no-restore`
- CI-style release build:
  - `nuget restore .\source\RTSCamera.sln`
  - `MSBuild.exe .\source\RTSCamera.sln /p:Configuration=Release /p:GamePath="<Bannerlord path>"`
- Single-project build:
  - `dotnet build source\RTSCamera\RTSCamera.csproj --no-restore`
  - `dotnet build source\RTSCamera.CommandSystem\RTSCamera.CommandSystem.csproj --no-restore`
  - `dotnet build source\RTSCameraAgentComponent\RTSCameraAgentComponent.csproj --no-restore`

### Tests and lint
- No automated test or lint command is defined in the repository.
- Manual regression scenarios live in `manual_test_cases.md`.

### Release metadata
- Version bumps are coordinated in:
  - `.github\resources\config.env`
  - `source\RTSCamera\Modules\RTSCamera\SubModule.xml`
  - `source\RTSCamera.CommandSystem\Modules\RTSCamera.CommandSystem\SubModule.xml`
- CI packaging and release creation are defined in `.github\workflows\Build.yml`.

### Release workflow
- Keep feature/fix commits separate from release commits.
- Apply gameplay/code changes on the target branch first, then do the version/changelog commit as a final release step.
- Release tags follow the pattern:
  - `release-v5.4.x-for-bannerlord-v1.4.7` on `master`
  - `release-v5.3.x-for-bannerlord-v1.3.15` on `v1.3.15`
- A normal release pass on a branch updates:
  - `CHANGELOG.md`
  - `CHANGELOG-zh-CN.md`
  - `MountBladeComCnDescription.txt`
  - `.github\resources\config.env`
  - `source\RTSCamera\Modules\RTSCamera\SubModule.xml`
  - `source\RTSCamera.CommandSystem\Modules\RTSCamera.CommandSystem\SubModule.xml`
- If release notes are shared across both maintained branches, `MountBladeComCnDescription.txt` uses merged headings such as `v5.4.14 / v5.3.36`; branch-only items are prefixed inline, such as `(v5.4.14)...`.

## High-level architecture

- The solution is split into three main C# projects plus the `mission-library` submodule:
  - `source\RTSCamera`: free camera, elevated camera, player control, mission speed, campaign integration, naval/TOR-specific behavior.
  - `source\RTSCamera.CommandSystem`: order UI, command queue, formation logic, volley/defensive hold orders, troop highlighting, order preview behavior.
  - `source\RTSCameraAgentComponent`: shared mission-starting agent component used by both modules.
  - `source\library\mission-library`: shared menu, config, mission startup, and utility infrastructure imported through shared projitems.

- `RTSCameraSubModule` and `CommandSystemSubModule` are the two runtime entry points. They:
  - detect optional modules/mods (`NavalDLC`, `Helmsman`, `TOR_Core`, `RBM`, `TAOM`);
  - register Harmony patches;
  - register option screens and hotkey categories;
  - attach mission-start handlers and shared agent components.

- Most gameplay behavior is implemented through three layers:
  - `src\Patch\...`: Harmony patches against TaleWorlds/Bannerlord classes.
  - `src\Logic\...`: long-lived mission behaviors and sub-logic objects.
  - `src\View\...`: mission views and visual helpers.

- `RTSCamera.CommandSystem` centers around queued and augmented orders:
  - visual order providers translate UI or hotkeys into custom orders;
  - `CommandQueueLogic` stores queued orders and tracks formation-specific execution state;
  - patch classes around `OrderController`, `OrderTroopPlacer`, `MissionOrderVM`, and Gauntlet order handlers keep previews, dragging, and issued orders in sync.

- Options and runtime config are driven by MissionLibrary menu/view-model infrastructure:
  - persisted config objects live in `src\Config\...Config.cs`;
  - menu structure and callbacks live in `src\Config\...OptionClassFactory.cs`;
  - many option callbacks update live mission behaviors immediately instead of only saving config.

- Packaging is part of the project files, not a separate script:
  - each module copies assemblies and `Modules\...` assets into `source\package\net472\...` / `source\package\net6.0\...`;
  - on Windows net472 builds are also deployed directly into the local Bannerlord `Modules` folder using the configured `GamePath`.

- Decompiled Bannerlord reference source is available locally at `D:\develop\src\Bannerlord.Binaries`.
  - Use it to inspect TaleWorlds and NavalDLC implementation details before changing Harmony patches.
  - Treat it as read-only reference code, not part of this repository.
  - When a patch target is unclear, search the decompiled tree first rather than guessing method names or control flow.
  - Use `.github\scripts\Decompile-Bannerlord.ps1` for automated regeneration.
  - The script supports:
    - `-InstallTool` to install `ilspycmd` automatically as a global .NET tool
    - `-DecompilerOutputRoot` to choose the parent folder for decompiled output
    - automatic game-version detection from `Modules\Native\SubModule.xml`, so output goes under a versioned subfolder such as `D:\develop\src\Bannerlord.Binaries\v1.4.7`
    - omitting `-Assemblies` decompiles every assembly in the script's map; pass `-Assemblies` to limit regeneration to the assemblies relevant to the patch you are changing
    - `NavalDLC*` assemblies are skipped automatically for game versions earlier than `v1.3`
  - Example usage:
    - `.\.github\scripts\Decompile-Bannerlord.ps1 -InstallTool`
    - `.\.github\scripts\Decompile-Bannerlord.ps1 -DecompilerOutputRoot D:\develop\src\Bannerlord.Binaries -Assemblies TaleWorlds.MountAndBlade,SandBox,NavalDLC`

## Key conventions

- Treat `master` and `v1.3.15` as separate release lines.
  - `master` uses `v5.4.x` versions for Bannerlord `v1.4.7`.
  - `v1.3.15` uses `v5.3.x` versions for Bannerlord `v1.3.15`.
  - Do not put branch-specific fixes into the other branch’s changelog.

- Keep code commits separate from release commits.
  - Feature/fix commits land first.
  - Version/changelog updates are committed afterward as a dedicated release commit.

- When changing user-facing text, update all linked localization sources together:
  - `ModuleData\module_strings.xml`
  - English `Languages\std_*.xml`
  - Chinese `Languages\CNs\*.xml`
  - French `Languages\FR\std_*.xml`
- The Chinese site/workshop/docs are maintained separately from in-game localization. Relevant files commonly include:
  - `CHANGELOG-zh-CN.md`
  - `MountBladeComCnDescription.txt`
  - `SteamWorkshopReadMe*.txt`

- `MountBladeComCnDescription.txt` uses merged headings when the same notes apply to both release lines, for example `v5.4.14 / v5.3.36`. Branch-only items are prefixed inline, such as `(v5.4.14)...`.

- Reuse existing option/config patterns instead of inventing new ones:
  - add config fields in `...Config.cs`;
  - expose them in `...OptionClassFactory.cs`;
  - back labels/hints with `module_strings.xml` plus all maintained language files.

- Localization changes are usually multi-surface changes, not single-file edits:
  - in-game strings live in `module_strings.xml` plus the English, Chinese, and French language XMLs;
  - player-facing docs and storefront text live separately in `README.md`, `README.zh-CN.md`, `SteamWorkshopReadMe*.txt`, and `MountBladeComCnDescription.txt`;
  - release-note wording between `CHANGELOG.md`, `CHANGELOG-zh-CN.md`, and `MountBladeComCnDescription.txt` is intentionally synchronized but may still carry branch-specific prefixes in the Chinese site description.
- Prefer matching existing string IDs and naming style when adding options or order text. New options typically need:
  - config field
  - option view-model entry
  - `module_strings.xml` entry
  - English/Chinese/French translations

- Optional-mod compatibility is usually implemented by detecting installed modules in submodule load and gating either:
  - which Harmony patches are applied; or
  - which options/orders are exposed.

- The build output and release metadata are repository state, not incidental artifacts. Changes to docs, changelogs, Workshop text, module versions, and `.github\resources\config.env` are part of normal release work in this repo.
