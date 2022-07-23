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
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace RTSCamera.CommandSystem.View
{
    //[OverrideView(typeof(MissionOrderUIHandler))]
    public class CommandSystemOrderUIHandler : MissionView, ISiegeDeploymentView
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
        private const float _slowDownAmountWhileOrderIsOpen = 0.25f;
        private const int _missionTimeSpeedRequestID = 864;
        private float _holdTime;
        private bool _holdExecuted;
        private DeploymentMissionView _deploymentMissionView;
        private List<DeploymentSiegeMachineVM> _deploymentPointDataSources;
        private CommandSystemOrderTroopPlacer _orderTroopPlacer;
        public GauntletLayer GauntletLayer;
        private IGauntletMovie _movie;
        private SpriteCategory _spriteCategory;
        public  MissionOrderVM DataSource;
        private SiegeDeploymentHandler _siegeDeploymentHandler;
        private BattleDeploymentHandler _battleDeploymentHandler;

        private bool _isInitialized;
        private bool _slowedDownMission;
        private float _latestDt;
        private bool _isTransferEnabled;

        private float _minHoldTimeForActivation => 0.0f;

        public bool IsSiegeDeployment { get; private set; }

        public bool IsBattleDeployment { get; private set; }

        public bool IsAnyDeployment => this.IsSiegeDeployment || this.IsBattleDeployment;

        public event Action<bool> OnCameraControlsToggled;

        public CommandSystemOrderUIHandler() => ViewOrderPriority = 12;

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            this._latestDt = dt;
            if (this.MissionScreen.IsPhotoModeEnabled)
                return;
            TickInput(dt);
            DataSource.Update();
            if (DataSource.IsToggleOrderShown)
            {
                _orderTroopPlacer.IsDrawingForced = DataSource.IsMovementSubOrdersShown;
                _orderTroopPlacer.IsDrawingFacing = DataSource.IsFacingSubOrdersShown;
                _orderTroopPlacer.IsDrawingForming = false;
                if (cursorState == MissionOrderVM.CursorState.Face)
                    MissionScreen.OrderFlag.SetArrowVisibility(true, OrderController.GetOrderLookAtDirection(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position.AsVec2));
                else
                    MissionScreen.OrderFlag.SetArrowVisibility(false, Vec2.Invalid);
                if (cursorState == MissionOrderVM.CursorState.Form)
                    MissionScreen.OrderFlag.SetWidthVisibility(true, OrderController.GetOrderFormCustomWidth(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position));
                else
                    MissionScreen.OrderFlag.SetWidthVisibility(false, -1f);
                if (TaleWorlds.InputSystem.Input.IsGamepadActive)
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
            if (IsAnyDeployment)
            {
                if (MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
                {
                    GauntletLayer.InputRestrictions.SetMouseVisibility(false);
                    this.OnCameraControlsToggled?.Invoke(true);
                }
                else
                {
                    GauntletLayer.InputRestrictions.SetInputRestrictions();
                    this.OnCameraControlsToggled?.Invoke(false);
                }
            }
            MissionScreen.OrderFlag.IsTroop = DataSource.ActiveTargetState == 0;
            MissionScreen.OrderFlag.Tick(dt);
        }

        public override bool OnEscape()
        {
            bool toggleOrderShown = DataSource.IsToggleOrderShown;
            DataSource.OnEscape();
            return !this.IsAnyDeployment && toggleOrderShown;
        }

        public override void OnMissionScreenActivate()
        {
            base.OnMissionScreenActivate();
            DataSource.AfterInitialize();
            _isInitialized = true;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!_isInitialized || !agent.IsHuman)
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
            _orderTroopPlacer = Mission.GetMissionBehavior<CommandSystemOrderTroopPlacer>();
            MissionScreen.SetOrderFlagVisibility(false);
            _siegeDeploymentHandler = Mission.GetMissionBehavior<SiegeDeploymentHandler>();
            _battleDeploymentHandler = Mission.GetMissionBehavior<BattleDeploymentHandler>();
            this.IsSiegeDeployment = this._siegeDeploymentHandler != null;
            this.IsBattleDeployment = this._battleDeploymentHandler != null;
            if (IsAnyDeployment)
            {
                _deploymentMissionView = Mission.GetMissionBehavior<DeploymentMissionView>();
                if (_deploymentMissionView != null)
                    _deploymentMissionView.OnDeploymentFinish += OnDeploymentFinish;
                _deploymentPointDataSources = new List<DeploymentSiegeMachineVM>();
            }
            DataSource = new MissionOrderVM(MissionScreen.CombatCamera, IsSiegeDeployment ? _siegeDeploymentHandler.DeploymentPoints.ToList() : new List<DeploymentPoint>(), ToggleScreenRotation, IsAnyDeployment, MissionScreen.GetOrderFlagPosition, RefreshVisuals, SetSuspendTroopPlacer, OnActivateToggleOrder, OnDeactivateToggleOrder, OnTransferFinished, new OnBeforeOrderDelegate(OnBeforeOrder),  false);
            if (IsSiegeDeployment)
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
            GauntletLayer = new GauntletLayer(ViewOrderPriority);
            GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _movie = GauntletLayer.LoadMovie(BannerlordConfig.OrderType == 0 ? _barOrderMovieName : _radialOrderMovieName, DataSource);
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
            this._spriteCategory = spriteData.SpriteCategories["ui_order"];
            this._spriteCategory.Load((ITwoDimensionResourceContext)resourceContext, uiResourceDepot);
            MissionScreen.AddLayer(GauntletLayer);
            if (BannerlordConfig.HideBattleUI)
                GauntletLayer._gauntletUIContext.ContextAlpha = 0.0f;
            if (IsAnyDeployment)
                GauntletLayer.InputRestrictions.SetInputRestrictions();
            else if (!DataSource.IsToggleOrderShown)
                ScreenManager.SetSuspendLayer(GauntletLayer, true);
            DataSource.InputRestrictions = GauntletLayer.InputRestrictions;
            ManagedOptions.OnManagedOptionChanged += OnManagedOptionChanged;
        }

        private void OnManagedOptionChanged(
            ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            switch (changedManagedOptionsType)
            {
                case ManagedOptions.ManagedOptionsType.OrderType:
                    this.GauntletLayer.ReleaseMovie(this._movie);
                    this._movie = this.GauntletLayer.LoadMovie(BannerlordConfig.OrderType == 0 ? "OrderBar" : "OrderRadial", (ViewModel)DataSource);
                    break;
                case ManagedOptions.ManagedOptionsType.OrderLayoutType:
                    this.DataSource?.OnOrderLayoutTypeChanged();
                    break;
                case ManagedOptions.ManagedOptionsType.SlowDownOnOrder:
                    if (BannerlordConfig.SlowDownOnOrder || !this._slowedDownMission)
                        break;
                    this.Mission.RemoveTimeSpeedRequest(864);
                    break;
                case ManagedOptions.ManagedOptionsType.HideBattleUI:
                    this.GauntletLayer._gauntletUIContext.ContextAlpha = BannerlordConfig.HideBattleUI ? 0.0f : 1f;
                    break;
            }
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
            _battleDeploymentHandler = null;
            _spriteCategory.Unload();
        }

        public override void OnConversationBegin()
        {
            base.OnConversationBegin();
            DataSource?.TryCloseToggleOrder(true);
        }

        public void OnActivateToggleOrder() => SetLayerEnabled(true);

        public void OnDeactivateToggleOrder()
        {
            if (DataSource.TroopController.IsTransferActive)
                return;
            SetLayerEnabled(false);
        }

        private void OnTransferFinished()
        {
            if (this.IsAnyDeployment)
                return;
            SetLayerEnabled(false);
        }

        public void OnAutoDeploy() => DataSource.DeploymentController.ExecuteAutoDeploy();

        public void OnBeginMission() => DataSource.DeploymentController.ExecuteBeginSiege();

        private void SetLayerEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                ExitWithRightClick = true;
                if (DataSource == null || DataSource.ActiveTargetState == 0)
                    _orderTroopPlacer.SuspendTroopPlacer = false;
                if (!this._slowedDownMission && BannerlordConfig.SlowDownOnOrder)
                {
                    this.Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(0.25f, 864));
                    this._slowedDownMission = true;
                }
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
                if (this._slowedDownMission)
                {
                    this.Mission.RemoveTimeSpeedRequest(864);
                    this._slowedDownMission = false;
                }
                MissionScreen.SetRadialMenuActiveState(false);
                Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(false));
            }
        }

        private void OnDeploymentFinish()
        {
            this.IsSiegeDeployment = false;
            this.IsBattleDeployment = false;
            DataSource.DeploymentController.FinalizeDeployment();
            _deploymentPointDataSources.Clear();
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (_deploymentMissionView == null)
                return;
            _deploymentMissionView.OnDeploymentFinish -= OnDeploymentFinish;
        }

        private void RefreshVisuals()
        {
            if (!IsSiegeDeployment)
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

        public void SelectFormationAtIndex(int index) => this.DataSource.OnSelect(index);

        public void DeselectFormationAtIndex(int index) => this.DataSource.TroopController.OnDeselectFormation(index);

        public void OnFiltersSet(List<(int, List<int>)> filterData) => this.DataSource.OnFiltersSet(filterData);

        public void SetIsOrderPreconfigured(bool isOrderPreconfigured) => this.DataSource.DeploymentController.SetIsOrderPreconfigured(isOrderPreconfigured);


        private void OnBeforeOrder() => this.TickOrderFlag(this._latestDt, true);

        private void TickOrderFlag(float dt, bool forceUpdate)
        {
            if (!(this.MissionScreen.OrderFlag.IsVisible | forceUpdate) || TaleWorlds.Engine.Utilities.EngineFrameNo == this.MissionScreen.OrderFlag.LatestUpdateFrameNo)
                return;
            this.MissionScreen.OrderFlag.Tick(this._latestDt);
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
                }
                _holdExecuted = false;
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
                    if ((selectedOrderItem != null ? (!selectedOrderItem.IsTitle ? 1 : 0) : 0) != 0 && TaleWorlds.InputSystem.Input.IsGamepadActive)
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
            if ((!TaleWorlds.InputSystem.Input.IsGamepadActive || DataSource.IsToggleOrderShown) && !Input.IsControlDown())
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
            if (!this.IsBattleDeployment && !IsSiegeDeployment)
            {
                if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectNextGroup))
                    DataSource.SelectNextTroop(1);
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.SelectPreviousGroup))
                    DataSource.SelectNextTroop(-1);
                else if (Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ToggleGroupSelection))
                    DataSource.ToggleSelectionForCurrentTroop();
            }
            if (formationTroopIndex != -1)
                DataSource.OnSelect(formationTroopIndex);
            if (!Input.IsGameKeyPressed(MissionOrderHotkeyCategory.ViewOrders))
                return;
            DataSource.ViewOrders();
        }
    }
}
