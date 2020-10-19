using System.Linq;
using RTSCamera.View.Basic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera.View
{
    public class WatchAgentSelectionData
    {
        public SelectionOptionData SelectionOptionData;
        private readonly FlyCameraMissionView _view = Mission.Current.GetMissionBehaviour<FlyCameraMissionView>();

        public WatchAgentSelectionData(MissionScreen missionScreen)
        {
            var agents = Mission.Current.PlayerTeam.ActiveAgents.Where(agent => agent.IsHero).ToList();
            SelectionOptionData = new SelectionOptionData(i =>
            {
                if (i >= 0 && i < agents.Count)
                {
                    WatchAgent(agents[i]);
                }
            }, () => agents.IndexOf(missionScreen.LastFollowedAgent), agents.Count,
                agents.Select(agent => new SelectionItem(false, agent.Name)));
        }

        private void WatchAgent(Agent agent)
        {
            _view?.FocusOnAgent(agent);
        }
    }
}
