# Changelog

## [e3.9.6] - 2020-10-25
### Fixed
- Fix the problem that config key page is transparent.

- Fix the problem that troop cards all show infantry icon, regardless of the actual troop type.

- Fix several minor problems.

## [e3.9.5] - 2020-10-20
### Fixed
- Fix the problem that UI becomes transparent in Bannerlord e1.5.4.

## [e3.9.4] - 2020-10-19
### Fixed
- Fix the problem that switch team doesn't work.

- Fix the problem that shift to accelerate camera does not work in e1.5.4.

## [e3.9.3] - 2020-10-19
### Changed
- Minor changes.

### Fixed
- Keep compatible with Bannerlord e1.5.4.

## [e3.9.2] - 2020-10-16
### Fixed
- Fix the problem that in transfer command, cursor is not shown.

## [e3.9.1] - 2020-10-15
### Fixed
- Fix minor problems.

## [e3.9.0] - 2020-10-15
### Added
- Player can select formations by clicking troop card.

### Fixed
- Fix a problem that player may be added twice to the same formation arrangement. It happens when player is using a siege weapon and switch to free camera mode. Now player character will stop use siege weapon when switch to free camera mode to avoid this problem.

- Fix the problem that in free camera mode pressing F may still make player character interact with other characters or siege weapons.

### Changed
- Change "Control Another Hero" to "Watch Another Hero". You can press F to control the hero when watching it.

## [e3.8.2] - 2020-10-05
### Fixed
- Fix a rare crash in base game.

- Fix the problem that movement command cannot be given when paused.

- Fix the problem that selecting formation with middle mouse button does not work when paused.

### Changed
- Improve the behavior of choosing character to control when the player character is killed in battle.

## [e3.8.1] - 2020-10-02
### Added
- Highlight order flag to make it more conspicuous.

### Changed
- Optimize performance.

### Fixed
- Fix bug of selecting formation.

## [e3.8.0] - 2020-09-28
### Added
- Add fix for circular arrangement of the base game.

### Fixed
- Fix compatibility with extension.

## [e3.7.2] - 2020-09-26
### Fixed
- Fix cavalry AI in "Charge to formation" mode.

## [e3.7.1] - 2020-09-26
### Fixed
- Fix crash in Bannerlord 1.5.1.

## [e3.7.0] - 2020-09-26
### Added
- Add "Watch battle" menu option when player is injured. You can command your troops in free camera but cannot take control of any units and directly fighting.

### Fixed
- Fix the problem that camera elevation angle and camera height may suddenly change when switching between player camera and free camera.

  Now the movement of the camera is smoother.

### Changed
- Now cheat options are only enabled when cheat mode of the base game is enabled.

## [e3.6.3] - 2020-09-19
### Fixed
- Fix a crash when pressing F at the end of tournament.

## [e3.6.2] - 2020-09-18
### Fixed
- Fix a crash in free camera when click "Begin Assault" button in siege.

## [e3.6.1] - 2020-09-18
### Fixed
- Enable "Attack Specific Formation" by default.

## [e3.6.0] - 2020-09-18
### Added
- Add "charge to formation" feature. You can click mouse middle button to an enemy formation and your soldiers will charge to it.

- Add camera smooth movement when camera mode changes or player character changes, etc.

- Free camera now uses smooth rotation by default.

### Changed
- Use middle mouse button instead of left mouse button to select formation to avoid accidentally giving movement orders when "Click to Select Formation" is enabled.

- Optimize behavior of click formation.

### Fixed
- Fix a rare crash that occurred when switch free camera too quickly.

- Fix the problem that player may charge to enemy alone.

- Fix the problem that player character may goes out of battle field and causes retreat in free camera mode.

- Fix the problem that banner on player character is not shown in free camera mode.

### Removed
- Remove "Prevent Player Fighting" option because the related problem has been fixed.

## [e3.5.15] - 2020-09-05
### Fixed
- Fix the problem of that ctrl or alt key does not work. Now camera speed can be adjusted by pressing ctrl + mosue scroll again.

- When the HUD is hidden, opening any focused view (such as when entering a conversation, pressing esc, etc.) will cause the HUD to be temporarily enabled.

- Fix player AI in arena practice after switched to free camera.

- Fix the problem that multiple player character may be spawned when switched to free camera in arena practice.

## [e3.5.14] - 2020-08-24
### Fixed
- Fix the problem that ally formation is wrong highlighted when "show contour" is enabled.

## [e3.5.13] - 2020-08-19
### Fixed
- Keep compatible with Bannelord e1.5.0.

## [e3.5.12] - 2020-08-15
### Fixed
- Fix hint text when selecting character.

### Changed
- Optimize behavior when pressing F key.

## [e3.5.11] - 2020-08-15
### Changed
- Rearrange UI of mod menu.

### Added
- Add hint for each button in mod menu.

## [e3.5.10] - 2020-08-09
### Fixed
- Fix key configuration.

- Fix crash when controlling soldier in hideout.

### Changed
- Soldiers within 20 meters will be considered first when looking for soldier to control.

- Enable selecting formation feature by default.

- Slow motion config will be saved when changed using hotkey.

## [e3.5.9] - 2020-08-02
### Added
- Enable feature that you can select formation by directly click on them.

### Fixed
- Fix crash when entering battle, assigning player to an empty formation and then giving an order such as shield wall.

## [e3.5.8] - 2020-08-01
### Fixed
- Fix the problem that cursor is not shown when give commands in free camera mode.

## [e3.5.7] - 2020-07-31
### Fixed
- Fix selecting character feature.

## [e3.5.6] - 2020-07-31
### Fixed
- Fix the problem that selecting character hint may be shown after conversation.

## [e3.5.5] - 2020-07-31
### Fixed
- Keep compatible with Bannerlord e1.4.3.

## [e3.5.4] - 2020-07-31
### Added
- Add feature: You can press `;` then left click to select a soldier, then lock camera to it or control it by pressing `F`.

- Add an option to prevent player fighting, to solve the problem that player character may charge into enemy formation when switched to free camera mode.

- Add an option to always set player's formation.

### Changed
- The default key to open RTS menu is changed from 'O' to 'L'.

## [e3.5.3] - 2020-06-07
### Fixed
- Fix the problem that changing config file may not work.


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
- Fix crash in tournament when controlling ally after player dead.

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
- Add options for controlling ally.

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

