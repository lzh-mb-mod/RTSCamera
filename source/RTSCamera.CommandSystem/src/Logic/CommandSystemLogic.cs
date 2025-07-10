using RTSCamera.CommandSystem.Logic.SubLogic;
using RTSCamera.CommandSystem.Patch;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic
{
    public class CommandSystemLogic : MissionLogic
    {
        public readonly FormationColorSubLogic FormationColorSubLogic = new FormationColorSubLogic();

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            FormationColorSubLogic.OnBehaviourInitialize();
            Patch_OrderTroopPlacer.OnBehaviorInitialize();

            Utilities.Utility.PrintOrderHint();
        }

        public override void OnRemoveBehavior()
        {
            FormationColorSubLogic.OnRemoveBehaviour();
            Patch_OrderTroopPlacer.OnRemoveBehavior();
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            FormationColorSubLogic.OnPreDisplayMissionTick(dt);
        }

        public override void AfterAddTeam(Team team)
        {
            FormationColorSubLogic.AfterAddTeam(team);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            FormationColorSubLogic.OnAgentBuild(agent, banner);
        }

        public override void OnAgentFleeing(Agent affectedAgent)
        {
            base.OnAgentFleeing(affectedAgent);

            FormationColorSubLogic.OnAgentFleeing(affectedAgent);
        }
    }
}
