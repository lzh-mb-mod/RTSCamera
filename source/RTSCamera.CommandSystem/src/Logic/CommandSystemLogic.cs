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
        public readonly FormationColorSubLogic FormationColorSubLogic = new FormationColorSubLogic();

        public override void OnCreated()
        {
            _querySystemSubLogic.OnCreated();
        }

        public override void OnBehaviourInitialize()
        {
            base.OnBehaviourInitialize();

            FormationColorSubLogic.OnBehaviourInitialize();
        }

        public override void OnRemoveBehaviour()
        {
            _querySystemSubLogic.OnRemoveBehaviour();
            FormationColorSubLogic.OnRemoveBehaviour();
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            FormationColorSubLogic.OnPreDisplayMissionTick(dt);
        }

        public override void AfterAddTeam(Team team)
        {
            _querySystemSubLogic.AfterAddTeam(team);
            FormationColorSubLogic.AfterAddTeam(team);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            _querySystemSubLogic.OnAgentBuild(agent, banner);
            FormationColorSubLogic.OnAgentBuild(agent, banner);
        }

        public override void OnAgentFleeing(Agent affectedAgent)
        {
            base.OnAgentFleeing(affectedAgent);

            FormationColorSubLogic.OnAgentFleeing(affectedAgent);
        }
    }
}
