using System;
using System.Collections.Generic;
using System.Linq;
using MissionLibrary.Event;
using SandBox.View.Missions;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ScreenSystem;
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
        private MissionSiegePrepareView _siegeMissionView;
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
                new OnBeforeOrderDelegate(OnBeforeOrder),
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
            if ((MissionScreen.OrderFlag.IsVisible || forceUpdate) && TaleWorlds.Engine.Utilities.EngineFrameNo != MissionScreen.OrderFlag.LatestUpdateFrameNo)
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
            if (_dataSource is null)
            {
                return;
            }

            if (!IsSiegeDeployment && !IsBattleDeployment)
            {
                if (Input.IsGameKeyDown(86) && !_dataSource.IsToggleOrderShown)
                {
                    _holdTime += dt;
                    if (_holdTime >= _minHoldTimeForActivation)
                    {
                        _dataSource.OpenToggleOrder(true, !_holdExecuted);
                        _holdExecuted = true;
                    }
                }
                else if (!Input.IsGameKeyDown(86))
                {
                    if (_holdExecuted && _dataSource.IsToggleOrderShown)
                    {
                        _dataSource.TryCloseToggleOrder(false);
                    }
                    _holdExecuted = false;
                    _holdTime = 0f;
                }
            }
            if (_dataSource.IsToggleOrderShown)
            {
                if (_dataSource.TroopController.IsTransferActive && _gauntletLayer.Input.IsHotKeyPressed("Exit"))
                {
                    _dataSource.TroopController.IsTransferActive = false;
                }
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
                    OrderItemVM lastSelectedOrderItem = _dataSource.LastSelectedOrderItem;
                    if (lastSelectedOrderItem != null && !lastSelectedOrderItem.IsTitle && TaleWorlds.InputSystem.Input.IsGamepadActive)
                    {
                        _dataSource.ApplySelectedOrder();
                    }
                    else
                    {
                        switch (cursorState)
                        {
                            case MissionOrderVM.CursorState.Move:
                                {
                                    IOrderable focusedOrderableObject = GetFocusedOrderableObject();
                                    if (focusedOrderableObject != null)
                                    {
                                        _dataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
                                    }
                                    break;
                                }
                            case MissionOrderVM.CursorState.Face:
                                _dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                            case MissionOrderVM.CursorState.Form:
                                _dataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                            default:
                                Debug.FailedAssert("false", "C:\\Develop\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\Singleplayer\\MissionGauntletSingleplayerOrderUIHandler.cs", "TickInput", 621);
                                break;
                        }
                    }
                }
                if (Input.IsKeyReleased(InputKey.RightMouseButton) && !_isAnyDeployment)
                {
                    _dataSource.OnEscape();
                }
            }
            int num = -1;
            if ((!TaleWorlds.InputSystem.Input.IsGamepadActive || _dataSource.IsToggleOrderShown) && !DebugInput.IsControlDown())
            {
                if (Input.IsGameKeyPressed(68))
                {
                    num = 0;
                }
                else if (Input.IsGameKeyPressed(69))
                {
                    num = 1;
                }
                else if (Input.IsGameKeyPressed(70))
                {
                    num = 2;
                }
                else if (Input.IsGameKeyPressed(71))
                {
                    num = 3;
                }
                else if (Input.IsGameKeyPressed(72))
                {
                    num = 4;
                }
                else if (Input.IsGameKeyPressed(73))
                {
                    num = 5;
                }
                else if (Input.IsGameKeyPressed(74))
                {
                    num = 6;
                }
                else if (Input.IsGameKeyPressed(75))
                {
                    num = 7;
                }
                else if (Input.IsGameKeyPressed(76))
                {
                    num = 8;
                }
            }
            if (num > -1)
            {
                _dataSource.OnGiveOrder(num);
            }
            int num2 = -1;
            if (Input.IsGameKeyPressed(77))
            {
                num2 = 100;
            }
            else if (Input.IsGameKeyPressed(78))
            {
                num2 = 0;
            }
            else if (Input.IsGameKeyPressed(79))
            {
                num2 = 1;
            }
            else if (Input.IsGameKeyPressed(80))
            {
                num2 = 2;
            }
            else if (Input.IsGameKeyPressed(81))
            {
                num2 = 3;
            }
            else if (Input.IsGameKeyPressed(82))
            {
                num2 = 4;
            }
            else if (Input.IsGameKeyPressed(83))
            {
                num2 = 5;
            }
            else if (Input.IsGameKeyPressed(84))
            {
                num2 = 6;
            }
            else if (Input.IsGameKeyPressed(85))
            {
                num2 = 7;
            }
            if (!IsBattleDeployment && !IsSiegeDeployment)
            {
                if (Input.IsGameKeyPressed(87))
                {
                    _dataSource.SelectNextTroop(1);
                }
                else if (Input.IsGameKeyPressed(88))
                {
                    _dataSource.SelectNextTroop(-1);
                }
                else if (Input.IsGameKeyPressed(89))
                {
                    _dataSource.ToggleSelectionForCurrentTroop();
                }
            }
            if (num2 != -1)
            {
                _dataSource.OnSelect(num2);
            }
            if (Input.IsGameKeyPressed(67))
            {
                _dataSource.ViewOrders();
            }
        }
    }
}
