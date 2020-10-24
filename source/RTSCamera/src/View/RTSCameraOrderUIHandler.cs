using RTSCamera.Event;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.LegacyGUI.Missions.Order;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.View
{
    [OverrideView(typeof(MissionOrderUIHandler))]
    public class RTSCameraOrderUIHandler : MissionView, ISiegeDeploymentView
    {
        private void RegisterReload()
        {
            MissionEvent.PreSwitchTeam += OnPreSwitchTeam;
            MissionEvent.PostSwitchTeam += OnPostSwitchTeam;
        }

        private void UnregisterReload()
        {
            MissionEvent.PreSwitchTeam -= OnPreSwitchTeam;
            MissionEvent.PostSwitchTeam -= OnPostSwitchTeam;
        }
        private void OnPreSwitchTeam()
        {
            dataSource.CloseToggleOrder();
            FinalizeViewAndVM();
        }

        private void OnPostSwitchTeam()
        {
            InitailizeViewAndVM();
            OnMissionScreenActivate();
        }

        public bool exitWithRightClick = true;

        private SiegeMissionView _siegeMissionView;
        private const float DEPLOYMENT_ICON_SIZE = 75f;
        private List<DeploymentSiegeMachineVM> _deploymentPointDataSources;
        private Vec2 _deploymentPointWidgetSize;
        private RTSCameraOrderTroopPlacer _orderTroopPlacer;
        public GauntletLayer gauntletLayer;
        public MissionOrderVM dataSource;
        private GauntletMovie _viewMovie;
        private SiegeDeploymentHandler _siegeDeploymentHandler;
        public bool IsDeployment;
        private bool isInitialized;
        private bool _isTransferEnabled;

        public RTSCameraOrderUIHandler()
        {
            ViewOrderPriorty = 19;
        }
        public void OnActivateToggleOrder()
        {
            exitWithRightClick = true;
            if (dataSource == null || dataSource.ActiveTargetState == 0)
                _orderTroopPlacer.SuspendTroopPlacer = false;
            MissionScreen.SetOrderFlagVisibility(true);
            if (gauntletLayer != null)
                ScreenManager.SetSuspendLayer(gauntletLayer, false);
            Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(true));
        }

        public void OnDeactivateToggleOrder()
        {
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (gauntletLayer != null)
                ScreenManager.SetSuspendLayer(gauntletLayer, true);
            Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(false));
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            RegisterReload();
            InitailizeViewAndVM();
        }

        private void InitailizeViewAndVM()
        {
            MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
            MissionScreen.OrderFlag = new OrderFlag(Mission, MissionScreen);
            _orderTroopPlacer = Mission.GetMissionBehaviour<RTSCameraOrderTroopPlacer>();
            MissionScreen.SetOrderFlagVisibility(false);
            _siegeDeploymentHandler = Mission.GetMissionBehaviour<SiegeDeploymentHandler>();
            IsDeployment = _siegeDeploymentHandler != null;
            if (IsDeployment)
            {
                _siegeMissionView = Mission.GetMissionBehaviour<SiegeMissionView>();
                if (_siegeMissionView != null)
                    _siegeMissionView.OnDeploymentFinish += OnDeploymentFinish;
                _deploymentPointDataSources = new List<DeploymentSiegeMachineVM>();
            }
            dataSource = new MissionOrderVM(Mission, MissionScreen.CombatCamera, IsDeployment ? _siegeDeploymentHandler.DeploymentPoints.ToList() : new List<DeploymentPoint>(), ToggleScreenRotation, IsDeployment, MissionScreen.GetOrderFlagPosition, RefreshVisuals, SetSuspendTroopPlacer, OnActivateToggleOrder, OnDeactivateToggleOrder);
            if (IsDeployment)
            {
                foreach (DeploymentPoint deploymentPoint in _siegeDeploymentHandler.DeploymentPoints)
                {
                    DeploymentSiegeMachineVM deploymentSiegeMachineVm = new DeploymentSiegeMachineVM(deploymentPoint, null, MissionScreen.CombatCamera, dataSource.OnRefreshSelectedDeploymentPoint, dataSource.OnEntityHover, false);
                    Vec3 origin = deploymentPoint.GameEntity.GetFrame().origin;
                    for (int index = 0; index < deploymentPoint.GameEntity.ChildCount; ++index)
                    {
                        if (deploymentPoint.GameEntity.GetChild(index).Tags.Contains("deployment_point_icon_target"))
                        {
                            Vec3 vec3 = origin + deploymentPoint.GameEntity.GetChild(index).GetFrame().origin;
                            break;
                        }
                    }
                    _deploymentPointDataSources.Add(deploymentSiegeMachineVm);
                    deploymentSiegeMachineVm.RemainingCount = 0;
                    _deploymentPointWidgetSize = new Vec2(75f / Screen.RealScreenResolutionWidth, 75f / Screen.RealScreenResolutionHeight);
                }
            }
            gauntletLayer = new GauntletLayer(ViewOrderPriorty);
            gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _viewMovie = gauntletLayer.LoadMovie("Order", dataSource);
            MissionScreen.AddLayer(gauntletLayer);
            if (IsDeployment)
                gauntletLayer.InputRestrictions.SetInputRestrictions();
            else if (!dataSource.IsToggleOrderShown)
                ScreenManager.SetSuspendLayer(gauntletLayer, true);
            dataSource.InputRestrictions = gauntletLayer.InputRestrictions;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            FinalizeViewAndVM();
            UnregisterReload();
        }

        private void FinalizeViewAndVM()
        {
            _deploymentPointDataSources = null;
            _orderTroopPlacer = null;
            gauntletLayer = null;
            dataSource.OnFinalize();
            dataSource = null;
            _viewMovie = null;
            _siegeDeploymentHandler = null;
        }

        private void OnDeploymentFinish()
        {
            IsDeployment = false;
            dataSource.FinalizeDeployment();
            _deploymentPointDataSources.Clear();
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (_siegeMissionView == null)
                return;
            _siegeMissionView.OnDeploymentFinish -= OnDeploymentFinish;
        }

        public override bool OnEscape()
        {
            return dataSource.CloseToggleOrder();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            TickInput(dt);
            dataSource.Tick(dt);
            if (dataSource.IsToggleOrderShown)
            {
                if (_orderTroopPlacer.SuspendTroopPlacer && dataSource.ActiveTargetState == 0)
                    _orderTroopPlacer.SuspendTroopPlacer = false;
                _orderTroopPlacer.IsDrawingForced = dataSource.IsMovementSubOrdersShown;
                _orderTroopPlacer.IsDrawingFacing = dataSource.IsFacingSubOrdersShown;
                _orderTroopPlacer.IsDrawingForming = false;
                _orderTroopPlacer.IsDrawingAttaching = cursorState == MissionOrderVM.CursorState.Attach;
                _orderTroopPlacer.UpdateAttachVisuals(cursorState == MissionOrderVM.CursorState.Attach);
                if (cursorState == MissionOrderVM.CursorState.Face)
                    MissionScreen.OrderFlag.SetArrowVisibility(true, OrderController.GetOrderLookAtDirection(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position.AsVec2));
                else
                    MissionScreen.OrderFlag.SetArrowVisibility(false, Vec2.Invalid);
                if (cursorState == MissionOrderVM.CursorState.Form)
                    MissionScreen.OrderFlag.SetWidthVisibility(true, OrderController.GetOrderFormCustomWidth(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position));
                else
                    MissionScreen.OrderFlag.SetWidthVisibility(false, -1f);
            }
            else
            {
                if (!_orderTroopPlacer.SuspendTroopPlacer)
                    _orderTroopPlacer.SuspendTroopPlacer = true;
            }
            if (IsDeployment)
            {
                gauntletLayer.InputRestrictions.SetInputRestrictions(gauntletLayer.InputRestrictions.MouseVisibility);
            }
            MissionScreen.OrderFlag.IsTroop = dataSource.ActiveTargetState == 0;
            MissionScreen.OrderFlag.Tick(dt);
        }

        private void RefreshVisuals()
        {
            if (!IsDeployment)
                return;
            foreach (DeploymentSiegeMachineVM deploymentPointDataSource in _deploymentPointDataSources)
                deploymentPointDataSource.RefreshWithDeployedWeapon();
        }

        public override void OnMissionScreenActivate()
        {
            base.OnMissionScreenActivate();
            dataSource.AfterInitialize();
            isInitialized = true;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!isInitialized || !agent.IsHuman)
                return;
            dataSource.AddTroops(agent);
        }

        public override void OnAgentRemoved(
          Agent affectedAgent,
          Agent affectorAgent,
          AgentState agentState,
          KillingBlow killingBlow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
            if (!affectedAgent.IsHuman)
                return;
            dataSource.RemoveTroops(affectedAgent);
        }

        private IOrderable GetFocusedOrderableObject()
        {
            return MissionScreen.OrderFlag.FocusedOrderableObject;
        }

        private void SetSuspendTroopPlacer(bool value)
        {
            _orderTroopPlacer.SuspendTroopPlacer = value;
            MissionScreen.SetOrderFlagVisibility(!value);
        }

        void ISiegeDeploymentView.OnEntityHover(GameEntity hoveredEntity)
        {
            if (gauntletLayer.HitTest())
                return;
            dataSource.OnEntityHover(hoveredEntity);
        }

        void ISiegeDeploymentView.OnEntitySelection(GameEntity selectedEntity)
        {
            dataSource.OnEntitySelect(selectedEntity);
        }

        private void ToggleScreenRotation(bool isLocked)
        {
            MissionScreen.SetFixedMissionCameraActive(isLocked);
        }

        [Conditional("DEBUG")]
        private void TickInputDebug()
        {
        }

        public MissionOrderVM.CursorState cursorState
        {
            get
            {
                return dataSource.IsFacingSubOrdersShown ? MissionOrderVM.CursorState.Face : MissionOrderVM.CursorState.Move;
            }
        }

        private void TickInput(float dt)
        {
            if (dataSource.IsToggleOrderShown)
            {
                if (dataSource.IsTransferActive && gauntletLayer.Input.IsHotKeyReleased("Exit"))
                    dataSource.IsTransferActive = false;
                if (dataSource.IsTransferActive != _isTransferEnabled)
                {
                    _isTransferEnabled = dataSource.IsTransferActive;
                    if (!_isTransferEnabled)
                    {
                        gauntletLayer.IsFocusLayer = false;
                        ScreenManager.TryLoseFocus(gauntletLayer);
                    }
                    else
                    {
                        gauntletLayer.IsFocusLayer = true;
                        ScreenManager.TrySetFocus(gauntletLayer);
                    }
                }
                if (dataSource.ActiveTargetState == 0 && Input.IsKeyReleased(InputKey.LeftMouseButton))
                {
                    switch (cursorState)
                    {
                        case MissionOrderVM.CursorState.Move:
                            IOrderable focusedOrderableObject = GetFocusedOrderableObject();
                            if (focusedOrderableObject != null)
                            {
                                dataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
                            }
                            break;
                        case MissionOrderVM.CursorState.Face:
                            dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                            break;
                        case MissionOrderVM.CursorState.Form:
                            dataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(Mission.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                            break;
                    }
                }
                //if (this.Input.IsAltDown())
                //{
                //    bool isMouseVisible = this.dataSource.IsTransferActive || !this.gauntletLayer.InputRestrictions.MouseVisibility;
                //    this.gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible, isMouseVisible ? InputUsageMask.Mouse : InputUsageMask.Invalid);
                //}
                if (exitWithRightClick && Input.IsKeyReleased(InputKey.RightMouseButton))
                    dataSource.OnEscape();
            }
            int pressedIndex = -1;
            if (!Input.IsControlDown())
            {
                if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder1))
                    pressedIndex = 0;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder2))
                    pressedIndex = 1;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder3))
                    pressedIndex = 2;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder4))
                    pressedIndex = 3;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder5))
                    pressedIndex = 4;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder6))
                    pressedIndex = 5;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder7))
                    pressedIndex = 6;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrder8))
                    pressedIndex = 7;
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectOrderReturn))
                    pressedIndex = 8;
            }
            if (pressedIndex > -1)
                dataSource.OnGiveOrder(pressedIndex);
            int formationTroopIndex = -1;
            if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.EveryoneHear))
                formationTroopIndex = 100;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group0Hear))
                formationTroopIndex = 0;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group1Hear))
                formationTroopIndex = 1;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group2Hear))
                formationTroopIndex = 2;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group3Hear))
                formationTroopIndex = 3;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group4Hear))
                formationTroopIndex = 4;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group5Hear))
                formationTroopIndex = 5;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group6Hear))
                formationTroopIndex = 6;
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.Group7Hear))
                formationTroopIndex = 7;
            if (formationTroopIndex != -1)
                dataSource.OnSelect(formationTroopIndex);
            if (!Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ViewOrders))
                return;
            dataSource.ViewOrders();
        }
    }
}
