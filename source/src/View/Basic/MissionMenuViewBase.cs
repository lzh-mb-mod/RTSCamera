using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.View.Basic
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
            ViewOrderPriorty = viewOrderPriority;
            _movieName = movieName;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            GauntletLayer = null;
            _dataSource?.OnFinalize();
            _dataSource = null;
            _movie = null;
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
            _dataSource = GetDataSource?.Invoke();
            GauntletLayer = new GauntletLayer(ViewOrderPriorty) { IsFocusLayer = true };
            GauntletLayer.InputRestrictions.SetInputRestrictions();
            GauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _movie = GauntletLayer.LoadMovie(_movieName, _dataSource);
            MissionScreen.AddLayer(GauntletLayer);
            ScreenManager.TrySetFocus(GauntletLayer);
            PauseGame();
        }

        public void DeactivateMenu()
        {
            _dataSource?.CloseMenu();
        }
        protected void OnCloseMenu()
        {
            IsActivated = false;
            _dataSource.OnFinalize();
            _dataSource = null;
            GauntletLayer.InputRestrictions.ResetInputRestrictions();
            MissionScreen.RemoveLayer(GauntletLayer);
            _movie = null;
            GauntletLayer = null;
            UnpauseGame();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (IsActivated)
            {
                if (GauntletLayer.Input.IsKeyReleased(InputKey.RightMouseButton) ||
                    GauntletLayer.Input.IsHotKeyReleased("Exit"))
                    DeactivateMenu();
            }
        }

        public override void OnRemoveBehaviour()
        {
            base.OnRemoveBehaviour();

            Game.Current.GameStateManager.ActiveStateDisabledByUser = false;
        }

        private bool _oldGameStatusDisabledStatus;

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
