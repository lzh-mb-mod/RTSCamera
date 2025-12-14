using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_ShipAgentSpawnLogicTeamSide
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
                harmony.Patch(AccessTools.TypeByName("ShipAgentSpawnLogicTeamSide").Method("AllocateAndDeployInitialTroopsOfPlayerTeam"),
                    prefix: new HarmonyMethod(typeof(Patch_ShipAgentSpawnLogicTeamSide).GetMethod(nameof(Prefix_AllocateAndDeployInitialTroopsOfPlayerTeam), BindingFlags.Static | BindingFlags.Public)));
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

        private static PropertyInfo _teamSide;
        private static MethodInfo _findTroopOrigin;
        private static MethodInfo _getShipAssignment;
        private static PropertyInfo _missionShip;
        private static MethodInfo _addReservedTroopToShip;
        private static MethodInfo _assignTroops;
        private static MethodInfo _initializeReinforcementTimers;
        private static MethodInfo _checkSpawnNextBatch;
        private static MethodInfo _getActiveHeroesOfShip;
        private static MethodInfo _assignCaptainToShipForDeploymentMode;

        public static bool Prefix_AllocateAndDeployInitialTroopsOfPlayerTeam(Object __instance, MissionLogic ____agentsLogic, MissionLogic ____shipsLogic)
        {
            if (!CommandBattleBehavior.CommandMode)
                return true;
            _teamSide ??= AccessTools.Property(__instance.GetType(), "TeamSide");
            _findTroopOrigin ??= AccessTools.Method(____agentsLogic.GetType(), "FindTroopOrigin");

            var teamSide = (TeamSideEnum)_teamSide.GetValue(__instance);
            // if player character doesn't exists, find a hero under player's command, else find any troop under player's command
            IAgentOriginBase troopOrigin = (IAgentOriginBase)_findTroopOrigin.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin.Troop.IsPlayerCharacter) });
            if (troopOrigin == null)
            {
                troopOrigin = (IAgentOriginBase)_findTroopOrigin.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin.IsUnderPlayersCommand && origin.Troop.IsHero ) });
                if (troopOrigin == null)
                {
                    troopOrigin = (IAgentOriginBase)_findTroopOrigin.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin.IsUnderPlayersCommand) });
                }
            }

            _getShipAssignment ??= AccessTools.Method(____shipsLogic.GetType(), "GetShipAssignment");
            var shipAssignment = _getShipAssignment.Invoke(____shipsLogic, new object[] { TeamSideEnum.PlayerTeam, FormationClass.Infantry });

            _missionShip ??= AccessTools.Property(shipAssignment.GetType(), "MissionShip");
            var missionShip = (MissionObject)_missionShip.GetValue(shipAssignment);

            _addReservedTroopToShip ??= AccessTools.Method(____agentsLogic.GetType(), "AddReservedTroopToShip");
            _addReservedTroopToShip.Invoke(____agentsLogic, new object[] { troopOrigin, missionShip });

            _assignTroops ??= AccessTools.Method(____agentsLogic.GetType(), "AssignTroops");
            _assignTroops.Invoke(____agentsLogic, new object[] { teamSide, false });

            _initializeReinforcementTimers ??= AccessTools.Method(____agentsLogic.GetType(), "InitializeReinforcementTimers");
            _initializeReinforcementTimers.Invoke(____agentsLogic, new object[] { teamSide, true, true });

            _checkSpawnNextBatch ??= AccessTools.Method(__instance.GetType(), "CheckSpawnNextBatch");
            _checkSpawnNextBatch.Invoke(__instance, null);

            _getActiveHeroesOfShip ??= AccessTools.Method(____agentsLogic.GetType(), "GetActiveHeroesOfShip");
            var activeHeroesOfShip = _getActiveHeroesOfShip.Invoke(____agentsLogic, new object[] { missionShip }) as IEnumerable<Agent>;

            Agent agent1 = activeHeroesOfShip.FirstOrDefault(agent => agent.IsPlayerTroop);
            // if no player troop, get any hero
            if (agent1 == null)
            {
                agent1 = activeHeroesOfShip.FirstOrDefault(agent => agent.IsHero);
            }

            var formation = Utilities.Utility.GetShipFormation(missionShip);
            if (formation.Captain == agent1)
                return false;

            _assignCaptainToShipForDeploymentMode ??= AccessTools.Method(____agentsLogic.GetType(), "AssignCaptainToShipForDeploymentMode");
            _assignCaptainToShipForDeploymentMode.Invoke(____agentsLogic, new object[] { agent1, missionShip, missionShip });
            return false;
        }

    }
}
