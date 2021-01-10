using System;
using HarmonyLib;

namespace RTSCamera.CommandSystem.Patch.CircularFormation
{
    public class PatchCircularFormation
    {
        private static readonly Harmony Harmony = new Harmony("RTSCameraCircularFormationPatch");
        private static bool _patched;
        public static void Patch()
        {
            try
            {
                if (_patched)
                    return;
                _patched = true;
                Patch_OrderController.Patch(Harmony);
                Patch_FormOrder.Patch(Harmony);
            }
            catch (Exception e)
            {
                RTSCamera.Utility.DisplayMessage(e.ToString());
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
                RTSCamera.Utility.DisplayMessage(e.ToString());
            }
        }
    }
}
