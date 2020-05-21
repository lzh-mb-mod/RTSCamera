using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{

    public class MissionMenuVM : MissionMenuVMBase
    {
        private readonly EnhancedMissionConfig _config;
        private readonly Mission _mission;
        private readonly SwitchFreeCameraLogic _switchFreeCameraLogic;
        private readonly MissionSpeedLogic _missionSpeedLogic;
        private readonly GameKeyConfigView _gameKeyConfigView;
        private readonly HideHUDLogic _hideHudLogic;

        private SelectionOptionDataVM _playerFormation;
        private SelectionOptionDataVM _controlAnotherHero;

        public string UseFreeCameraByDefaultString { get; } = GameTexts.FindText("str_em_use_free_camera_by_default").ToString();
        public string SwitchFreeCameraString { get; } = GameTexts.FindText("str_em_switch_free_camera").ToString();
        public string DisableDeathString { get; } = GameTexts.FindText("str_em_disable_death").ToString();
        public string TogglePauseString { get; } = GameTexts.FindText("str_em_toggle_pause").ToString();
        public string SlowMotionModeString { get; } = GameTexts.FindText("str_em_slow_motion_mode").ToString();

        public string ControlAlliesOptionsDescriptionString { get; } =
            GameTexts.FindText("str_em_control_allies_options_description").ToString();

        public string ControlAlliesAfterDeathString { get; } =
            GameTexts.FindText("str_em_control_allies_after_death").ToString();

        public string PreferToControlCompanionsString { get; } =
            GameTexts.FindText("str_em_prefer_to_control_companions").ToString();

        public string ControlTroopsInPlayerPartyOnlyString { get; } =
            GameTexts.FindText("str_em_control_troops_in_player_party_only").ToString();

        public string DisplayMessageString { get; } = GameTexts.FindText("str_em_display_mod_message").ToString();

        public string ToggleUIString { get; } = GameTexts.FindText("str_em_toggle_ui").ToString();

        public string ConfigKeyString { get; } = GameTexts.FindText("str_em_gamekey_config").ToString();


        [DataSourceProperty] public bool UseFreeCameraByDefault
        {
            get => _config.UseFreeCameraByDefault;
            set
            {
                if (_config.UseFreeCameraByDefault == value)
                    return;
                _config.UseFreeCameraByDefault = value;
                OnPropertyChanged(nameof(UseFreeCameraByDefault));
            }
        }

        public void SwitchFreeCamera()
        {
            _switchFreeCameraLogic?.SwitchCamera();
            CloseMenu();
        }

        [DataSourceProperty] public bool SwitchFreeCameraEnabled => _switchFreeCameraLogic != null;

        [DataSourceProperty]
        public bool DisableDeath
        {
            get => _config.DisableDeath;
            set
            {
                if (_config.DisableDeath == value)
                    return;
                _config.DisableDeath = !_config.DisableDeath;
                _mission.GetMissionBehaviour<DisableDeathLogic>()?.SetDisableDeath(_config.DisableDeath);
                this.OnPropertyChanged(nameof(DisableDeath));
            }
        }

        [DataSourceProperty]
        public NumericVM RaisedHeight { get; }

        [DataSourceProperty]
        public SelectionOptionDataVM PlayerFormation
        {
            get => _playerFormation;
            set
            {
                if (_playerFormation == value)
                    return;
                _playerFormation = value;
                OnPropertyChanged(nameof(PlayerFormation));
            }
        }

        public void TogglePause()
        {
            _missionSpeedLogic?.TogglePause();
            CloseMenu();
        }

        [DataSourceProperty] public bool AdjustSpeedEnabled => this._missionSpeedLogic != null;


        [DataSourceProperty]
        public bool SlowMotionMode
        {
            get => _mission.Scene.SlowMotionMode;
            set
            {
                if (_mission.Scene.SlowMotionMode == value)
                    return;
                _missionSpeedLogic?.SetSlowMotionMode(value);
                OnPropertyChanged(nameof(SlowMotionMode));
            }
        }

        [DataSourceProperty]
        public NumericVM SpeedFactor { get; }

        [DataSourceProperty]
        public bool ControlAlliesAfterDeath
        {
            get => _config.ControlAlliesAfterDeath;
            set
            {
                if (_config.ControlAlliesAfterDeath == value)
                    return;
                _config.ControlAlliesAfterDeath = !_config.ControlAlliesAfterDeath;
                this.OnPropertyChanged(nameof(ControlAlliesAfterDeath));
            }
        }

        [DataSourceProperty]
        public bool PreferToControlCompanions
        {
            get => _config.PreferToControlCompanions;
            set
            {
                if (_config.PreferToControlCompanions == value)
                    return;
                _config.PreferToControlCompanions = !_config.PreferToControlCompanions;
                this.OnPropertyChanged(nameof(PreferToControlCompanions));
            }
        }

        [DataSourceProperty]
        public bool ControlTroopsInPlayerPartyOnly
        {
            get => _config.ControlTroopsInPlayerPartyOnly;
            set
            {
                if (_config.ControlTroopsInPlayerPartyOnly == value)
                    return;
                _config.ControlTroopsInPlayerPartyOnly = !_config.ControlTroopsInPlayerPartyOnly;
                this.OnPropertyChanged(nameof(ControlTroopsInPlayerPartyOnly));
            }
        }

        [DataSourceProperty]
        public SelectionOptionDataVM ControlAnotherHero
        {
            get => _controlAnotherHero;
            set
            {
                if (_controlAnotherHero == value)
                    return;
                _controlAnotherHero = value;
                OnPropertyChanged(nameof(_controlAnotherHero));
            }
        }

        [DataSourceProperty]
        public bool DisplayMessage
        {
            get => _config.DisplayMessage;
            set
            {
                if (_config.DisplayMessage == value)
                    return;
                _config.DisplayMessage = value;
                OnPropertyChanged(nameof(DisplayMessage));
            }
        }

        public void ToggleUI()
        {
            _hideHudLogic?.ToggleUI();
            CloseMenu();
        }

        [DataSourceProperty]
        public MBBindingList<ExtensionVM> Extensions { get; }

        public void ConfigKey()
        {
            _gameKeyConfigView?.Activate();
        }

        public override void CloseMenu()
        {
            _config.Serialize();
            _hideHudLogic?.EndTemporarilyOpenUI();
            base.CloseMenu();
        }

        public MissionMenuVM(Mission mission, Action closeMenu)
            : base(closeMenu)
        {
            this._config = EnhancedMissionConfig.Get();
            this._mission = mission;
            this._switchFreeCameraLogic = _mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            this.PlayerFormation = new SelectionOptionDataVM(new SelectionOptionData(
                (int i) =>
                {
                    if (i != _config.PlayerFormation)
                    {
                        _config.PlayerFormation = i;
                        Utility.SetPlayerFormation((FormationClass) _config.PlayerFormation);
                    }
                }, () => _config.PlayerFormation,
                (int)FormationClass.NumberOfRegularFormations, new[]
                {
                    new SelectionItem(true, "str_troop_group_name", "0"),
                    new SelectionItem(true, "str_troop_group_name", "1"),
                    new SelectionItem(true, "str_troop_group_name", "2"),
                    new SelectionItem(true, "str_troop_group_name", "3"),
                    new SelectionItem(true, "str_troop_group_name", "4"),
                    new SelectionItem(true, "str_troop_group_name", "5"),
                    new SelectionItem(true, "str_troop_group_name", "6"),
                    new SelectionItem(true, "str_troop_group_name", "7"),
                }), GameTexts.FindText("str_em_player_formation"));
            this.RaisedHeight =
                new NumericVM(GameTexts.FindText("str_em_raised_height_after_switching_to_free_camera").ToString(),
                    _config.RaisedHeight, 0.0f, 50f, true,
                    height => _config.RaisedHeight = height);
            this._missionSpeedLogic = _mission.GetMissionBehaviour<MissionSpeedLogic>();
            this.SpeedFactor = new NumericVM(GameTexts.FindText("str_em_slow_motion_factor").ToString(),
                _mission.Scene.SlowMotionFactor, 0.01f, 3.0f, false,
                factor => { _missionSpeedLogic.SetSlowMotionFactor(factor); });
            this._hideHudLogic = Mission.Current.GetMissionBehaviour<HideHUDLogic>();
            _hideHudLogic?.BeginTemporarilyOpenUI();

            this.ControlAnotherHero = new SelectionOptionDataVM(
                new ControlTroopsSelectionData().SelectionOptionData,
                GameTexts.FindText("str_em_control_another_hero"));

            this.Extensions = new MBBindingList<ExtensionVM>();
            foreach (var extension in EnhancedMissionExtension.Extensions)
            {
                Extensions.Add(new ExtensionVM(extension.ButtonName, () =>  extension.OpenExtensionMenu(_mission)));
            }
            this._gameKeyConfigView = Mission.Current.GetMissionBehaviour<GameKeyConfigView>();
        }
    }
}
