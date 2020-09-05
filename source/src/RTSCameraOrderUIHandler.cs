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

namespace RTSCamera
{
    [OverrideView(typeof(MissionOrderUIHandler))]
    public class RTSCameraOrderUIHandler : MissionView, ISiegeDeploymentView
    {

        private SwitchTeamLogic _controller;
        private void RegisterReload()
        {
            if (_controller == null)
            {
                foreach (var missionLogic in this.Mission.MissionLogics)
                {
                    if (missionLogic is SwitchTeamLogic controller)
                    {
                        _controller = controller;
                        break;
                    }
                }
                if (_controller != null)
                {
                    _controller.PreSwitchTeam += OnPreSwitchTeam;
                    _controller.PostSwitchTeam += OnPostSwitchTeam;
                }
            }
        }
        private void OnPreSwitchTeam()
        {
            this.dataSource.CloseToggleOrder();
            this.OnMissionScreenFinalize();
        }

        private void OnPostSwitchTeam()
        {
            this.OnMissionScreenInitialize();
            this.OnMissionScreenActivate();
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
        private bool IsDeployment;
        private bool isInitialized;
        private bool _isTransferEnabled;

        public RTSCameraOrderUIHandler()
        {
            this.ViewOrderPriorty = 19;
        }
        public void OnActivateToggleOrder()
        {
            exitWithRightClick = true;
            if (this.dataSource == null || this.dataSource.ActiveTargetState == 0)
                this._orderTroopPlacer.SuspendTroopPlacer = false;
            this.MissionScreen.SetOrderFlagVisibility(true);
            if (this.gauntletLayer != null)
                ScreenManager.SetSuspendLayer((ScreenLayer)this.gauntletLayer, false);
            Game.Current.EventManager.TriggerEvent<MissionPlayerToggledOrderViewEvent>(new MissionPlayerToggledOrderViewEvent(true));
        }

        public void OnDeactivateToggleOrder()
        {
            this._orderTroopPlacer.SuspendTroopPlacer = true;
            this.MissionScreen.SetOrderFlagVisibility(false);
            if (this.gauntletLayer != null)
                ScreenManager.SetSuspendLayer((ScreenLayer)this.gauntletLayer, true);
            Game.Current.EventManager.TriggerEvent<MissionPlayerToggledOrderViewEvent>(new MissionPlayerToggledOrderViewEvent(false));
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            RegisterReload();
            this.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
            this.MissionScreen.OrderFlag = new OrderFlag(this.Mission, this.MissionScreen);
            this._orderTroopPlacer = this.Mission.GetMissionBehaviour<RTSCameraOrderTroopPlacer>();
            this.MissionScreen.SetOrderFlagVisibility(false);
            this._siegeDeploymentHandler = this.Mission.GetMissionBehaviour<SiegeDeploymentHandler>();
            this.IsDeployment = this._siegeDeploymentHandler != null;
            if (this.IsDeployment)
            {
                this._siegeMissionView = this.Mission.GetMissionBehaviour<SiegeMissionView>();
                if (this._siegeMissionView != null)
                    this._siegeMissionView.OnDeploymentFinish += new OnPlayerDeploymentFinishDelegate(this.OnDeploymentFinish);
                this._deploymentPointDataSources = new List<DeploymentSiegeMachineVM>();
            }
            this.dataSource = new MissionOrderVM(this.Mission, this.MissionScreen.CombatCamera, this.IsDeployment ? this._siegeDeploymentHandler.DeploymentPoints.ToList<DeploymentPoint>() : new List<DeploymentPoint>(), new Action<bool>(this.ToggleScreenRotation), this.IsDeployment, new GetOrderFlagPositionDelegate(this.MissionScreen.GetOrderFlagPosition), new OnRefreshVisualsDelegate(this.RefreshVisuals), new ToggleOrderPositionVisibilityDelegate(this.SetSuspendTroopPlacer), new OnToggleActivateOrderStateDelegate(this.OnActivateToggleOrder), new OnToggleActivateOrderStateDelegate(this.OnDeactivateToggleOrder));
            if (this.IsDeployment)
            {
                foreach (DeploymentPoint deploymentPoint in this._siegeDeploymentHandler.DeploymentPoints)
                {
                    DeploymentSiegeMachineVM deploymentSiegeMachineVm = new DeploymentSiegeMachineVM(deploymentPoint, (SiegeWeapon)null, this.MissionScreen.CombatCamera, new Action<DeploymentSiegeMachineVM>(this.dataSource.OnRefreshSelectedDeploymentPoint), new Action<DeploymentPoint>(this.dataSource.OnEntityHover), false);
                    Vec3 origin = deploymentPoint.GameEntity.GetFrame().origin;
                    for (int index = 0; index < deploymentPoint.GameEntity.ChildCount; ++index)
                    {
                        if (((IEnumerable<string>)deploymentPoint.GameEntity.GetChild(index).Tags).Contains<string>("deployment_point_icon_target"))
                        {
                            Vec3 vec3 = origin + deploymentPoint.GameEntity.GetChild(index).GetFrame().origin;
                            break;
                        }
                    }
                    this._deploymentPointDataSources.Add(deploymentSiegeMachineVm);
                    deploymentSiegeMachineVm.RemainingCount = 0;
                    this._deploymentPointWidgetSize = new Vec2(75f / TaleWorlds.Engine.Screen.RealScreenResolutionWidth, 75f / TaleWorlds.Engine.Screen.RealScreenResolutionHeight);
                }
            }
            this.gauntletLayer = new GauntletLayer(this.ViewOrderPriorty, "GauntletLayer");
            this.gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            this._viewMovie = this.gauntletLayer.LoadMovie("Order", (ViewModel)this.dataSource);
            this.MissionScreen.AddLayer((ScreenLayer)this.gauntletLayer);
            if (this.IsDeployment)
                this.gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            else if (!this.dataSource.IsToggleOrderShown)
                ScreenManager.SetSuspendLayer((ScreenLayer)this.gauntletLayer, true);
            this.dataSource.InputRestrictions = this.gauntletLayer.InputRestrictions;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            this._deploymentPointDataSources = (List<DeploymentSiegeMachineVM>)null;
            this._orderTroopPlacer = null;
            this.gauntletLayer = (GauntletLayer)null;
            this.dataSource.OnFinalize();
            this.dataSource = (MissionOrderVM)null;
            this._viewMovie = (GauntletMovie)null;
            this._siegeDeploymentHandler = (SiegeDeploymentHandler)null;
        }

        private void OnDeploymentFinish()
        {
            this.IsDeployment = false;
            this.dataSource.FinalizeDeployment();
            this._deploymentPointDataSources.Clear();
            this._orderTroopPlacer.SuspendTroopPlacer = true;
            this.MissionScreen.SetOrderFlagVisibility(false);
            if (this._siegeMissionView == null)
                return;
            this._siegeMissionView.OnDeploymentFinish -= new OnPlayerDeploymentFinishDelegate(this.OnDeploymentFinish);
        }

        public override bool OnEscape()
        {
            return this.dataSource.CloseToggleOrder();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            this.TickInput(dt);
            this.dataSource.Tick(dt);
            if (this.dataSource.IsToggleOrderShown)
            {
                if (this._orderTroopPlacer.SuspendTroopPlacer && this.dataSource.ActiveTargetState == 0)
                    this._orderTroopPlacer.SuspendTroopPlacer = false;
                this._orderTroopPlacer.IsDrawingForced = this.dataSource.IsMovementSubOrdersShown;
                this._orderTroopPlacer.IsDrawingFacing = this.dataSource.IsFacingSubOrdersShown;
                this._orderTroopPlacer.IsDrawingForming = false;
                this._orderTroopPlacer.IsDrawingAttaching = this.cursorState == MissionOrderVM.CursorState.Attach;
                this._orderTroopPlacer.UpdateAttachVisuals(this.cursorState == MissionOrderVM.CursorState.Attach);
                if (this.cursorState == MissionOrderVM.CursorState.Face)
                    this.MissionScreen.OrderFlag.SetArrowVisibility(true, OrderController.GetOrderLookAtDirection(this.Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, this.MissionScreen.OrderFlag.Position.AsVec2));
                else
                    this.MissionScreen.OrderFlag.SetArrowVisibility(false, Vec2.Invalid);
                if (this.cursorState == MissionOrderVM.CursorState.Form)
                    this.MissionScreen.OrderFlag.SetWidthVisibility(true, OrderController.GetOrderFormCustomWidth(this.Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, this.MissionScreen.OrderFlag.Position));
                else
                    this.MissionScreen.OrderFlag.SetWidthVisibility(false, -1f);
            }
            else
            {
                if (!this._orderTroopPlacer.SuspendTroopPlacer)
                    this._orderTroopPlacer.SuspendTroopPlacer = true;
                this.gauntletLayer.InputRestrictions.ResetInputRestrictions();
            }
            if (this.IsDeployment)
            {
                if (this.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton))
                    this.gauntletLayer.InputRestrictions.SetMouseVisibility(false);
                else
                    this.gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            }
            this.MissionScreen.OrderFlag.IsTroop = this.dataSource.ActiveTargetState == 0;
            this.MissionScreen.OrderFlag.Tick(dt);
        }

        private void RefreshVisuals()
        {
            if (!this.IsDeployment)
                return;
            foreach (DeploymentSiegeMachineVM deploymentPointDataSource in this._deploymentPointDataSources)
                deploymentPointDataSource.RefreshWithDeployedWeapon();
        }

        public override void OnMissionScreenActivate()
        {
            base.OnMissionScreenActivate();
            this.dataSource.AfterInitialize();
            this.isInitialized = true;
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!this.isInitialized || !agent.IsHuman)
                return;
            this.dataSource.AddTroops(agent);
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
            this.dataSource.RemoveTroops(affectedAgent);
        }

        private IOrderable GetFocusedOrderableObject()
        {
            return this.MissionScreen.OrderFlag.FocusedOrderableObject;
        }

        private void SetSuspendTroopPlacer(bool value)
        {
            this._orderTroopPlacer.SuspendTroopPlacer = value;
            this.MissionScreen.SetOrderFlagVisibility(!value);
        }

        void ISiegeDeploymentView.OnEntityHover(GameEntity hoveredEntity)
        {
            if (this.gauntletLayer.HitTest())
                return;
            this.dataSource.OnEntityHover(hoveredEntity);
        }

        void ISiegeDeploymentView.OnEntitySelection(GameEntity selectedEntity)
        {
            this.dataSource.OnEntitySelect(selectedEntity);
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
                return this.dataSource.IsFacingSubOrdersShown ? MissionOrderVM.CursorState.Face : MissionOrderVM.CursorState.Move;
            }
        }

        private void TickInput(float dt)
        {
            if (this.dataSource.IsToggleOrderShown)
            {
                if (this.dataSource.IsTransferActive && this.gauntletLayer.Input.IsHotKeyReleased("Exit"))
                    this.dataSource.IsTransferActive = false;
                if (this.dataSource.IsTransferActive != this._isTransferEnabled)
                {
                    this._isTransferEnabled = this.dataSource.IsTransferActive;
                    if (!this._isTransferEnabled)
                    {
                        this.gauntletLayer.IsFocusLayer = false;
                        ScreenManager.TryLoseFocus((ScreenLayer)this.gauntletLayer);
                    }
                    else
                    {
                        this.gauntletLayer.IsFocusLayer = true;
                        ScreenManager.TrySetFocus((ScreenLayer)this.gauntletLayer);
                    }
                }
                if (this.dataSource.ActiveTargetState == 0 && this.Input.IsKeyReleased(InputKey.LeftMouseButton))
                {
                    switch (this.cursorState)
                    {
                        case MissionOrderVM.CursorState.Move:
                            IOrderable focusedOrderableObject = this.GetFocusedOrderableObject();
                            if (focusedOrderableObject != null)
                            {
                                this.dataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
                                break;
                            }
                            break;
                        case MissionOrderVM.CursorState.Face:
                            this.dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Scene, UIntPtr.Zero, this.MissionScreen.GetOrderFlagPosition(), false));
                            break;
                        case MissionOrderVM.CursorState.Form:
                            this.dataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(Mission.Scene, UIntPtr.Zero, this.MissionScreen.GetOrderFlagPosition(), false));
                            break;
                    }
                }
                //if (this.Input.IsAltDown())
                //{
                //    bool isMouseVisible = this.dataSource.IsTransferActive || !this.gauntletLayer.InputRestrictions.MouseVisibility;
                //    this.gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible, isMouseVisible ? InputUsageMask.Mouse : InputUsageMask.Invalid);
                //}
                if (exitWithRightClick && this.Input.IsKeyReleased(InputKey.RightMouseButton))
                    this.dataSource.OnEscape();
            }
            int pressedIndex = -1;
            if (!this.Input.IsControlDown())
            {
                if (this.Input.IsGameKeyPressed(53))
                    pressedIndex = 0;
                else if (this.Input.IsGameKeyPressed(54))
                    pressedIndex = 1;
                else if (this.Input.IsGameKeyPressed(55))
                    pressedIndex = 2;
                else if (this.Input.IsGameKeyPressed(56))
                    pressedIndex = 3;
                else if (this.Input.IsGameKeyPressed(57))
                    pressedIndex = 4;
                else if (this.Input.IsGameKeyPressed(58))
                    pressedIndex = 5;
                else if (this.Input.IsGameKeyPressed(59))
                    pressedIndex = 6;
                else if (this.Input.IsGameKeyPressed(60))
                    pressedIndex = 7;
                else if (this.Input.IsGameKeyPressed(61))
                    pressedIndex = 8;
            }
            if (pressedIndex > -1)
                this.dataSource.OnGiveOrder(pressedIndex);
            int formationTroopIndex = -1;
            if (this.Input.IsGameKeyPressed(62))
                formationTroopIndex = 100;
            else if (this.Input.IsGameKeyPressed(63))
                formationTroopIndex = 0;
            else if (this.Input.IsGameKeyPressed(64))
                formationTroopIndex = 1;
            else if (this.Input.IsGameKeyPressed(65))
                formationTroopIndex = 2;
            else if (this.Input.IsGameKeyPressed(66))
                formationTroopIndex = 3;
            else if (this.Input.IsGameKeyPressed(67))
                formationTroopIndex = 4;
            else if (this.Input.IsGameKeyPressed(68))
                formationTroopIndex = 5;
            else if (this.Input.IsGameKeyPressed(69))
                formationTroopIndex = 6;
            else if (this.Input.IsGameKeyPressed(70))
                formationTroopIndex = 7;
            if (formationTroopIndex != -1)
                this.dataSource.OnSelect(formationTroopIndex);
            if (!this.Input.IsGameKeyPressed(52))
                return;
            this.dataSource.ViewOrders();
        }
    }
}
