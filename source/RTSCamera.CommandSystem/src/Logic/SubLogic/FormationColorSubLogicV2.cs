using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCameraAgentComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;

namespace RTSCamera.CommandSystem.Logic.SubLogic
{
    public class FormationColorSubLogicV2
    {
        private static readonly OrderType[] movementOrderTypes =
        {
            OrderType.Move,
            OrderType.MoveToLineSegment,
            OrderType.MoveToLineSegmentWithHorizontalLayout,
            OrderType.Charge,
            OrderType.ChargeWithTarget,
            OrderType.StandYourGround,
            OrderType.FollowMe,
            OrderType.FollowEntity,
            OrderType.Retreat,
            OrderType.AdvanceTenPaces,
            OrderType.FallBackTenPaces,
            OrderType.Advance,
            OrderType.FallBack,
            OrderType.AttackEntity
        };

        public uint _invisibleGroundMarkerColor = new Color(0.0f, 0.0f, 0.0f, 0.0f).ToUnsignedInteger();

        public enum FormationRoleType
        {
            PlayerTeam,
            PlayerAllyTeam,
            EnemyTeam,
            Neutral,
        }
        public enum FormationColorType
        {
            Normal,
            Highlight,
            Targeted,
            MouseOver,
            MouseOverHighlight,
            MouseOverTargeted,
        }

        public enum FormationColorWithTeam
        {
            Normal,
            PlayerTeamHighlight,
            PlayerTeamTargeted,
            PlayerTeamMouseOver,
            PlayerTeamMouseOverHighlight,
            PlayerTeamMouseOverTargeted,
            AllyTeamHighlight,
            AllyTeamTargeted,
            AllyTeamMouseOver,
            AllyTeamMouseOverHighlight,
            AllyTeamMouseOverTargeted,
            EnemyTeamHighlight,
            EnemyTeamTargeted,
            EnemyTeamMouseOver,
            EnemyTeamMouseOverHighlight,
            EnemyTeamMouseOverTargeted,
            NeutralHighlight,
            NeutralTargeted,
            NeutralMouseOver,
            NeutralMouseOverHighlight,
            NeutralMouseOverTargeted
        }


        // highlight, targeted, mouse over
        // highlight overwrites targeted
        public class FormationColorStatus
        {
            public bool IsSelected { get; set; }
            public bool IsTargeted;
            public bool IsMouseOver;
            public bool IsDirty;
            private FormationColorType _formationColorType;

            public void UpdateFormationColorType(FormationColorSubLogicV2 logic)
            {
                var newColorType = GetFormationColorType(logic);
                IsDirty |= _formationColorType != newColorType;
                _formationColorType = newColorType;
            }

            public void Select(FormationColorSubLogicV2 logic, bool selected)
            {
                IsSelected = selected;
                UpdateFormationColorType(logic);
            }

            public void Target(FormationColorSubLogicV2 logic, bool targeted)
            {
                IsTargeted = targeted;
                UpdateFormationColorType(logic);
            }

            public void MouseOver(FormationColorSubLogicV2 logic, bool mouseOver)
            {
                IsMouseOver = mouseOver;
                UpdateFormationColorType(logic);
            }

            public FormationColorType GetFormationColorType(FormationColorSubLogicV2 logic)
            {
                bool mouseOver = IsMouseOver && logic.ShouldMouseOverFormationWithShowingOrder;
                bool isHighlighted = IsSelected && logic.ShouldHighlightWhenShowingOrder || logic.ShouldHighlightWhenShowingIndicator;
                bool isTargeted = IsTargeted && logic.ShouldHighlightWhenShowingOrder;
                if (isHighlighted)
                {
                    if (mouseOver)
                        return FormationColorType.MouseOverHighlight;
                    return FormationColorType.Highlight;
                }
                if (isTargeted)
                {
                    if (mouseOver)
                        return FormationColorType.MouseOverTargeted;
                    return FormationColorType.Targeted;
                }
                if (mouseOver)
                    return FormationColorType.MouseOver;
                return FormationColorType.Normal;
            }

            public FormationColorWithTeam GetFormationColorResult(FormationColorSubLogicV2 logic, FormationRoleType roleType)
            {
                var focusColor = GetFormationColorType(logic);
                if (roleType == FormationRoleType.PlayerTeam)
                {
                    if (focusColor == FormationColorType.Normal)
                        return FormationColorWithTeam.Normal;
                    if (focusColor == FormationColorType.MouseOver)
                        return FormationColorWithTeam.PlayerTeamMouseOver;
                    if (focusColor == FormationColorType.Highlight)
                        return FormationColorWithTeam.PlayerTeamHighlight;
                    if (focusColor == FormationColorType.MouseOverHighlight)
                        return FormationColorWithTeam.PlayerTeamMouseOverHighlight;
                    if (focusColor == FormationColorType.Targeted)
                        return FormationColorWithTeam.PlayerTeamTargeted;
                    if (focusColor == FormationColorType.MouseOverTargeted)
                        return FormationColorWithTeam.PlayerTeamMouseOverTargeted;
                }
                else if (roleType == FormationRoleType.PlayerAllyTeam)
                {
                    if (focusColor == FormationColorType.Normal)
                        return FormationColorWithTeam.Normal;
                    if (focusColor == FormationColorType.MouseOver)
                        return FormationColorWithTeam.AllyTeamMouseOver;
                    if (focusColor == FormationColorType.Highlight)
                        return FormationColorWithTeam.AllyTeamHighlight;
                    if (focusColor == FormationColorType.MouseOverHighlight)
                        return FormationColorWithTeam.AllyTeamMouseOverHighlight;
                    if (focusColor == FormationColorType.Targeted)
                        return FormationColorWithTeam.AllyTeamTargeted;
                    if (focusColor == FormationColorType.MouseOverTargeted)
                        return FormationColorWithTeam.AllyTeamMouseOverTargeted;
                }
                else if (roleType == FormationRoleType.EnemyTeam)
                {
                    if (focusColor == FormationColorType.Normal)
                        return FormationColorWithTeam.Normal;
                    if (focusColor == FormationColorType.MouseOver)
                        return FormationColorWithTeam.EnemyTeamMouseOver;
                    if (focusColor == FormationColorType.Highlight)
                        return FormationColorWithTeam.EnemyTeamHighlight;
                    if (focusColor == FormationColorType.MouseOverHighlight)
                        return FormationColorWithTeam.EnemyTeamMouseOverHighlight;
                    if (focusColor == FormationColorType.Targeted)
                        return FormationColorWithTeam.EnemyTeamTargeted;
                    if (focusColor == FormationColorType.MouseOverTargeted)
                        return FormationColorWithTeam.EnemyTeamMouseOverTargeted;
                }
                else if (roleType == FormationRoleType.Neutral)
                {
                    if (focusColor == FormationColorType.Normal)
                        return FormationColorWithTeam.Normal;
                    if (focusColor == FormationColorType.MouseOver)
                        return FormationColorWithTeam.NeutralMouseOver;
                    if (focusColor == FormationColorType.Highlight)
                        return FormationColorWithTeam.NeutralHighlight;
                    if (focusColor == FormationColorType.MouseOverHighlight)
                        return FormationColorWithTeam.NeutralMouseOverHighlight;
                    if (focusColor == FormationColorType.Targeted)
                        return FormationColorWithTeam.NeutralTargeted;
                    if (focusColor == FormationColorType.MouseOverTargeted)
                        return FormationColorWithTeam.NeutralMouseOverTargeted;
                }

                return FormationColorWithTeam.Normal;
            }

            public uint? GetFormationColorResultInt(FormationColorSubLogicV2 logic, FormationRoleType roleType)
            {
                switch (GetFormationColorResult(logic, roleType))
                {
                    case  FormationColorWithTeam.Normal:
                        return null;
                    case FormationColorWithTeam.PlayerTeamMouseOver:
                    case FormationColorWithTeam.PlayerTeamMouseOverTargeted:
                        return new Color(0.65f, 0.90f, 1f).ToUnsignedInteger();
                    case FormationColorWithTeam.PlayerTeamHighlight:
                        return new Color(0.0f, 0.6f, 1f).ToUnsignedInteger();
                    case FormationColorWithTeam.PlayerTeamTargeted:
                        return new Color(0.1f, 0.1f, 0.9f).ToUnsignedInteger();
                    case FormationColorWithTeam.PlayerTeamMouseOverHighlight:
                        return new Color(0.1f, 0.82f, 0.86f).ToUnsignedInteger();
                    case FormationColorWithTeam.AllyTeamMouseOver:
                    case FormationColorWithTeam.AllyTeamMouseOverTargeted:
                        return new Color(0.8f, 0.85f, 0.50f).ToUnsignedInteger();
                    case FormationColorWithTeam.AllyTeamHighlight:
                        return new Color(0.1f, 0.62f, 0.25f).ToUnsignedInteger();
                    case FormationColorWithTeam.AllyTeamTargeted:
                        return new Color(0.5f, 0.1f, 0.8f).ToUnsignedInteger();
                    case FormationColorWithTeam.AllyTeamMouseOverHighlight:
                        return new Color(0.5f, 1.0f, 0.6f).ToUnsignedInteger();
                    case FormationColorWithTeam.EnemyTeamMouseOver:
                        return new Color(0.80f, 0.54f, 0.45f).ToUnsignedInteger();
                    case FormationColorWithTeam.EnemyTeamHighlight:
                    case FormationColorWithTeam.EnemyTeamTargeted:
                        return new Color(0.62f, 0.09f, 0.05f).ToUnsignedInteger();
                    case FormationColorWithTeam.EnemyTeamMouseOverHighlight:
                    case FormationColorWithTeam.EnemyTeamMouseOverTargeted:
                        //return new Color(0.89f, 0.45f, 0.5f).ToUnsignedInteger();
                        return new Color(0.89f, 0.4f, 0.1f).ToUnsignedInteger();
                    case FormationColorWithTeam.NeutralMouseOver:
                    case FormationColorWithTeam.NeutralMouseOverTargeted:
                        return new Color(0.9f, 0.9f, 0.9f).ToUnsignedInteger();
                    case FormationColorWithTeam.NeutralHighlight:
                        return new Color(0.5f, 0.5f, 0.5f).ToUnsignedInteger();
                    case FormationColorWithTeam.NeutralMouseOverHighlight:
                        return new Color(0.7f, 0.7f, 0.7f).ToUnsignedInteger();
                    case FormationColorWithTeam.NeutralTargeted:
                        return new Color(0.3f, 0.3f, 0.9f).ToUnsignedInteger();
                }
                return null;
            }
        }

        //public uint _playerTeamHighlightColor = new Color(0.4f, 0.8f, 0.4f).ToUnsignedInteger();
        //public uint _playerTeamTargetedColor = new Color(0.3f, 0.3f, 0.9f).ToUnsignedInteger();
        //public uint _playerTeamMouseOverColor = new Color(0.27f, 0.72f, 0.9f).ToUnsignedInteger();
        //public uint _playerTeamMouseOverHighlightColor = new Color(0.27f, 0.72f, 0.9f).ToUnsignedInteger();
        //public uint _playerTeamMouseOverTargetedColor = new Color(0.27f, 0.72f, 0.9f).ToUnsignedInteger();
        //public uint _allyHighlightColor = new Color(0.4f, 0.8f, 0.4f).ToUnsignedInteger();
        //public uint _allyTargetedColor = new Color(0.3f, 0.3f, 0.9f).ToUnsignedInteger();
        //public uint _allyMouseOverColor = new Color(0.27f, 0.72f, 0.9f).ToUnsignedInteger();
        //public uint _allyMouseOverHighlightColor = new Color(0.27f, 0.72f, 0.9f).ToUnsignedInteger();
        //public uint _allyMouseOverTargetedColor = new Color(0.27f, 0.72f, 0.9f).ToUnsignedInteger();
        //public uint _enemyHighlightColor = new Color(0.89f, 0.27f, 0.81f).ToUnsignedInteger();
        //public uint _enemyTargetedColor = new Color(0.9f, 0.09f, 0.18f).ToUnsignedInteger();
        //public uint _enemyMouseOverColor = new Color(0.89f, 0.54f, 0.45f).ToUnsignedInteger();
        //public uint _enemyMouseOverHighlightColor = new Color(0.89f, 0.54f, 0.45f).ToUnsignedInteger();
        //public uint _enemyMouseOverTargetedColor = new Color(0.89f, 0.54f, 0.45f).ToUnsignedInteger();
        //public uint _neutralHighlightColor = new Color(1, 1, 1).ToUnsignedInteger();
        //public uint _neutralTargetedColor = new Color(1, 1, 1).ToUnsignedInteger();
        //public uint _neutralMouseOverColor = new Color(1, 1, 1).ToUnsignedInteger();
        //public uint _neutralMouseOverHighlightColor = new Color(1, 1, 1).ToUnsignedInteger();
        //public uint _neutralMouseOverTargetedColor = new Color(1, 1, 1).ToUnsignedInteger();
        private readonly Stack<Agent> _agentsNewlyAddedToFormations = new Stack<Agent>();
        private readonly List<Agent> _agentsRemovedFromFormations = new List<Agent>();
        private readonly List<Agent> _agentsWithEmptyFormations = new List<Agent>();
        private readonly Dictionary<Formation, FormationColorStatus> _formationColorStatusDictionary = new Dictionary<Formation, FormationColorStatus>();
        private readonly FormationColorStatus _colorStatusOfNoFormationAgents = new FormationColorStatus();

        private OrderController PlayerOrderController => Mission.Current.PlayerTeam?.PlayerOrderController;
        private Formation _mouseOverFormation;
        private MissionGauntletSingleplayerOrderUIHandler _orderUiHandler;
        private readonly CommandSystemConfig _config = CommandSystemConfig.Get();

        private bool _isShowIndicatorDown;

        private bool _isOrderShown;
        private bool _isFreeCamera;

        private bool ShouldHighlightWhenShowingIndicator => _isShowIndicatorDown &&( !_isFreeCamera && HighlightEnabledInCharacterMode() && _config.HighlightTroopsWhenShowingIndicators == ShowMode.Always || (_isFreeCamera && HighlightEnabledInRtsMode() && _config.HighlightTroopsWhenShowingIndicators >= ShowMode.FreeCameraOnly));

        private bool ShouldHighlightWhenShowingOrder => (_isOrderShown || Mission.Current?.Mode == MissionMode.Deployment) && (!_isFreeCamera && HighlightEnabledInCharacterMode() || (_isFreeCamera && HighlightEnabledInRtsMode()));
        private bool ShouldMouseOverFormationWithShowingOrder => ShouldHighlightWhenShowingOrder && MouseOverEnabled();

        public Func<bool> HighlightEnabledInCharacterMode { get; }
        public Func<bool> HighlightEnabledInRtsMode { get; }
        public Func<bool> MouseOverEnabled { get; }
        public Func<bool> ShouldHighlightAgentWithoutFormation { get; }
        public Action<Agent, int, uint?, bool, bool> SetAgentColor { get; }
        public Action<Agent, bool> ClearAgentHighlight { get; }
        public Action<Agent> UpdateAgentColor { get; }
        public Action<Formation> ClearTargetOrSelectedFormationColor { get; }
        public Action<Formation> UpdateFormationColor { get; }

        private readonly Queue<Action> _actionQueue = new Queue<Action>();

        public FormationColorSubLogicV2(Func<bool> highlightEnabledInCharacterMode,
            Func<bool> highlightEnabledInRtsMode,
            Func<bool> mouseOverEnabled,
            Func<bool> shouldHighlightAgentWithoutFormation,
            Action<Agent, int, uint?, bool, bool> setAgentColor,
            Action<Agent, bool> clearAgentHighlight,
            Action<Agent> updateAgentColor,
            Action<Formation> clearTargetOrSelectedFormationColor,
            Action<Formation> updateFormationColor)
        {
            HighlightEnabledInCharacterMode = highlightEnabledInCharacterMode;
            HighlightEnabledInRtsMode = highlightEnabledInRtsMode;
            MouseOverEnabled = mouseOverEnabled;
            ShouldHighlightAgentWithoutFormation = shouldHighlightAgentWithoutFormation;
            SetAgentColor = setAgentColor;
            ClearAgentHighlight = clearAgentHighlight;
            UpdateAgentColor = updateAgentColor;
            ClearTargetOrSelectedFormationColor = clearTargetOrSelectedFormationColor;
            UpdateFormationColor = updateFormationColor;
        }

        public void OnBehaviourInitialize()
        {
            Mission.Current.Teams.OnPlayerTeamChanged += Mission_OnPlayerTeamChanged;
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            _orderUiHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
        }

        public void OnRemoveBehaviour()
        {
            _actionQueue.Clear();
            _agentsNewlyAddedToFormations.Clear();
            _agentsRemovedFromFormations.Clear();
            _agentsWithEmptyFormations.Clear();
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            Mission.Current.Teams.OnPlayerTeamChanged -= Mission_OnPlayerTeamChanged;
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;

        }

        public void OnShowIndicatorKeyDownUpdate(bool isShowIndicatorDown)
        {
            _isShowIndicatorDown = isShowIndicatorDown;
            UpdateAllFormationColorTypes();
        }

        private void UpdateAllFormationColorTypes()
        {
            foreach (var team in Mission.Current.Teams)
            {
                foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
                {
                    GetFormationColorStatus(formation).UpdateFormationColorType(this);
                }
            }
            if (ShouldHighlightAgentWithoutFormation())
            {
                _colorStatusOfNoFormationAgents.UpdateFormationColorType(this);
            }
        }

        private FormationColorStatus GetFormationColorStatus(Formation formation)
        {
            if (!_formationColorStatusDictionary.TryGetValue(formation, out var colorStatus))
            {
                colorStatus = new FormationColorStatus();
                _formationColorStatusDictionary.Add(formation, colorStatus);
            }
            return colorStatus;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            _isFreeCamera = freeCamera;
            {
                foreach (var team in Mission.Current.Teams)
                {
                    foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
                    {
                        GetFormationColorStatus(formation).UpdateFormationColorType(this);
                    }
                }
                if (ShouldHighlightAgentWithoutFormation())
                {
                    _colorStatusOfNoFormationAgents.UpdateFormationColorType(this);
                }
            }
        }

        public void OnPreDisplayMissionTick(float dt)
        {
            try
            {
                while (_agentsRemovedFromFormations.Count > 0)
                {
                    var agent = _agentsRemovedFromFormations[_agentsRemovedFromFormations.Count - 1];
                    _agentsRemovedFromFormations.RemoveAt(_agentsRemovedFromFormations.Count - 1);
                    if (agent.Formation != null && IsFormationDirty(agent.Formation))
                        continue;

                    ClearAgentHighlight(agent, true);
                }

                while (_agentsNewlyAddedToFormations.Count > 0)
                {
                    var agent = _agentsNewlyAddedToFormations.Pop();
                    SetAgentColorAccordingToFormation(agent, true);
                }

                var dirtyFormationStatus = _formationColorStatusDictionary.Where(pair => pair.Value.IsDirty);
                foreach (var pair in dirtyFormationStatus)
                {
                    pair.Value.IsDirty = false;
                    var color = pair.Value.GetFormationColorResultInt(this, GetRoleType(pair.Key));
                    pair.Key.ApplyActionOnEachUnit(agent => SetAgentColor(agent, (int)ColorLevel.MouseOverFormation, color, true, true));
                }
                if (ShouldHighlightAgentWithoutFormation())
                {
                    if (_colorStatusOfNoFormationAgents.IsDirty)
                    {
                        _colorStatusOfNoFormationAgents.IsDirty = false;
                        foreach (var agent in _agentsWithEmptyFormations)
                        {
                            var color = _colorStatusOfNoFormationAgents.GetFormationColorResultInt(this, GetRoleType(agent));
                            bool isRunnigAway = agent.IsRunningAway || (agent.CommonAIComponent?.IsRetreating ?? false);
                            if (color != null && isRunnigAway)
                            {
                                color = Vec3.Lerp(Color.FromUint(color.Value).ToVec3(), Color.White.ToVec3(), 0.5f).ToARGB;
                            }
                            SetAgentColor(agent, (int)ColorLevel.MouseOverFormation, color, true, true);
                        }
                    }
                }
                else
                {
                    foreach (var agent in _agentsWithEmptyFormations)
                    {
                        ClearAgentHighlight(agent, true);
                    }
                    _agentsWithEmptyFormations.Clear();
                }
            }
            catch (Exception e)
            {
                Utility.DisplayMessageForced(e.ToString());
            }
        }

        private bool IsFormationDirty(Formation formation)
        {
            return _formationColorStatusDictionary.TryGetValue(formation, out var colorStatus) && colorStatus.IsDirty;
        }

        public void AfterAddTeam(Team team)
        {
            team.OnOrderIssued += OnOrderIssued;
            team.PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
        }

        public void OnUnitAdded(Formation formation, Agent agent)
        {
            if (_agentsRemovedFromFormations.Count > 0 && _agentsRemovedFromFormations[_agentsRemovedFromFormations.Count - 1] == agent)
            {
                _agentsRemovedFromFormations.RemoveAt(_agentsRemovedFromFormations.Count - 1);
            }
            _agentsNewlyAddedToFormations.Push(agent);

            if (ShouldHighlightAgentWithoutFormation())
            {
                _agentsWithEmptyFormations.Remove(agent);
            }
        }

        public void OnUnitRemoved(Formation formation, Agent agent)
        {
            if (agent.State != AgentState.Active || Mission.Current.IsMissionEnding/* || !Mission.Current.IsDeploymentFinished*/)
                return;
            if (_agentsNewlyAddedToFormations.Count > 0 && _agentsNewlyAddedToFormations.Peek() == agent)
            {
                _agentsNewlyAddedToFormations.Pop();
            }
            _agentsRemovedFromFormations.Add(agent);

            if (ShouldHighlightAgentWithoutFormation())
            {
                _agentsWithEmptyFormations.Add(agent);
                _colorStatusOfNoFormationAgents.IsDirty = true;
            }
        }

        public void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!agent.IsHuman)
                return;

            if (agent.Formation == null)
            {
                if (ShouldHighlightAgentWithoutFormation())
                {
                    _agentsWithEmptyFormations.Add(agent);
                    _colorStatusOfNoFormationAgents.IsDirty = true;
                }
                ClearAgentHighlight(agent, true);
                return;
            }

            if (_formationColorStatusDictionary.TryGetValue(agent.Formation, out var colorStatus))
            {
                if (colorStatus.IsDirty)
                    return;
                var color = colorStatus.GetFormationColorResultInt(this, GetRoleType(agent.Formation));
                if (color.HasValue)
                {
                    SetAgentColor(agent, (int)ColorLevel.MouseOverFormation, color, true, true);
                }
            }
        }

        public void OnAgentFleeing(Agent affectedAgent)
        {
            if (!affectedAgent.IsHuman)
                return;

            //if (ShouldHighlightAgentWithoutFormation())
            //{
            //    _agentsWithEmptyFormations.Remove(affectedAgent);
            //}
            //SetAgentColorAccordingToFormation(affectedAgent, true);
            //ClearAgentHighlight(affectedAgent, true);
            if (affectedAgent.Formation == null)
            {
                _colorStatusOfNoFormationAgents.IsDirty = true;
            }
        }

        public void OnAgentRemoved(Agent affectedAgent)
        {
            if (!affectedAgent.IsHuman)
                return;

            if (affectedAgent.Formation != null)
                return;

            _agentsRemovedFromFormations.Remove(affectedAgent);
            if (ShouldHighlightAgentWithoutFormation())
            {
                _agentsWithEmptyFormations.Remove(affectedAgent);
            }
        }

        private void SetAgentColorAccordingToFormation(Agent agent, bool updateInstantly)
        {
            if (agent.Formation == null)
            {
                ClearAgentHighlight(agent, updateInstantly);
                return;
            }

            if (_formationColorStatusDictionary.TryGetValue(agent.Formation, out var colorStatus))
            {
                if (colorStatus.IsDirty)
                    return;
                var color = colorStatus.GetFormationColorResultInt(this, GetRoleType(agent.Formation));
                SetAgentColor(agent, (int)ColorLevel.MouseOverFormation, color, true, updateInstantly);
            }
        }

        public void MouseOver(Formation formation)
        {
            if (formation == _mouseOverFormation)
                return;
            if (_mouseOverFormation != null)
            {
                GetFormationColorStatus(_mouseOverFormation).MouseOver(this, false);
            }
            _mouseOverFormation = formation;
            if (formation != null)
                GetFormationColorStatus(formation).MouseOver(this, true);
        }

        public void OnMouseOverEnabledChanged(bool enable)
        {
            UpdateAllFormationColorTypes();
        }

        private void OnToggleOrderViewEvent(MissionPlayerToggledOrderViewEvent e)
        {
            _isOrderShown = e.IsOrderEnabled;
            //OnSelectedFormationsChanged();
            UpdateAllFormationColorTypes();
        }

        public void OnMovementOrderChanged(Formation formation)
        {
            RefreshTargetedFormations();
        }

        private void  RefreshTargetedFormations()
        {
            var targetedFormations = PlayerOrderController?.SelectedFormations
                .Select(formation => formation.TargetFormation).Where(formation => formation != null).ToList() ?? new List<Formation>();
            if (Utility.IsTeamValid(Mission.Current.PlayerEnemyTeam))
            {
                targetedFormations.AddRange(Mission.Current.PlayerEnemyTeam.FormationsIncludingSpecialAndEmpty
                    .Select(formation => formation.TargetFormation).Where(formation => formation != null));
            }
            foreach (var team in Mission.Current.Teams)
            {
                foreach (var f in team.FormationsIncludingSpecialAndEmpty)
                {
                    GetFormationColorStatus(f).Target(this, targetedFormations.Contains(f));
                }
            }
        }

        public void OnMovementOrderChanged(IEnumerable<Formation> appliedFormations)
        {
            RefreshTargetedFormations();
        }

        private void OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
        {
            if (movementOrderTypes.FindIndex(o => o == orderType) == -1)
                return;
            OnMovementOrderChanged(appliedFormations);
        }

        private void OrderController_OnSelectedFormationsChanged()
        {
            OnSelectedFormationsChanged();

            // TODO: verify whether the following is needed.
            //if (_orderUiHandler == null)
            //    return;

            //foreach (OrderTroopItemVM troop in ((MissionOrderVM)typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_orderUiHandler)).TroopController.TroopList)
            //{
            //    troop.IsSelectable = PlayerOrderController.IsFormationSelectable(troop.Formation);
            //    troop.IsSelected = troop.IsSelectable && PlayerOrderController.IsFormationListening(troop.Formation);
            //}
        }

        private void OnSelectedFormationsChanged()
        {
            var selectedFormations = PlayerOrderController?.SelectedFormations ?? new List<Formation>();

            foreach (var team in Mission.Current.Teams)
            {
                foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
                {
                    GetFormationColorStatus(formation).Select(this, selectedFormations.Contains(formation));
                }
            }
            RefreshTargetedFormations();
        }

        private void Mission_OnPlayerTeamChanged(Team arg1, Team arg2)
        {
            OnSelectedFormationsChanged();
        }

        private static FormationRoleType GetRoleType(Formation formation)
        {
            var team = formation.Team;
            if (team == null)
            {
                return FormationRoleType.Neutral;
            }
            if (team.IsPlayerTeam)
            {
                return FormationRoleType.PlayerTeam;
            }
            if (team.IsPlayerAlly)
            {
                return FormationRoleType.PlayerAllyTeam;
            }
            if (Utility.IsEnemy(formation))
            {
                return FormationRoleType.EnemyTeam;
            }
            return FormationRoleType.Neutral;
        }

        private static FormationRoleType GetRoleType(Agent agent)
        {
            var team = agent.Team;
            if (team == null)
            {
                if (Mission.Current.PlayerTeam == null || !Mission.Current.PlayerTeam.IsValid || Mission.Current.PlayerTeam.ActiveAgents.Count == 0)
                    return FormationRoleType.Neutral;
                if (agent.IsEnemyOf(Mission.Current.PlayerTeam.ActiveAgents[0]))
                {
                    return FormationRoleType.EnemyTeam;
                }
                else if (agent.IsFriendOf(Mission.Current.PlayerTeam.ActiveAgents[0]))
                {
                    return FormationRoleType.PlayerAllyTeam;
                }
                else
                {
                    return FormationRoleType.Neutral;
                }
            }
            if (team.IsPlayerTeam)
            {
                return FormationRoleType.PlayerTeam;
            }
            if (team.IsPlayerAlly)
            {
                return FormationRoleType.PlayerAllyTeam;
            }
            if (Utility.IsEnemy(agent))
            {
                return FormationRoleType.EnemyTeam;
            }
            return FormationRoleType.Neutral;
        }
    }
}
