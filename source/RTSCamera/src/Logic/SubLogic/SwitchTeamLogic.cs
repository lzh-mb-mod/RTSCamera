using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;

namespace RTSCamera.Logic.SubLogic
{
    public class SwitchTeamLogic
    {
        private readonly RTSCameraLogic _logic;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();
        private ControlTroopLogic _controlTroopLogic;

        public Mission Mission => _logic.Mission;

        public SwitchTeamLogic(RTSCameraLogic logic)
        {
            _logic = logic;
        }

        public void OnBehaviourInitialize()
        {
            _controlTroopLogic = _logic.ControlTroopLogic;
        }

        public void OnMissionTick(float dt)
        {
            if (!NativeConfig.CheatMode)
                return;

            if (_config.SwitchTeamHotkeyEnabled && RTSCameraGameKeyCategory.GetKey(GameKeyEnum.SwitchTeam).IsKeyPressed(Mission.InputManager))
                SwapTeam();
        }

        public void SwapTeam()
        {
            if (!NativeConfig.CheatMode)
                return;
            if (!Utility.IsTeamValid(Mission.PlayerEnemyTeam))
                return;
            if (Mission.GetMissionBehavior<SiegeDeploymentHandler>() != null)
                return;
            bool firstTime = Mission.PlayerEnemyTeam.PlayerOrderController.Owner == null;
            var targetAgent = Mission.PlayerEnemyTeam.PlayerOrderController.Owner;
            // Fix a rare crash in e1.4.3 when targetAgent.Team == null && targetAgent.IsDeleted == true and even **targetAgent.IsActive() == true**.
            targetAgent = !Utility.IsAgentDead(targetAgent) && Utility.IsTeamValid(targetAgent?.Team)
                ? Mission.PlayerEnemyTeam.PlayerOrderController.Owner
                : !Utility.IsAgentDead(Mission.PlayerEnemyTeam.GeneralAgent) && Utility.IsTeamValid(Mission.PlayerEnemyTeam.GeneralAgent?.Team) ? Mission.PlayerEnemyTeam.GeneralAgent : Mission.PlayerEnemyTeam.Leader;
            
            if (Utility.IsAgentDead(targetAgent))
            {
                Utility.DisplayLocalizedText("str_rts_camera_enemy_wiped_out");
                return;
            }
            Utility.DisplayLocalizedText("str_rts_camera_switch_to_enemy_team");

            MissionLibrary.Event.MissionEvent.OnPreSwitchTeam();
            Mission.PlayerEnemyTeam.PlayerOrderController.Owner = targetAgent;
            Mission.PlayerTeam = Mission.PlayerEnemyTeam;
            _controlTroopLogic.SetToMainAgent(targetAgent);
            MissionLibrary.Event.MissionEvent.OnPostSwitchTeam();

            // TODO
            if (firstTime)
            {
                foreach (var formation in Mission.PlayerTeam.FormationsIncludingEmpty)
                {
                    bool isAIControlled = formation.IsAIControlled;
                    bool isSplittableByAI = formation.IsSplittableByAI;
                    formation.PlayerOwner = Mission.MainAgent;
                    formation.SetControlledByAI(isAIControlled, isSplittableByAI);
                }
            }
        }
    }
}
