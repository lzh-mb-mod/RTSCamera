using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionAgentLabelView
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
                    typeof(MissionAgentLabelView).GetMethod("IsAllyInAllyTeam",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_MissionAgentLabelView).GetMethod(nameof(IsAllyInAllyTeam_Prefix),
                            BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }

        public static bool IsAllyInAllyTeam_Prefix(MissionAgentLabelView __instance, Agent agent, ref bool __result)
        {
            // show agent label for main agent if the camera is not following it
            if (agent == __instance?.Mission?.MainAgent && agent != __instance.MissionScreen.LastFollowedAgent)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
