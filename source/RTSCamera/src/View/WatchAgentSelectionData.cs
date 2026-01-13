using MissionSharedLibrary.Utilities;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RTSCamera.View
{
    public class WatchAgentSelectionData
    {
        public MissionSharedLibrary.View.ViewModelCollection.Options.Selection.SelectionOptionData SelectionOptionData;
        private readonly FlyCameraMissionView _view = Mission.Current.GetMissionBehavior<FlyCameraMissionView>();

        public WatchAgentSelectionData(MissionScreen missionScreen)
        {
            var agents = (Mission.Current.PlayerTeam?.ActiveAgents ?? Mission.Current.Agents).Where(agent => agent.Character != null && agent.IsHero).ToList();
            agents.Sort((agent1, agent2) =>
            {
                var isHigherInMemberRoster = Utility.IsHigherInMemberRoster(agent1, agent2);
                if (isHigherInMemberRoster == true)
                {
                    return -1;
                }
                if (isHigherInMemberRoster == false)
                {
                    return 1;
                }
                return agent1.Index - agent2.Index;
            });
            SelectionOptionData = new MissionSharedLibrary.View.ViewModelCollection.Options.Selection.SelectionOptionData(i =>
            {
                if (Mission.Current?.Mode == TaleWorlds.Core.MissionMode.Deployment)
                    return;
                if (i >= 0 && i < agents.Count)
                {
                    WatchAgent(agents[i]);
                }
            }, () => agents.IndexOf(missionScreen.LastFollowedAgent), () => agents.Count,
                () => agents.Select(agent => new MissionSharedLibrary.View.ViewModelCollection.Options.Selection.SelectionItem(false, agent.Name)));
        }

        private void WatchAgent(Agent agent)
        {
            _view?.FocusOnAgent(agent);
        }
    }
}
