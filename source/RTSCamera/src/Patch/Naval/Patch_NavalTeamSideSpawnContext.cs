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
    public class Patch_NavalTeamSideSpawnContext
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
                harmony.Patch(AccessTools.TypeByName("NavalTeamSideSpawnContext").Method("AllocateAndDeployInitialTroopsOfPlayerTeam"),
                    prefix: new HarmonyMethod(typeof(Patch_NavalTeamSideSpawnContext).GetMethod(nameof(Prefix_AllocateAndDeployInitialTroopsOfPlayerTeam), BindingFlags.Static | BindingFlags.Public)));
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
        private static PropertyInfo _captain;

        public static bool Prefix_AllocateAndDeployInitialTroopsOfPlayerTeam(Object __instance, MissionLogic ____agentsLogic, MissionLogic ____shipsLogic)
        {
            if (!CommandBattleBehavior.CommandMode)
                return true;
            if (__instance == null || ____agentsLogic == null || ____shipsLogic == null)
                return true;
            _teamSide ??= AccessTools.Property(__instance.GetType(), "TeamSide");
            _findTroopOrigin ??= AccessTools.Method(____agentsLogic.GetType(), "FindTroopOrigin");

            var teamSide = (TeamSideEnum)_teamSide.GetValue(__instance);
            // if player character doesn't exists, find a hero under player's command, else find any troop under player's command
            IAgentOriginBase troopOrigin = (IAgentOriginBase)_findTroopOrigin.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin?.Troop?.IsPlayerCharacter == true) });
            if (troopOrigin == null)
            {
                troopOrigin = (IAgentOriginBase)_findTroopOrigin.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin?.IsUnderPlayersCommand == true && origin.Troop?.IsHero == true) });
                if (troopOrigin == null)
                {
                    troopOrigin = (IAgentOriginBase)_findTroopOrigin.Invoke(____agentsLogic, new object[] { teamSide, (Predicate<IAgentOriginBase>)(origin => origin?.IsUnderPlayersCommand == true) });
                }
            }
            if (troopOrigin == null)
                throw new InvalidOperationException("Failed to find a player-commanded troop for Naval deployment.");

            _getShipAssignment ??= AccessTools.Method(____shipsLogic.GetType(), "GetShipAssignment");
            var shipAssignment = _getShipAssignment.Invoke(____shipsLogic, new object[] { TeamSideEnum.PlayerTeam, FormationClass.Infantry });
            if (shipAssignment == null)
                throw new InvalidOperationException("Failed to find the player ship assignment for Naval deployment.");

            _missionShip ??= AccessTools.Property(shipAssignment.GetType(), "MissionShip");
            var missionShip = (MissionObject)_missionShip.GetValue(shipAssignment);
            if (missionShip == null)
                throw new InvalidOperationException("The player ship assignment has no mission ship.");

            _addReservedTroopToShip ??= AccessTools.Method(____agentsLogic.GetType(), "AddReservedTroopToShip");
            _addReservedTroopToShip.Invoke(____agentsLogic, new object[] { troopOrigin, missionShip });

            _assignTroops ??= AccessTools.Method(____agentsLogic.GetType(), "AssignTroops");
            _assignTroops.Invoke(____agentsLogic, new object[] { teamSide, false });

            _initializeReinforcementTimers ??= AccessTools.Method(____agentsLogic.GetType(), "InitializeReinforcementTimers");
            _initializeReinforcementTimers.Invoke(____agentsLogic, new object[] { teamSide, true, true });

            _checkSpawnNextBatch ??= AccessTools.Method(__instance.GetType(), "CheckSpawnNextBatch");
            _checkSpawnNextBatch.Invoke(__instance, null);

            _getActiveHeroesOfShip ??= AccessTools.Method(____agentsLogic.GetType(), "GetActiveHeroesOfShip");
            var activeHeroesOfShip = _getActiveHeroesOfShip.Invoke(
                ____agentsLogic, new object[] { missionShip }) as IEnumerable<Agent> ?? Enumerable.Empty<Agent>();

            Agent agent1 = activeHeroesOfShip.FirstOrDefault(agent => agent.IsPlayerTroop);
            // if no player troop, get any hero
            if (agent1 == null)
            {
                agent1 = activeHeroesOfShip.FirstOrDefault(agent => agent.IsHero);
            }

            _captain ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:Captain");
            if ((Agent)_captain.GetValue(missionShip) == agent1)
                return false;

            _assignCaptainToShipForDeploymentMode ??= AccessTools.Method(____agentsLogic.GetType(), "AssignCaptainToShipForDeploymentMode");
            _assignCaptainToShipForDeploymentMode.Invoke(____agentsLogic, new object[] { agent1, missionShip, missionShip });
            return false;
        }

    }
}
