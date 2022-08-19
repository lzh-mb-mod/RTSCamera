using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.CommandSystem.View
{
	[OverrideView(typeof(MissionFormationMarkerUIHandler))]
	public class CommandSystemMissionGauntletFormationMarker : MissionGauntletSingleplayerBattleUIBase
	{
		private MissionFormationMarkerVM _dataSource;

		private GauntletLayer _gauntletLayer;

		private List<CompassItemUpdateParams> _formationTargets;

		private CommandSystemOrderUIHandler _orderHandler;

		protected override void OnCreateView()
		{
			_formationTargets = new List<CompassItemUpdateParams>();
			_dataSource = new MissionFormationMarkerVM(base.Mission, base.MissionScreen.CombatCamera);
			_gauntletLayer = new GauntletLayer(ViewOrderPriority);
			_gauntletLayer.LoadMovie("FormationMarker", _dataSource);
			base.MissionScreen.AddLayer(_gauntletLayer);
			_orderHandler = base.Mission.GetMissionBehavior<CommandSystemOrderUIHandler>();
		}

		protected override void OnDestroyView()
		{
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_dataSource.OnFinalize();
			_dataSource = null;
		}

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (base.IsViewActive)
			{
				if (!_orderHandler.IsBattleDeployment)
				{
					_dataSource.IsEnabled = base.Input.IsGameKeyDown(5);
				}
				_dataSource.Tick(dt);
			}
		}

		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			if (base.IsViewActive)
			{
				_gauntletLayer._gauntletUIContext.ContextAlpha = 0f;
			}
		}

		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			if (base.IsViewActive)
			{
				_gauntletLayer._gauntletUIContext.ContextAlpha = 1f;
			}
		}
	}
}
