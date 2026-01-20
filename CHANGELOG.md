# Changelog

## [v5.3.23] - 2026-01-20
### Fixed
- Fix crash when attacked siege weapon is destroyed.

- Fix the issue that when player character likes to ride horse when controlled by AI.

- Fix the issue that Command System may create too much materials and make the game unstable.

## [v5.3.22] - 2026-01-19
### Fixed
- Fix incorrect hint text when stop using siege weapon.

- Skip using that siege weapon order in command queue when the siege weapon is destroyed.

## [v5.3.21] - 2026-01-18
### Added
- Add hint text when giving orders to use siege weapons.

### Improved
- After clicking on siege weapon to stop using it, prevent formation from automatically using it again.

### Fixed
- Fix missing volley hotkey text.

- Fix the issue that order UI is always closed when clicking on siege weapons in RTS view.

## [v5.3.20] - 2026-01-18
### Fixed
- Fix crash caused by auto volley hotkey in non-battle mission.

## [v5.3.19] - 2026-01-17
### Added
- Add auto volley order and related options.

- Hero list in mod menu will be sorted by order in player party member roster.

### Fixed
- Fix possible out of bound issue.

## [v5.3.18] - 2026-01-11
### Improved
- Improve performance when clicking on ground to move formations.

## [v5.3.17] - 2026-01-09
### Fixed
- Fix crash in tournament after a round, if watching the round and locking camera on a unit.

- Fix crash in loading screen when trying to defending the castle in the last stand.

## [v5.3.16] - 2026-01-08
### Added
- Add volley order.

- Add an option for disable native attack.

- Add an option for formation speeds sync behavior.

- Add an option to adjust the threshold for ratio of mounted units in a formation to determine whether to increase spacing between units. In vanilla the threshold is 0.1.

### Fixed
- Fix the issue that player character cannot be controlled after deployment if free camera is turned off in deployment stage.

- Fix the issue that trying to control a unit who is picking up items and then switch to free camera, the unit AI will be stuck in picking up state.

- Fix the issue that when multiple formations are given orders in command queue, they will not consume orders correctly.

### Improved
- Slightly improved performance of movement order.

## [v5.3.15] - 2025-12-28
### Fixed
- Fix crash with game version lower than v1.3.12, caused by distance fix in RTS mode.

## [v5.3.14] - 2025-12-28
### Fixed
- Fix the crash when war sail DLC is absent.

- Fix distance shown in RTS mode.

### Added
- Add an option to sync speed of locked formations under movement order. Locked formations will adjust movement speed to arrive at the target simultaneously.

## [v5.3.13] - 2025-12-18
### Fixed
- Fix the issue that RTS cannot be selected in BLSE launcher if game version is earlier than v1.3.11.

- Add hideout fastforward option back.

## [v5.3.12] - 2025-12-15
### Improved
- The unit's ground marker can be seen below.

- Improve square formation when unit count is square number.

- Improve unit position in square and circle formation.

- Improve performance when movement orders are shown.

### Fixed
- Fix potential crash at the end of siege.

- Fix order issue in sea battle when option "Switch Camera on Ordering" is turned on.

## [v5.3.11] - 2025-12-13
### Fixed
- Fix startup crash when there're other mods failing to load.

- Fix the issue that background of game option in campaign becomes transparent after exiting a scene.

- Fix the issue that clicking middle mouse button on the enemy formation or giving facing order using mouse button may close order UI in rts camera mode.

- Fix the issue that banner bearer is never highlighted.

## [v5.3.10] - 2025-12-11
### Added
- Troops in formations are highlighted when pressing alt.

### Fixed
- Fix time speed option.

- Fix the issue that background of game option becomes transparent after binding hotkey in the mod.

- Fix performance issue when giving orders.

## [v5.3.9] - 2025-12-11
### Fixed
- Fix startup crash on some environment.

- Fix crash in Naval battle on some environment.

- Fix performance issue when giving orders.

- Fix compatibility with Torch mod in Command Battle mode.

- Fix possible crash when time speed and fast forward both enabled.

- Disable mod menu popup in conversation.

## Added
- Add French translation.

## [v5.3.8] - 2025-12-08
### Fixed
- Fix the issue that in ship battle, when soldier pilot ship on, current movement order is not highlighted in order UI.

- Fix the issue that unit marker is still shown after being removed from formation in rare case.

- Fix the issue that setting hotkey of openning menu to left mouse button or right mouse button making it impossible to open hotkey UI.

### Improved
- Improve the default formation width after changing formation arrangement.

## Added
- Apply fix to advance order for throwing formation.

## [v5.3.7] - 2025-12-06
### Fixed
- Fix crash on startup when failing to loading game texts. This may happen when other mods have broken text files.

- Fix the issue that in winter season, the target arrow and unit marker will be covered by snow.

### Added
- Support keeping options hidden in mod menu.

## [v5.3.6] - 2025-12-05
### Fixed
- Fix crashes when starting battle and finishing deployment.

- Fix crash when player loses connection to ship and try to capture a new ship.

- Fix the issue that player is not AI controlled when default to free camera in battle.

- Disable and Hide formation option in naval battle.

## [v5.3.5] - 2025-12-01
### Fixed
- Fix the issue in the base game that player character cannot be teleported in deployment mode. Now during deployment stage player character can be teleported when controlled by AI in free camera mode.

- Fix the issue in previous version that order UI is closed after giving orders in deployment stage.

- Fix the issue in previous version that transfer troops UI cannot be opened.

### Improved
- Improved "Soldier Control Ship" command in sea battle.

## [v5.3.4] - 2025-11-29
### Fixed
- Fix a crash when switching camera frequently on ship.

## [v5.3.3] - 2025-11-29
### Added
- Add command "Soldier Control Ship On" and "Soldier Control Ship Off" if player is not piloting the ship. When turned on the ship will be piloted by soldier.

### Improved
- In sea battle, available commands depends on current camera mode and soldier control mode and will be refreshed instantly.

### Fixed
- Fix the issue that player cannot give orders to the first ship, when player is down and control units on other ships.

## [v5.3.2] - 2025-11-27
### Fixed
- Fix the crash when player is injured and command a sea battle.

## [v5.3.1] - 2025-11-26
### Fixed
- Fix the issue that player cannot use ship weapon.

## [v5.3.0] - 2025-11-26
### Fixed
- Support v1.3.4 and War Sail DLC.

## [v5.2.3] - 2025-09-24
### Fixed
- Fix support to select formations using left mouse button.

## [v5.2.2] - 2025-09-22
### Fixed
- Fix the issue that player cannot leaving in sneaking toturial mission.

## [v5.2.1] - 2025-09-22
### Fixed
- Fix crash related with incompatible shader.

- Fix crash when player is down.

### Changed
- Camera will not lock to other units automatically when the unit that camera is locking to is killed.

## [v5.2.0] - 2025-09-19
### Fixed
- Keep compatible with game v1.3.0.

## [v5.1.13] - 2025-09-14
### Fixed
- Fix crash in commanding mode when player party is in army.

- Fix the issue that agent may ride horses after switching to free camera

- Fix black screen when fast forwarding in hideout.

### Improved
- Size of circle formation is smaller when circle formation preference is set to "Tight".

## [v5.1.12] - 2025-09-12
### Fixed
- Fixed facing order in order queue.

- Fix highlight of focus attack.

## [v5.1.11] - 2025-09-10
### Fixed
- Fix the display of facing to enemy formation order.

## [v5.1.10] - 2025-09-10
### Added
- Support facing to specific enemy formation.

- Add option for circle formation tight/loose preference.

- Add option for clickable UI extension to allow setting command target when giving order by clicking UI.

### Fix
- Fix performance issue of hollow square formation.

- Fix the issue that facing order is determined by the formation with most units when moving multiple formations together.

## [v5.1.9] - 2025-09-09
### Added
- Support targeting when clicking advance or facing button by holding Alt.

- Add hotkey to fastforward.

- Add option to face the enemy by default.

- Add preview arrow for focus attack order.

### Fixed
- Fix several order position preview issues.

- Fix the issue that player may be stuck in walk mode after deployment.

- Fix the issue that hotkey config is reset on second startup.

### Changed
- Move clickable order UI to Command System.

## [v5.1.8] - 2025-09-01
### Fixed
- Fix the issue that in arena practice fight, ground marker is shown with black color.

### Changed
- Changed player formation option.

## [v5.1.7] - 2025-08-21
### Fixed
- Fix crash in The Old Realms.

## [v5.1.6] - 2025-08-20
### Added
- Make order button clickable.
- Add option to change movement target style.

## [v5.1.5] - 2025-08-18
### Added
- Linux is supported.

### Fixed
- Minor formation layout issue.

## [v5.1.4] - 2025-08-16
### Fixed
- Fix formation frame when AI controls formation.

## [v5.1.3] - 2025-08-16
### Added
- Add unit ground marker.
- Add formation frame marker.
- Add hollow square formation for player.

### Fixed
- Fix direction of corner units in square formation.

## [v5.1.1] - 2025-08-16
### Added
- Add option to not keep order UI open in free camera mode.

### Fixed
- Fix camera vertical movement speed.

### Changed
- Make movement target more visible at night by default.

## [v5.1.0] - 2025-08-13
### Added
- Hold shift to add orders to queue so that troops will execute them one by one. For example, let cavalry follow a given path and then charge.
- Multiple formations will be locked when moving together.

## [v5.0.7] - 2025-07-25
### Added
- Add option to fast forward in hideout.

## [v5.0.6] - 2025-07-24
### Changed
- Remake hotkey page to support multiple shortcuts. Note that shortcut config will be reset.
- Support controller input in free camera mode.

### Fixed
- Fix startup issue when Command system is enabled alone.

## [v5.0.5] - 2025-07-21
### Changed
- Replace option "Control ally after death" with "Control ally after death timing".
- Fast forward UI will be shown in free camera mode. In previous versions it's only shown when Control ally after death option is enabled.
- By default, switching to free camera will automatically control a unit if player unit is down to avoid issue that orders cannot be given.

## [v5.0.4] - 2025-07-19
### Fixed
- Fix crash caused by selectable troop cards.

## [v5.0.3] - 2025-07-18
### Fixed
- Fix an error message when issuing movement orders.

## [v5.0.2] - 2025-07-18
### Fixed
- Fix the issue in base game: the order position preview is not consistent with final order when dragging formation on the ground.
- Fix the issue in base game: narrowed width of a formation cannot be kept after a movement command.

### Added
- Added an option to show hotkey hint in free camera mode.
- Added an option to decide whether to rotate camera when switching from free camera.

## [v5.0.1] - 2025-07-13
### Fixed
- Fix crash when player is dead and switch to agent camera, if 'control ally after death' option is disabled.

## [v5.0.0] - 2025-07-12
### Added
- In free camera mode, while command UI is open, you can press `E` to stick camera to selected formation.
- Press `Alt` while middle-button clicking to target the enemy without interrupting previous movement orders. For example, **let your archers focus shooting while holding position**.
- Add option for always giving orders in free camera.
- Add slow motion in free camera.
- Automatically open ordering UI in free camera.
- Automatically switch to free camera in deployment stage.

### Changed
- Remove some default hotkeys to avoid accidental triggering.

## [e4.1.16] - 2022-10-14
### Fixed
- Fix crash in custom battle when issue orders.

- Fix the "Player Controller In Free Camera" option.

- Fix the issue that camera is not restricted by deployment boundary.

## [e4.1.15] - 2022-10-12
### Fixed
- Fix the issue that control hint doesn't correctly disappear when free camera mode isn't on.

### Changed
- Change the "Always Set Player Formation" bool option to dropdown option "Auto Set Player Formation" that contains "Never", "Deployment Stage" and "Always".

### Improved
- Optimized the algorithm of looking for character to control. Hero appears first in party member list will be selected first.

## [e4.1.11] - 2022-09-10
### Fixed
- Fix the issue that pressing `G` will cause player drop weapon in free camera mode.

## [e4.1.8] - 2022-09-05
### Added
- Limit camera distance by Tactics and Scouting skill.

- Use free camera and issue orders in free camera will give Scouting and Tactics skill xp.

## [e4.1.0] - 2022-08-23
### Fixed
- Keep compatible with Bannerlord e1.8.0

## [e4.0.0] - 2022-07-23
### Fixed
- Keep compatible with Bannlerord e1.7.2.

- Fix the issue that switching to free camera after victory may cause the game crash.

  I found that the game may crash if letting AI control the player character after victory.

  So the solution is not to set the player to AI mode after victory.

## [e3.9.25] - 2021-03-14
### Fixed
- Keep compatible with Bannerlord e1.5.9.

## [e3.9.24] - 2021-02-18
### Fixed
- Remove modification to infantry AI when Realistic Battle AI Module is enabled to avoid problems.

- Improve stability.

## [e3.9.21] [e3.9.22] - 2021-02-01
### Fixed
- Fix the problem that the player character may not be able to be controlled when battle wins in free camera mode.

- Fix a crash when there's only player in the team, battle wins, then switch to free camera.

## [e3.9.19] [e3.9.20] - 2021-01-31
### Fixed
- Fix wrong format of version number.

- Fix a crash when wander around in a city, switch to free camera, and press alt.

- Fix the problem that after victory, switch to free camera will cause the enemy cannot retreat.

## [e3.9.17] [e3.9.18] - 2021-01-30
### Fixed
-  Fix the bug when player is not the general and the 'always set player formation option' is on, the formation that player are assigned to will not be controlled by AI.

## [e3.9.15] [e3.9.16] - 2021-01-24
### Fixed
- Fix the bug that player cannot be assigned to the infantry formation.

## [e3.9.13] [e3.9.14] - 2021-01-16

### Added
- Add hotkey option for selecting formation.

- Add hint text for "Charge to formation" order.

### Fixed
- Fix the problem that disabling "Attack specific formation" option has no effect.

### Changed
- Improved AI under "Charge to Formation" order.

### Removed
- Remove "Fix circular arrangement" option.

## [e3.9.11] [e3.9.12]
### Fixed
- Fix the problem that when CommandSystem is disabled, after switching to enemy team, the order troop placer still gives order to original team.

## [e3.9.9] [e3.9.10]
### Fixed
- Fix the problem that hotkey are not saved.
- Fix crash when watch tournament and open the mod menu.

## [e3.9.8]
### Fixed
- Keep compatible with Bannerlord e1.5.6

## [e3.9.7]
### Fixed
Keep compatible with Bannerlord e1.5.5.
### Changed
Refactored the UI system.
Move features related to command system to a new mod called RTSCamera.CommandSystem.

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

