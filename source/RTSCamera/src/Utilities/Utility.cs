using RTSCamera.Config.HotKey;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Utilities
{
    public class Utility
    {
        public static void PrintUsageHint()
        {
            var keyName = MissionSharedLibrary.Utilities.Utility. TextForKey(RTSCameraGameKeyCategory.GetKey(GameKeyEnum.FreeCamera));
            var hint = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_switch_camera_hint").SetTextVariable("KeyName", keyName).ToString();
            MissionSharedLibrary.Utilities.Utility.DisplayMessageOutOfMission(hint);
        }
    }
}
