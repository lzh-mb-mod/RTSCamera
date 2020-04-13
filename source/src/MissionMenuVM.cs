using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    public class NumericVM : ViewModel
    {

        private readonly float _initialValue;
        private float _min;
        private float _max;
        private float _optionValue;
        private bool _isDiscrete;
        private Action<float> _updateAction;
        private int _roundScale;
        private bool _isVisible;

        public NumericVM(string name, float initialValue, float min, float max, bool isDiscrete, Action<float> updateAction, int roundScale = 100, bool isVisible = true)
        {
            Name = name;
            _initialValue = initialValue;
            _min = min;
            _max = max;
            _optionValue = initialValue;
            _isDiscrete = isDiscrete;
            _updateAction = updateAction;
            _roundScale = roundScale;
            _isVisible = isVisible;
        }
        public string Name { get; }

        [DataSourceProperty]
        public float Min
        {
            get => this._min;
            set
            {
                if (Math.Abs(value - this._min) < 0.01f)
                    return;
                this._min = value;
                this.OnPropertyChanged(nameof(Min));
            }
        }

        [DataSourceProperty]
        public float Max
        {
            get => this._max;
            set
            {
                if (Math.Abs(value - this._max) < 0.01f)
                    return;
                this._max = value;
                this.OnPropertyChanged(nameof(Max));
            }
        }

        [DataSourceProperty]
        public float OptionValue
        {
            get => this._optionValue;
            set
            {
                if (Math.Abs((double)value - (double)this._optionValue) < 0.01f)
                    return;
                this._optionValue = MathF.Round(value * _roundScale) / (float)_roundScale;
                this.OnPropertyChanged(nameof(OptionValue));
                this.OnPropertyChanged(nameof(OptionValueAsString));
                this._updateAction(OptionValue);
            }
        }

        [DataSourceProperty]
        public bool IsDiscrete
        {
            get => this._isDiscrete;
            set
            {
                if (value == this._isDiscrete)
                    return;
                this._isDiscrete = value;
                this.OnPropertyChanged(nameof(IsDiscrete));
            }
        }

        [DataSourceProperty]
        public string OptionValueAsString => !this.IsDiscrete ? this._optionValue.ToString("F") : ((int)this._optionValue).ToString();



        [DataSourceProperty]
        public bool IsVisible
        {
            get => this._isVisible;
            set
            {
                if (value == this._isVisible)
                    return;
                this._isVisible = value;
                this.OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public class MissionMenuVM : ViewModel
    {
        private EnhancedMissionConfig _config;
        private Mission _mission;
        private SwitchFreeCameraLogic _switchFreeCameraLogic;
        private MissionSpeedLogic _missionSpeedLogic;
        private GameKeyConfigView _gameKeyConfigView;
        private ChangeBodyPropertiesBase _changeBodyProperties = ChangeBodyPropertiesBase.Get();

        private Action _closeMenu;

        private SelectionOptionDataVM _playerFormation;

        public string UseFreeCameraByDefaultString { get; } = GameTexts.FindText("str_use_free_camera_by_default").ToString();
        public string SwitchFreeCameraString { get; } = GameTexts.FindText("str_switch_free_camera").ToString();
        public string DisableDeathString { get; } = GameTexts.FindText("str_disable_death").ToString();
        public string TogglePauseString { get; } = GameTexts.FindText("str_toggle_pause").ToString();
        public string SlowMotionModeString { get; } = GameTexts.FindText("str_slow_motion_mode").ToString();

        public string UseRealisticBlockingString { get; } = GameTexts.FindText("str_use_realistic_blocking").ToString();

        public string ChangeMeleeAIString { get; } = GameTexts.FindText("str_change_melee_ai").ToString();
        public string MeleeAIString { get; } = GameTexts.FindText("str_melee_ai").ToString();

        public string ChangeRangedAIString { get; } = GameTexts.FindText("str_change_ranged_ai").ToString();
        public string RangedAIString { get; } = GameTexts.FindText("str_ranged_ai").ToString();

        public string ConfigKeyString { get; } = GameTexts.FindText("str_gamekey_config").ToString();

        public string DisplayMessageString { get; } = GameTexts.FindText("str_display_mod_message").ToString();

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

        [DataSourceProperty] public bool EnableChangingBodyProperties => _changeBodyProperties != null;

        [DataSourceProperty]
        public bool UseRealisticBlocking
        {
            get => _changeBodyProperties?.UseRealisticBlocking ?? false;
            set
            {
                if (_changeBodyProperties == null || _changeBodyProperties.UseRealisticBlocking == value)
                    return;
                _changeBodyProperties.UseRealisticBlocking = value;
                this.OnPropertyChanged(nameof(UseRealisticBlocking));
            }
        }

        [DataSourceProperty]
        public bool ChangeMeleeAI
        {
            get => _changeBodyProperties?.ChangeMeleeAI ?? false;
            set
            {
                if (_changeBodyProperties == null || _changeBodyProperties.ChangeMeleeAI == value)
                    return;
                _changeBodyProperties.ChangeMeleeAI = value;
                this.MeleeAI.IsVisible = value;
                OnPropertyChanged(nameof(ChangeMeleeAI));
            }
        }

        [DataSourceProperty]
        public NumericVM MeleeAI { get; }

        [DataSourceProperty]
        public bool ChangeRangedAI
        {
            get => _changeBodyProperties?.ChangeRangedAI ?? false;
            set
            {
                if (_changeBodyProperties == null || _changeBodyProperties.ChangeRangedAI == value)
                    return;
                _changeBodyProperties.ChangeRangedAI = value;
                this.RangedAI.IsVisible = value;
                OnPropertyChanged(nameof(ChangeRangedAI));
            }
        }

        [DataSourceProperty]
        public NumericVM RangedAI { get; }

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

        public void ConfigKey()
        {
            _gameKeyConfigView?.Activate();
        }

        private void CloseMenu()
        {
            _config.Serialize();
            _changeBodyProperties?.SaveConfig();
            this._closeMenu?.Invoke();
        }

        public MissionMenuVM(Action closeMenu)
        {
            this._config = EnhancedMissionConfig.Get();
            this._closeMenu = closeMenu;
            this._mission = Mission.Current;
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
                }), GameTexts.FindText("str_player_formation"));
            this._missionSpeedLogic = _mission.GetMissionBehaviour<MissionSpeedLogic>();
            this.SpeedFactor = new NumericVM(GameTexts.FindText("str_slow_motion_factor").ToString(),
                _mission.Scene.SlowMotionFactor, 0.01f, 3.0f, false,
                factor => { _missionSpeedLogic.SetSlowMotionFactor(factor); });

            this.MeleeAI = new NumericVM(MeleeAIString, _changeBodyProperties?.MeleeAI ?? 0, 0, 100, true,
                combatAI =>
                {
                    if (_changeBodyProperties == null)
                        return;
                    _changeBodyProperties.MeleeAI = (int) combatAI;
                }, 1, ChangeMeleeAI);

            this.RangedAI = new NumericVM(RangedAIString, _changeBodyProperties?.RangedAI ?? 0, 0, 100, true,
                combatAI =>
                {
                    if (_changeBodyProperties == null)
                        return;
                    _changeBodyProperties.RangedAI = (int)combatAI;
                }, 1, ChangeRangedAI);

            this._gameKeyConfigView = Mission.Current.GetMissionBehaviour<GameKeyConfigView>();
        }
    }
}
