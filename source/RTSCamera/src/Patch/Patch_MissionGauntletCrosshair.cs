using System;
using System.Reflection;
using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using TaleWorlds.MountAndBlade.GauntletUI;

namespace RTSCamera.Patch
{
    public class Patch_MissionGauntletCrosshair
    {
        public static bool Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    typeof(MissionGauntletCrosshair).GetMethod("GetShouldCrosshairBeVisible",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    new HarmonyMethod(typeof(Patch_MissionGauntletCrosshair).GetMethod(
                        nameof(Prefix_GetShouldCrosshairBeVisible), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static bool Prefix_GetShouldCrosshairBeVisible(ref bool __result)
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
