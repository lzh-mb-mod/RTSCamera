using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch.Naval
{
    public class Patch_MissionGauntletNavalOrderUIHandler
    {
        private static MethodInfo _setIsFormationTargetingDisabled;
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
                return false;
            }

            return true;
        }

        public static bool Prefix_OnSelectedFormationsChanged(object __instance,
            OrderController ____orderController,
            MissionFormationTargetSelectionHandler ____formationTargetHandler,
            Object ____shipTargetHandler)
        {
            bool isDisabled = !Patch_NavalDLCHelpers.IsShipOrderAvailable();
            ____formationTargetHandler?.SetIsFormationTargetingDisabled(isDisabled);
            if (____shipTargetHandler == null)
                return false;
            _setIsFormationTargetingDisabled ??= AccessTools.Method("NavalDLC.View.MissionViews.NavalShipTargetSelectionHandler:SetIsFormationTargetingDisabled");
            _setIsFormationTargetingDisabled.Invoke(____shipTargetHandler, new object[] { isDisabled });
            return false;
        }
    }
}
