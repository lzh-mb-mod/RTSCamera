using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using TaleWorlds.Core;

namespace RTSCamera.CommandSystem
{
    public static class Utility
    {
        public static void PrintOrderHint()
        {
            if (CommandSystemConfig.Get().ClickToSelectFormation)
            {
                RTSCamera.Utility.DisplayMessage(GameTexts
                    .FindText("str_rts_camera_command_system_click_to_select_formation_hint").SetTextVariable("KeyName",
                        RTSCamera.Utility.TextForKey(CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)))
                    .ToString());
            }

            if (CommandSystemConfig.Get().AttackSpecificFormation)
            {
                RTSCamera.Utility.DisplayMessage(GameTexts
                    .FindText("str_rts_camera_command_system_attack_specific_formation_hint").SetTextVariable("KeyName",
                        RTSCamera.Utility.TextForKey(CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)))
                    .ToString());
            }
        }
    }
}
