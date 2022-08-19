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
using TaleWorlds.TwoDimension;

namespace RTSCamera.CommandSystem.View
{
    [OverrideView(typeof(MissionOrderUIHandler))]
    public class CommandSystemOrderUIHandler : MissionView, ISiegeDeploymentView
    {
        public static bool _isInSwitchTeamEvent;
        public bool ExitWithRightClick = true;
        private const string _radialOrderMovieName = "OrderRadial";
        private const string _barOrderMovieName = "OrderBar";
        private const float _slowDownAmountWhileOrderIsOpen = 0.25f;
        private const int _missionTimeSpeedRequestID = 864;
        private float _holdTime;
        private bool _holdExecuted;
        private DeploymentMissionView _deploymentMissionView;
        private List<DeploymentSiegeMachineVM> _deploymentPointDataSources;
        private CommandSystemOrderTroopPlacer _orderTroopPlacer;
        public GauntletLayer _gauntletLayer;
        private IGauntletMovie _movie;
        private SpriteCategory _spriteCategory;
        public MissionOrderVM _dataSource;
        private SiegeDeploymentHandler _siegeDeploymentHandler;
        private BattleDeploymentHandler _battleDeploymentHandler;
        private bool isInitialized;
        private bool _isTransferEnabled;
        private float _minHoldTimeForActivation => 0.0f;
        private bool _isGamepadActive => Input.GetIsControllerConnected() && !Input.GetIsMouseActive();
        public event Action<bool> OnCameraControlsToggled;
        private bool _slowedDownMission;
        private float _latestDt;

        public bool IsSiegeDeployment { get; private set; }
        public bool IsBattleDeployment { get; private set; }

        public bool _isAnyDeployment
        {
            get
            {
                return IsSiegeDeployment || IsBattleDeployment;
            }
        }

        public CommandSystemOrderUIHandler() => ViewOrderPriority = 12;

        private void RegisterReload()
        {
            if (_isInSwitchTeamEvent)
                return;

            MissionEvent.PreSwitchTeam += OnPreSwitchTeam;
            MissionEvent.PostSwitchTeam += OnPostSwitchTeam;
        }

        private void UnregisterReload()
        {
            if (_isInSwitchTeamEvent)
                return;

            MissionEvent.PreSwitchTeam -= OnPreSwitchTeam;
            MissionEvent.PostSwitchTeam -= OnPostSwitchTeam;
        }
        private void OnPreSwitchTeam()
        {
            _dataSource.TryCloseToggleOrder();
            _isInSwitchTeamEvent = true;
            OnMissionScreenFinalize();
            _isInSwitchTeamEvent = false;
        }

        private void OnPostSwitchTeam()
        {
            _isInSwitchTeamEvent = true;
            OnMissionScreenInitialize();
            OnMissionScreenActivate();
            _isInSwitchTeamEvent = false;
        }

        public override void OnMissionScreenTick(float dt)
        {
            _latestDt = dt;
            if (!MissionScreen.IsPhotoModeEnabled && _dataSource != null)
            {
                TickInput(dt);
                _dataSource.Update();
                if (_dataSource.IsToggleOrderShown)
                {
                    _orderTroopPlacer.IsDrawingForced = _dataSource.IsMovementSubOrdersShown;
                    _orderTroopPlacer.IsDrawingFacing = _dataSource.IsFacingSubOrdersShown;
                    _orderTroopPlacer.IsDrawingForming = false;
                    if (cursorState == MissionOrderVM.CursorState.Face)
                    {
                        Vec2 orderLookAtDirection = OrderController.GetOrderLookAtDirection(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position.AsVec2);
                        MissionScreen.OrderFlag.SetArrowVisibility(true, orderLookAtDirection);
                    }
                    else
                    {
                        MissionScreen.OrderFlag.SetArrowVisibility(false, Vec2.Invalid);
                    }
                    if (cursorState == MissionOrderVM.CursorState.Form)
                    {
                        float orderFormCustomWidth = OrderController.GetOrderFormCustomWidth(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position);
                        MissionScreen.OrderFlag.SetWidthVisibility(true, orderFormCustomWidth);
                    }
                    else
                    {
                        MissionScreen.OrderFlag.SetWidthVisibility(false, -1f);
                    }
                    if (TaleWorlds.InputSystem.Input.IsGamepadActive)
                    {
                        OrderItemVM lastSelectedOrderItem = _dataSource.LastSelectedOrderItem;
                        if (lastSelectedOrderItem == null || lastSelectedOrderItem.IsTitle)
                        {
                            MissionScreen.SetRadialMenuActiveState(false);
                            if (_orderTroopPlacer.SuspendTroopPlacer && _dataSource.ActiveTargetState == 0)
                            {
                                _orderTroopPlacer.SuspendTroopPlacer = false;
                            }
                        }
                        else
                        {
                            MissionScreen.SetRadialMenuActiveState(true);
                            if (!_orderTroopPlacer.SuspendTroopPlacer)
                            {
                                _orderTroopPlacer.SuspendTroopPlacer = true;
                            }
                        }
                    }
                }
                else if (_dataSource.TroopController.IsTransferActive)
                {
                    _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                }
                else
                {
                    if (!_orderTroopPlacer.SuspendTroopPlacer)
                    {
                        _orderTroopPlacer.SuspendTroopPlacer = true;
                    }
                    _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                }
                if (_isAnyDeployment)
                {
                    if (MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
                    {
                        _gauntletLayer.InputRestrictions.SetMouseVisibility(false);
                        Action<bool> onCameraControlsToggled = OnCameraControlsToggled;
                        if (onCameraControlsToggled != null)
                        {
                            onCameraControlsToggled(true);
                        }
                    }
                    else
                    {
                        _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                        Action<bool> onCameraControlsToggled2 = OnCameraControlsToggled;
                        if (onCameraControlsToggled2 != null)
                        {
                            onCameraControlsToggled2(false);
                        }
                    }
                }
                MissionScreen.OrderFlag.IsTroop = (_dataSource.ActiveTargetState == 0);
                TickOrderFlag(_latestDt, false);
            }
        }

        public override bool OnEscape()
        {
            bool isToggleOrderShown = _dataSource.IsToggleOrderShown;
            _dataSource.OnEscape();
            return !_isAnyDeployment && isToggleOrderShown;
        }

        public override void OnMissionScreenActivate()
        {
            _dataSource.AfterInitialize();
            isInitialized = true;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!isInitialized || !agent.IsHuman)
                return;
            _dataSource.TroopController.AddTroops(agent);
        }

        public override void OnAgentRemoved(
          Agent affectedAgent,
          Agent affectorAgent,
          AgentState agentState,
          KillingBlow killingBlow)
        {
            if (!affectedAgent.IsHuman)
                return;
            _dataSource.TroopController.RemoveTroops(affectedAgent);
        }

        public override void OnMissionScreenInitialize()
        {
            RegisterReload();

            MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
            MissionScreen.OrderFlag = new OrderFlag(Mission, MissionScreen);
            _orderTroopPlacer = Mission.GetMissionBehavior<CommandSystemOrderTroopPlacer>();
            MissionScreen.SetOrderFlagVisibility(false);
            _siegeDeploymentHandler = Mission.GetMissionBehavior<SiegeDeploymentHandler>();
            _battleDeploymentHandler = Mission.GetMissionBehavior<BattleDeploymentHandler>();
            IsSiegeDeployment = (_siegeDeploymentHandler != null);
            IsBattleDeployment = (_battleDeploymentHandler != null);
            if (_isAnyDeployment)
            {
                _deploymentMissionView = Mission.GetMissionBehavior<DeploymentMissionView>();
                if (_deploymentMissionView != null)
                {
                    DeploymentMissionView deploymentMissionView = _deploymentMissionView;
                    deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Combine(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(OnDeploymentFinish));
                }
                _deploymentPointDataSources = new List<DeploymentSiegeMachineVM>();
            }
            _dataSource = new MissionOrderVM(
                MissionScreen.CombatCamera,
                IsSiegeDeployment ? _siegeDeploymentHandler.DeploymentPoints.ToList<DeploymentPoint>() : new List<DeploymentPoint>(),
                new Action<bool>(ToggleScreenRotation),
                _isAnyDeployment,
                new GetOrderFlagPositionDelegate(MissionScreen.GetOrderFlagPosition),
                new OnRefreshVisualsDelegate(RefreshVisuals),
                new ToggleOrderPositionVisibilityDelegate(SetSuspendTroopPlacer),
                new OnToggleActivateOrderStateDelegate(OnActivateToggleOrder),
                new OnToggleActivateOrderStateDelegate(OnDeactivateToggleOrder),
                new OnToggleActivateOrderStateDelegate(OnTransferFinished),
                false);

            if (IsSiegeDeployment)
            {
                foreach (DeploymentPoint deploymentPoint in _siegeDeploymentHandler.DeploymentPoints)
                {
                    DeploymentSiegeMachineVM deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(deploymentPoint, null, MissionScreen.CombatCamera, new Action<DeploymentSiegeMachineVM>(_dataSource.DeploymentController.OnRefreshSelectedDeploymentPoint), new Action<DeploymentPoint>(_dataSource.DeploymentController.OnEntityHover), false);
                    Vec3 v = deploymentPoint.GameEntity.GetFrame().origin;
                    for (int i = 0; i < deploymentPoint.GameEntity.ChildCount; i++)
                    {
                        if (deploymentPoint.GameEntity.GetChild(i).HasTag("deployment_point_icon_target"))
                        {
                            v += deploymentPoint.GameEntity.GetChild(i).GetFrame().origin;
                            break;
                        }
                    }
                    _deploymentPointDataSources.Add(deploymentSiegeMachineVM);
                    deploymentSiegeMachineVM.RemainingCount = 0;
                }
            }
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            string text = (BannerlordConfig.OrderType == 0) ? "OrderBar" : "OrderRadial";
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            _spriteCategory = spriteData.SpriteCategories["ui_order"];
            _spriteCategory.Load(resourceContext, uiresourceDepot);
            _movie = _gauntletLayer.LoadMovie(text, _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
            if (BannerlordConfig.HideBattleUI)
            {
                _gauntletLayer._gauntletUIContext.ContextAlpha = 0f;
            }
            if (_isAnyDeployment)
            {
                _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            }
            else if (!_dataSource.IsToggleOrderShown)
            {
                ScreenManager.SetSuspendLayer(_gauntletLayer, true);
            }
            _dataSource.InputRestrictions = _gauntletLayer.InputRestrictions;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
        }
        public override bool IsReady()
        {
            return _spriteCategory.IsCategoryFullyLoaded();
        }
        private void OnManagedOptionChanged(
            ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.OrderType)
            {
                _gauntletLayer.ReleaseMovie(_movie);
                string text = (BannerlordConfig.OrderType == 0) ? "OrderBar" : "OrderRadial";
                _movie = _gauntletLayer.LoadMovie(text, _dataSource);
                return;
            }
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.OrderLayoutType)
            {
                MissionOrderVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.OnOrderLayoutTypeChanged();
                return;
            }
            else
            {
                if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.HideBattleUI)
                {
                    _gauntletLayer._gauntletUIContext.ContextAlpha = (BannerlordConfig.HideBattleUI ? 0f : 1f);
                    return;
                }
                if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.SlowDownOnOrder && !BannerlordConfig.SlowDownOnOrder && _slowedDownMission)
                {
                    Mission.RemoveTimeSpeedRequest(864);
                }
                return;
            }
        }

        public override void OnMissionScreenFinalize()
        {
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
            _deploymentPointDataSources = null;
            _orderTroopPlacer = null;
            _movie = null;
            _gauntletLayer = null;
            _dataSource.OnFinalize();
            _dataSource = null;
            _siegeDeploymentHandler = null;
            _spriteCategory.Unload();
            _battleDeploymentHandler = null;

            UnregisterReload();
        }

        private void FinalizeViewAndVm()
        {
            ManagedOptions.OnManagedOptionChanged -= OnManagedOptionChanged;
            _deploymentPointDataSources = null;
            _orderTroopPlacer = null;
            _movie = null;
            _gauntletLayer = null;
            _dataSource.OnFinalize();
            _dataSource = null;
            _siegeDeploymentHandler = null;
            _spriteCategory.Unload();
        }

        public override void OnConversationBegin()
        {
            MissionOrderVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.TryCloseToggleOrder(true);
        }

        public void OnActivateToggleOrder()
        {
            SetLayerEnabled(true);
        }

        public void OnDeactivateToggleOrder()
        {
            if (!_dataSource.TroopController.IsTransferActive)
            {
                SetLayerEnabled(false);
            }
        }

        private void OnTransferFinished()
        {
            if (!_isAnyDeployment)
            {
                SetLayerEnabled(false);
            }
        }
        public void OnAutoDeploy()
        {
            _dataSource.DeploymentController.ExecuteAutoDeploy();
        }
        public void OnBeginMission()
        {
            _dataSource.DeploymentController.ExecuteBeginSiege();
        }

        private void SetLayerEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                if (_dataSource == null || _dataSource.ActiveTargetState == 0)
                {
                    _orderTroopPlacer.SuspendTroopPlacer = false;
                }
                if (!_slowedDownMission && BannerlordConfig.SlowDownOnOrder)
                {
                    Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(0.25f, 864));
                    _slowedDownMission = true;
                }
                MissionScreen.SetOrderFlagVisibility(true);
                if (_gauntletLayer != null)
                {
                    ScreenManager.SetSuspendLayer(_gauntletLayer, false);
                }
                Game.Current.EventManager.TriggerEvent<MissionPlayerToggledOrderViewEvent>(new MissionPlayerToggledOrderViewEvent(true));
                return;
            }
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (_gauntletLayer != null)
            {
                ScreenManager.SetSuspendLayer(_gauntletLayer, true);
            }
            if (_slowedDownMission)
            {
                Mission.RemoveTimeSpeedRequest(864);
                _slowedDownMission = false;
            }
            MissionScreen.SetRadialMenuActiveState(false);
            Game.Current.EventManager.TriggerEvent<MissionPlayerToggledOrderViewEvent>(new MissionPlayerToggledOrderViewEvent(false));
        }

        private void OnDeploymentFinish()
        {
            IsSiegeDeployment = false;
            IsBattleDeployment = false;
            _dataSource.OnDeploymentFinished();
            _deploymentPointDataSources.Clear();
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (_deploymentMissionView != null)
            {
                DeploymentMissionView deploymentMissionView = _deploymentMissionView;
                deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Remove(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(OnDeploymentFinish));
            }
        }

        private void RefreshVisuals()
        {
            if (IsSiegeDeployment)
            {
                foreach (DeploymentSiegeMachineVM deploymentSiegeMachineVM in _deploymentPointDataSources)
                {
                    deploymentSiegeMachineVM.RefreshWithDeployedWeapon();
                }
            }
        }

        private IOrderable GetFocusedOrderableObject() => MissionScreen.OrderFlag.FocusedOrderableObject;

        private void SetSuspendTroopPlacer(bool value)
        {
            _orderTroopPlacer.SuspendTroopPlacer = value;
            MissionScreen.SetOrderFlagVisibility(!value);
        }

        public void SelectFormationAtIndex(int index)
        {
            _dataSource.OnSelect(index);
        }

        public void DeselectFormationAtIndex(int index)
        {
            _dataSource.TroopController.OnDeselectFormation(index);
        }

        public void OnFiltersSet(List<ValueTuple<int, List<int>>> filterData)
        {
            _dataSource.OnFiltersSet(filterData);
        }
        public void SetIsOrderPreconfigured(bool isOrderPreconfigured)
        {
            _dataSource.DeploymentController.SetIsOrderPreconfigured(isOrderPreconfigured);
        }

        private void OnBeforeOrder()
        {
            TickOrderFlag(_latestDt, true);
        }

        private void TickOrderFlag(float dt, bool forceUpdate)
        {
            if ((MissionScreen.OrderFlag.IsVisible || forceUpdate) /*&& TaleWorlds.Engine.Utilities.EngineFrameNo != MissionScreen.OrderFlag.LatestUpdateFrameNo*/)
            {
                MissionScreen.OrderFlag.Tick(_latestDt);
            }
        }

        void ISiegeDeploymentView.OnEntityHover(GameEntity hoveredEntity)
        {
            if (!_gauntletLayer.HitTest())
            {
                _dataSource.DeploymentController.OnEntityHover(hoveredEntity);
            }
        }

        void ISiegeDeploymentView.OnEntitySelection(GameEntity selectedEntity)
        {
            _dataSource.DeploymentController.OnEntitySelect(selectedEntity);
        }

        private void ToggleScreenRotation(bool isLocked) => MissionScreen.SetFixedMissionCameraActive(isLocked);

        public MissionOrderVM.CursorState cursorState
        {
            get
            {
                if (_dataSource.IsFacingSubOrdersShown)
                {
                    return MissionOrderVM.CursorState.Face;
                }
                return MissionOrderVM.CursorState.Move;
            }
        }

        private void TickInput(float dt)
        {
            if (Input.IsGameKeyDown(MissionOrderHotkeyCategory.HoldOrder) && !_dataSource.IsToggleOrderShown)
            {
                _holdTime += dt;
                if (_holdTime >= (double)_minHoldTimeForActivation)
                {
                    _dataSource.OpenToggleOrder(true);
                    _holdExecuted = true;
                }
            }
            else if (!Input.IsGameKeyDown(MissionOrderHotkeyCategory.HoldOrder))
            {
                if (_holdExecuted && _dataSource.IsToggleOrderShown)
                {
                    _dataSource.TryCloseToggleOrder();
                    _holdExecuted = false;
                }
                _holdTime = 0.0f;
            }
            if (_dataSource.IsToggleOrderShown)
            {
                if (_dataSource.TroopController.IsTransferActive && _gauntletLayer.Input.IsHotKeyPressed("Exit"))
                    _dataSource.TroopController.IsTransferActive = false;

                if (_dataSource.TroopController.IsTransferActive != _isTransferEnabled)
                {
                    _isTransferEnabled = _dataSource.TroopController.IsTransferActive;
                    if (!_isTransferEnabled)
                    {
                        _gauntletLayer.IsFocusLayer = false;
                        ScreenManager.TryLoseFocus(_gauntletLayer);
                    }
                    else
                    {
                        _gauntletLayer.IsFocusLayer = true;
                        ScreenManager.TrySetFocus(_gauntletLayer);
                    }
                }
                if (_dataSource.ActiveTargetState == 0 && (Input.IsKeyReleased(InputKey.LeftMouseButton) || Input.IsKeyReleased(InputKey.ControllerRTrigger)))
                {
                    OrderItemVM selectedOrderItem = _dataSource.LastSelectedOrderItem;
                    if ((selectedOrderItem != null ? (!selectedOrderItem.IsTitle ? 1 : 0) : 0) != 0 && _isGamepadActive)
                    {
                        _dataSource.ApplySelectedOrder();
                    }
                    else
                    {
                        switch (cursorState)
                        {
                            case MissionOrderVM.CursorState.Move:
                                IOrderable focusedOrderableObject = GetFocusedOrderableObject();
                                if (focusedOrderableObject != null)
                                {
                                    _dataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
                                }
                                break;
                            case MissionOrderVM.CursorState.Face:
                                _dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                            case MissionOrderVM.CursorState.Form:
                                _dataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                        }
                    }
                }
                if (ExitWithRightClick && Input.IsKeyReleased(InputKey.RightMouseButton))
                    _dataSource.OnEscape();
            }

            int pressedIndex = -1;
            if ((!_isGamepadActive || _dataSource.IsToggleOrderShown) && !Input.IsControlDown())
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
                _dataSource.OnGiveOrder(pressedIndex);

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
                _dataSource.SelectNextTroop(1);
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectPreviousGroup))
                _dataSource.SelectNextTroop(-1);
            else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ToggleGroupSelection))
                _dataSource.ToggleSelectionForCurrentTroop();

            if (formationTroopIndex != -1)
                _dataSource.OnSelect(formationTroopIndex);

            if (!Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ViewOrders))
                return;

            _dataSource.ViewOrders();
        }
    }
}
