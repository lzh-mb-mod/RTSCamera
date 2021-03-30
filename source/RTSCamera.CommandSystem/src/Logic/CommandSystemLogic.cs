using RTSCamera.CommandSystem.Logic.SubLogic;
using RTSCamera.CommandSystem.QuerySystem;
using RTSCamera.CommandSystem.View;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic
{
    public class CommandSystemLogic : MissionLogic
    {
        private readonly QuerySystemSubLogic _querySystemSubLogic = new QuerySystemSubLogic();
        public readonly FormationColorMissionView FormationColorMissionView = new FormationColorMissionView();

        public override void OnCreated()
        {
            _querySystemSubLogic.OnCreated();
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            FormationColorMissionView.OnMissionScreenInitialize();
        }

        public override void OnRemoveBehaviour()
        {
            _querySystemSubLogic.OnRemoveBehaviour();
            FormationColorMissionView.OnMissionScreenFinalize();
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            FormationColorMissionView.OnMissionScreenTick(dt);
        }

        public override void AfterAddTeam(Team team)
        {
            _querySystemSubLogic.AfterAddTeam(team);
            FormationColorMissionView.AfterAddTeam(team);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            _querySystemSubLogic.OnAgentBuild(agent, banner);
            FormationColorMissionView.OnAgentBuild(agent, banner);
        }

        public override void OnAgentFleeing(Agent affectedAgent)
        {
            base.OnAgentFleeing(affectedAgent);

            FormationColorMissionView.OnAgentFleeing(affectedAgent);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            _querySystemSubLogic.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }
    }
}
