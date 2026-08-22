using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.SiegeWeapon;

namespace RTSCamera.Patch.Fix
{
    //[HarmonyLib.HarmonyPatch(typeof(RangedSiegeWeaponView), "HandleUserInput")]
    public class Patch_RangedSiegeWeaponView
    {
        private static bool _patched;
        private static readonly BindingFlags InstanceNonPublic =
            BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly MethodInfo HandleUserInput =
            typeof(RangedSiegeWeaponView).GetMethod("HandleUserInput", InstanceNonPublic);
        private static readonly MethodInfo StartUsingWeaponCamera =
            typeof(RangedSiegeWeaponView).GetMethod("StartUsingWeaponCamera", InstanceNonPublic);
        private static readonly MethodInfo HandleUserCameraRotation =
            typeof(RangedSiegeWeaponView).GetMethod("HandleUserCameraRotation", InstanceNonPublic);
        private static readonly MethodInfo ResetCamera =
            typeof(RangedSiegeWeaponView).GetMethod("ResetCamera", InstanceNonPublic);
        private static readonly MethodInfo HandleUserAiming =
            typeof(RangedSiegeWeaponView).GetMethod("HandleUserAiming", InstanceNonPublic);

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                harmony.Patch(
                    HandleUserInput,
                    prefix: new HarmonyMethod(
                        typeof(Patch_RangedSiegeWeaponView).GetMethod(nameof(Prefix_HandleUserInput),
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
        public static bool Prefix_HandleUserInput(float dt, RangedSiegeWeaponView __instance, ref bool ____isInWeaponCameraMode)
        {
            // In the original code, the condition is to check pilot agent is IsMainAgent.
            // We modify it to check if the controller is Player.
            bool isForceUse = Mission.Current?.MainAgent != null && Mission.Current.MainAgent.IsPlayerControlled && __instance.RangedSiegeWeapon.PlayerForceUse;
            bool usingWeapon = __instance.PilotAgent != null && __instance.PilotAgent.Controller == AgentControllerType.Player || isForceUse;
            if (__instance.CameraHolder != null && usingWeapon)
            {
                if (!____isInWeaponCameraMode)
                {
                    ____isInWeaponCameraMode = true;
                    StartUsingWeaponCamera.Invoke(__instance, new object[0]);
                }
                if (isForceUse)
                {
                    HandleUserCameraRotation.Invoke(__instance, new object[1] { dt });
                }
            }
            if (____isInWeaponCameraMode && !usingWeapon)
            {
                ____isInWeaponCameraMode = false;
                ResetCamera?.Invoke(__instance, new object[0]);
            }

            if (__instance.PilotAgent != null && (__instance.PilotAgent.Controller == AgentControllerType.Player || isForceUse))
                HandleUserAiming.Invoke(__instance, new object[1] { dt });
            return false;
        }
    }
}
