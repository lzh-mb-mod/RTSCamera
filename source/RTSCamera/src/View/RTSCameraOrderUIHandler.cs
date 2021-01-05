using System;
using System.Collections.Generic;
using System.Linq;
using MissionLibrary.Event;
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
            DataSource.TryCloseToggleOrder();
            FinalizeViewAndVm();
        }

        private void OnPostSwitchTeam()
        {
            InitializeViewAndVm();
            OnMissionScreenActivate();
        }

        public bool ExitWithRightClick = true;


        private const string _radialOrderMovieName = "OrderRadial";
        private const string _barOrderMovieName = "OrderBar";
        private float _holdTime;
        private bool _holdExecuted;
        private SiegeMissionView _siegeMissionView;
        private List<DeploymentSiegeMachineVM> _deploymentPointDataSources;
        private RTSCameraOrderTroopPlacer _orderTroopPlacer;
        public GauntletLayer GauntletLayer;
        private GauntletMovie _movie;
        public  MissionOrderVM DataSource;
        private SiegeDeploymentHandler _siegeDeploymentHandler;
        public bool IsDeployment;
        private bool isInitialized;
        private bool _isTransferEnabled;

        private float _minHoldTimeForActivation => 0.0f;

        private bool _isGamepadActive => Input.GetIsControllerConnected() && !Input.GetIsMouseActive();

        public RTSCameraOrderUIHandler() => ViewOrderPriorty = 19;

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            TickInput(dt);
            DataSource.Update();
            if (DataSource.IsToggleOrderShown)
            {
                _orderTroopPlacer.IsDrawingForced = DataSource.IsMovementSubOrdersShown;
                _orderTroopPlacer.IsDrawingFacing = DataSource.IsFacingSubOrdersShown;
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
                if (_isGamepadActive)
                {
                    OrderItemVM selectedOrderItem = DataSource.LastSelectedOrderItem;
                    if ((selectedOrderItem != null ? (selectedOrderItem.IsTitle ? 1 : 0) : 1) != 0)
                    {
                        MissionScreen.SetRadialMenuActiveState(false);
                        if (_orderTroopPlacer.SuspendTroopPlacer && DataSource.ActiveTargetState == 0)
                            _orderTroopPlacer.SuspendTroopPlacer = false;
                    }
                    else
                    {
                        MissionScreen.SetRadialMenuActiveState(true);
                        if (!_orderTroopPlacer.SuspendTroopPlacer)
                            _orderTroopPlacer.SuspendTroopPlacer = true;
                    }
                }
            }
            else if (DataSource.TroopController.IsTransferActive)
            {
                GauntletLayer.InputRestrictions.SetInputRestrictions();
            }
            else
            {
                if (!_orderTroopPlacer.SuspendTroopPlacer)
                    _orderTroopPlacer.SuspendTroopPlacer = true;
                GauntletLayer.InputRestrictions.ResetInputRestrictions();
            }
            if (IsDeployment)
            {
                if (MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
                    GauntletLayer.InputRestrictions.SetMouseVisibility(false);
                else
                    GauntletLayer.InputRestrictions.SetInputRestrictions();
            }
            MissionScreen.OrderFlag.IsTroop = DataSource.ActiveTargetState == 0;
            MissionScreen.OrderFlag.Tick(dt);
        }

        public override bool OnEscape()
        {
            int num = DataSource.IsToggleOrderShown ? 1 : 0;
            DataSource.OnEscape();
            return num != 0;
        }

        public override void OnMissionScreenActivate()
        {
            base.OnMissionScreenActivate();
            DataSource.AfterInitialize();
            isInitialized = true;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!isInitialized || !agent.IsHuman)
                return;
            DataSource.TroopController.AddTroops(agent);
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
            DataSource.TroopController.RemoveTroops(affectedAgent);
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            RegisterReload();
            InitializeViewAndVm();
        }

        private void InitializeViewAndVm()
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
            DataSource = new MissionOrderVM(MissionScreen.CombatCamera, IsDeployment ? _siegeDeploymentHandler.DeploymentPoints.ToList() : new List<DeploymentPoint>(), ToggleScreenRotation, IsDeployment, MissionScreen.GetOrderFlagPosition, RefreshVisuals, SetSuspendTroopPlacer, OnActivateToggleOrder, OnDeactivateToggleOrder, OnTransferFinished, false);
            if (IsDeployment)
            {
                foreach (DeploymentPoint deploymentPoint in _siegeDeploymentHandler.DeploymentPoints)
                {
                    DeploymentSiegeMachineVM deploymentSiegeMachineVm = new DeploymentSiegeMachineVM(deploymentPoint, null, MissionScreen.CombatCamera, DataSource.DeploymentController.OnRefreshSelectedDeploymentPoint, DataSource.DeploymentController.OnEntityHover, false);
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
                }
            }
            GauntletLayer = new GauntletLayer(ViewOrderPriorty);
            GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _movie = GauntletLayer.LoadMovie(BannerlordConfig.OrderType == 0 ? _barOrderMovieName : _radialOrderMovieName, DataSource);
            MissionScreen.AddLayer(GauntletLayer);
            if (IsDeployment)
                GauntletLayer.InputRestrictions.SetInputRestrictions();
            else if (!DataSource.IsToggleOrderShown)
                ScreenManager.SetSuspendLayer(GauntletLayer, true);
            DataSource.InputRestrictions = GauntletLayer.InputRestrictions;
            ManagedOptions.OnManagedOptionChanged += OnManagedOptionChanged;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            FinalizeViewAndVm();
            UnregisterReload();
        }

        private void FinalizeViewAndVm()
        {
            ManagedOptions.OnManagedOptionChanged -= OnManagedOptionChanged;
            _deploymentPointDataSources = null;
            _orderTroopPlacer = null;
            _movie = null;
            GauntletLayer = null;
            DataSource.OnFinalize();
            DataSource = null;
            _siegeDeploymentHandler = null;
        }

        private void OnManagedOptionChanged(
          ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            if (changedManagedOptionsType != ManagedOptions.ManagedOptionsType.OrderType)
                return;
            GauntletLayer.ReleaseMovie(_movie);
            _movie = GauntletLayer.LoadMovie(BannerlordConfig.OrderType == 0 ? "OrderBar" : "OrderRadial", DataSource);
        }

        private void TickInput(float dt)
        {
            if (Input.IsGameKeyDown(MissionOrderHotkeyCategory.HoldOrder) && !DataSource.IsToggleOrderShown)
            {
                _holdTime += dt;
                if (_holdTime >= (double)_minHoldTimeForActivation)
                {
                    DataSource.OpenToggleOrder(true);
                    _holdExecuted = true;
                }
            }
            else if (!Input.IsGameKeyDown(MissionOrderHotkeyCategory.HoldOrder))
            {
                if (_holdExecuted && DataSource.IsToggleOrderShown)
                {
                    DataSource.TryCloseToggleOrder();
                    _holdExecuted = false;
                }
                _holdTime = 0.0f;
            }
            if (DataSource.IsToggleOrderShown)
            {
                if (DataSource.TroopController.IsTransferActive && GauntletLayer.Input.IsHotKeyPressed("Exit"))
                    DataSource.TroopController.IsTransferActive = false;
                if (DataSource.TroopController.IsTransferActive != _isTransferEnabled)
                {
                    _isTransferEnabled = DataSource.TroopController.IsTransferActive;
                    if (!_isTransferEnabled)
                    {
                        GauntletLayer.IsFocusLayer = false;
                        ScreenManager.TryLoseFocus(GauntletLayer);
                    }
                    else
                    {
                        GauntletLayer.IsFocusLayer = true;
                        ScreenManager.TrySetFocus(GauntletLayer);
                    }
                }
                if (DataSource.ActiveTargetState == 0 && (Input.IsKeyReleased(InputKey.LeftMouseButton) || Input.IsKeyReleased(InputKey.ControllerRTrigger)))
                {
                    OrderItemVM selectedOrderItem = DataSource.LastSelectedOrderItem;
                    if ((selectedOrderItem != null ? (!selectedOrderItem.IsTitle ? 1 : 0) : 0) != 0 && _isGamepadActive)
                    {
                        DataSource.ApplySelectedOrder();
                    }
                    else
                    {
                        switch (cursorState)
                        {
                            case MissionOrderVM.CursorState.Move:
                                IOrderable focusedOrderableObject = GetFocusedOrderableObject();
                                if (focusedOrderableObject != null)
                                {
                                    DataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
                                }
                                break;
                            case MissionOrderVM.CursorState.Face:
                                DataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                            case MissionOrderVM.CursorState.Form:
                                DataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                        }
                    }
                }
                if (ExitWithRightClick && Input.IsKeyReleased(InputKey.RightMouseButton))
                    DataSource.OnEscape();
            }
            int pressedIndex = -1;
            if ((!_isGamepadActive || DataSource.IsToggleOrderShown) && !Input.IsControlDown())
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
                DataSource.OnGiveOrder(pressedIndex);
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
            if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectNextGroup))
                DataSource.SelectNextTroop(1);
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectPreviousGroup))
                DataSource.SelectNextTroop(-1);
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ToggleGroupSelection))
                DataSource.ToggleSelectionForCurrentTroop();
            if (formationTroopIndex != -1)
                DataSource.OnSelect(formationTroopIndex);
            if (!Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ViewOrders))
                return;
            DataSource.ViewOrders();
        }

        public void OnActivateToggleOrder() => SetLayerEnabled(true);

        public void OnDeactivateToggleOrder()
        {
            if (DataSource.TroopController.IsTransferActive)
                return;
            SetLayerEnabled(false);
        }

        private void OnTransferStarted()
        {
        }

        private void OnTransferFinished() => SetLayerEnabled(false);

        private void SetLayerEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                ExitWithRightClick = true;
                if (DataSource == null || DataSource.ActiveTargetState == 0)
                    _orderTroopPlacer.SuspendTroopPlacer = false;
                MissionScreen.SetOrderFlagVisibility(true);
                if (GauntletLayer != null)
                    ScreenManager.SetSuspendLayer(GauntletLayer, false);
                Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(true));
            }
            else
            {
                _orderTroopPlacer.SuspendTroopPlacer = true;
                MissionScreen.SetOrderFlagVisibility(false);
                if (GauntletLayer != null)
                    ScreenManager.SetSuspendLayer(GauntletLayer, true);
                MissionScreen.SetRadialMenuActiveState(false);
                Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(false));
            }
        }

        private void OnDeploymentFinish()
        {
            IsDeployment = false;
            DataSource.DeploymentController.FinalizeDeployment();
            _deploymentPointDataSources.Clear();
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (_siegeMissionView == null)
                return;
            _siegeMissionView.OnDeploymentFinish -= OnDeploymentFinish;
        }

        private void RefreshVisuals()
        {
            if (!IsDeployment)
                return;
            foreach (DeploymentSiegeMachineVM deploymentPointDataSource in _deploymentPointDataSources)
                deploymentPointDataSource.RefreshWithDeployedWeapon();
        }

        private IOrderable GetFocusedOrderableObject() => MissionScreen.OrderFlag.FocusedOrderableObject;

        private void SetSuspendTroopPlacer(bool value)
        {
            _orderTroopPlacer.SuspendTroopPlacer = value;
            MissionScreen.SetOrderFlagVisibility(!value);
        }

        void ISiegeDeploymentView.OnEntityHover(GameEntity hoveredEntity)
        {
            if (GauntletLayer.HitTest())
                return;
            DataSource.DeploymentController.OnEntityHover(hoveredEntity);
        }

        void ISiegeDeploymentView.OnEntitySelection(GameEntity selectedEntity) => DataSource.DeploymentController.OnEntitySelect(selectedEntity);

        private void ToggleScreenRotation(bool isLocked) => MissionScreen.SetFixedMissionCameraActive(isLocked);

        public MissionOrderVM.CursorState cursorState => DataSource.IsFacingSubOrdersShown ? MissionOrderVM.CursorState.Face : MissionOrderVM.CursorState.Move;
    }
}
