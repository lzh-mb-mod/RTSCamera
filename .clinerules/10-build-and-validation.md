# Build and validation

## Prerequisites

- Initialize submodules before the first build:
  `git submodule update --init --recursive`
  The solution depends on `source\library\mission-library`.
- Local builds need a Bannerlord install path via the MSBuild property `GamePath`.
  The default local path is stored in
  `source\library\mission-library\source\BasicSharedLibrary\GamePath.props`.
- On Windows, net472 builds also deploy straight into the configured Bannerlord
  `Modules` folder, so a successful local build mutates the game install.

## Commands

Quick local build (preferred for iteration):

```powershell
dotnet build source\RTSCamera.sln --no-restore
```

CI-style Release build (what `.github\workflows\Build.yml` does):

```powershell
nuget restore .\source\RTSCamera.sln
MSBuild.exe .\source\RTSCamera.sln /p:Configuration=Release /p:GamePath="<Bannerlord path>"
```

Single project (faster when only one module changed):

```powershell
dotnet build source\RTSCamera\RTSCamera.csproj --no-restore
dotnet build source\RTSCamera.CommandSystem\RTSCamera.CommandSystem.csproj --no-restore
dotnet build source\RTSCameraAgentComponent\RTSCameraAgentComponent.csproj --no-restore
```

## Tests and lint

- No automated test project, test runner, or lint configuration exists in the repository.
  Do not invent `dotnet test` steps or claim test coverage.
- Manual regression scenarios: `manual_test_cases.md`.
- Treat "builds cleanly" as the maximum automated guarantee, then list the in-game
  scenarios the user must verify.

## Verifying Harmony patches

- Automated build success does not prove a patch still applies. Patches and transpilers only
  fail at runtime, when Harmony resolves the target method or IL.
- Previous sessions used a session-only console harness (a .NET 6 project that loads the
  built `RTSCamera.dll` / `RTSCamera.CommandSystem.dll` plus decompiled game DLLs, then
  invokes every `Patch(Harmony)` method and runs transpilers over the original IL). It lived
  under the Copilot session directory and its hard-coded paths are stale. Recreate it under
  a scratch folder outside the repository when a full patch-application check is needed;
  never commit it.
- For targeted checks, confirm the target member still exists in the decompiled source:
  `D:\develop\src\Bannerlord.Binaries\<version>\...`. Do not validate against a different
  game version than the branch targets.

## Decompiled reference sources

- Decompiled TaleWorlds/SandBox/NavalDLC sources are available at
  `D:\develop\src\Bannerlord.Binaries`, one folder per game version
  (`v1.2.12`, `v1.3.4`, `v1.3.5`, `v1.3.15`, `v1.4.5`, `v1.4.6`, `v1.4.7`, `v1.4.8`,
  `v1.5.0`, `v1.5.1`, `v1.5.5`).
- Treat them as read-only reference code. They are not part of this repository.
- Regenerate with `.github\scripts\Decompile-Bannerlord.ps1`:
  - `-InstallTool` installs `ilspycmd` as a global .NET tool.
  - `-DecompilerOutputRoot` selects the parent output folder.
  - The game version is detected automatically from `Modules\Native\SubModule.xml` and used
    as the versioned subfolder name.
  - Omitting `-Assemblies` decompiles every mapped assembly; pass a comma-separated list to
    limit regeneration to the assemblies behind the patch you are changing.
  - `NavalDLC*` assemblies are skipped automatically for game versions earlier than `v1.3`.

```powershell
.\.github\scripts\Decompile-Bannerlord.ps1 -InstallTool
.\.github\scripts\Decompile-Bannerlord.ps1 -DecompilerOutputRoot D:\develop\src\Bannerlord.Binaries -Assemblies TaleWorlds.MountAndBlade,SandBox,NavalDLC
```

## CI and packaging

- `.github\workflows\Build.yml` restores NuGet packages, builds Release, packages both
  modules, uploads artifacts, and creates the GitHub release.
- `.github\resources\FileFilters.regexp` controls which game assemblies the pipeline
  downloads for compilation; add a pattern there when a new TaleWorlds assembly is referenced.
- Packaging is done inside the project files: each module copies assemblies and
  `Modules\...` assets into `source\package\net472\...` and `source\package\net6.0\...`.
