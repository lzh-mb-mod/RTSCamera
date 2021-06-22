using RTSCamera.CommandSystem.Logic.Component;
using RTSCamera.CommandSystem.QuerySystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic.SubLogic
{
    public class QuerySystemSubLogic
    {
        public void OnCreated()
        {
            QueryDataStore.EnsureInitialized();
        }

        public void AfterAddTeam(Team team)
        {
            QueryDataStore.AddTeam(team);
        }

        public void OnRemoveBehaviour()
        {
            QueryDataStore.Clear();
        }

        public void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.AddComponent(new CommandSystemAgentComponent(agent));
        }
    }
}
