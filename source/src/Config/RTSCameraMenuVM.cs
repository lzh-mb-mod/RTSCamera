using System;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using RTSCamera.Patch;
using RTSCamera.Patch.CircularFormation;
using RTSCamera.View;
using RTSCamera.View.Basic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Config
{
    public class RTSCameraMenuVM : MissionMenuVMBase
    {
        private readonly RTSCameraConfig _config;
        private readonly Mission _mission;
        private readonly RTSCameraLogic _rtsCameraLogic;
        private readonly SwitchFreeCameraLogic _switchFreeCameraLogic;
        private readonly SwitchTeamLogic _switchTeamLogic;
        private readonly MissionSpeedLogic _missionSpeedLogic;
        private readonly RTSCameraGameKeyConfigView _gameKeyConfigView;
        private readonly HideHUDView _hideHudView;
        private readonly FormationColorMissionView _contourView;
        private readonly RTSCameraSelectCharacterView _selectCharacterView;

        private SelectionOptionDataVM _playerFormation;
        private SelectionOptionDataVM _watchAnotherHero;

        public string TitleString { get; } = GameTexts.FindText("str_rts_camera_mod_name").ToString();

        public string UseFreeCameraByDefaultString { get; } = GameTexts.FindText("str_rts_camera_use_free_camera_by_default").ToString();
        public string SwitchFreeCameraString { get; } = GameTexts.FindText("str_rts_camera_switch_free_camera").ToString();

        public string AlwaysSetPlayerFormationString { get; } = GameTexts.FindText("str_rts_camera_always_set_player_formation").ToString();

        public string TogglePauseString { get; } = GameTexts.FindText("str_rts_camera_toggle_pause").ToString();

        public string ConstantSpeedString { get; } = GameTexts.FindText("str_rts_camera_constant_speed").ToString();
        public string OutdoorString { get; } = GameTexts.FindText("str_rts_camera_outdoor").ToString();
        public string RestrictByBoundariesString { get; } = GameTexts.FindText("str_rts_camera_restrict_by_boundaries").ToString();

        public string SlowMotionModeString { get; } = GameTexts.FindText("str_rts_camera_slow_motion_mode").ToString();

        public string ClickToSelectFormationString { get; } = GameTexts.FindText("str_rts_camera_click_to_select_formation").ToString();

        public string AttackSpecificFormationString { get; } =
            GameTexts.FindText("str_rts_camera_attack_specific_formation").ToString();

        public string FixCircularArrangementString { get; } =
            GameTexts.FindText("str_rts_camera_fix_circular_arrangement").ToString();

        public string DisplayMessageString { get; } = GameTexts.FindText("str_rts_camera_display_mod_message").ToString();

        public string ToggleUIString { get; } = GameTexts.FindText("str_rts_camera_toggle_ui").ToString();

        public string ConfigKeyString { get; } = GameTexts.FindText("str_rts_camera_gamekey_config").ToString();

        public string ControlAllyOptionsDescriptionString { get; } =
            GameTexts.FindText("str_rts_camera_control_ally_options_description").ToString();

        public string SelectCharacterString { get; } = GameTexts.FindText("str_rts_camera_select_character").ToString();

        public string ControlAllyAfterDeathString { get; } =
            GameTexts.FindText("str_rts_camera_control_ally_after_death").ToString();

        public string PreferToControlCompanionsString { get; } =
            GameTexts.FindText("str_rts_camera_prefer_to_control_companions").ToString();

        public string ControlTroopsInPlayerPartyOnlyString { get; } =
            GameTexts.FindText("str_rts_camera_control_troops_in_player_party_only").ToString();

        public string UnbalancedOptionsDescriptionString { get; } =
            GameTexts.FindText("str_rts_camera_unbalanced_options_description").ToString();
        public string DisableDeathString { get; } = GameTexts.FindText("str_rts_camera_disable_death").ToString();

        public string DisableDeathHotkeyEnabledString { get; } = GameTexts.FindText("str_rts_camera_disable_death_hotkey_enabled").ToString();

        public string SwitchTeamString { get; } = GameTexts.FindText("str_rts_camera_switch_team").ToString();

        public string SwitchTeamHotkeyEnabledString { get; } = GameTexts.FindText("str_rts_camera_switch_team_hotkey_enabled").ToString();


        public void SwitchFreeCamera()
        {
            _switchFreeCameraLogic?.SwitchCamera();
            CloseMenu();
        }

        public HintViewModel SwitchFreeCameraHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_switch_free_camera_hint").ToString());

        [DataSourceProperty] public bool SwitchFreeCameraEnabled => _switchFreeCameraLogic != null;

        [DataSourceProperty]
        public bool UseFreeCameraByDefault
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

        public HintViewModel UseFreeCameraByDefaultHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_use_free_camera_by_default_hint").ToString());

        [DataSourceProperty]
        public NumericVM RaisedHeight { get; }

        public HintViewModel RaisedHeightHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_raised_height_hint").ToString());

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

        public HintViewModel PlayerFormationHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_player_formation_hint").ToString());

        [DataSourceProperty]
        public bool AlwaysSetPlayerFormation
        {
            get => _config.AlwaysSetPlayerFormation;
            set
            {
                if (_config.AlwaysSetPlayerFormation == value)
                    return;
                _config.AlwaysSetPlayerFormation = value;
                var formationClass = (FormationClass)PlayerFormation.Selector.SelectedIndex;
                _switchFreeCameraLogic.CurrentPlayerFormation = formationClass;
                Utility.SetPlayerFormation(formationClass);
                OnPropertyChanged(nameof(AlwaysSetPlayerFormation));
            }
        }

        public HintViewModel AlwaysSetPlayerFormationHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_always_set_player_formation_hint").ToString());

        [DataSourceProperty] public bool AdjustSpeedEnabled => _missionSpeedLogic != null;

        public void TogglePause()
        {
            _missionSpeedLogic?.TogglePause();
            CloseMenu();
        }

        public HintViewModel TogglePauseHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_toggle_pause_hint").ToString());


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

        public HintViewModel SlowMotionModeHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_slow_motion_hint").ToString());

        [DataSourceProperty]
        public NumericVM SpeedFactor { get; }

        public HintViewModel SlowMotionFactorHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_slow_motion_factor_hint").ToString());

        [DataSourceProperty]
        public bool ConstantSpeed
        {
            get => _config.ConstantSpeed;
            set
            {
                _config.ConstantSpeed = value;
                var view = _mission.GetMissionBehaviour<FlyCameraMissionView>();
                if (view != null)
                    view.ConstantSpeed = value;
            }
        }

        public HintViewModel ConstantSpeedHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_constant_speed_hint").ToString());

        [DataSourceProperty]
        public bool Outdoor
        {
            get => _config.Outdoor;
            set
            {
                _config.Outdoor = value;
                var view = _mission.GetMissionBehaviour<FlyCameraMissionView>();
                if (view != null)
                    view.Outdoor = value;
            }
        }

        public HintViewModel OutdoorHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_outdoor_hint").ToString());

        [DataSourceProperty]
        public bool RestrictByBoundaries
        {
            get => _config.RestrictByBoundaries;
            set
            {
                _config.RestrictByBoundaries = value;
                var view = _mission.GetMissionBehaviour<FlyCameraMissionView>();
                if (view != null)
                    view.RestrictByBoundaries = value;
            }
        }

        public HintViewModel RestrictByBoundariesHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_restricted_by_boundaries_hint").ToString());

        [DataSourceProperty]
        public bool ClickToSelectFormation
        {
            get => _config.ClickToSelectFormation;
            set
            {
                if (_config.ClickToSelectFormation == value)
                    return;
                _contourView?.SetEnableContourForSelectedFormation(value);
                OnPropertyChanged(nameof(ClickToSelectFormation));
            }
        }

        public HintViewModel ClickToSelectFormationHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_click_to_select_formation_hint").ToString());

        [DataSourceProperty]
        public bool AttackSpecificFormation
        {
            get => _config.AttackSpecificFormation;
            set
            {
                if (_config.AttackSpecificFormation == value)
                    return;
                _config.AttackSpecificFormation = value;
                if (_config.AttackSpecificFormation)
                {
                    PatchChargeToFormation.Patch();
                }
                else
                {
                    PatchChargeToFormation.UnPatch();
                }
                OnPropertyChanged(nameof(AttackSpecificFormation));
            }
        }

        public HintViewModel AttackSpecificFormationHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_attack_specific_formation_hint").ToString());

        [DataSourceProperty]
        public bool FixCircularArrangement
        {
            get => _config.FixCircularArrangement;
            set
            {
                if (_config.FixCircularArrangement == value)
                    return;
                _config.FixCircularArrangement = value;
                if (_config.FixCircularArrangement)
                {
                    PatchCircularFormation.Patch();
                }
                else
                {
                    PatchCircularFormation.UnPatch();
                }
                OnPropertyChanged(nameof(FixCircularArrangement));
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

        public HintViewModel DisplayMessageHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_display_message_hint").ToString());

        public void ToggleUI()
        {
            _hideHudView?.ToggleUI();
            CloseMenu();
        }

        public HintViewModel ToggleUIHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_toggle_ui_hint").ToString());

        [DataSourceProperty]
        public MBBindingList<ExtensionVM> Extensions { get; }

        public void ConfigKey()
        {
            _gameKeyConfigView?.Activate();
        }

        public HintViewModel ConfigKeyHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_config_key_hint").ToString());

        [DataSourceProperty]
        public SelectionOptionDataVM WatchAnotherHero
        {
            get => _watchAnotherHero;
            set
            {
                if (_watchAnotherHero == value)
                    return;
                _watchAnotherHero = value;
                OnPropertyChanged(nameof(_watchAnotherHero));
            }
        }

        public HintViewModel WatchAnotherHeroHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_watch_another_hero_hint").ToString());

        public void SelectCharacter()
        {
            _selectCharacterView.IsSelectingCharacter = true;
            CloseMenu();
        }

        public HintViewModel SelectCharacterHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_select_character_hint").ToString());

        [DataSourceProperty]
        public bool ControlAllyAfterDeath
        {
            get => _config.ControlAllyAfterDeath;
            set
            {
                if (_config.ControlAllyAfterDeath == value)
                    return;
                _config.ControlAllyAfterDeath = !_config.ControlAllyAfterDeath;
                OnPropertyChanged(nameof(ControlAllyAfterDeath));
            }
        }

        public HintViewModel ControlAllyAfterDeathHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_control_ally_after_death_hint").ToString());

        [DataSourceProperty]
        public bool PreferToControlCompanions
        {
            get => _config.PreferToControlCompanions;
            set
            {
                if (_config.PreferToControlCompanions == value)
                    return;
                _config.PreferToControlCompanions = !_config.PreferToControlCompanions;
                OnPropertyChanged(nameof(PreferToControlCompanions));
            }
        }

        public HintViewModel PreferToControlCompanionsHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_prefer_to_control_companions_hint").ToString());

        [DataSourceProperty]
        public bool ControlTroopsInPlayerPartyOnly
        {
            get => _config.ControlTroopsInPlayerPartyOnly;
            set
            {
                if (_config.ControlTroopsInPlayerPartyOnly == value)
                    return;
                _config.ControlTroopsInPlayerPartyOnly = !_config.ControlTroopsInPlayerPartyOnly;
                OnPropertyChanged(nameof(ControlTroopsInPlayerPartyOnly));
            }
        }

        public HintViewModel ControlTroopsInPlayerPartyOnlyHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_control_troops_in_player_party_only_hint").ToString());

        public bool CheatEnabled => NativeConfig.CheatMode;

        [DataSourceProperty]
        public bool DisableDeath
        {
            get => _config.DisableDeath;
            set
            {
                if (_config.DisableDeath == value)
                    return;
                _config.DisableDeath = value;
                _rtsCameraLogic.DisableDeathLogic.SetDisableDeath(_config.DisableDeath);
                OnPropertyChanged(nameof(DisableDeath));
            }
        }

        public HintViewModel DisableDeathHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_disable_death_hint").ToString());

        [DataSourceProperty]
        public bool DisableDeathHotkeyEnabled
        {
            get => _config.DisableDeathHotkeyEnabled;
            set
            {
                if (_config.DisableDeathHotkeyEnabled == value)
                    return;
                _config.DisableDeathHotkeyEnabled = value;
                OnPropertyChanged(nameof(DisableDeathHotkeyEnabled));
            }
        }

        public void SwitchTeam()
        {
            _switchTeamLogic?.SwapTeam();
            CloseMenu();
        }

        public HintViewModel SwitchTeamHint { get; } =
            new HintViewModel(GameTexts.FindText("str_rts_camera_switch_team_hint").ToString());

        [DataSourceProperty]
        public bool SwitchTeamHotkeyEnabled
        {
            get => _config.SwitchTeamHotkeyEnabled;
            set
            {
                if (_config.SwitchTeamHotkeyEnabled == value)
                    return;
                _config.SwitchTeamHotkeyEnabled = value;
                OnPropertyChanged(nameof(SwitchTeamHotkeyEnabled));

            }
        }

        public override void CloseMenu()
        {
            _config.Serialize();
            _hideHudView?.EndTemporarilyOpenUI();
            base.CloseMenu();
        }

        public RTSCameraMenuVM(Mission mission, Action closeMenu)
            : base(closeMenu)
        {
            _config = RTSCameraConfig.Get();
            _mission = mission;
            _rtsCameraLogic = _mission.GetMissionBehaviour<RTSCameraLogic>();
            _switchFreeCameraLogic = _rtsCameraLogic.SwitchFreeCameraLogic;
            _switchTeamLogic = _rtsCameraLogic.SwitchTeamLogic;
            PlayerFormation = new SelectionOptionDataVM(new SelectionOptionData(
                i =>
                {
                    if ((i != _config.PlayerFormation || _config.AlwaysSetPlayerFormation) &&
                        i > 0 && i < (int)FormationClass.NumberOfAllFormations)
                    {
                        _config.PlayerFormation = i;
                        _switchFreeCameraLogic.CurrentPlayerFormation = (FormationClass) i;
                        Utility.SetPlayerFormation((FormationClass) i);
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
                    new SelectionItem(true, "str_troop_group_name", "7")
                }), GameTexts.FindText("str_rts_camera_player_formation"));
            RaisedHeight =
                new NumericVM(GameTexts.FindText("str_rts_camera_raised_height_after_switching_to_free_camera").ToString(),
                    _config.RaisedHeight, 0.0f, 50f, true,
                    height => _config.RaisedHeight = height);
            _missionSpeedLogic = _rtsCameraLogic.MissionSpeedLogic;
            SpeedFactor = new NumericVM(GameTexts.FindText("str_rts_camera_slow_motion_factor").ToString(),
                _mission.Scene.SlowMotionFactor, 0.01f, 3.0f, false,
                factor => { _missionSpeedLogic.SetSlowMotionFactor(factor); });
            _contourView = _mission.GetMissionBehaviour<FormationColorMissionView>();
            _hideHudView = Mission.Current.GetMissionBehaviour<HideHUDView>();

            Extensions = new MBBindingList<ExtensionVM>();
            foreach (var extension in RTSCameraExtension.Extensions)
            {
                Extensions.Add(new ExtensionVM(extension.ButtonName, () => extension.OpenExtensionMenu(_mission)));
            }
            _gameKeyConfigView = Mission.Current.GetMissionBehaviour<RTSCameraGameKeyConfigView>();

            _selectCharacterView = Mission.Current.GetMissionBehaviour<RTSCameraSelectCharacterView>();
            if (_selectCharacterView == null)
                return;

            WatchAnotherHero = new SelectionOptionDataVM(
                new WatchAgentSelectionData(_selectCharacterView.MissionScreen).SelectionOptionData,
                GameTexts.FindText("str_rts_camera_watch_another_hero"));
        }
    }
}
