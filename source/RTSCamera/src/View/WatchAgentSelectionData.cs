using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera.View
{
    public class WatchAgentSelectionData
    {
        public MissionSharedLibrary.View.ViewModelCollection.Options.Selection.SelectionOptionData SelectionOptionData;
        private readonly FlyCameraMissionView _view = Mission.Current.GetMissionBehaviour<FlyCameraMissionView>();

        public WatchAgentSelectionData(MissionScreen missionScreen)
        {
            var agents = (Mission.Current.PlayerTeam?.ActiveAgents ?? Mission.Current.Agents).Where(agent => agent.Character != null && agent.IsHero).ToList();
            SelectionOptionData = new MissionSharedLibrary.View.ViewModelCollection.Options.Selection.SelectionOptionData(i =>
            {
                if (i >= 0 && i < agents.Count)
                {
                    WatchAgent(agents[i]);
                }
            }, () => agents.IndexOf(missionScreen.LastFollowedAgent), agents.Count,
                agents.Select(agent => new MissionSharedLibrary.View.ViewModelCollection.Options.Selection.SelectionItem(false, agent.Name)));
        }

        private void WatchAgent(Agent agent)
        {
            _view?.FocusOnAgent(agent);
        }
    }
}
