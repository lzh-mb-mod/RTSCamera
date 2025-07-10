# RTS Camera
(Old name: EnhancedMission, Enhanced Mission RTS Camera)

A mod for Mount&Blade II Bannerlord that provides free camera and more features in battle.

## Save Compability
This mod does not read or write stuffs to your game save. Installing or removing this mod has no impact on your game save.

## Main features

- RTS Camera

  - Smoothly toggle free camera mode at any time in a battle.

  - **New:** Stick camera to seleted formation.

  - Control one of your soldiers and continue fighting/commanding after player character is injured in battle.

  - Select your troops by directly clicking on troop cards.

- Command System

  - Issue focus charge command by clicking middle button on enemy units.

  - **New:** Focus attack on enemy units without interupting previous movement orders. For example, **let your archers focus shooting while holding position**.

  - Select your troops by clicking middle button on units.

  - Movement target becomes more visible on RTS view.

## Main usage

- RTS Camera
  - You can press `F10` to toggle free camera.

  - **New:** In free camera mode, while command UI is open, you can press `E` to stick camera to selected formation.
  
  - After your character is killed in battle, you can press `E` to follow one of your soldiers and press `E` again to control it and continue fighting.

    - You can press `L` to open menu and check `Control Ally After Death` option to do this automatically.
  
  - You can select your formations by clicking on troop cards.

    - Press `Alt` to show the mouse so that you can click on troop cards.

- Command System

  - You can make your formation charge to the enemy formation by clicking middle mouse button on enemy units.

    - **New:** Press `Alt` while clicking to target the enemy interupting previous movement orders. For example, **let your archers focus shooting while holding position**.

  - You can select your formations by clicking middle mouse button on soldiers.

- For more features, you can read `Details` below or press `L` to open mod menu.

## How to Install
1. Remove any old installation of this mod. You can go to `Modules` folder of bannerlord installation folder (For example `C:\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord\Moudles\`) and remove folders like `RTSCamera` or `EnhancedMission`.

2. Copy `RTSCamera` and `RTSCamera.CommandSystem` folder you downloaded into Bannerlord `Modules` folder. Or use Vortex to install it automatically.

## Details

- In a battle, press `L` to open detailed menu of this mod.

  - Switch between options of `RTS Camera` or `Command System` by clicking tab list on the left of the menu.

  - You can close the menu by pressing `esc`, left clicking outside the menu or right clicking anywhere.

### RTS Camera

- Press `F10` to toggle free camera.
  Player character will be controlled by AI when switched to free camera.

  - Order UI will be opened automatically.

  - You can enable `Switch Camera on Ordering` option, to always give orders in free camera.

  - **New:** When command UI is open in free camera mode, you can press `E` to stick camera to selected formation.

  - You can enable `Slow Motion On RTS view", to turn on slow motion in free camera. 

  - Controls in free camera:

    - Use `W`, `A`, `S`, `D`, `Space`, `Z` and mouse middle button to move the camera.

    - Use `shift` to speed up camera movement.

    - Move your mouse to rotate the camera, or when order panel is opened, drag right mouse button to rotate the camera.

    - Left click on the ground and drag to change the position, direction and width of the formation you selected.

      - Hold `ctrl` when dragging to arrange multiple formations vertically.

    - Hold `ctrl` and scroll mouse to adjust camera movement speed.

    - Hold `ctrl` and click middle mouse button to reset camera movement speed.

    - Hold `ctrl` and drag middle mouse button vertically to adjust camera height.

- After your character is killed in battle, you can press `E` to follow one of your soldiers and press `E` again to control it and continue fighting.

  - You can immediately control one of your soldiers to avoid all your formations been delegated to AI when your charater dies, by enabling `Control Ally After Death` option. It's always enabled in free camera mode to ensure a smooth gaming experience.

  - Soldiers in the same formation as player character will be considered first when deciding which soldier to control.

- When the camera is following a character, you can press `E` to control the character. You can make the camera follow a character in the following ways:

  - In free camera mode press E` to follow the player character.

  - Press `L` to open the menu and selecting a hero in `Watch Another Hero` drop-down list.

  - Press `;` and click a character, then pressing `E`.

  - Press left/right mouse button to change the character that the camera is following.

- If your character is injured in campaign map and you encounter an enemy party, you can still choose `Command the battle` option to begin the battle. In this battle you can command your troops in free camera but cannot directly control a character and fighting.

- Camera distance to player character can be limited by enabling `Limit Camera Distance` option.

  - Distance limit is determined by scouting and tactics level.

  - After `Limit Camera Distance` is enabled, using free camera and ordering in free camera can improve scouting and tactics skill level.

### Command System
- You can make your formation charge to the enemy formation by clicking middle mouse button on enemy units.

  - After the enemy formation is eliminated, your troops will stay at where they are. To change this behavior, you can press `L` to open menu, and set the option `After enemy formation eliminated` to `Charge`.

  - The target enemy formation that your selected troops are charging to will be highlighted with red outline when you open command panel in free camera mode.

- **New:** Press `Alt` while clicking to target the enemy without interupting previous movement orders.

  - For archers they can shoot target enemy while holding positions.

  - Note that for melee units, they will still follow previous movement orders, which may limit their ability to reach the target.

- Movement target marker is more visible in free camera. The original marker is hard to see if camera is too high.

- You can select your formations by clicking middle mouse button on soldiers.

  - Selected troops will be highlighted with green outline.

- Your formation targeted by the enemy formation will be highlighted with dark blue outline when you open command panel in free camera mode.

- Movement orders will be shown under troop cards.

## Hotkeys
You can config hotkeys by pressing `L` to open menu, and click `Config Key`.

You can click `+` or `-` to edit key sequence.

Here is a list of default hotkeys:

- `L`: open mod menu.

- `Middle Mouse Button`: In Command System, select formation.

- `F10`: Toggle free camera.

- `E`: Focus on troop/formation; Control troop.

- `;`: Select character.

- `w`, `A`, `S`, `D`: Camera movement.

- `Space`, `Z`: Camera up/down.

- `Right Shift` + `=`, `Right Shift` + `-`: Increase/Decrease camera distance limit.


## Configuration
- The configuration files are saved in directory `(user directory)\Documents\Mount and Blade II Bannerlord\Configs\RTSCamera\` and `(user directory)\Documents\Mount and Blade II Bannerlord\Configs\MissionLibrary\`.

  The main config is saved in files `RTSCameraConfig.xml` and `CommandSystemConfig.xml`.

  The hot key config is saved in file `RTSCameraGameKeyConfig.xml` and `CommandSystemGameKeyConfig.xml`.

  You can modify them manually, but if you edit them incorrectly or remove them, the configuration will be reset to default.

## Troubleshoot
- If the launcher can not start:

  - Uninstall all the third-party mods and reinstall them one by one to detect which one cause the launcher cannot start.

- If it shows "Unable to initialize Steam API":

  - Please start steam first, and make sure that Bannerlord is in your steam account.

- If the game crashed after starting:

  - If you upgraded this mod from version lower than e3.2.0, pleas remove the old mod folder "EnhancedMission" to prevent crash.
  
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
