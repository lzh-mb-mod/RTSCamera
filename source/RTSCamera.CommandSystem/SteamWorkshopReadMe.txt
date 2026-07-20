[h1]RTS Command for v1.3.x (War Sail DLC Compatible)[/h1]
(full name RTS Camera Command System)

[h2]Main Features[/h2]
[list]
[*]Reworked the command system in the base game.
[*]Order target is highlighted to be visible from far distance.
[*]Add volley order.
[*]Troops with throwing weapons will correctly keep distance under engage order.
[*]In siege battle you can click on siege weapons to make your troops stop using it.
[*]Square and Circle formations are improved.
[*]Allows to give a series of orders and let your troops execute them one by one. For example, move along a given path and then shooting/charging.
[*]If you move/turn multiple formations at the same time, their relative positions/directions will be locked.
[*]Fix issues in the base game about formation width and unit spacing.
[*]Fix the issue in the base game that engage order for ranged troops may cause the game freeze.
[*]Set attack target without interrupting previous movement orders. For example, let your archers focus shooting while holding position.
[*]Facing to enemy order now has an optional enemy formation target.
[*]Allows you to select your formation using middle mouse button.
[*]Select your troops and give orders by clicking left mouse button on order UI.
[*]Replace square formation with hollow square formation for player.
[/list]

[h2]Recommended[/h2]
[list]
[*]RTS Camera: another part of the RTS mod, for free camera view when giving orders. [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3596692403]Steam Workshop[/url]
[/list]

[h2]Dependency[/h2]
[list]
[*]Does not require ButterLib, MCM, or UIExtenderEx.
[*]If War Sails DLC is enabled, it should be placed before this mod.
[/list]

[h2]Game Compatibility[/h2]
Compatible with v1.3.4-v1.3.15 and War Sail DLC. For other versions:
[list]
[*]v1.2.12: [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2879991827]Steam Workshop[/url]
[*]v1.3.4-v1.3.15: [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3596693285]Steam Workshop[/url]
[*]v1.4.5-v1.4.7: [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3747771970]Steam Workshop[/url]
[/list]

[h2]Mod Compatibility[/h2]
[list]
[*]Compatible with The Old Reams War in the Mountains. Please report if crashes.
[*][b]Incompatible with "RTS Camera Universal". That one is made by the other guy, packing my old RTS mods into one mod, conflicting with this mod. Please make sure not to use them together.[/b]
[*]If you use Realm of Thrones, please move RTS Camera/Command System above Realm of Thrones, or "text with id xxxxx does't exist" will show up.
[/list]

[h2]Save Compatibility[/h2]
This mod won't read or write anything from or into your save, so you can feel free to enable or uninstall this mod at any time.

[h2]Main Usage[/h2]
Please enable "RTS Camera" and "RTSCamera.CommandSystem" in launcher.

For complete usage: [url=https://github.com/lzh-mb-mod/RTSCamera/blob/master/README.md]README.md[/url]

[list]
[*]Hold Shift and give orders to add orders to queue. Your troops will execute them one by one.
[*]When you move/turn multiple formations together, relative positions/directions will be locked.
[*]Hold Ctrl while dragging on the ground, formations' widths will also be locked.
[*]Hold Alt to disable the locking behavior and revert to behavior in the base game.
[*]Hold Alt while clicking the enemy formation to target the enemy without interrupting previous movement orders.
[*]Click middle mouse button on the enemy formation to make your formation attack it.
[*]Click middle mouse button on your formation to select it.
[*]The attacked enemy formation will be highlighted with red color when you open command panel.
[*]Your attacked formation will be highlighted with dark blue color when you open command panel.
[*]Your selected formation will be highlighted with blue color when you open command panel.
[*]Auto Volley: Press F3 F6 (if using legacy order layout, F8 F1) or H. Soldiers will aim and wait for others and fire when most soldiers are ready.
[*]Manual Volley: Press F3 F7 (if using legacy order layout, F8 F2) or J. Soldiers will aim and wait for your orders to fire.
[*]Volley Fire: Press F3 F8 (if using legacy order layout, F8 F3) or K. Let soldiers in manual volley to fire.
[*]Press L to open config menu during battles.
[/list]

[h2]Hotkeys[/h2]
[list]
[*]Shift - Hold to add order to queue when giving orders.
[*]Ctrl - Hold to lock formation widths while dragging on the ground.
[*]Alt - Hold to cancel formation locking during movement or turning.
[*]Click mouse middle button - Select/Attack formation.
[*]H - Auto volley.
[*]J - Manual volley.
[*]K - Volley fire.
[*]Hold Alt + Click mouse middle button - Attack enemy formation without interrupting previous movement orders
[*]L - Open the config menu.
[*]All the hotkeys can be changed in config menu.
[/list]

[h2]More Details[/h2]
[b]Improved square and circle formation:[/b]
[list]
[*]When switching between square/cicle formation and other formations, the unit count in the outside rank is kept the same as the unit count of the first rank of line/sheild wall/loose formation if possible.
[*]This allows soldiers to form formations more quickly and avoids them crowding together.
[*]In the base game when the unit count in square formation is square number, the center unit is not correctly placed. This is fixed in this mod.
[*]Player can use hollow square formation. Not enabled for AI.
[*]When formation is switched to circle formation, the circle formation will be tighter. This can be changed in option.
[*]You can enable the "Add Defensive Hold Order" option in config menu so that you can give this order to troops. With this order your troops under defensive arrangement (shield wall, square, circle) will hold positions and shields.
[/list]

[h2]Troubleshooting[/h2]
If there're a lot of "text with id xxxxx doesn't exist":
[list]
[*]If you enabled Realm of Thrones, please move RTS Camera and Command System above it. As version 6.2 of Realm of Thrones, there's a bug in its file that causes all the mods below it cannot load text correctly.
[/list]

If crash on startup, please check the following:
[list]
[*]Check Modules folder and make sure there's no RTSCamera or RTSCamera.CommandSystem. If you use mods from Steam workshop then your Modules folder should be clean.
[*]If you are using any of the following mods from me, please make sure they are updated:
[*]BattleMiniMap
[*]ImprovedCombatAI
[*]CinematicCamera
[/list]

[h2]Feedback or Suggestion[/h2]
Feel free to comment below or send email to [url=mailto:lizhenhuan1019@outlook.com]lizhenhuan1019@outlook.com[/url]

[h2]Links[/h2]
[list]
[*]Nexusmods: [url=https://www.nexusmods.com/mountandblade2bannerlord/mods/355]RTS Camera[/url]
[*]Source code: [url=https://github.com/lzh-mb-mod/RTSCamera]GitHub[/url]
[/list]

[h2]Donate[/h2]
[url=https://ko-fi.com/lizhenhuan1019]Buy me a coffee[/url]
