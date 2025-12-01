using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Reflection;
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
                harmony.Patch(AccessTools.TypeByName("NavalDLCHelpers").Method("IsPlayerCaptainOfFormationShip"),
                    prefix: new HarmonyMethod(typeof(Patch_NavalDLCHelpers).GetMethod(nameof(Prefix_IsPlayerCaptainOfFormationShip), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(AccessTools.TypeByName("NavalDLCHelpers").Method("IsAgentCaptainOfFormationShip"),
                    prefix: new HarmonyMethod(typeof(Patch_NavalDLCHelpers).GetMethod(nameof(Prefix_IsAgentCaptainOfFormationShip), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_IsShipOrdersAvailable(ref bool __result)
        {
            if (Mission.Current == null || !Mission.Current.IsNavalBattle || Mission.Current.PlayerTeam?.PlayerOrderController == null)
            {
                return true;
            }
            MBReadOnlyList<Formation> selectedFormations = Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations;
            if (selectedFormations == null)
                return true;

            __result = IsShipOrderAvailable();
            return false;
        }

        public static bool IsShipOrderAvailable()
        {
            MBReadOnlyList<Formation> selectedFormations = Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations;
            if (selectedFormations == null)
                return true;
            if (Agent.Main != null)
            {
                var navalShipsLogic = Utilities.Utility.GetNavalShipsLogic(Mission.Current);
                if (navalShipsLogic == null)
                    return true;
                for (int index = 0; index < selectedFormations.Count; ++index)
                {
                    var ship = Utilities.Utility.GetShip(navalShipsLogic, selectedFormations[index].Team.TeamSide, selectedFormations[index].FormationIndex);
                    if (ship == null)
                        return false;
                    var shipFormation = (Formation)AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:Formation").GetValue(ship);
                    var isShipAIControlled = Utilities.Utility.IsShipAIControlled(ship);
                    var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;
                    if (isSpectatorCamera)
                    {
                        if (Agent.Main.Formation != shipFormation)
                        {
                            // Enable ship order when in free camera and ship is controlled by AI.
                            return true;
                        }
                        else
                        {
                            return Utilities.Utility.GetPlayerShipControllerInFreeCamera() == PlayerShipController.AI;
                        }
                    }
                    else
                    {
                        if (Agent.Main.Formation != null && Agent.Main.Formation == shipFormation && shipFormation.Team.IsPlayerTeam && !isShipAIControlled)
                            return false;
                    }
                }
            }
            return true;
        }

        public static bool Prefix_IsPlayerCaptainOfFormationShip(Formation formation, ref bool __result)
        {
            return Prefix_IsAgentCaptainOfFormationShip(Agent.Main, formation, ref __result);
        }

        public static bool Prefix_IsAgentCaptainOfFormationShip(Agent agent, Formation formation, ref bool __result)
        {
            var navalShipLogic = Utilities.Utility.GetNavalShipsLogic(Mission.Current);
            if (navalShipLogic == null)
            {
                __result = false;
                return false;
            }
            var ship = Utilities.Utility.GetShip(navalShipLogic, formation.Team.TeamSide, formation.FormationIndex);
            if (ship == null)
            {
                __result = false;
                return false;
            }
            var captain = (Agent)AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:Captain").GetValue(ship);
            var shipFormation = (Formation)AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:Formation").GetValue(ship);
            var isShipAIControlled = Utilities.Utility.IsShipAIControlled(ship);
            __result = !isShipAIControlled && (captain != null && agent == captain || agent.IsMainAgent && agent.Formation == shipFormation);
            return false;
        }
    }
}
