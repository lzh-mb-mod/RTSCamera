# RTS Camera

A mod for Mount&Blade II Bannerlord that provides free camera and better commanding in battle.

## Save Compability
This mod does not read or write stuffs to your game save. Installing or removing this mod has no impact on your game save.

## Main features

This mod is composed of 2 parts, RTS Camera and Command System.

- RTS Camera

  - Smoothly toggle free camera mode at any time in a battle.

  - Stick camera to selected formation.

  - Control one of your soldiers and continue fighting/commanding after player character is injured in battle.

  - Time speed control in battle. Automatic fast forward in hideout battle.

- Command System

  - Reworked the command system in the base game.

  - Troops with throwing weapons will correctly keep distance under advance order.

  - Allows to give a series of orders and let your troops execute them one by one. For example, move along a given path and then shooting/charging.

  - When giving orders to multiple formations:

    - Relative positions/directions of selected formations are locked in moving order and turning order.

    - Layout between selected formations are improved when player drags on the ground.

  - Add volley order.

    - Auto Volley: soldiers will aim and wait for others and fire when most soldiers are ready.

    - Manual Volley: soldiers will aim and wait for your orders to fire.

    - Volley Fire: Let soldiers in manual volley to fire.

  - In siege battle you can click on siege weapons to make your troops stop using it.

  - Square and Circle formations are improved.

  - Fix issues related with formation width and unit spacing in the base game.

  - Issue focus charge command by clicking middle button on enemy units.

  - Select your troops by clicking middle button on units.

  - Set attack target without interrupting previous movement orders. For example, **let your archers focus shooting while holding position**.

  - Facing to enemy order now has an optional enemy formation target.

  - Movement target becomes more visible.

  - Select your troops and give orders by directly clicking on order UI.

## Main usage

- RTS Camera
  - You can press `F10` to toggle free camera.

  - In free camera mode, while command UI is open, you can press `E` to stick camera to selected formation.
  
  - After your character is killed in battle, you can press `E` to follow one of your soldiers and press `E` again to control it and continue fighting.

    - You can press `L` to open menu and set `When To Control Ally After Death` option to 'Always", so that you will control another unit automatically after you are down.

- Command System

  - Hold `Shift` and give orders to add orders to queue. Your troops will execute them one by one.

  - If you move/turn multiple formations at the same time, their relative positions/directions will be locked.

    - If you give move order by dragging on the ground, formations will be placed better than they are in the base game.

    - Hold `Ctrl` while dragging on the ground, formations' widths and relative positions will be locked.

    - Hold `Alt` to disable the locking behavior described above.

  - You can make your formation charge to the enemy formation by clicking middle mouse button on enemy units.

    - Hold `Alt` while clicking to target the enemy without interrupting previous movement orders. For example, **let your archers focus shooting while holding position**.

  - You can select your formations by clicking middle mouse button on soldiers.
  
  - You can select your formations by clicking on troop cards and give orders by clicking on order buttons.

    - Hold `Alt` to show the mouse.

  - Auto Volley: Press `F3 F6` (if using legacy order layout, `F8 F1`) or `H`.

  - Manual Volley: Press `F3 F7` (if using legacy order layout, `F8 F2`) or `J`.

  - Volley Fire: Press `F3 F8` (if using legacy order layout, `F8 F3`) or `K`.

- For more features, you can read `Details` below or press `L` to open mod menu.

## How to Install
* Unzip the downloaded zip file. Copy `RTSCamera` and `RTSCamera.CommandSystem` folder you downloaded into Bannerlord `Modules` folder.
* Unblock all the dlls at (GamePath)\Modules\RTSCamera\bin\Win64_Shipping_Client and (GamePath)\Modules\RTSCamera.CommandSystem\bin\Win64_Shipping_Client:
  * Right click on each dll, click Properties and check "Unblock".
  * Or: go to (GamePath)\Modules, right click, open Powershell, and then run:  gci -Recurse RTSCamera* | Unblock-File

## Details

- In a battle, press `L` to open detailed menu of this mod.

  - Switch between options of `RTS Camera` or `Command System` by clicking tab list on the left of the menu.

  - You can close the menu by pressing `esc`, left clicking outside the menu or right clicking anywhere.

### RTS Camera

- Press `F10` to toggle free camera.
  
  Player character will be controlled by AI when switched to free camera.

  - If it shows “can’t give orders in this moment”, it's because your character is down and you are not controlling any unit. Press E twice to control a unit, then you can continue to issue orders.
  
  - Order UI will be opened automatically.

  - You can enable `Switch Camera on Ordering` option, to always give orders in free camera.

  - When command UI is open in free camera mode, you can press `E` to stick camera to selected formation.

  - You can enable `Slow Motion On RTS view", to turn on slow motion in free camera. 

  - Controls in free camera:

    - Use `W`, `A`, `S`, `D`, `Space`, `Z` and mouse middle button to move the camera.

    - Use `shift` to speed up camera movement.

    - Move your mouse to rotate the camera, or when order panel is opened, drag right mouse button to rotate the camera.

    - Left click on the ground and drag to change the position, direction and width of the formation you selected.

    - Hold `right ctrl` and `up` or `down` to adjust camera movement speed.

    - Hold `right ctrl` and click middle mouse button to reset camera movement speed.

    - Hold `ctrl` and drag middle mouse button vertically to adjust camera height.

- After your character is killed in battle, you can press `E` to follow one of your soldiers and press `E` again to control it and continue fighting.

  - You can immediately control one of your soldiers to avoid all your formations been delegated to AI when your character dies, by setting `When To Control Ally After Death` option to `Always`. It's always enabled in free camera mode to ensure a smooth gaming experience.

  - Soldiers in the same formation as player character will be considered first when deciding which soldier to control.

- When the camera is following a character, you can press `E` to control the character. You can make the camera follow a character in the following ways:

  - In free camera mode press `E` to follow the player character.

  - Press `L` to open the menu and selecting a hero in `Watch Another Hero` drop-down list.

  - Press `;` and click a character, then pressing `E`.

  - Press left/right mouse button to change the character that the camera is following.

- If your character is injured in campaign map and you encounter an enemy party, you can still choose `Command the battle` option to begin the battle. In this battle you can command your troops in free camera but cannot directly control a character and fighting.

- Camera distance to player character can be limited by enabling `Limit Camera Distance` option.

  - Distance limit is determined by scouting and tactics level.

  - After `Limit Camera Distance` is enabled, using free camera and ordering in free camera can improve scouting and tactics skill level.

- Click on castle gate won't attack the enemy formation behind the gate, which is an issue in the base game.

### Command System
- Hold `Shift` when giving orders to add the order to queue. Your troops will execute orders in queue one by one.

  - Order queue will be cleared if you give new orders without holding `Shift`.

  - Movement targets in queue will be marked as flags.

  - In free camera mode, movement paths will be marked using arrows.

- If you move/turn multiple formations at the same time, their relative positions/directions will be locked.

  - If you give move order by dragging on the ground, formations will be placed better than they are in the base game.

  - Hold `Ctrl` while dragging on the ground, formations relative positions and widths will be locked.

  - If you don't want the locking behavior described above, hold `Alt` and the behavior will be reverted to original.

- Improved square and circle formation:

  - When switching between square/circle formation and other formations, the unit count in the outside rank is kept the same as the unit count of the first rank of line/shield wall/loose formation if possible.

    This allows soldiers to form formations more quickly and avoids them crowding together.

  - In the base game when the unit count in square formation is square number, the center unit is not correctly placed. This is fixed in RTS.

  - Player can use hollow square formation. Not enabled for AI.

  - When formation is switched to circle formation, the circle formation will be tighter. This can be changed in option.

- Fixed the following issues in the base game (at least exists in v1.2.12):

  - Issue: If you switch to circle formation and back to line formation, formation width becomes much longer as it should be.

  - Issue: If you drag on the ground to lower the formation width and unit spacing, and in the next order move the formation, the unit spacing in preview is inconsistent with the actual order given.

  - Issue: If you place the formation to a narrow place by clicking on the ground, and in the next order move it back, the formation width is not recovered. In this mod the formation width is changed only when you drag on the ground.

  - Issue: (Especially to circle/square formation) If you drag on the ground to set the formation width to minimum, press `F1` and place the order flag next to a wall without actually giving orders, the frame rate will drop dramatically.

  - Issue: If you try to give order to attack the gate in siege, it may results in a order attacking the enemy formation behind the gate.

- You can make your formation charge to the enemy formation by clicking middle mouse button on enemy units.

  - After the enemy formation is eliminated, your troops will stay at where they are. To change this behavior, you can press `L` to open menu, and set the option `After enemy formation eliminated` to `Charge`.

  - The target enemy formation that your selected troops are charging to will be highlighted with red color when you open command panel.

- Hold `Alt` while clicking to target the enemy without interrupting previous movement orders.

  - For archers they can shoot target enemy while holding positions.

  - Note that for melee units, they will still follow previous movement orders, which may limit their ability to reach the target.

- Place your mouse on the enemy formation marker and giving facing to enemy order. You formation will facing to the selected enemy formation only.

- Movement target marker is more visible. The original marker is hard to see if camera is too high.

- You can select your formations by clicking middle mouse button on soldiers.

  - Selected troops will be highlighted with green color when you open command panel.

- Your formation targeted by the enemy formation will be highlighted with dark blue color when you open command panel.

- Movement orders will be shown under troop cards.

- Fix direction of units on the corner of square formation.

## Hotkeys
You can config hotkeys by pressing `L` to open menu, and click `Config Key`.

You can click `Add Shortcut` to add a new way to trigger hotkey.

You can click `Extend Key Combo` to add more keys into key combo. The hotkey will be triggered when all the keys in a key combo are pressed.

Here is a list of default hotkeys:

RTS Camera:
- `L`: open mod menu.

- `F10`: Toggle free camera.

- `E`: Focus on troop/formation; Control troop.

- `;`: Select character.

- `w`, `A`, `S`, `D`: Camera movement.

- `Space`, `Z`: Camera up/down.

- `Right Shift` + `=`, `Right Shift` + `-`: Increase/Decrease camera distance limit.

Command System:
- `Middle Mouse Button`: Select formation.

- `Left Alt` or `Right Alt`: Keep movement order when attacking the enemy formation; Toggle formation lock behavior.

- `Left Shift` or `Right Shift`: Add command to queue.

- `Left Ctrl` or `Right Ctrl`: Keep formation width when dragging.

- `H`: Auto volley.

- `J`: Manual volley.

- `K`: Volley fire.

There're more hotkeys configurable but are disabled by default. You can configure them by pressing L and click Config Key on the top.

## Configuration
- The configuration files are saved in directory `(user directory)\Documents\Mount and Blade II Bannerlord\Configs\RTSCamera\` and `(user directory)\Documents\Mount and Blade II Bannerlord\Configs\MissionLibrary\`.

  The main config is saved in files `RTSCameraConfig.xml` and `CommandSystemConfig.xml`.

  The hot key config is saved in file `RTSCameraGameKeyConfig.xml` and `CommandSystemGameKeyConfig.xml`.

  The hot key that opens the menu is saved in file `(user directory)\Documents\Mount and Blade II Bannerlord\Configs\MissionLibrary\GeneralGameKeyConfig.xml`.

  You can modify them manually, but if you edit them incorrectly or remove them, the configuration will be reset to default.

## War Sail DLC Related
### RTS Camera
- When switch to free camera mode, AI will control your character and pilot player ship. You can give ship orders to player ship, such as Engage, Skirmish, etc.

- When player ship is piloted by AI, selecting all formations will no longer exclude the playe ship, so that it will be easier to give orders like "all ships attack!". 

- When player is injured and switched to another soldier on the other ship, ship orders will be updated accordingly so that you can give ship orders to the old ship (engage, skirmish, etc.) and troop orders to the new ship (defend ship, charge, etc.).

- When you are controlling your character and not piloting your ship, a new order (with hotkey F5) to toggle soldier piloting your ship will be added. This is the same as the Helmsman mod.

  To avoid conflict and weird behavior, when RTS Camera detects Helmsman is installed, it will not add the new order for the first ship.

  For the other ships, Helmsman doesn't take effect, so RTS Camera will add the new order for them.

  - Note that in this case, as of Helmsman v1.0.0, using Helmsman to toggle controller of the first ship when you are piloting other ships may cause your player character stops piloting.

## Troubleshoot
- If the launcher can not start:

  - Uninstall all the third-party mods and reinstall them one by one to detect which one cause the launcher cannot start.

- If it shows "Unable to initialize Steam API":

  - Please start steam first, and make sure that Bannerlord is in your steam account.

- If the game crashed after starting:

  - If it shows: `Cannot load: ..\..\Modules\RTSCamera\bin\Win64_Shipping_Client\RTSCamera.dll`，please unblock all the dll files under `Modules\RTSCamera` and `Modules\RTSCamera.CommandSystem`.
  
  - I would appreciate it if you send dump file to me to help me to solve the crash by followinig steps below:

    - Click `Yes` when the game crashes and ask whether to collect information.

    - Before sending files to TaleWorlds, go to `C:\ProgramData\Mount and Blade II Bannerlord\crashes`(**Not the game installation path**) and find the folder related to the crash by timestamp.

    - Then send `dump.dmp` file in the folder to me.

- If you forget the hotkey set for opening menu:

  - You can remove the config file so that config will be reset to default.

## Optional File
- ["Battle Mini Map"](https://www.nexusmods.com/mountandblade2bannerlord/mods/2672): Battle mini map.

- ["Improved Combat AI"](https://www.nexusmods.com/mountandblade2bannerlord/mods/449/): Adjusting combat AI.

- ["Cinematic Camera"](https://www.nexusmods.com/mountandblade2bannerlord/mods/1627): Adjusting camera parameters such as moving speed, depth of field, etc.

## Source Code

You can get source code at [github.com](https://github.com/lzh-mb-mod/RTSCamera).

## Contact with me
- Email:

  - lizhenhuan1019@outlook.com

  - lizhenhuan1019@qq.com
