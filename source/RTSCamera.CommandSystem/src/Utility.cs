using RTSCamera.CommandSystem.Config;

namespace RTSCamera.CommandSystem
{
    public static class Utility
    {
        public static void PrintOrderHint()
        {
            if (CommandSystemConfig.Get().ClickToSelectFormation)
            {
                RTSCamera.Utility.DisplayLocalizedText("str_rts_camera_command_system_click_to_select_formation_hint");
            }

            if (CommandSystemConfig.Get().AttackSpecificFormation)
            {
                RTSCamera.Utility.DisplayLocalizedText("str_rts_camera_command_system_attack_specific_formation_hint");
            }
        }
    }
}
