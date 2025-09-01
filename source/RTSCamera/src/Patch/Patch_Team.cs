using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_Team
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // recover player formation from general formation
                harmony.Patch(
                    typeof(Team).GetMethod("OnDeployed",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_Team).GetMethod(nameof(Prefix_OnDeployed),
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

        public static bool Prefix_OnDeployed(Team __instance)
        {
            RTSCameraLogic.Instance?.SwitchFreeCameraLogic.OnEarlyTeamDeployed(__instance);

            return true;
        }
    }
}
