using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Logic;
using RTSCamera.Patch.Naval;
using RTSCamera.View;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static TaleWorlds.MountAndBlade.Mission;

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

        public static void PrintHelmsmanWarning()
        {
            MissionSharedLibrary.Utilities.Utility.DisplayMessage("RTS Camera: Helmsman detected. Will disable soldier control command in RTS.", new Color(1, 0, 0));
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
                    if (agent.IsCameraAttachable())
                    {
                        controller.IsDisabled = false;
                    }
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

        public static MissionObject GetShip(MissionBehavior navalShipLogic, TeamSideEnum teamSide, FormationClass formationClass)
        {
            var getShipAssignmentMethod = AccessTools.Method(navalShipLogic.GetType(), "GetShipAssignment");
            var shipAssignment = getShipAssignmentMethod.Invoke(navalShipLogic, new object[] { teamSide, formationClass });

            var missionShipProperty = AccessTools.Property(shipAssignment.GetType(), "MissionShip");
            var missionShip = (MissionObject)missionShipProperty.GetValue(shipAssignment);
            return missionShip;
        }

        private static PropertyInfo _shipControllerMachine;

        public static UsableMachine GetShipControllerMachine(MissionObject ship)
        {
            _shipControllerMachine ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:ShipControllerMachine");
            return (UsableMachine)_shipControllerMachine.GetValue(ship);
        }
        public static bool IsShipPilotByPlayer(MissionObject missionShip)
        {
            var shipControllerMachine = GetShipControllerMachine(missionShip);

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
                if (formation == Agent.Main.Formation && !CommandBattleBehavior.CommandMode && !(RTSCameraSubModule.IsHelmsmanInstalled && formation.FormationIndex == FormationClass.Infantry))
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

        public enum ShipControllerType
        {
            None,
            AI,
            Player,
        }

        public static bool IsShipAIControlled(MissionObject ship)
        {
            return (bool)AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:IsAIControlled").GetValue(ship);
        }

        public static bool IsShipPlayerControlled(MissionObject ship)
        {
            return (bool)AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:IsPlayerControlled").GetValue(ship);
        }

        public static MissionObject GetAgentSteppedShip(Agent agent)
        {
            var component = GetAgentComponent(agent, AccessTools.TypeByName("NavalDLC.Missions.AgentNavalComponent")) as AgentComponent;
            if (component == null)
                return null;
            return AccessTools.Property(component.GetType(), "SteppedShip").GetValue(component) as MissionObject;
            
        }

        public static void CancelAIPilotPlayerShip(Mission mission)
        {
            var playerShip = GetPlayerShip(mission);

            var captain = GetShipFormation(playerShip).Captain;
            var shipControllerMachine = GetShipControllerMachine(playerShip);
            if (shipControllerMachine.PilotAgent != null && shipControllerMachine.PilotAgent.IsAIControlled && shipControllerMachine.PilotAgent != captain)
            {
                shipControllerMachine.PilotAgent.StopUsingGameObject(flags: Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
            }
            if (shipControllerMachine.PilotStandingPoint.MovingAgent != null && shipControllerMachine.PilotStandingPoint.MovingAgent.IsAIControlled && shipControllerMachine.PilotStandingPoint.MovingAgent != captain)
            {
                shipControllerMachine.PilotStandingPoint.MovingAgent.StopUsingGameObject(flags: Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject | Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
            }
        }

        private static FieldInfo _components = AccessTools.Field("TaleWorlds.MountAndBlade.Agent:_components");

        public static AgentComponent GetAgentComponent(Agent agent, Type componentType)
        {
            var components = (MBList<AgentComponent>)_components.GetValue(agent);
            for (int index = 0; index < components.Count; ++index)
            {
                if (componentType.IsAssignableFrom(components[index].GetType()))
                {
                    return components[index]; 
                }
            }
            return null;
        }

        public static void TryToSetPlayerFormationClass(FormationClass formationClass)
        {
            if (Mission.Current.IsNavalBattle)
            {
                var navalShipLogic = GetNavalShipsLogic(Mission.Current);
                if (navalShipLogic == null)
                    return;
                var ship = GetShip(navalShipLogic, TeamSideEnum.PlayerTeam, formationClass);
                if (ship == null)
                    return;
            }

#if DEBUG
            MissionSharedLibrary.Utilities.Utility.DisplayMessage($"Setting player formation to {formationClass}");
#endif
            MissionSharedLibrary.Utilities.Utility.SetPlayerFormationClass(formationClass);
        }

        public static PlayerShipController GetPlayerShipControllerInFreeCamera()
        {
            if (CommandBattleBehavior.CommandMode)
                return PlayerShipController.AI;
            return RTSCameraConfig.Get().PlayerShipControllerInFreeCamera;
        }

        private static MethodInfo _setIsFormationTargetingDisabled;

        public static void RefreshOrderTargetDisabled()
        {
            if (!Mission.Current.IsNavalBattle)
                return;
            bool isDisabled = !Patch_NavalDLCHelpers.IsShipOrderAvailable();
            var orderUIHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
            if (orderUIHandler != null)
            {
                MissionFormationTargetSelectionHandler formationTargetSelectionHandler = (MissionFormationTargetSelectionHandler)AccessTools.Field("TaleWorlds.MountAndBlade.GauntletUI.GauntletOrderUIHandler:_formationTargetHandler").GetValue(orderUIHandler);
                formationTargetSelectionHandler?.SetIsFormationTargetingDisabled(isDisabled);
                var navalOrderUIHandlerType = AccessTools.TypeByName("MissionGauntletNavalOrderUIHandler");
                if (navalOrderUIHandlerType != null && navalOrderUIHandlerType.IsAssignableFrom(orderUIHandler.GetType()))
                {
                    MissionView shipTargetHandler = (MissionView)AccessTools.Field("NavalDLC.GauntletUI.MissionViews.MissionGauntletNavalOrderUIHandler:_shipTargetHandler").GetValue(orderUIHandler);
                    if (shipTargetHandler == null)
                        return;
                    _setIsFormationTargetingDisabled ??= AccessTools.Method("NavalDLC.View.MissionViews.NavalShipTargetSelectionHandler:SetIsFormationTargetingDisabled");
                    _setIsFormationTargetingDisabled.Invoke(shipTargetHandler, new object[] { isDisabled });
                }
            }
        }

        private static PropertyInfo _shipOrder;

        public static Object GetShipOrder(MissionObject ship)
        {
            _shipOrder ??= AccessTools.Property("NavalDLC.Missions.Objects.MissionShip:ShipOrder");
            return _shipOrder.GetValue(ship);
        }

        public enum ShipMovementOrderEnum
        {
            Stop = 0,
            Move = 1,
            Retreat = 2,
            Follow = 3,
            StaticOrderCount = 3,
            Engage = 4,
            Skirmish = 5,
        }

        private static PropertyInfo _movementOrderEnum;
        public static ShipMovementOrderEnum GetShipMovementOrderEnum(Object shipOrder)
        {
            _movementOrderEnum ??= AccessTools.Property("NavalDLC.Missions.ShipOrder:MovementOrderEnum");
            return (ShipMovementOrderEnum)_movementOrderEnum.GetValue(shipOrder);
        }
    }
}
