RTS Command for v1.3.x (War Sail DLC Compatible)
(full name RTS Camera Command System)


Main features:
Reworked the command system in the base game.
Order target is highlighted to be visible from far distance.
Add volley order.
Troops with throwing weapons will correctly keep distance under engage order.
Square and Circle formations are improved.
Allows to give a series of orders and let your troops execute them one by one. For example, move along a given path and then shooting/charging.
If you move/turn multiple formations at the same time, their relative positions/directions will be locked.
Fix issues in the base game about formation width and unit spacing.
Set attack target without interrupting previous movement orders. For example, let your archers focus shooting while holding position.
Facing to enemy order now has an optional enemy formation target.
Allows you to select your formation using middle mouse button.
Select your troops and give orders by clicking left mouse button on order UI.
Replace square formation with hollow square formation for player.


Recommend:
RTS Camera: another part of the RTS mod, for free camera view when giving orders: https://steamcommunity.com/sharedfiles/filedetails/?id=3596692403


Dependency:
Do not require any of ButterLib/MCM/UIExtenderEx.
If War Sails DLC is enabled, it should be placed before this mod.


Compatibility:
Compatible with v1.3.4-v1.3.10 and War Sail DLC. For other versions:
v1.2.12: https://steamcommunity.com/sharedfiles/filedetails/?id=2879991827
v1.3.4-v1.3.10: https://steamcommunity.com/sharedfiles/filedetails/?id=3596693285


Save compatibility:
This mod won't read/write anything from/into your save, so you can feel free to enable or uninstall this mod at any time.


Main Usage:
Please enable "RTS Camera" and "RTSCamera.CommandSystem" in launcher.
For complete usage: https://github.com/lzh-mb-mod/RTSCamera/blob/master/README.md

Hold Shift and give orders to add orders to queue. Your troops will execute them one by one.
When you move/turn multiple formations together, relative positions/directions will be locked.
    - Hold `Ctrl` while dragging on the ground, formations' widths will also be locked.
    - Hold `Alt` to disable the locking behavior and revert to behavior in the base game.
Hold `Alt` while clicking the enemy formation to target the enemy without interrupting previous movement orders.
Click middle mouse button on the enemy formation to make your formation attack it.
Click middle mouse button on your formation to select it.
The attacked enemy formation will be highlighted with red outline when you open command panel.
Your attacked formation will be highlighted with dark blue outline when you open command panel.
Your selected formation will be highlighted with green outline when you open command panel.

Press L to open config menu during battles.


Hotkeys:
Shift                      -  Hold to add order to queue when giving orders.
Ctrl                       -  Hold to lock formation widths while dragging on the ground.
Alt                        -  Hold to cancel formation locking during movement or turning.
Click mouse middle button  -  Select/Attack formation.
Hold Alt + Click mouse middle button - Attack enemy formation without interrupting previous movement orders
L                          -    Open the config menu.
All the hotkeys can be changed in config menu.


More Details:
Improved square and circle formation:
  - When switching between square/cicle formation and other formations, the unit count in the outside rank is kept the same as the unit count of the first rank of line/sheild wall/loose formation if possible.
    This allows soldiers to form formations more quickly and avoids them crowding together.
  - In the base game when the unit count in square formation is square number, the center unit is not correctly placed. This is fixed in this mod.
  - Player can use hollow square formation. Not enabled for AI.
  - When formation is switched to circle formation, the circle formation will be tighter. This can be changed in option.


Troubleshooting
If there're a lot of "text with id xxxxx doesn't exist":
    If you enabled Realm of Thrones, please move RTS Camera and Command System above it. As version 6.2 of Realm of Thrones, there's a bug in its file that causes all the mods below it cannot load text correctly.

If crash on startup, please check the following:
1. Check Modules folder and make sure there's no RTSCamera or RTSCamera.CommandSystem. If you use mods from Steam workshop then your Modules folder should be clean.
2. If you are using any of the following mods from me, please make sure they are updated:
- BattleMiniMap
- ImprovedCombatAI
- CinematicCamera

Feedback or suggestion
Feel free to comment below or send email to lizhenhuan1019@outlook.com


links:
Nexusmods: https://www.nexusmods.com/mountandblade2bannerlord/mods/355
source code: https://github.com/lzh-mb-mod/RTSCamera
