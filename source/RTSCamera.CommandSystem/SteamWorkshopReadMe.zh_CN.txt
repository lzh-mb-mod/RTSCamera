RTS Command (全名 RTS Camera Command System)


主要功能：
重做了游戏中的指挥系统。
高亮指令目标以在远处时也能看见。
可下达一系列指令，并让你的部队挨个执行这些指令。例如，可让部队沿给定路径移动，然后开始射击或冲锋。
若你同时移动/旋转多个编队，他们的相对位置和方向会锁定。
修复了游戏本体中的编队宽度和单位间距的问题。
可在下达集中攻击指令的同时维持原本的移动指令。例如，让你的弓箭手待在原地同时攻击指定的目标。
面朝敌军指令可以选择一个的敌方编队作为目标。
可用鼠标左键点击指挥界面来选择编队或下达指令。
加入了空心方阵，替换了原本的实心方阵。


推荐：
RTS Camera：RTS系列的另一mod，提供指挥时切换到自由视角的功能：https://steamcommunity.com/sharedfiles/filedetails/?id=3596692403


前置依赖：
不需要任何前置如ButterLib/MCM/UIExtenderEx。
如果你启用了War Sails DLC，请在mod列表中把它放在本mod之前而非之后。


兼容性：
与游戏本体v1.3.4-v1.3.10兼容，并兼容战帆DLC。其它版本：
v1.2.12: https://steamcommunity.com/sharedfiles/filedetails/?id=2879991827
v1.3.4-v1.3.10: https://steamcommunity.com/sharedfiles/filedetails/?id=3596693285


存档兼容性：
这个mod不会向存档读写任何东西，放心随时启用随时卸载。


使用指南：
请在启动器中启用"RTSCamera"和"RTSCamera.CommandSystem".
完整指南：https://github.com/lzh-mb-mod/RTSCamera/blob/master/README.zh-CN.md

按住`Shift`并下达指令，可将指令添加到指令队列。部队会挨个执行队列中的指令。
若你同时移动/旋转多个编队，他们的相对位置和方向会锁定。
    - 当你在地面拖动部队时，按住`Ctrl`可锁定编队宽度和相对位置。
    - 按住`Alt`可禁用锁定，回退到游戏默认行为。
指挥时对着敌方士兵按下鼠标中键可让你的编队攻击该士兵所在的编队。
对着己方士兵按下鼠标中键可选择该编队。
被集中攻击的敌方编队会用红色轮廓标出。
己方选中的编队会以绿色轮廓标出。
己方被集中攻击的编队会以深蓝色轮廓标出。

在场景中按L键可打开配置菜单。


快捷键列表：
鼠标中键       - 选择/攻击编队
Alt + 鼠标中键 - 攻击敌人同时维持原有移动指令
左Alt或右Alt - 攻击敌方编队时保持移动指令；切换编队锁定行为。
左Shift或右Shift - 将指令添加到队列。
左Ctrl或右Ctrl - 拖动时保持编队宽度不变。
L             - 打开配置菜单
所有快捷键均可在配置菜单中设置。


问题排查
如果进游戏以后显示"text with id xxxxx does't exist":
    如果你启用了权力的游戏，请在启动器中把RTS Camera, RTS Camera Command System移到权游之前。至少在目前的权游6.2版本，它里面的文件有bug，导致后续所有mod都不能正确加载字符串。


问题反馈和建议
可直接留言或发邮件到lizhenhuan1019@outlook.com


链接：
Nexusmods: https://www.nexusmods.com/mountandblade2bannerlord/mods/355
中文站: https://bbs.mountblade.com.cn/thread-2061243-1-1.html
源代码：https://github.com/lzh-mb-mod/RTSCamera