using System;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    public class ControlTroopsSelectionData
    {
        public SelectionOptionData SelectionOptionData;
        private EnhancedMissionConfig _config = EnhancedMissionConfig.Get();
        private ControlTroopLogic _logic = Mission.Current.GetMissionBehaviour<ControlTroopLogic>();

        public ControlTroopsSelectionData()
        {
            var agents = Mission.Current.PlayerTeam.ActiveAgents.Where(agent => agent.IsHero).ToList();
            SelectionOptionData = new SelectionOptionData(i =>
                {
                    if (i >= 0 && i < agents.Count && i != agents.IndexOf(Mission.Current.MainAgent))
                        SwitchMainAgent(agents[i]);
                }, () => agents.IndexOf(Mission.Current.MainAgent), agents.Count,
                agents.Select(agent => new SelectionItem(false, agent.Name)));
        }

        private void SwitchMainAgent(Agent agent)
        {
            _logic?.ControlAgent(agent);
        }
    }
}
