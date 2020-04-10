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
        private EnhancedMissionOrderUIHandler _orderUIHandler;
        private GameKeyConfigView _gameKeyConfigView;

        private Action _closeMenu;

        private SelectionOptionDataVM _playerFormation;

        public string SwitchFreeCameraString { get; } = GameTexts.FindText("str_switch_free_camera").ToString();
        public string DisableDeathString { get; } = GameTexts.FindText("str_disable_death").ToString();
        public string TogglePauseString { get; } = GameTexts.FindText("str_toggle_pause").ToString();
        public string ResetSpeedString { get; } = GameTexts.FindText("str_reset_speed").ToString();

        public string UseRealisticBlockingString { get; } = GameTexts.FindText("str_use_realistic_blocking").ToString();

        public string ChangeCombatAIString { get; } = GameTexts.FindText("str_change_combat_ai").ToString();
        public string CombatAIString { get; } = GameTexts.FindText("str_combat_ai").ToString();

        public string ConfigKeyString { get; } = GameTexts.FindText("str_gamekey_config").ToString();

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

        public void ResetSpeed()
        {
            SpeedFactor.OptionValue = 1.0f;
            _missionSpeedLogic?.ResetSpeed();
        }

        [DataSourceProperty]
        public NumericVM SpeedFactor { get; }

        [DataSourceProperty]
        public bool UseRealisticBlocking
        {
            get => this._config.UseRealisticBlocking;
            set
            {
                if (this._config.UseRealisticBlocking == value)
                    return;
                this._config.UseRealisticBlocking = value;
                ApplyUseRealisticBlocking();
                this.OnPropertyChanged(nameof(UseRealisticBlocking));
            }
        }

        [DataSourceProperty]
        public bool ChangeCombatAI
        {
            get => this._config.ChangeCombatAI;
            set
            {
                if (this._config.ChangeCombatAI == value)
                    return;
                this._config.ChangeCombatAI = value;
                this.CombatAI.IsVisible = value;
                ApplyCombatAI();
                this.OnPropertyChanged(nameof(ChangeCombatAI));
            }
        }

        [DataSourceProperty]
        public NumericVM CombatAI { get; }

        public void ConfigKey()
        {
            _gameKeyConfigView?.Activate();
        }

        private void CloseMenu()
        {
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
                    _config.PlayerFormation = i;
                    Utility.SetPlayerFormation((FormationClass)_config.PlayerFormation);
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
                _mission.Scene.SlowMotionMode ? _mission.Scene.SlowMotionFactor : 1.0f, 0.01f, 3.0f, false,
                factor => { _missionSpeedLogic.SetSlowMotionFactor(factor); });

            this.ChangeCombatAI = this._config.ChangeCombatAI;
            this.CombatAI = new NumericVM(CombatAIString, _config.CombatAI, 0, 100, true,
                combatAI =>
                {
                    this._config.CombatAI = (int)combatAI;
                    ApplyCombatAI();
                }, 1, this._config.ChangeCombatAI);

            this._gameKeyConfigView = Mission.Current.GetMissionBehaviour<GameKeyConfigView>();
        }

        private void ApplyCombatAI()
        {
            if (ChangeCombatAI)
            {
                foreach (var agent in _mission.Agents)
                {
                    AgentStatModel.SetAgentAIStat(agent, agent.AgentDrivenProperties, _config.CombatAI);
                    agent.UpdateAgentProperties();
                }
            }
            else
            {
                foreach (var agent in _mission.Agents)
                {
                    MissionGameModels.Current.AgentStatCalculateModel.InitializeAgentStats(agent, agent.SpawnEquipment,
                        agent.AgentDrivenProperties, null);
                    agent.UpdateAgentProperties();
                }
            }
        }

        private void ApplyUseRealisticBlocking()
        {
            if (UseRealisticBlocking)
            {
                foreach (var agent in _mission.Agents)
                {
                    AgentStatModel.SetUseRealisticBlocking(agent.AgentDrivenProperties, true);
                    agent.UpdateAgentProperties();
                }
            }
            else
            {
                foreach (var agent in _mission.Agents)
                {
                    agent.AgentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, agent.Controller != Agent.ControllerType.Player ? 1f : 0.0f);
                    agent.UpdateAgentProperties();
                }
            }
        }
    }
}
