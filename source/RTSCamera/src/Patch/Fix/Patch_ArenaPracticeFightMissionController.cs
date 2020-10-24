using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
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
