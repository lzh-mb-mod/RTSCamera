using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace RTSCamera.CommandSystem.View
{
	[OverrideView(typeof(MissionOrderOfBattleUIHandler))]
	public class CommandSystemOrderOfBattleUIHandler : MissionView
	{
		public CommandSystemOrderOfBattleUIHandler(OrderOfBattleVM dataSource)
		{
			this._dataSource = dataSource;
			this.ViewOrderPriority = 13;
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._deploymentMissionView = base.Mission.GetMissionBehavior<DeploymentMissionView>();
			DeploymentMissionView deploymentMissionView = this._deploymentMissionView;
			deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Combine(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(this.OnDeploymentFinish));
			this._playerRoleMissionController = base.Mission.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>();
			this._playerRoleMissionController.OnPlayerTurnToChooseFormationToLead += this.OnPlayerTurnToChooseFormationToLead;
			this._playerRoleMissionController.OnAllFormationsAssignedSergeants += this.OnAllFormationsAssignedSergeants;
			this._orderUIHandler = base.Mission.GetMissionBehavior<CommandSystemOrderUIHandler>();
			this._orderUIHandler.OnCameraControlsToggled += this.OnCameraControlsToggled;
			this._orderTroopPlacer = base.Mission.GetMissionBehavior<CommandSystemOrderTroopPlacer>();
			CommandSystemOrderTroopPlacer orderTroopPlacer = this._orderTroopPlacer;
			orderTroopPlacer.OnUnitDeployed = (Action)Delegate.Combine(orderTroopPlacer.OnUnitDeployed, new Action(this.OnUnitDeployed));
			this._gauntletLayer = new GauntletLayer(this.ViewOrderPriority, "GauntletLayer", false);
			this._movie = this._gauntletLayer.LoadMovie("OrderOfBattle", this._dataSource);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			SpriteData spriteData = UIResourceManager.SpriteData;
			TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
			ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
			this._orderOfBattleCategory = spriteData.SpriteCategories["ui_order_of_battle"];
			this._orderOfBattleCategory.Load(resourceContext, uiresourceDepot);
			base.MissionScreen.AddLayer(this._gauntletLayer);
			OrderOfBattleVM dataSource = this._dataSource;
			dataSource.OnHeroSelectionToggle = (Action)Delegate.Combine(dataSource.OnHeroSelectionToggle, new Action(this.OnHeroSelectionToggled));
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		}

		private void OnHeroSelectionToggled()
		{
			if (this._dataSource.IsHeroSelectionShown)
			{
				this._gauntletLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this._gauntletLayer);
				return;
			}
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			this._gauntletLayer.IsFocusLayer = false;
		}

		public override bool IsReady()
		{
			return this._isDeploymentFinished || this._orderOfBattleCategory.IsLoaded;
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);
			if (this._isActive)
			{
				this.TickInput();
				this._dataSource.Tick();
			}
		}

		private void TickInput()
		{
			if (base.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.RightMouseButton) || base.MissionScreen.SceneLayer.Input.IsKeyDown(InputKey.ControllerLTrigger))
			{
				this._gauntletLayer.InputRestrictions.SetMouseVisibility(false);
			}
			else
			{
				this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			}
			if (this._dataSource.IsHeroSelectionShown && (this._gauntletLayer.Input.IsHotKeyReleased("Confirm") || this._gauntletLayer.Input.IsHotKeyReleased("Exit")))
			{
				this._dataSource.ExecuteCloseHeroAssignment();
			}
		}

		public override void OnMissionScreenFinalize()
		{
			this._dataSource.OnFinalize();
			this._dataSource = null;
			this._gauntletLayer.ReleaseMovie(this._movie);
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			this._orderOfBattleCategory.Unload();
			base.OnMissionScreenFinalize();
		}

		public override bool OnEscape()
		{
			return this._dataSource.OnEscape();
		}

		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			this._gauntletLayer._gauntletUIContext.ContextAlpha = 0f;
		}

		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			this._gauntletLayer._gauntletUIContext.ContextAlpha = 1f;
		}

		public override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return !this._isActive;
		}

		private void OnPlayerTurnToChooseFormationToLead(Dictionary<int, Agent> lockedFormationIndicesAndSergeants, List<int> remainingFormationIndices)
		{
			this._cachedOrderTypeSetting = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.OrderType);
			ManagedOptions.SetConfig(ManagedOptions.ManagedOptionsType.OrderType, 1f);
			this._dataSource.Initialize(base.Mission, base.MissionScreen.CombatCamera, new Action<int>(this.SelectFormationAtIndex), new Action<int>(this.DeselectFormationAtIndex), new Action(this.OnAutoDeploy), new Action(this.OnBeginMission), lockedFormationIndicesAndSergeants, new Action<Agent>(this.FocusOnAgent));
			this._orderUIHandler.SetIsOrderPreconfigured(this._dataSource.IsOrderPreconfigured);
			this._isActive = true;
		}

		private void OnAllFormationsAssignedSergeants(Dictionary<int, Agent> formationsWithLooselyAssignedSergeants)
		{
			this._dataSource.OnAllFormationsAssignedSergeants(formationsWithLooselyAssignedSergeants);
		}

		private void OnDeploymentFinish()
		{
			this._isActive = false;
			this._isDeploymentFinished = true;
			this._dataSource.FinalizeDeployment();
			DeploymentMissionView deploymentMissionView = this._deploymentMissionView;
			deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Remove(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(this.OnDeploymentFinish));
			this._playerRoleMissionController.OnPlayerTurnToChooseFormationToLead -= this.OnPlayerTurnToChooseFormationToLead;
			this._playerRoleMissionController.OnAllFormationsAssignedSergeants -= this.OnAllFormationsAssignedSergeants;
			CommandSystemOrderTroopPlacer orderTroopPlacer = this._orderTroopPlacer;
			orderTroopPlacer.OnUnitDeployed = (Action)Delegate.Remove(orderTroopPlacer.OnUnitDeployed, new Action(this.OnUnitDeployed));
			this._orderUIHandler.OnCameraControlsToggled -= this.OnCameraControlsToggled;
			this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
			ManagedOptions.SetConfig(ManagedOptions.ManagedOptionsType.OrderType, this._cachedOrderTypeSetting);
			this._orderOfBattleCategory.Unload();
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			this._gauntletLayer.IsFocusLayer = false;
		}

		private void OnCameraControlsToggled(bool isEnabled)
		{
			this._dataSource.AreCameraControlsEnabled = isEnabled;
		}

		private void FocusOnAgent(Agent agent)
		{
			if (base.MissionScreen.CombatCamera.Position.DistanceSquared(agent.GetEyeGlobalPosition()) > 17.5f)
			{
				base.MissionScreen.SetAgentToFollow(agent);
			}
		}

		private void SelectFormationAtIndex(int index)
		{
			CommandSystemOrderUIHandler orderUIHandler = this._orderUIHandler;
			if (orderUIHandler == null)
			{
				return;
			}
			orderUIHandler.SelectFormationAtIndex(index);
		}

		private void DeselectFormationAtIndex(int index)
		{
			CommandSystemOrderUIHandler orderUIHandler = this._orderUIHandler;
			if (orderUIHandler == null)
			{
				return;
			}
			orderUIHandler.DeselectFormationAtIndex(index);
		}

		private void OnAutoDeploy()
		{
			this._orderUIHandler.OnAutoDeploy();
		}

		private void OnBeginMission()
		{
			this._orderUIHandler.OnBeginMission();
			this._orderUIHandler.OnFiltersSet(this._dataSource.CurrentConfiguration);
		}

		private void OnUnitDeployed()
		{
			this._dataSource.OnUnitDeployed();
		}

		private const float MinDistanceToTriggerFollowAgent = 17.5f;

		private OrderOfBattleVM _dataSource;

		private GauntletLayer _gauntletLayer;

		private IGauntletMovie _movie;

		private SpriteCategory _orderOfBattleCategory;

		private DeploymentMissionView _deploymentMissionView;

		private CommandSystemOrderUIHandler _orderUIHandler;

		private AssignPlayerRoleInTeamMissionController _playerRoleMissionController;

		private CommandSystemOrderTroopPlacer _orderTroopPlacer;

		private bool _isActive;

		private bool _isDeploymentFinished;

		private float _cachedOrderTypeSetting;
	}
}
