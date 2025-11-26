using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.Patch.Fix
{
    //[HarmonyLib.HarmonyPatch(typeof(RangedSiegeWeaponView), "HandleUserInput")]
    public class Patch_RangedSiegeWeaponView
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
                    typeof(RangedSiegeWeaponView).GetMethod("HandleUserInput",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_RangedSiegeWeaponView).GetMethod(nameof(Prefix_HandleUserInput),
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
        public static bool Prefix_HandleUserInput(float dt, RangedSiegeWeaponView __instance, ref bool ____isInWeaponCameraMode)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            // In the original code, the condition is to check pilot agent is IsMainAgent.
            // We modify it to check if the controller is Player.
            bool isForceUse = Mission.Current?.MainAgent != null && Mission.Current.MainAgent.IsPlayerControlled && __instance.RangedSiegeWeapon.PlayerForceUse;
            bool usingWeapon = __instance.PilotAgent != null && __instance.PilotAgent.Controller == AgentControllerType.Player || isForceUse;
            if (__instance.CameraHolder != null && usingWeapon)
            {
                if (!____isInWeaponCameraMode)
                {
                    ____isInWeaponCameraMode = true;
                    typeof(RangedSiegeWeaponView).GetMethod("StartUsingWeaponCamera", bindingFlags).Invoke(__instance, new object[0]);
                }
                typeof(RangedSiegeWeaponView).GetMethod("HandleUserCameraRotation", bindingFlags)
                    .Invoke(__instance, new object[1] { dt });
            }
            if (____isInWeaponCameraMode && !usingWeapon)
            {
                ____isInWeaponCameraMode = false;
                typeof(RangedSiegeWeaponView).GetMethod("ResetCamera", bindingFlags)?.Invoke(__instance, new object[0]);
            }

            if (__instance.PilotAgent != null && (__instance.PilotAgent.Controller == AgentControllerType.Player || isForceUse))
                typeof(RangedSiegeWeaponView).GetMethod("HandleUserAiming", bindingFlags)
                    .Invoke(__instance, new object[1] { dt });
            return false;
        }
    }
}
