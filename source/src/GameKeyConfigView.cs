using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

namespace EnhancedMission
{
    class GameKeyConfigView : MissionView
    {
        private GauntletLayer _gauntletLayer;
        private GameKeyConfigVM _dataSource;
        private KeybindingPopup _keybindingPopup;
        private GameKeyOptionVM _currentGameKey;

        public GameKeyConfigView()
        {
            ViewOrderPriorty = 1000;
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _keybindingPopup = new KeybindingPopup(SetHotKey, MissionScreen);
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            _keybindingPopup.OnToggle(false);
            _keybindingPopup = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (this._gauntletLayer == null)
                return;
            if (!this._keybindingPopup.IsActive && this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                this._dataSource.ExecuteCancel();
            }
            this._keybindingPopup.Tick();
        }

        public void Activate()
        {
            _dataSource = new GameKeyConfigVM(OnKeyBindRequest, Deactivate);
            _gauntletLayer = new GauntletLayer(ViewOrderPriorty);
            _gauntletLayer.LoadMovie(nameof(GameKeyConfigView), _dataSource);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.IsFocusLayer = true;
            MissionScreen.AddLayer(_gauntletLayer);
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        public void Deactivate()
        {
            _gauntletLayer.InputRestrictions.ResetInputRestrictions();
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            this._dataSource.OnFinalize();
            this._dataSource = null;
        }

        private void OnKeyBindRequest(GameKeyOptionVM requestedHotKeyToChange)
        {
            _currentGameKey = requestedHotKeyToChange;
            _keybindingPopup.OnToggle(true);
        }

        private void SetHotKey(Key key)
        {
            //if (_dataSource.Groups.First<GameKeyGroupVM>((g => g.GameKeys.Contains(this._currentGameKey))).GameKeys.Any<GameKeyOptionVM>(keyVM => keyVM.CurrentKey.InputKey == key.InputKey))
            //    InformationManager.AddQuickInformation(new TextObject("{=n4UUrd1p}Already in use"));
            /*else*/ if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                this._currentGameKey = null;
                this._keybindingPopup.OnToggle(false);
            }
            else
            {
                this._currentGameKey?.Set(key.InputKey);
                this._currentGameKey = null;
                this._keybindingPopup.OnToggle(false);
            }
        }

    }
}
