using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class PatchChargeToFormation
    {
        private static readonly Harmony Harmony = new Harmony("RTSCameraChargeToFormationPatch");
        private static bool _patched;

        public static void Patch()
        {
            try
            {
                if (_patched)
                    return;
                _patched = true;
                Harmony.Patch(
                    typeof(MovementOrder).GetMethod("GetSubstituteOrder",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MovementOrder).GetMethod("GetSubstituteOrder_Prefix",
                        BindingFlags.Static | BindingFlags.Public), Priority.First));
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }

        public static void UnPatch()
        {
            try
            {
                if (!_patched)
                    return;
                _patched = false;
                Harmony.UnpatchAll(Harmony.Id);
            }
            catch (Exception e)
            {
                Utility.DisplayMessage(e.ToString());
            }
        }
    }
}
