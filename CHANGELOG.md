# Changelog

## [e3.5.2] - 2020-06-07
### Changed
- Disable show contour feature to avoid problems.

## [e3.5.1] - 2020-06-06
### Added
- Add an option for disabling hotkey for switching team.

### Changed
- Reset hotkeys of previous version to prevent problems relating to hotkeys.

## [e3.5.0] - 2020-06-04
### Added
- Add feature: Now player can select troops by directly clicking on them.

- Troops that will be selected and has been selected will be highlighted with contour.

### Changed
- Player formation logic is reverted to the previous version.

## [e3.4.0] - 2020-06-02
### Fixed
- Fix the problem that in RTS camera mode, when player starts to use ranged siege machine, the camera will be fixed on the siege machine.

- Fix the problem that when player are using siege machine, switch RTS Camera on and off may cause the player unable to use siege machine by AI any more.

### Changed
- Now player will only be added to formation when switching to RTS Camera mode and will be removed from formation when switching back.

## [e3.3.2] - 2020-05-31
### Fixed
- Fix broken hot key for switching team.

## [e3.3.1] - 2020-05-31
### Added
- Add switch team feature. Press F12 by default to use it.

## [e3.3.0] - 2020-05-29
### Fixed
- Fix harmony patch.

### Added
- Add "constant speed" option: Camera speed will not be affected by camera height after toggled on.

- Add "outdoor" option: After toggled off, camera can go into houses.

- Add "restrict by boundaries" option: After toggled off, camera can go out of scene boundary.

## [e3.2.2] - 2020-05-27
### Fixed
- Fix crash in tournament when controlling allies after player dead.

## [e3.2.1] - 2020-05-25
### Changed
- Change logic of controlling companions.

- Change the thickness of raycast that determines depth of field distance from 0.01 to 0.5.

### Fixed
- Fix crash in siege.

- Fix a problem that adjusting camera parameters in Cinematic Camera may cause camera rotates.

## [e3.2.0] - 2020-05-23
### Changed
- rename Module folder to RTSCamera; rename project to RTS Camera.

## [e3.1.0] - 2020-05-21
### Added
- Add options for controlling allies.

- Add support for Cinematic Camera.

## [e3.0.0] - 2020-04-25
### Fixed
- Keep compatible with Bannerlord e1.3.0.

## [e2.0.1] - 2020-04-25
### Fixed
- Fix the problem that change to target reticule during a battle will be reverted after battle.

### Changed
- Change mod name to RTS Camera.

## [e2.0.0] - 2020-04-17
### Fixed
- Fix the bug that the quit text may become "retreat" rather than done after victory.

- Fix the bug that the targeting reticule may be hidden accidentally.

### Changed
- Overhaul the extension feature.

## [e1.0.11] - 2020-04-15
### Added
- When switch to rts camera, it will be raised to a configurable height.

- Now rts camera can lock agents by left click or right click when order UI is closed.

### Changed
- Remove restriction that config key cannot conflict with each other.

- Now opening mod menu will cause HUD temporarily enabled to show the menu.

### Fixed
- Fix the problems that when game is paused, the rts camera cannot be rotated by putting mouse on the edge on the screen.

## [e1.0.10] - 2020-04-14
### Fixed
- Fix the bug that after clicking "toggle HUD" button in mod menu, the mod menu is not closed.

## [e1.0.9] - 2020-04-14
### Added
- Add toggle HUD feature.

  You can press `]` key to toggle HUD. Or if you rebind the key and forget what you have set it to, you can press `Home` key to toggle UI, which always works.

### Changed
- Change default key for disable death to `End`.

### Fixed
- Fix the problem that targeting reticule is shown when rts camera is enabled and player is using ranged weapon.

- Fix a crash when switching to free camera after victory.

## [e1.0.8] - 2020-04-13
### Fixed
- Fixed the bug that display message option is not saved.

## [e1.0.7] - 2020-04-13
### Changed
- Change slow motion mode logic.

### Add
- Add hot key for slow motion mode. Default to ' key.

- Add display message option.

## [e1.0.6] - 2020-04-12
### Added
- Save config for "change combat ai" and "use realistic blocking" options.

## [e1.0.5] - 2020-04-12
### Fixed
- Support Bannerlord e1.1.0

### Changed
- Move "use realistic blocking" and "change combat ai" feature to another mod called "EnhancedMission Change AI"(renamed to "Improved Combat AI" now).

## [e1.0.4] - 2020-04-12
### Added
- Add "use free camera by default" option.

- Use mouse to move camera.

### Changed
- Now RTS camera will not be interrupted by player's death.

### Fixed
- Player can drag on the ground when game is paused or slow down, defictive though.

## [e1.0.3-hotfix] - 2020-04-11
- Fix bug that hot key config will be reset to default after each game start.

## [e1.0.3] - 2020-04-11
### Fixed
- Fix crash when new config for hot key is created.

### Changed
- Enable smooth mode for rts camera.

## [e1.0.2] - 2020-04-10
### Fixed
- Fix the bug that player can control enemy troop after player dead.

## [e1.0.1] - 2020-04-10
### Added
- Add key rebinding feature.

## [e1.0.0] - 2020-04-09
### Added
- Switch to rts-style camera and issue orders.

  Add player to a individual formation.

- Control your troop after dead.

- Pause mission.

- Adjust mission speed.

- Adjust combat AI.

- Option to use realistic blocking introduced in b0.8.1.

- disable and enable damage.

