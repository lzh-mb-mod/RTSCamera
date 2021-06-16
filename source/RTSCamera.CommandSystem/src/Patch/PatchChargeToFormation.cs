using System;
using System.Reflection;
using HarmonyLib;
using MissionSharedLibrary.Utilities;
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
                //Harmony.Patch(
                //    typeof(Formation).GetMethod("GetOrderPositionOfUnit", BindingFlags.Instance | BindingFlags.Public),
                //    prefix: new HarmonyMethod(
                //        typeof(Patch_Formation).GetMethod("GetOrderPositionOfUnit_Prefix", BindingFlags.Static | BindingFlags.Public)));

                //Harmony.Patch(typeof(MovementOrder).GetMethod("GetPosition", BindingFlags.Instance | BindingFlags.Public),
                //    prefix: new HarmonyMethod(
                //        typeof(Patch_MovementOrder).GetMethod("GetPosition_Prefix", BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MovementOrder).GetMethod("GetSubstituteOrder",
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MovementOrder).GetMethod("GetSubstituteOrder_Prefix",
                        BindingFlags.Static | BindingFlags.Public)));

                Harmony.Patch(
                    typeof(MovementOrder).GetMethod("SetChargeBehaviorValues",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MovementOrder).GetMethod("SetChargeBehaviorValues_Prefix",
                        BindingFlags.Static | BindingFlags.Public), Priority.First));

                Harmony.Patch(
                    typeof(HumanAIComponent).GetMethod("GetFormationFrame",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_HumanAIComponent).GetMethod("GetFormationFrame_Prefix",
                            BindingFlags.Static | BindingFlags.Public), Priority.First));

                //Harmony.Patch(
                //    typeof(FacingOrder).GetMethod("GetDirection", BindingFlags.Instance | BindingFlags.Public),
                //    prefix: new HarmonyMethod(typeof(Patch_FacingOrder).GetMethod("GetDirection_Prefix",
                //        BindingFlags.Static | BindingFlags.Public)));

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
