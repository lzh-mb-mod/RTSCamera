using System.ComponentModel;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;

namespace EnhancedMission
{
    class ControlTroopAfterPlayerDeadLogic : MissionLogic
    {

        public void ControlTroopAfterDead()
        {
            // Mission.MainAgent may be null because of free camera mode.
            if (Utility.IsPlayerDead() && this.Mission.PlayerTeam != null && Utility.IsAgentDead(this.Mission.PlayerTeam.PlayerOrderController.Owner))
            {
                var missionScreen = ScreenManager.TopScreen as MissionScreen;
                Agent closestAllyAgent = missionScreen?.LastFollowedAgent?.IsActive() ?? false ? missionScreen?.LastFollowedAgent : 
                                         this.Mission.GetClosestAllyAgent(this.Mission.PlayerTeam,
                                             new WorldPosition(this.Mission.Scene,
                                                 this.Mission.Scene.LastFinalRenderCameraPosition).GetGroundVec3(),
                                             1000) ?? this.Mission.PlayerTeam.Leader;
                if (closestAllyAgent != null)
                {
                    Utility.DisplayLocalizedText("str_control_troop");
                    closestAllyAgent.Controller = Agent.ControllerType.Player;
                    var switchCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
                    if (switchCameraLogic != null && switchCameraLogic.isSpectatorCamera)
                        switchCameraLogic.SwitchCamera();
                }
                else
                    Utility.DisplayLocalizedText("str_no_troop_to_control");
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (this.Mission.InputManager.IsKeyPressed(TaleWorlds.InputSystem.InputKey.F))
            {
                ControlTroopAfterDead();
            }
        }
    }
}