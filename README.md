# Enhanced Mission

A mod for Mount&Blade Bannerlord that provides features in mission (the game state when you are in a scene rather than in a campaign map).

## Features

- Switch to rts-style camera.

- Controll your troops after dead.

- Undead mode. HP of All agents will not change after switched on.

- Realistic blocking.
  
  Realistic blocking is the blocking mechanism that introduced in Beta 0.8.1 and removed in Beta 0.8.4 in Multiplayer mode. Now in Single-player, It's only enabled for all the other characters except the player by default.

  If you enable the option in this mod, then player will use it too.

- Adjust combat AI between 0 and 100.

  By default combat AI is determined by the level of soldiers. If you set combat AI to "Good" in game option, soldiers with level of 40 will have highest combat AI.

  In this mod, you can change all troops' combat AI using "combat AI" option. If you set the combat AI to "100", their combat AI will be as if they are at level 40.

- Pause game and adjust time speed.

- Hotkey rebinding.

- Configuration saving. The battle configuration is saved in directory `(user directory)\Documents\Mount and Blade II Bannerlord\Configs\EnhancedMission\`.
  
  The main config is saved in file `EnhancedMissionConfig.xml`.

  The hot key config is saved in file `GameKeyConfig.xml`.

  You can modify them manually, but if you edit it incorrectly or remove them, the configuration will be reset to default.

## How to install
1. Copy `Modules` folder into Bannerlord installation folder(For example `C:\Program Files\Steam\steamapps\common\Mount & Blade II Bannerlord - Beta`). It should be merged with `Modules` of the game.

## How to use
- Start the launcher and choose Single player mode. In `Mods` panel select `EnhancedMission` mod and click `PLAY`.

  Then play the game as usual.

- After entering a mission (scene):

  - Press `O(letter)` (by default) to open menu of this mod. You can access the features of this mod in it.

    Or you can use the following hotkeys by default:

  - Press `F10` to switch between rts-style camera and main agent camera.

  - Press `F` key or `F10` key to control one of your troops after you being killed.

  - Press `F11` to disable death.

  - Press `P` key to pause game.

- How to play in rts camera:

  - In a mission, press `F10` to switch to rts camera.

  - Your player character will be added to the formation chosen in mod menu.

  - Use `W`, `A`, `S`, `D`, `Space`, `Z` and mouse middle button to move the camera.

  - Use `shift` to speed up camera movement.

  - Move your mouse to rotate the camera, or when order panel is opened, drag right button to rotate the camera.

  - Left click on the ground and drag to change the position, direction and width of the formation you selected.

## Troubleshoot
- If the launcher can not start:

  - Uninstall all the third-party mods and reinstall them one by one to detect which one cause the launcher cannot start.

- If it shows "Unable to initialize Steam API":

  - Please start steam first, and make sure that Bannerlord is in your steam account.

- If the game crashed after starting:

  - Please uncheck the mod in launcher and wait for mod update.

    Optionally you can tell me the step to reproduce the crash.

- If you forget the hotkey you set for opening menu:

  - you can remove the config file so that it will be reset to default.

## Contact with me
* Please mail to: lizhenhuan1019@qq.com
