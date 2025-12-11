using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Patch.Fix;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch.Naval
{
    public class Patch_MissionGauntletNavalOrderUIHandler
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(AccessTools.TypeByName("MissionGauntletNavalOrderUIHandler").Method("OnSelectedFormationsChanged"),
                    prefix: new HarmonyMethod(typeof(Patch_MissionGauntletNavalOrderUIHandler).GetMethod(nameof(Prefix_OnSelectedFormationsChanged), BindingFlags.Static | BindingFlags.Public)));

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

        public static bool Prefix_OnSelectedFormationsChanged(object __instance,
            OrderController ____orderController,
            MissionFormationTargetSelectionHandler ____formationTargetHandler,
            Object ____shipTargetHandler)
        {
            // MissionOrderVM.OnTroopFormationSelected will close the order UI and open it again to refresh the available orders.
            // We should allow it to be closed so that orders will be refreshed. Or the advance visual orders may not be available when switching from agent view to rts view.
            Patch_MissionOrderVM.AllowClosingOrderUI = true;
            return false;
        }
    }
}
