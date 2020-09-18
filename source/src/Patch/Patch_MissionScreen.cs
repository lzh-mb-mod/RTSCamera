using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera.Patch
{
    public class Patch_MissionScreen
    {
        public static bool OnMissionModeChange_Prefix(MissionScreen __instance, MissionMode oldMissionMode, bool atStart)
        {
            if (__instance.Mission.Mode == MissionMode.Battle && oldMissionMode == MissionMode.Deployment)
            {
                Utility.SmoothMoveToAgent(__instance, true);
                return false;
            }

            return true;
        }
    }
}
