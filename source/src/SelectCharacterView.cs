using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;

namespace RTSCamera
{
    public class SelectCharacterView : MissionView
    {
        private bool _isSelectingCharacter = false;
        private Agent _mouseOverAgent = null;
        private Agent _selectedAgent = null;
        private static readonly uint MouseOverColor = new Color(0.3f, 1.0f, 1.0f).ToUnsignedInteger();
        private static readonly uint SelectedColor = new Color(0.2f, 0.5f, 1.0f).ToUnsignedInteger();
        private static readonly uint EnemyMouseOverColor = new Color(0.98f, 0.6f, 0.5f).ToUnsignedInteger();
        private static readonly uint EnemySelectedColor = new Color(0.98f, 0.2f, 0.3f).ToUnsignedInteger();
        private readonly GameKeyConfig _gameKeyConfig = GameKeyConfig.Get();
        private GauntletLayer _gauntletLayer;
        private SelectCharacterVM _dataSource;
        private ControlTroopLogic _controlTroopLogic;
        private SwitchFreeCameraLogic _switchFreeCameraLogic;
        private FlyCameraMissionView _flyCameraMissionView;
        private SwitchTeamLogic _switchTeamLogic;

        public bool IsSelectingCharacter
        {
            get => _isSelectingCharacter;
            set
            {
                if (_isSelectingCharacter == value)
                    return;
                _isSelectingCharacter = value;
                if (_isSelectingCharacter)
                {
                    GameTexts.SetVariable("KeyName",
                        Utility.TextForKey(GameKeyConfig.Get().GetKey(GameKeyEnum.ControlTroop)));
                    _dataSource.SelectCharacterHintString = GameTexts.FindText("str_em_select_character_hint").ToString();
                }
                ScreenManager.SetSuspendLayer(_gauntletLayer, !_isSelectingCharacter);
                if (_isSelectingCharacter)
                {
                    ScreenManager.TrySetFocus(_gauntletLayer);
                }
                else
                {
                    MouseOverAgent = null;
                    SelectedAgent = null;
                }
            }
        }

        public Agent MouseOverAgent
        {
            get => _mouseOverAgent;
            set
            {
                if (_mouseOverAgent == value)
                    return;
                _mouseOverAgent?.GetComponent<AgentContourComponent>()?.SetContourColor(3, null, false);
                _mouseOverAgent = value;
                SetMouseOver(value);
            }
        }

        public Agent SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                if (_selectedAgent == value)
                    return;
                _selectedAgent?.GetComponent<AgentContourComponent>()?.SetContourColor(2, null, false);
                _selectedAgent = value;
                SetSelected(value);
            }
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            ViewOrderPriorty = 26;
            _gauntletLayer = new GauntletLayer(this.ViewOrderPriorty) { IsFocusLayer = false };
            _dataSource = new SelectCharacterVM();
            _gauntletLayer.LoadMovie(nameof(SelectCharacterView), _dataSource);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            MissionScreen.AddLayer(_gauntletLayer);
            ScreenManager.SetSuspendLayer(_gauntletLayer, true);

            _controlTroopLogic = Mission.GetMissionBehaviour<ControlTroopLogic>();
            _switchFreeCameraLogic = Mission.GetMissionBehaviour<SwitchFreeCameraLogic>();
            _flyCameraMissionView = Mission.GetMissionBehaviour<FlyCameraMissionView>();
            _switchTeamLogic = Mission.GetMissionBehaviour<SwitchTeamLogic>();
            if (_switchTeamLogic != null)
                _switchTeamLogic.PostSwitchTeam += OnPostSwitchTeam;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            _dataSource.OnFinalize();
            _dataSource = null;
            _gauntletLayer.InputRestrictions.ResetInputRestrictions();
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            if (_switchTeamLogic != null)
                _switchTeamLogic.PostSwitchTeam -= OnPostSwitchTeam;
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);

            agent.AddComponent(new AgentContourComponent(agent));
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.SelectCharacter)) ||
                IsSelectingCharacter && (_gauntletLayer.Input.IsKeyPressed(_gameKeyConfig.GetKey(GameKeyEnum.SelectCharacter)) ||
                                         _gauntletLayer.Input.IsKeyPressed(InputKey.RightMouseButton) ||
                                         _gauntletLayer.Input.IsKeyPressed(InputKey.Escape)))
            {
                IsSelectingCharacter = !IsSelectingCharacter;
            }

            if (IsSelectingCharacter)
            {
                SelectCharacter();
            }

            if (_gauntletLayer.Input.IsKeyPressed(InputKey.LeftMouseButton))
            {
                SelectedAgent = MouseOverAgent;
            }
        }

        private void OnPostSwitchTeam()
        {
            SetMouseOver(MouseOverAgent);
            SetSelected(SelectedAgent);
        }

        private void SetMouseOver(Agent agent)
        {
            agent?.GetComponent<AgentContourComponent>()?.SetContourColor(3, Utility.IsEnemy(agent) ? EnemyMouseOverColor : MouseOverColor, true);
        }

        private void SetSelected(Agent agent)
        {
            agent?.GetComponent<AgentContourComponent>()?.SetContourColor(2, Utility.IsEnemy(agent) ? EnemySelectedColor : SelectedColor, true);
        }

        private void SelectCharacter()
        {
            MissionScreen.ScreenPointToWorldRay(Input.GetMousePositionRanged(), out var rayBegin, out var rayEnd);
            var agent = Mission.RayCastForClosestAgent(rayBegin, rayEnd, out var distance, -1, 0.3f);
            if (agent != null && agent.IsMount)
                agent = agent.RiderAgent ?? null;
            MouseOverAgent = agent;
        }
    }
}
