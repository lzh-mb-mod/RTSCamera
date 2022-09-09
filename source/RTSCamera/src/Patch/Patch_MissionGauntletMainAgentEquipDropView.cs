using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.Patch
{
    public class Patch_MissionGauntletMainAgentEquipDropView
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_MissionGauntletMainAgentEquipDropView));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionGauntletMainAgentEquipDropView).GetMethod("IsMainAgentAvailable",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    new HarmonyMethod(typeof(Patch_MissionGauntletMainAgentEquipDropView).GetMethod(
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
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
