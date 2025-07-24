using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.Patch
{
    public class Patch_CrosshairVM
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
                    typeof(CrosshairVM).GetMethod(nameof(CrosshairVM.ShowHitMarker),
                        BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(Patch_CrosshairVM).GetMethod(
                        nameof(Prefix_ShowHitMarker), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(CrosshairVM).GetMethod(nameof(CrosshairVM.SetReloadProperties), BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_CrosshairVM).GetMethod(nameof(Postfix_SetReloadProperties),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(CrosshairVM).GetMethod(nameof(CrosshairVM.SetArrowProperties), BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_CrosshairVM).GetMethod(nameof(Prefix_SetArrowProperties),
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
            // Hide hit marker in spectator camera.
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
                return false;
            return true;
        }

        public static void Postfix_SetReloadProperties(CrosshairVM __instance)
        {
            // Hide reload phases in spectator camera.
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
            {
                __instance.IsReloadPhasesVisible = false;
            }
        }

        public static bool Prefix_SetArrowProperties(CrosshairVM __instance)
        {
            // Hide attack direction arrow in spectator camera.
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
            {
                __instance.TopArrowOpacity = 0;
                __instance.BottomArrowOpacity = 0;
                __instance.RightArrowOpacity = 0;
                __instance.LeftArrowOpacity = 0;
                return false;
            }
            return true;
        }
    }
}
