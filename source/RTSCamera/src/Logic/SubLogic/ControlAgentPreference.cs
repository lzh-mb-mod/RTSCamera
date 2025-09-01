using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic
{
    public class ControlAgentPreference
    {
        public Agent BestAgent;
        public Agent BestHero;
        private Mission Mission => Mission.Current;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();

        public void UpdateAgentPreferenceFromTeam(Team team, Vec3 position, bool ignoreRetreatingAgents, bool controlTroopsInPlayerPartyOnly)
        {
            foreach (var agent in team.ActiveAgents)
            {
                UpdateAgentPreference(agent, position, ignoreRetreatingAgents, controlTroopsInPlayerPartyOnly);
            }
        }

        public void UpdateAgentPreferenceFromFormation(FormationClass formationClass, Vec3 position, bool ignoreRetreatingAgents, bool controlTroopsInPlayerPartyOnly)
        {
            if (formationClass < 0 || formationClass >= FormationClass.NumberOfAllFormations)
            {
                return;
            }

            var formation = Mission.PlayerTeam.GetFormation(formationClass);
            formation.ApplyActionOnEachUnit(agent => UpdateAgentPreference(agent, position, ignoreRetreatingAgents, controlTroopsInPlayerPartyOnly));
        }

        private void UpdateAgentPreference(Agent agent, Vec3 position, bool ignoreRetreatingAgents, bool controlTroopsInPlayerPartyOnly)
        {
            if (!CanControl(agent) || (ignoreRetreatingAgents && agent.IsRunningAway))
                return;
            if (!controlTroopsInPlayerPartyOnly || Utility.IsInPlayerParty(agent) || WatchBattleBehavior.WatchMode)
            {
                if (BestAgent == null || !BestAgent.IsHero && agent.IsHero || (!Utility.IsInPlayerParty(BestAgent) && Utility.IsInPlayerParty(agent)) ||
                    BestAgent.IsHero && agent.IsHero && (Utility.IsHigherInMemberRoster(agent, BestAgent) ??
                                                         BestAgent.Position.DistanceSquared(position) >
                                                         agent.Position.DistanceSquared(position)) ||
                    !BestAgent.IsHero && !agent.IsHero &&
                    BestAgent.Position.DistanceSquared(position) > agent.Position.DistanceSquared(position))
                {
                    BestAgent = agent;
                }

                if (!_config.PreferUnitsInSameFormation && agent.IsHero)
                {
                    // intended to find best hero at team scope.
                    if (BestHero == null || (Utility.IsHigherInMemberRoster(agent, BestHero) ??
                                             BestHero.Position.DistanceSquared(position) >
                                             agent.Position.DistanceSquared(position)))
                    {
                        BestHero = agent;
                    }
                }
            }
        }

        private bool CanControl(Agent agent)
        {
            return agent.IsHuman && agent.IsActive();
        }
    }
}
