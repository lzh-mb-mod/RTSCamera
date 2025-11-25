using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_NavalDLCHelpers 
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
                harmony.Patch(AccessTools.TypeByName("NavalDLCHelpers").Method("IsShipOrdersAvailable"),
                    prefix: new HarmonyMethod(typeof(Patch_NavalDLCHelpers).GetMethod(nameof(Prefix_IsShipOrdersAvailable), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        private static Type NavalShipsLogicType = AccessTools.TypeByName("NavalDLC.Missions.MissionLogics.NavalShipsLogic");
        private static MethodInfo GetShipAssignmentMethod = AccessTools.Method("NavalDLC.Missions.MissionLogics.NavalShipsLogic:GetShipAssignment");
        private static PropertyInfo HasMissionShipProperty = AccessTools.Property("NavalDLC.ShipAssignment:HasMissionShip");
        private static PropertyInfo MissionShipProperty = AccessTools.Property("NavalDLC.ShipAssignment:MissionShip");

        public static bool Prefix_IsShipOrdersAvailable(ref bool __result)
        {
            if (Mission.Current == null || !Mission.Current.IsNavalBattle || Mission.Current.PlayerTeam?.PlayerOrderController == null)
            {
                return true;
            }
            MBReadOnlyList<Formation> selectedFormations = Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations;
            if (selectedFormations == null)
                return true;
            var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;

            __result = IsShipOrderAvailable();
            return false;
        }

        private static MissionObject GetShip(MissionBehavior navalShipsLogic, TeamSideEnum teamSide, FormationClass formationIndex)
        {
            Object shipAssignment = GetShipAssignmentMethod.Invoke(navalShipsLogic, new object[] { teamSide, formationIndex });
            if ((bool)HasMissionShipProperty.GetValue(shipAssignment))
            {
                return (MissionObject)MissionShipProperty.GetValue(shipAssignment);
            }
            return null;
        }

        public static bool IsShipOrderAvailable()
        {
            MBReadOnlyList<Formation> selectedFormations = Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations;
            if (selectedFormations == null)
                return true;
            var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;
            if (isSpectatorCamera && RTSCameraConfig.Get().PlayerShipControllerInFreeCamera == PlayerShipControllerInFreeCamera.AI)
            {
                // Enable ship order when in free camera and PlayerShipControllerInFreeCamera is set to AI.
                return true;
            }
            else
            {
                if (Agent.Main != null)
                {
                    for (int index = 0; index < selectedFormations.Count; ++index)
                    {
                        if (Agent.Main.Formation != null && Agent.Main.Formation.FormationIndex == selectedFormations[index].FormationIndex && selectedFormations[index].Team.IsPlayerTeam)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }
}
