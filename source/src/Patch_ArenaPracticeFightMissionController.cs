using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    //[HarmonyLib.HarmonyPatch(typeof(ArenaPracticeFightMissionController), "StartPractice")]
    public class Patch_ArenaPracticeFightMissionController
    {
        public static bool StartPractice_Prefix()
        {
            if (Mission.Current?.MainAgent != null)
            {
                Mission.Current.MainAgent.FadeOut(true, false);
            }

            return true;
        }
    }
}
