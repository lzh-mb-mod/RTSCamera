using RTSCamera.Config;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Logic.SubLogic
{
    public class ControlAgentPreference
    {
        public Agent NearestAgent;
        public Agent NearestCompanion;
        private Mission Mission => Mission.Current;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();

        public void UpdateAgentPreferenceFromTeam(Team team, Vec3 position)
        {
            foreach (var agent in team.ActiveAgents)
            {
                UpdateAgentPreference(agent, position);
            }
        }

        public void UpdateAgentPreferenceFromFormation(FormationClass formationClass, Vec3 position)
        {
            if (formationClass < 0 || formationClass > FormationClass.NumberOfAllFormations)
            {
                return;
            }

            var formation = Mission.PlayerTeam.GetFormation(formationClass);
            formation.ApplyActionOnEachUnit(agent => UpdateAgentPreference(agent, position));
        }

        private void UpdateAgentPreference(Agent agent, Vec3 position)
        {
            if (!CanControl(agent))
                return;
            if (!_config.ControlTroopsInPlayerPartyOnly || Utility.IsInPlayerParty(agent))
            {
                if (agent.IsHero)
                {
                    if (_config.PreferToControlCompanions)
                    {
                        if (agent.Character is CharacterObject character)
                        {
                            if (character.HeroObject.IsPlayerCompanion &&
                                (NearestCompanion == null || NearestCompanion.Position.DistanceSquared(position) >
                                    agent.Position.DistanceSquared(position)))
                            {
                                NearestCompanion = agent;
                            }
                        }
                    }

                }

                // Prefer hero agent.
                if (NearestAgent == null || !NearestAgent.IsHero && agent.IsHero ||
                    (!NearestAgent.IsHero || agent.IsHero) && 
                    NearestAgent.Position.DistanceSquared(position) > agent.Position.DistanceSquared(position))
                {
                    NearestAgent = agent;
                }
            }
        }

        private bool CanControl(Agent agent)
        {
            return agent.IsHuman && agent.IsActive();
        }
    }
}
