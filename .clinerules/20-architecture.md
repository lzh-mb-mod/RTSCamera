# Architecture

## Solution layout

- `source\RTSCamera` — free camera, elevated camera, player control, mission speed,
  campaign integration, naval/TOR-specific behavior.
- `source\RTSCamera.CommandSystem` — order UI, command queue, formation logic,
  volley/defensive-hold orders, troop highlighting, order preview behavior.
- `source\RTSCameraAgentComponent` — shared mission-starting agent component used by both
  modules.
- `source\library\mission-library` (git submodule) — shared menu, config, mission startup,
  and utility infrastructure imported through shared projitems. Changes here must be
  committed in the submodule repository first, then the parent pointer.

## Runtime entry points

`RTSCameraSubModule` and `CommandSystemSubModule` are the only module entry points. They:

- detect optional modules/mods (`NavalDLC`, `Helmsman`, `TOR_Core` for RTSCamera;
  `RBM`, `TAOM` for CommandSystem);
- register Harmony patches;
- register option screens and hotkey categories;
- attach mission-start handlers and shared agent components.

Optional-mod support means: detect the installed module during submodule load, then gate
either which Harmony patches are registered or which options/orders are exposed. Never take
a hard assembly reference on an optional DLC; use reflection with cached accessors.

## Behavior layers

Most gameplay behavior lives in three layers per module:

- `src\Patch\...` — Harmony patches against TaleWorlds/Bannerlord classes. `src\Patch\Fix\`
  holds narrower workarounds; `src\Patch\Naval\` holds NavalDLC-only compatibility.
- `src\Logic\...` — long-lived mission behaviors and sub-logic objects.
- `src\View\...` — mission views and visual helpers (Gauntlet movies and layers).

`RTSCamera.CommandSystem` centers on queued and augmented orders:

- visual order providers translate UI or hotkeys into custom orders;
- `CommandQueueLogic` stores queued orders and tracks per-formation execution state;
- patch classes around `OrderController`, `OrderTroopPlacer`, `MissionOrderVM`, and Gauntlet
  order handlers keep previews, dragging, and issued orders in sync.

## Options and configuration

Options are driven by MissionLibrary menu/view-model infrastructure:

- persisted config objects: `src\Config\...Config.cs`;
- menu structure and callbacks: `src\Config\...OptionClassFactory.cs`;
- many option callbacks update live mission behaviors immediately instead of only saving
  config, so option changes can require touching logic code too.

## Important project-wide references

- `source\RTSCamera.sln` — solution including both modules, the agent component, and
  mission-library shared projects.
- `source\RTSCamera\src\RTSCameraSubModule.cs` — patch registration surface for RTSCamera.
- `source\RTSCamera.CommandSystem\src\CommandSystemSubModule.cs` — patch registration and
  order UI configuration for CommandSystem.
- `source\RTSCamera.CommandSystem\src\Logic\CommandQueueLogic.cs` — central queue logic.
- `source\RTSCamera.CommandSystem\src\Logic\SubLogic\FormationColorSubLogicV2.cs` —
  formation/agent highlighting.
