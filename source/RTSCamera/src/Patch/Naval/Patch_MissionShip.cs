using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;

namespace RTSCamera.Patch.Naval
{
    public class Patch_MissionShip
    {
        private static PropertyInfo _isSinking = AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:IsSinking");
        private static PropertyInfo _isPlayerShip = AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:IsPlayerShip");
        private static PropertyInfo _hasController = AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:HasController");
        private static MethodInfo _setController = AccessTools.Method("NavalDLC.Missions.Objects.MissionShip:SetController");
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
                harmony.Patch(AccessTools.TypeByName("MissionShip").Method("UpdateController"),
                    prefix: new HarmonyMethod(typeof(Patch_MissionShip).GetMethod(nameof(Prefix_UpdateController), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_UpdateController(object __instance)
        {
            if ((bool)_isSinking.GetValue(__instance))
                return false;
            // Only handles player ship. For AI ship we do not change the original logic.
            if (!(bool)_isPlayerShip.GetValue(__instance))
                return true;
            var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;
            // Only handles free camera mode.
            if (!isSpectatorCamera)
                return true;
            var controller = RTSCameraConfig.Get().PlayerShipControllerInFreeCamera;
            _setController.Invoke(__instance, new object[] { RTSCameraConfig.Get().PlayerShipControllerInFreeCamera, true });
            return false;
        }
    }
}
