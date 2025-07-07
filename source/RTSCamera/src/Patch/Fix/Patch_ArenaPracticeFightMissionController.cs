using HarmonyLib;
using MissionSharedLibrary.Utilities;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    //[HarmonyLib.HarmonyPatch(typeof(ArenaPracticeFightMissionController), "StartPractice")]
    public class Patch_ArenaPracticeFightMissionController
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(ArenaPracticeFightMissionController).GetMethod("StartPractice",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_ArenaPracticeFightMissionController).GetMethod(nameof(Prefix_StartPractice),
                            BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static bool Prefix_StartPractice()
        {
            // In the original code, main agent will fade out only if the controller type is Player
            // We need to fade out the main agent event if the controller type is AI. This happen in free camera mode.
            if (Mission.Current?.MainAgent != null)
            {
                Mission.Current.MainAgent.FadeOut(true, false);
            }

            return true;
        }
    }
}
