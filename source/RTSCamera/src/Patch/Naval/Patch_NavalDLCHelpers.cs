using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using System;
using System.Linq.Expressions;
using System.Reflection;
using TaleWorlds.Engine;
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
                var captain = AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:Captain");
                var ship = Expression.Parameter(typeof(object), "ship");
                _getCaptain = Expression.Lambda<Func<object, Agent>>(
                    Expression.Property(Expression.Convert(ship, captain.DeclaringType), captain),
                    ship).Compile();
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
                MBDebug.Print(e.ToString());
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
                return false;
            if (Agent.Main != null)
            {
                var navalShipsLogic = Utility.GetNavalShipsLogic(Mission.Current);
                if (navalShipsLogic == null)
                    return false;
                for (int index = 0; index < selectedFormations.Count; ++index)
                {
                    var formation = selectedFormations[index];
                    if (formation?.Team == null)
                        return false;
                    var ship = Utilities.Utility.GetShip(navalShipsLogic, formation.Team.TeamSide, formation.FormationIndex);
                    if (ship == null)
                        return false;
                    var shipFormation = Utilities.Utility.GetShipFormation(ship);
                    if (shipFormation?.Team == null)
                        return false;
                    var isShipAIControlled = Utilities.Utility.IsShipAIControlled(ship);
                    var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;
                    if (isSpectatorCamera)
                    {
                        if (Agent.Main.Formation == shipFormation &&
                            Utilities.Utility.GetPlayerShipControllerInFreeCamera() != PlayerShipController.AI)
                        {
                            return false;
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

        private static Func<object, Agent> _getCaptain;

        public static bool Prefix_IsAgentCaptainOfFormationShip(Agent agent, Formation formation, ref bool __result)
        {
            if (agent == null || formation?.Team == null)
            {
                __result = false;
                return false;
            }

            var navalShipLogic = Utility.GetNavalShipsLogic(Mission.Current);
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
            var captain = _getCaptain(ship);
            var shipFormation = Utilities.Utility.GetShipFormation(ship);
            var isShipAIControlled = Utilities.Utility.IsShipAIControlled(ship);
            __result = !isShipAIControlled &&
                       (captain != null && agent == captain || agent.IsMainAgent && agent.Formation == shipFormation);
            return false;
        }
    }
}
