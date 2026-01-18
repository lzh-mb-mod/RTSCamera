RTS Camera for v1.3.x (兼容战帆DLC)


主要功能：
- 战斗中丝滑切换自由视角。
- 玩家角色在战场上受伤后可控制下属士兵并继续战斗和指挥。
- 控制战斗中的时间流速。在藏身处战斗中可以自动开启快进。


推荐：
RTS Command：RTS mod的另一部分，提供更好的指挥系统。
 https://steamcommunity.com/sharedfiles/filedetails/?id=3596693285


战帆DLC相关：
当你没在开船时，会新增让士兵驾驶你的船只的命令（快捷键F5）。这功能与舵手（Helmsman） mod 相同。
为避免冲突，当RTS检测到已安装舵手（Helmsman）时，RTS不会为第一艘船添加该命令。
由于舵手（Helmsman）对其它船只不生效，当你控制其它船只上的角色时，RTS会添加该命令。


前置依赖：
不需要任何前置如ButterLib/MCM/UIExtenderEx。


兼容性：
与游戏本体v1.3.4-v1.3.13兼容，并兼容战帆DLC。其它版本：
v1.2.12: https://steamcommunity.com/sharedfiles/filedetails/?id=2879991660
v1.3.4-v1.3.13: https://steamcommunity.com/sharedfiles/filedetails/?id=3596692403


注意：
请清理Modules文件夹和创意工坊订阅以确保启动器中没有重复的mod，否则不管你勾选的是哪个mod，启动器加载的mod都可能不是你勾选的那一个然后导致崩溃。


存档兼容性：
这个mod不会向存档读写任何东西，放心随时启用随时卸载。


使用指南：
请在启动器中启用"RTSCamera"和"RTSCamera.CommandSystem".
完整指南：https://github.com/lzh-mb-mod/RTSCamera/blob/master/README.zh-CN.md
按F10可切换自由视角模式。
当镜头未锁定一个单位时按E可将镜头锁定至某一个单位。
当镜头锁定至某一个单位后按E可控制该单位。

在场景中按L键可打开配置菜单。
配置菜单中将“阵亡后控制队友”选项设置为“总是”，可在玩家倒下后立即控制合适的队友，以避免部队被托管给AI指挥。


快捷键列表：
F10 - 切换自由视角模式
E  -    让镜头锁定一个角色，或控制镜头锁定的角色
L  -    打开配置菜单
;   -    开始用鼠标选择单位，选择后可以按两下E来控制该单位。
所有快捷键均可在配置菜单中设置。


问题排查
如果游戏启动时崩溃，请检查下面列出的事项：
1. 检查Modules文件夹，确保没有RTSCamera或者RTSCamera.CommandSystem。如果你使用创意工坊的mod，Modules文件夹不应有同一mod。
2. 如果你使用了下面任何一个mod，请确保它们都更新到最新了
- BattleMiniMap战斗小地图
- ImprovedCombatAI改进的战斗AI
- CinematicCamera电影镜头


Mod兼容性
和Retinues v1.3.14.21一起使用有已知的性能问题（已在Retinues v1.3.14.22中修复）。你可以把选项“玩家编队分配方式”设置为“默认或将领编队”然后重新进入战斗，来避免性能问题。
在The Old Realms中的崩溃在v5.1.7中修复。
权游mod中若出现"text with id xxxxx does't exist"，请在启动器中将RTS Camera移至权游上方。
Helmsman舵手：如果你用RTS Camera，你不需要使用Helmsman舵手。RTS Camera提供和舵手一样的功能。

问题反馈和建议
可直接留言或发邮件到lizhenhuan1019@outlook.com


链接：
Nexusmods: https://www.nexusmods.com/mountandblade2bannerlord/mods/355
中文站下载区：https://bbs.mountblade.com.cn/download_2000.html
源代码：https://github.com/lzh-mb-mod/RTSCamera