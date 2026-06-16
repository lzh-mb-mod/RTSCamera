using HarmonyLib;
using Microsoft.VisualBasic;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_MissionShip
    {
        private static PropertyInfo _isSinking;
        private static PropertyInfo _isPlayerShip;
        private static MethodInfo _setController;
        private static bool _patched;

        public static bool ShouldAIControlPlayerShipInPlayerMode;

        public static bool AIPilotShipCommandJustGiven = false;
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
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_UpdateController(MissionObject __instance)
        {
            _isSinking ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:IsSinking");
            if ((bool)_isSinking.GetValue(__instance))
                return false;
            // Only handles player ship. For AI ship we do not change the original logic.
            _isPlayerShip ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:IsPlayerShip");
            if (!(bool)_isPlayerShip.GetValue(__instance))
                return true;
            var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;
            if (isSpectatorCamera)
            {
                var controller = Utilities.Utility.GetPlayerShipControllerInFreeCamera();
                _setController ??= AccessTools.Method("NavalDLC.Missions.Objects.MissionShip:SetController");
                // convert to int to avoid exception on Linux proton.
                _setController.Invoke(__instance, new object[] { (int)controller, true });
                return false;
            }
            else
            {
                if (Agent.Main == null || Utilities.Utility.IsShipPilotByPlayer(__instance))
                {
                    var isPlayerControlled = Utilities.Utility.IsShipPlayerControlled(__instance);
                    // Player has been piloting the ship for a while.
                    if (isPlayerControlled)
                    {
                        if (AIPilotShipCommandJustGiven)
                        {
                            AIPilotShipCommandJustGiven = false;
                            ShouldAIControlPlayerShipInPlayerMode = true;
                            var shipControllerMachine = Utilities.Utility.GetShipControllerMachine(__instance);
                            shipControllerMachine.PilotAgent.StopUsingGameObject();
                            SetShipController(__instance, PlayerShipController.AI);
                            return false;
                        }
                    }
                    AIPilotShipCommandJustGiven = false;
                    return true;
                }
                AIPilotShipCommandJustGiven = false;
                var formation = Utilities.Utility.GetShipFormation(__instance);
                if (formation != null && formation.IsAIControlled)
                {
                    ShouldAIControlPlayerShipInPlayerMode = true;
                }
                var shipFormation = Utilities.Utility.GetShipFormation(__instance);

                var playerControlledShip = Utilities.Utility.GetPlayerControlledShip(Mission.Current);
                // When player stops piloting the ship, and PlayerControlledShip is not updated yet because ship controller is not updated.
                if (__instance == playerControlledShip)
                {
                    var steeringMode = RTSCameraConfig.Get().SteeringModeWhenPlayerStopsPiloting;

                    if (steeringMode == SteeringMode.None)
                    {
                        if (ShouldAIControlPlayerShipInPlayerMode)
                        {
                            ShouldAIControlPlayerShipInPlayerMode = false;
                            Utility.DisplayLocalizedText("str_rts_camera_soldiers_stop_controlling_ship");
                        }
                    }
                    else if (!ShouldAIControlPlayerShipInPlayerMode && !(RTSCameraSubModule.IsHelmsmanInstalled && shipFormation.FormationIndex == FormationClass.Infantry))
                    {
                        ShouldAIControlPlayerShipInPlayerMode = true;
                        Utility.DisplayLocalizedText("str_rts_camera_soldiers_start_controlling_ship");
                    }
                    if (steeringMode == SteeringMode.DelegateCommand)
                    {
                        shipFormation.SetControlledByAI(true);
                    }
                    //if (Mission.Current.IsOrderMenuOpen)
                    //{
                    //    RTSCameraLogic.Instance.SwitchFreeCameraLogic.RefreshOrders();
                    //}
                }
                // When helmsman is installed, exclude infantry formation because helmsman will handle it.
                if (ShouldAIControlPlayerShipInPlayerMode && !(RTSCameraSubModule.IsHelmsmanInstalled && shipFormation.FormationIndex == FormationClass.Infantry))
                {
                    SetShipController(__instance, PlayerShipController.AI);
                    return false;
                }
                return true;
            }
        }

        private static void SetShipController(MissionObject ship, PlayerShipController controller)
        {
            _setController ??= AccessTools.Method("NavalDLC.Missions.Objects.MissionShip:SetController");
            // convert to int to avoid exception on Linux proton.
            _setController.Invoke(ship, new object[] { (int)controller, true });
        }
    }
}
