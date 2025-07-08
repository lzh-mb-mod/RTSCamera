using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;

namespace RTSCamera.Patch
{
    public class Patch_MissionGauntletMainAgentEquipmentControllerView
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
                    typeof(MissionGauntletMainAgentEquipmentControllerView).GetMethod("IsMainAgentAvailable",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    new HarmonyMethod(typeof(Patch_MissionGauntletMainAgentEquipmentControllerView).GetMethod(
                        nameof(Prefix_IsMainAgentAvailable), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_IsMainAgentAvailable(ref bool __result)
        {
            // Disable pick equipment view in free camera mode
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
