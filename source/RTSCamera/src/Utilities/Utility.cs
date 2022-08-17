using RTSCamera.Config.HotKey;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Utilities
{
    public class Utility
    {
        public static void PrintUsageHint()
        {
            var keyName = RTSCameraGameKeyCategory.GetKey(GameKeyEnum.FreeCamera).ToSequenceString();
            var hint = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_switch_camera_hint").SetTextVariable("KeyName", keyName).ToString();
            MissionSharedLibrary.Utilities.Utility.DisplayMessageForced(hint);
        }


        public static void UpdateMainAgentControllerInFreeCamera(Agent agent, Agent.ControllerType controller)
        {
            switch (controller)
            {
                case Agent.ControllerType.None:
                    MissionSharedLibrary.Utilities.Utility.PlayerControlAgent(agent);
                    agent.LookDirection = agent.GetMovementDirection().ToVec3();
                    break;
                case Agent.ControllerType.AI:
                    MissionSharedLibrary.Utilities.Utility.AIControlMainAgent(true, true);
                    break;
                case Agent.ControllerType.Player:
                    MissionSharedLibrary.Utilities.Utility.PlayerControlAgent(agent);
                    break;
            }
        }

        public static void UpdateMainAgentControllerState(Agent agent, bool isSpectatorCamera, Agent.ControllerType playerControllerInFreeCamera)
        {

            var controller = Mission.Current.GetMissionBehavior<MissionMainAgentController>();
            if (controller != null)
            {
                if (agent.Controller == Agent.ControllerType.Player && (!isSpectatorCamera || playerControllerInFreeCamera == Agent.ControllerType.Player))
                {
                    controller.CustomLookDir = isSpectatorCamera ? agent.LookDirection : Vec3.Zero;
                    controller.IsDisabled = false;
                }
                else
                {
                    controller.CustomLookDir = Vec3.Zero;
                    controller.IsDisabled = true;
                    controller.InteractionComponent.ClearFocus();
                }
            }
        }
    }
}
