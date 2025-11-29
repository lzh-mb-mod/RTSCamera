using HarmonyLib;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Logic;
using RTSCamera.View;
using SandBox.Missions.MissionLogics.Arena;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
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
            var hint = TaleWorlds.MountAndBlade.Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_switch_camera_hint").SetTextVariable("KeyName", keyName).ToString();
            MissionSharedLibrary.Utilities.Utility.DisplayMessage(hint);
        }

        public static void PrintOrderHint()
        {
            var hint = GameTexts.FindText("str_rts_camera_focus_on_formation_hint");
            hint.SetTextVariable("KeyName", RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ControlTroop).ToSequenceString());
            MissionSharedLibrary.Utilities.Utility.DisplayMessage(hint.ToString());
        }


        public static void UpdateMainAgentControllerInFreeCamera(Agent agent, AgentControllerType controller)
        {
            switch (controller)
            {
                case AgentControllerType.None:
                    MissionSharedLibrary.Utilities.Utility.PlayerControlAgent(agent);
                    agent.LookDirection = agent.GetMovementDirection().ToVec3();
                    break;
                case AgentControllerType.AI:
                    MissionSharedLibrary.Utilities.Utility.AIControlMainAgent(
                        Mission.Current.Mode != MissionMode.StartUp &&
                        Mission.Current.Mode != MissionMode.Conversation &&
                        //Mission.Current.Mode != MissionMode.Stealth &&
                        Mission.Current.Mode != MissionMode.Barter &&
                        Mission.Current.Mode != MissionMode.Deployment &&
                        Mission.Current.Mode != MissionMode.Replay, true);
                    break;
                case AgentControllerType.Player:
                    MissionSharedLibrary.Utilities.Utility.PlayerControlAgent(agent);
                    break;
            }
        }

        public static void UpdateMainAgentControllerState(Agent agent, bool isSpectatorCamera, AgentControllerType playerControllerInFreeCamera)
        {
            var controller = Mission.Current.GetMissionBehavior<MissionMainAgentController>();
            if (controller != null)
            {
                if (agent.Controller == AgentControllerType.Player &&
                    (!isSpectatorCamera ||
                     playerControllerInFreeCamera == AgentControllerType.Player))
                {
                    controller.CustomLookDir = isSpectatorCamera ? agent.LookDirection : Vec3.Zero;
                    controller.Enable();
                }
                else
                {
                    controller.CustomLookDir = Vec3.Zero;
                    controller.Disable();
                    controller.InteractionComponent.ClearFocus();
                }
            }
        }

        public static bool IsArenaCombat(Mission mission)
        {
            foreach (var missionLogic in mission.MissionLogics)
            {
                if (missionLogic is ArenaAgentStateDeciderLogic)
                    return true;
            }

            return false;
        }

        public static bool IsBattleCombat(Mission mission)
        {
            return mission.Mode == MissionMode.Battle && mission.CombatType == Mission.MissionCombatType.Combat &&
                   !IsArenaCombat(mission);
        }

        public static void FastForwardInHideout(Mission mission)
        {
            mission.SetFastForwardingFromUI(true);
            MissionSharedLibrary.Utilities.Utility.DisplayLocalizedText("str_rts_camera_fast_forward_hideout_hint");
            var formationToFollow = mission.MainAgent?.Formation ?? mission.PlayerTeam.FormationsIncludingSpecialAndEmpty?.FirstOrDefault(f => f.CountOfUnits > 0);
            if (formationToFollow != null)
            {
                mission.GetMissionBehavior<FlyCameraMissionView>()?.FocusOnFormation(formationToFollow);
            }
            foreach (var formation in mission.PlayerTeam.FormationsIncludingSpecialAndEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
                }
            }
        }

        public static MissionBehavior GetNavalShipsLogic(Mission mission)
        {
            return MissionSharedLibrary.Utilities.Utility.GetMissionBehaviorOfType(mission, AccessTools.TypeByName("NavalDLC.Missions.MissionLogics.NavalShipsLogic"));
        }

        public static MissionObject GetShip(MissionBehavior missionShipLogic, TeamSideEnum teamSide, FormationClass formationClass)
        {
            var getShipAssignmentMethod = AccessTools.Method(missionShipLogic.GetType(), "GetShipAssignment");
            var shipAssignment = getShipAssignmentMethod.Invoke(missionShipLogic, new object[] { teamSide, formationClass });

            var missionShipProperty = AccessTools.Property(shipAssignment.GetType(), "MissionShip");
            var missionShip = (MissionObject)missionShipProperty.GetValue(shipAssignment);
            return missionShip;
        }

        private static PropertyInfo _shipControllerMachine;

        public static bool IsShipPilotByPlayer(MissionObject missionShip)
        {
            _shipControllerMachine ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:ShipControllerMachine");
            var shipControllerMachine = (UsableMachine)_shipControllerMachine.GetValue(missionShip);

            var pilotAgent = shipControllerMachine.PilotAgent;
            return pilotAgent != null && pilotAgent.IsPlayerControlled;
        }

        private static PropertyInfo _playerControlledShip;

        public static MissionObject GetPlayerControlledShip(Mission mission)
        {
            var navalShipsLogic = GetNavalShipsLogic(mission);
            _playerControlledShip ??= AccessTools.Property("NavalDLC.Missions.MissionLogics.NavalShipsLogic:PlayerControlledShip");
            return (MissionObject)_playerControlledShip.GetValue(navalShipsLogic);
        }

        private static PropertyInfo _formation;

        public static Formation GetShipFormation(MissionObject playerShip)
        {
            _formation ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:Formation");
            return (Formation)_formation.GetValue(playerShip);
        }

        public static bool ShouldAddToggleShipOrderOrder()
        {
            if (Agent.Main == null)
                return false;
            var playerControlledShip = GetPlayerControlledShip(Mission.Current);
            var playerControlledShipFormation = playerControlledShip == null ? null : GetShipFormation(playerControlledShip);
            var selectedFormations = Mission.Current.PlayerTeam.PlayerOrderController.SelectedFormations;
            foreach (var formation in selectedFormations)
            {
                if (formation == playerControlledShipFormation && Agent.Main.IsPlayerControlled)
                {
                    return false;
                }
                var isSpectatorCamera = RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false;
                var controller = RTSCameraConfig.Get().PlayerShipControllerInFreeCamera;
                if (isSpectatorCamera && controller == PlayerShipController.AI)
                {
                    return false;
                }
                // When helmsman is installed, exclude infantry formation because helmsman will handle it.
                if (formation == Agent.Main.Formation && !(RTSCameraSubModule.IsHelmsmanInstalled && formation.FormationIndex == FormationClass.Infantry))
                {
                    return true;
                }
            }
            return false;
        }

        public static MissionObject GetPlayerShip(Mission mission)
        {
            if (Agent.Main == null || Agent.Main.Formation == null)
                return null;
            var navalShipsLogic = GetNavalShipsLogic(mission);
            if (navalShipsLogic == null)
                return null;
            return GetShip(navalShipsLogic, TeamSideEnum.PlayerTeam, Agent.Main.Formation.FormationIndex);
        }

        public static void CancelAIPilotPlayerShip(Mission mission)
        {
            var playerShip = GetPlayerShip(mission);

            _shipControllerMachine ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:ShipControllerMachine");
            var captain = GetShipFormation(playerShip).Captain;
            var shipControllerMachine = (UsableMachine)_shipControllerMachine.GetValue(playerShip);
            if (shipControllerMachine.PilotAgent != null && shipControllerMachine.PilotAgent.IsAIControlled && shipControllerMachine.PilotAgent != captain)
            {
                shipControllerMachine.PilotAgent.StopUsingGameObject(flags: Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
            }
            if (shipControllerMachine.PilotStandingPoint.MovingAgent != null && shipControllerMachine.PilotStandingPoint.MovingAgent.IsAIControlled && shipControllerMachine.PilotStandingPoint.MovingAgent != captain)
            {
                shipControllerMachine.PilotStandingPoint.MovingAgent.StopUsingGameObject(flags: Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
            }
        }
    }
}
