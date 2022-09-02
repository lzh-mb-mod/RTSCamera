using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using RTSCamera.Logic;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.Patch
{
    public class Patch_CrosshairVM
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_CrosshairVM));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(CrosshairVM).GetMethod(nameof(CrosshairVM.ShowHitMarker),
                        BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(Patch_CrosshairVM).GetMethod(
                        nameof(Prefix_ShowHitMarker), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(CrosshairVM).GetMethod("SetReloadProperties", BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_CrosshairVM).GetMethod(nameof(Postfix_SetReloadProperties),
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

        public static bool Prefix_ShowHitMarker()
        {
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
                return false;
            return true;
        }

        public static void Postfix_SetReloadProperties(CrosshairVM __instance)
        {
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
            {
                __instance.IsReloadPhasesVisible = false;
            }
        }
    }
}
