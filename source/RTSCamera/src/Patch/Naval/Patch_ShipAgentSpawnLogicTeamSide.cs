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

        public static bool Prefix_AllocateAndDeployInitialTroopsOfPlayerTeam(Object __instance, MissionLogic ____agentsLogic, MissionLogic ____shipsLogic)
        {
            if (!CommandBattleBehavior.CommandMode)
                return true;
            var teamSideProperty = AccessTools.Property(__instance.GetType(), "TeamSide");
            var findTroopOriginMethod = AccessTools.Method(____agentsLogic.GetType(), "FindTroopOrigin");

            var teamSide = (TeamSideEnum)teamSideProperty.GetValue(__instance);
            // if player character doesn't exists, find a hero under player's command, else find any troop under player's command
            IAgentOriginBase troopOrigin = (IAgentOriginBase)findTroopOriginMethod.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin.Troop.IsPlayerCharacter) });
            if (troopOrigin == null)
            {
                troopOrigin = (IAgentOriginBase)findTroopOriginMethod.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin.IsUnderPlayersCommand && origin.Troop.IsHero ) });
                if (troopOrigin == null)
                {
                    troopOrigin = (IAgentOriginBase)findTroopOriginMethod.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin.IsUnderPlayersCommand) });
                }
            }

            var getShipAssignmentMethod = AccessTools.Method(____shipsLogic.GetType(), "GetShipAssignment");
            var shipAssignment = getShipAssignmentMethod.Invoke(____shipsLogic, new object[] { TeamSideEnum.PlayerTeam, FormationClass.Infantry });

            var missionShipProperty = AccessTools.Property(shipAssignment.GetType(), "MissionShip");
            var missionShip = (MissionObject)missionShipProperty.GetValue(shipAssignment);

            var addReservedTroopToShipMethod = AccessTools.Method(____agentsLogic.GetType(), "AddReservedTroopToShip");
            addReservedTroopToShipMethod.Invoke(____agentsLogic, new object[] { troopOrigin, missionShip });
            
            var assignTroops = AccessTools.Method(____agentsLogic.GetType(), "AssignTroops");
            assignTroops.Invoke(____agentsLogic, new object[] { teamSide, false });

            var initializeReinforcementTimersMethod = AccessTools.Method(____agentsLogic.GetType(), "InitializeReinforcementTimers");
            initializeReinforcementTimersMethod.Invoke(____agentsLogic, new object[] { teamSide, true, true });

            var checkSpawnNextBatchMethod = AccessTools.Method(__instance.GetType(), "CheckSpawnNextBatch");
            checkSpawnNextBatchMethod.Invoke(__instance, null);

            var getActiveHeroesOfShipMethod = AccessTools.Method(____agentsLogic.GetType(), "GetActiveHeroesOfShip");
            var activeHeroesOfShip = getActiveHeroesOfShipMethod.Invoke(____agentsLogic, new object[] { missionShip }) as IEnumerable<Agent>;

            Agent agent1 = activeHeroesOfShip.FirstOrDefault(agent => agent.IsPlayerTroop);
            // if no player troop, get any hero
            if (agent1 == null)
            {
                agent1 = activeHeroesOfShip.FirstOrDefault(agent => agent.IsHero);
            }

            var formationProperty = AccessTools.Property(missionShip.GetType(), "Formation");
            var formation = formationProperty.GetValue(missionShip) as Formation;
            if (formation.Captain == agent1)
                return false;

            var assignCaptainToShipForDeploymentModeMethod = AccessTools.Method(____agentsLogic.GetType(), "AssignCaptainToShipForDeploymentMode");
            assignCaptainToShipForDeploymentModeMethod.Invoke(____agentsLogic, new object[] { agent1, missionShip, missionShip });
            return false;
        }

    }
}
