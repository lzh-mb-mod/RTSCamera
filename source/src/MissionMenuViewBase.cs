using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera
{
    public class MissionMenuViewBase : MissionView
    {
        private readonly string _movieName;
        private MissionMenuVMBase _dataSource;
        protected GauntletLayer GauntletLayer;
        private GauntletMovie _movie;

        protected Func<MissionMenuVMBase> GetDataSource;
        public bool IsActivated { get; set; }

        public MissionMenuViewBase(int viewOrderPriority, string movieName)
        {
            this.ViewOrderPriorty = viewOrderPriority;
            _movieName = movieName;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            this.GauntletLayer = null;
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
            if (GetDataSource == null)
                return;
            this._dataSource = GetDataSource?.Invoke();
            this.GauntletLayer = new GauntletLayer(this.ViewOrderPriorty) { IsFocusLayer = true };
            this.GauntletLayer.InputRestrictions.SetInputRestrictions();
            this.GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            this._movie = this.GauntletLayer.LoadMovie(_movieName, _dataSource);
            this.MissionScreen.AddLayer(this.GauntletLayer);
            ScreenManager.TrySetFocus(this.GauntletLayer);
            PauseGame();
        }

        public void DeactivateMenu()
        {
            _dataSource?.CloseMenu();
        }
        protected void OnCloseMenu()
        {
            IsActivated = false;
            this._dataSource.OnFinalize();
            this._dataSource = null;
            this.GauntletLayer.InputRestrictions.ResetInputRestrictions();
            this.MissionScreen.RemoveLayer(this.GauntletLayer);
            this._movie = null;
            this.GauntletLayer = null;
            UnpauseGame();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (IsActivated)
            {
                if (this.GauntletLayer.Input.IsKeyReleased(InputKey.RightMouseButton) ||
                    this.GauntletLayer.Input.IsHotKeyReleased("Exit"))
                    DeactivateMenu();
            }
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            Game.Current.GameStateManager.ActiveStateDisabledByUser = false;
        }

        private bool _oldGameStatusDisabledStatus = false;

        private void PauseGame()
        {
            MBCommon.PauseGameEngine();
            _oldGameStatusDisabledStatus = Game.Current.GameStateManager.ActiveStateDisabledByUser;
            Game.Current.GameStateManager.ActiveStateDisabledByUser = true;
        }

        private void UnpauseGame()
        {
            MBCommon.UnPauseGameEngine();
            Game.Current.GameStateManager.ActiveStateDisabledByUser = _oldGameStatusDisabledStatus;
        }
    }
}
