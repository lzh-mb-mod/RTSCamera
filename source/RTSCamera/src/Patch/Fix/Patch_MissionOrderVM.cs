using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionOrderVM
    {
        private static readonly PropertyInfo LastSelectedOrderSetType =
            typeof(MissionOrderVM).GetProperty(nameof(MissionOrderVM.LastSelectedOrderSetType),
                BindingFlags.Instance | BindingFlags.Public);

        public static bool AllowEscape = true;

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("CheckCanBeOpened",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(
                        nameof(Prefix_CheckCanBeOpened), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("AfterInitialize", BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_AfterInitialize),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod(nameof(MissionOrderVM.OnEscape),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Prefix_OnEscape),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_OnOrder),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(MissionOrderVM).GetMethod("OnTransferFinished",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_MissionOrderVM).GetMethod(nameof(Postfix_OnTransferFinished),
                        BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }
        }

        public static bool Prefix_CheckCanBeOpened(MissionOrderVM __instance, bool displayMessage, ref bool __result)
        {
            // In free camera mode, order UI can be opened event if main agent is controller by AI
            if (Agent.Main != null && !Agent.Main.IsPlayerControlled && Mission.Current
                .GetMissionBehavior<RTSCameraLogic>()?.SwitchFreeCameraLogic.IsSpectatorCamera == true)
            {
                __result = true;
                return false;
            }

            return true;
        }

        public static void Postfix_AfterInitialize(MissionOrderVM __instance)
        {
            LastSelectedOrderSetType.SetValue(__instance, (object)OrderSetType.None);
            AllowEscape = true;
        }

        public static bool Prefix_OnEscape(MissionOrderVM __instance)
        {
            // Do nothing during draging camera using right mouse button.
            return AllowEscape;
        }
        public static void Postfix_OnOrder(MissionOrderVM __instance)
        {
            // Keep orders UI open after issuing an order in free camera mode.
            if (!__instance.IsToggleOrderShown && !__instance.TroopController.IsTransferActive && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                __instance.OpenToggleOrder(false);
            }
        }

        public static void Postfix_OnTransferFinished(MissionOrderVM __instance)
        {
            // Keep orders UI open after transfer finished in free camera mode.
            if (!__instance.IsToggleOrderShown && RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera == true && RTSCameraConfig.Get().KeepOrderUIOpenInFreeCamera)
            {
                __instance.OpenToggleOrder(false);
            }
        }
    }
}
