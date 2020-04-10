using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace EnhancedMission
{
    public class MissionMenuView : MissionView
    {
        private MissionMenuVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private GauntletMovie _movie;
        private EnhancedMissionConfig _config;
        private GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();

        public bool IsActivated { get; set; }

        public MissionMenuView()
        {
            this.ViewOrderPriorty = 24;
            _config = EnhancedMissionConfig.Get();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            this._gauntletLayer = null;
            this._dataSource?.OnFinalize();
            this._dataSource = null;
            this._movie = null;
        }

        public void ToggleMenu()
        {
            if (IsActivated)
                DeactivateMenu();
            else
                ActivateMenu();
        }

        public void ActivateMenu()
        {
            IsActivated = true;
            this._dataSource = new MissionMenuVM(this.DeactivateMenu);
            this._gauntletLayer = new GauntletLayer(this.ViewOrderPriorty) { IsFocusLayer = true };
            this._gauntletLayer.InputRestrictions.SetInputRestrictions();
            this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            this._movie = this._gauntletLayer.LoadMovie(nameof(MissionMenuView), _dataSource);
            this.MissionScreen.AddLayer(this._gauntletLayer);
            ScreenManager.TrySetFocus(this._gauntletLayer);
            PauseGame();
        }

        public void DeactivateMenu()
        {
            IsActivated = false;
            this._dataSource.OnFinalize();
            this._dataSource = null;
            this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
            this.MissionScreen.RemoveLayer(this._gauntletLayer);
            this._movie = null;
            this._gauntletLayer = null;
            UnpauseGame();
            EnhancedMissionConfig.Get().Serialize();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (IsActivated)
            {
                if (this._gauntletLayer.Input.IsKeyReleased(InputKey.RightMouseButton) ||
                    this._gauntletLayer.Input.IsKeyReleased(_gameKeyConfig.GetKey(GameKeyEnum.OpenMenu)) ||
                    this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
                    DeactivateMenu();
            }
            else if (this.Input.IsKeyReleased(_gameKeyConfig.GetKey(GameKeyEnum.OpenMenu)))
                ActivateMenu();
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (_config.ChangeCombatAI)
            {
                AgentStatModel.SetAgentAIStat(agent, agent.AgentDrivenProperties, _config.CombatAI);
                agent.UpdateAgentProperties();
            }
        }

        private static bool _oldGameStatusDisabledStatus = false;

        private static void PauseGame()
        {
            MBCommon.PauseGameEngine();
            _oldGameStatusDisabledStatus = Game.Current.GameStateManager.ActiveStateDisabledByUser;
            Game.Current.GameStateManager.ActiveStateDisabledByUser = true;
        }

        private static void UnpauseGame()
        {
            MBCommon.UnPauseGameEngine();
            Game.Current.GameStateManager.ActiveStateDisabledByUser = _oldGameStatusDisabledStatus;
        }
    }
}
