using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions.SiegeWeapon;

namespace RTSCamera.Patch.Fix
{
    //[HarmonyLib.HarmonyPatch(typeof(RangedSiegeWeaponView), "HandleUserInput")]
    public class Patch_RangedSiegeWeaponView
    {
        public static bool HandleUserInput_Prefix(float dt, RangedSiegeWeaponView __instance, ref bool ____isInWeaponCameraMode)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
            if (__instance.PilotAgent != null && __instance.PilotAgent.Controller == Agent.ControllerType.Player && __instance.CameraHolder != null)
            {
                if (!____isInWeaponCameraMode)
                {
                    ____isInWeaponCameraMode = true;
                    typeof(RangedSiegeWeaponView).GetMethod("StartUsingWeaponCamera", bindingFlags)?.Invoke(__instance, new object[0]);
                }
                typeof(RangedSiegeWeaponView).GetMethod("HandleUserCameraRotation", bindingFlags)
                    ?.Invoke(__instance, new object[1] { dt });
            }
            if (____isInWeaponCameraMode && (__instance.PilotAgent == null || __instance.PilotAgent.Controller != Agent.ControllerType.Player))
            {
                ____isInWeaponCameraMode = false;
                typeof(RangedSiegeWeaponView).GetMethod("ResetCamera", bindingFlags)?.Invoke(__instance, new object[0]);
            }

            if (__instance.PilotAgent != null && __instance.PilotAgent.Controller == Agent.ControllerType.Player)
                typeof(RangedSiegeWeaponView).GetMethod("HandleUserAiming", bindingFlags)
                    ?.Invoke(__instance, new object[1] { dt });
            return false;
        }
    }
}
