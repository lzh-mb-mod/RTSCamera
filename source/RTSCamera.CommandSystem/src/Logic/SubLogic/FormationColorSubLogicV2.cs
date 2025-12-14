using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCameraAgentComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using MathF = TaleWorlds.Library.MathF;

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

        public uint _allySelectedColor = new Color(0.5f, 1.0f, 0.5f).ToUnsignedInteger();
        public uint _allyTargetColor = new Color(0.3f, 0.3f, 1.0f).ToUnsignedInteger();
        public uint _mouseOverAllyColor = new Color(0.3f, 0.8f, 1.0f).ToUnsignedInteger();
        public uint _enemySelectedColor = new Color(0.98f, 0.3f, 0.9f).ToUnsignedInteger();
        public uint _enemyTargetColor = new Color(1f, 0.1f, 0.2f).ToUnsignedInteger();
        public uint _mouseOverEnemyColor = new Color(0.98f, 0.6f, 0.5f).ToUnsignedInteger();
        private readonly List<Formation> _enemyAsTargetFormations = new List<Formation>();
        private readonly List<Formation> _allyAsTargetFormations = new List<Formation>();
        private readonly List<Formation> _allySelectedFormations = new List<Formation>();
        private readonly Stack<Agent> _agentsWithEmptyFormations = new Stack<Agent>();
        private readonly List<List<bool>> _isFormationDirty = new List<List<bool>>();

        private OrderController PlayerOrderController => Mission.Current.PlayerTeam?.PlayerOrderController;
        private Formation _mouseOverFormation;
        private MissionGauntletSingleplayerOrderUIHandler _orderUiHandler;
        private readonly CommandSystemConfig _config = CommandSystemConfig.Get();

        private bool _isShowIndicatorDown;

        private bool _isOrderShown;
        private bool _isFreeCamera;
        //private bool HighlightEnabled => (_config.SelectedFormationHighlightMode >= ShowMode.FreeCameraOnly || _config.TargetFormationHighlightMode >= ShowMode.FreeCameraOnly) && _isOrderShown && _config.ShouldHighlightWithOutline();
        private bool HighlightEnabledForSelectedFormation => _isOrderShown && (!_isFreeCamera && HighlightEnabledInCharacterMode() || (_isFreeCamera && HighlightEnabledInRtsMode()));
        private bool HighlightEnabledForTargetFormation => _isOrderShown && (!_isFreeCamera && HighlightEnabledInCharacterMode() || (_isFreeCamera && HighlightEnabledInRtsMode()));

        private bool ShouldForceHightlightFormation => _isShowIndicatorDown && (!_isFreeCamera && HighlightEnabledInCharacterMode() && _config.HighlightTroopsWhenShowingIndicators == ShowMode.Always || (_isFreeCamera && HighlightEnabledInRtsMode() && _config.HighlightTroopsWhenShowingIndicators >= ShowMode.FreeCameraOnly));

        private bool ShouldHighlightFormation => _isOrderShown && (!_isFreeCamera && HighlightEnabledInCharacterMode() || (_isFreeCamera && HighlightEnabledInRtsMode()));
        private bool ShouldMouseOverFormation => _isOrderShown && MouseOverEnabled();

        public Func<bool> HighlightEnabledInCharacterMode { get; }
        public Func<bool> HighlightEnabledInRtsMode { get; }
        public Func<bool> MouseOverEnabled { get; }
        public Action<Agent, int, uint?, bool, bool> SetAgentColor { get; }
        public Action<Agent> ClearAgentHighlight { get; }
        public Action<Agent> UpdateAgentColor { get; }
        public Action<Formation> ClearTargetOrSelectedFormationColor { get; }
        public Action<Formation> UpdateFormationColor { get; }

        private readonly Queue<Action> _actionQueue = new Queue<Action>();

        public FormationColorSubLogicV2(Func<bool> highlightEnabledInCharacterMode, Func<bool> highlightEnabledInRtsMode, Func<bool> mouseOverEnabled, Action<Agent, int, uint?, bool, bool> setAgentColor, Action<Agent> clearAgentHighlight, Action<Agent> updateAgentColor, Action<Formation> clearTargetOrSelectedFormationColor, Action<Formation> updateFormationColor)
        {
            HighlightEnabledInCharacterMode = highlightEnabledInCharacterMode;
            HighlightEnabledInRtsMode = highlightEnabledInRtsMode;
            MouseOverEnabled = mouseOverEnabled;
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
            _enemyAsTargetFormations.Clear();
            _allyAsTargetFormations.Clear();
            _allySelectedFormations.Clear();
            _isFormationDirty.Clear();
            _agentsWithEmptyFormations.Clear();
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            Mission.Current.Teams.OnPlayerTeamChanged -= Mission_OnPlayerTeamChanged;
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;

        }

        public void OnShowIndicatorKeyDownUpdate(bool isShowIndicatorDown)
        {
            _isShowIndicatorDown = isShowIndicatorDown;
            if (ShouldForceHightlightFormation)
            {
                HighlightAllFormations();
            }
            else
            {
                if (ShouldHighlightFormation)
                {
                    SetFocusColor();
                }
                else
                {
                    ClearAllySelectedColor();
                    ClearEnemyFocusColor();
                    ClearAllyAsTargetColor();
                }
            }
        }

        private void HighlightAllFormations()
        {
            foreach (var team in Mission.Current.Teams)
            {
                foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
                {
                    bool isEnemy = Utility.IsEnemy(formation);
                    if (isEnemy)
                    {
                        SetFormationAsTargetColor(formation, isEnemy);
                    }
                    else
                    {
                        SetFormationSelectedColor(formation, isEnemy);
                    }
                }
            }
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            _isFreeCamera = freeCamera;
            if (_isOrderShown && !ShouldForceHightlightFormation)
            {
                bool shouldHighlight = _isFreeCamera && HighlightEnabledInRtsMode() || !_isFreeCamera && HighlightEnabledInCharacterMode();
                if (shouldHighlight)
                {
                    SetFocusColor();
                }
                else
                {
                    ClearAllySelectedColor();
                    ClearEnemyFocusColor();
                    ClearAllyAsTargetColor();
                }
            }
        }

        private void ClearAllyContour()
        {
            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationFocusColor(formation);
            }

            _allySelectedFormations.Clear();
        }

        public void OnPreDisplayMissionTick(float dt)
        {
            try
            {
                bool noAction = _actionQueue.IsEmpty();
                while (!_actionQueue.IsEmpty())
                    _actionQueue.Dequeue()?.Invoke();
                

                while (_agentsWithEmptyFormations.Count > 0)
                {
                    var agent = _agentsWithEmptyFormations.Pop();
                    if (agent.Formation != null && IsFormationDirty(agent.Formation))
                        continue;
                    UpdateAgentColor(agent);
                }
                var teamCount = MathF.Min(Mission.Current.Teams.Count, _isFormationDirty.Count);
                for (var teamIndex = 0; teamIndex < teamCount; ++teamIndex)
                {
                    var team = Mission.Current.Teams[teamIndex];
                    for (var formationIndex = 0; formationIndex < team.FormationsIncludingSpecialAndEmpty.Count; ++formationIndex)
                    {
                        var formation = team.FormationsIncludingSpecialAndEmpty[formationIndex];
                        if (_isFormationDirty[teamIndex][formationIndex])
                        {
                            _isFormationDirty[teamIndex][formationIndex] = false;
                            UpdateFormationColor(team.FormationsIncludingSpecialAndEmpty[formationIndex]);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Utility.DisplayMessageForced(e.ToString());
            }
        }

        private bool IsFormationDirty(Formation formation)
        {
            if (formation.Team.TeamIndex < _isFormationDirty.Count)
                return false;
            return _isFormationDirty[formation.Team.TeamIndex][formation.Index];
        }

        private void SetFormationDirty(Formation formation, bool isFormationDirty)
        {
            if (formation.Team.TeamIndex >= _isFormationDirty.Count)
                return;
            _isFormationDirty[formation.Team.TeamIndex][formation.Index] = isFormationDirty;
        }

        public void AfterAddTeam(Team team)
        {
            if (team.TeamIndex >= _isFormationDirty.Count)
            {
                for (int i = _isFormationDirty.Count; i <= team.TeamIndex; i++)
                {
                    var list = new List<bool>();
                    for (int j = 0; j < team.FormationsIncludingSpecialAndEmpty.Count; j++)
                    {
                        list.Add(false);
                    }
                    _isFormationDirty.Add(list);
                }
            }
            team.OnOrderIssued += OnOrderIssued;
            team.OnFormationsChanged += OnFormationsChanged;
            team.PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
        }

        public void OnUnitAdded(Formation formation, Agent agent)
        {
            SetAgentColorAccordingToFormation(agent);
            SetFormationDirty(formation, true);
            if (_agentsWithEmptyFormations.Count > 0 && _agentsWithEmptyFormations.Peek() == agent)
            {
                _agentsWithEmptyFormations.Pop();
            }
        }

        // TODO: what if the order UI is closed in last tick, and agent is removed in this tick?
        // Selected formations will be updated in this tick and will not refresh the removed agent.
        // So the agent will keep the highlight until it is assigned to another formation or the formation is highlighted again.
        // Should we handle this edge case for all removed agent?
        public void OnUnitRemoved(Formation formation, Agent agent)
        {
            if (agent.State != AgentState.Active || Mission.Current.IsMissionEnding || !Mission.Current.IsDeploymentFinished)
                return;
            ClearAgentHighlight(agent);
            _agentsWithEmptyFormations.Push(agent);
        }

        private bool IsFormationHighlighted(Formation formation)
        {
            var highlightedFormations = _enemyAsTargetFormations.Concat(_allyAsTargetFormations).Concat(_allySelectedFormations);
            if (_mouseOverFormation != null)
                highlightedFormations.Append(_mouseOverFormation);
            return highlightedFormations.GroupBy(formation => formation).Select(g => g.Key).Contains(formation) || ShouldForceHightlightFormation;
        }

        public void OnAgentBuild(Agent agent, Banner banner)
        {
            SetAgentColorAccordingToFormation(agent);
            if (agent.Formation != null && IsFormationHighlighted(agent.Formation))
            {
                UpdateAgentColor(agent);
            }
        }

        public void OnAgentFleeing(Agent affectedAgent)
        {
            SetAgentColorAccordingToFormation(affectedAgent);
            UpdateAgentColor(affectedAgent);
        }

        private void SetAgentColorAccordingToFormation(Agent agent)
        {
            if (agent.Formation == null)
            {
                ClearAgentHighlight(agent);
            }
            else if (agent.Formation != null)
            {
                bool isEnemy = Utility.IsEnemy(agent.Formation);
                if (agent.Formation == _mouseOverFormation)
                    SetAgentMouseOverColor(agent, isEnemy);
                if (isEnemy)
                {
                    if (_enemyAsTargetFormations.Contains(agent.Formation) || ShouldForceHightlightFormation)
                        SetAgentAsTargetColor(agent, true);
                }
                else
                {
                    if (_allySelectedFormations.Contains(agent.Formation) || ShouldForceHightlightFormation)
                        SetAgentSelectedColor(agent, false);
                    if (_allyAsTargetFormations.Contains(agent.Formation))
                        SetAgentAsTargetColor(agent, false);
                }
            }
        }

        public void MouseOver(Formation formation)
        {
            if (formation == _mouseOverFormation)
                return;
            if (_mouseOverFormation != null)
                ClearFormationMouseOverColor(_mouseOverFormation);
            if (!ShouldMouseOverFormation)
                return;
            if (formation != null)
            {
                if (ShouldHighlightFormation)
                    SetFormationMouseOverColor(formation, Utility.IsEnemy(formation));
            }
        }

        public void SetEnableColorForSelectedFormation(bool enable)
        {
            if (ShouldHighlightFormation)
            {
                SetFocusColor();
            }
            else
            {
                ClearColor();
            }
        }

        private void OnToggleOrderViewEvent(MissionPlayerToggledOrderViewEvent e)
        {
            _isOrderShown = e.IsOrderEnabled;
            if (ShouldForceHightlightFormation)
                return;
            if (ShouldHighlightFormation)
            {
                SetFocusColor();
            }
            else
            {
                ClearColor();
            }
        }

        public void OnMovementOrderChanged(Formation formation)
        {
            if (!HighlightEnabledForTargetFormation && !ShouldForceHightlightFormation)
            {
                return;
            }

            if (_allySelectedFormations.Contains(formation))
            {
                ClearEnemyFocusColor();
                SetFocusColor();
            }
        }

        public void OnMovementOrderChanged(IEnumerable<Formation> appliedFormations)
        {
            if (!HighlightEnabledForTargetFormation && !ShouldForceHightlightFormation)
                return;
            if (!_allySelectedFormations.Intersect(appliedFormations).IsEmpty())
            {
                ClearEnemyFocusColor();
                SetFocusColor();
            }
        }

        private void OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
        {
            if (movementOrderTypes.FindIndex(o => o == orderType) == -1)
                return;
            OnMovementOrderChanged(appliedFormations);
        }

        private void OnFormationsChanged(Team team, Formation formation)
        {
            if (!ShouldHighlightFormation && !ShouldForceHightlightFormation)
                return;

            if (ShouldForceHightlightFormation)
            {
                HighlightAllFormations();
                return;
            }
            var mouseOverFormation = _mouseOverFormation;
            _mouseOverFormation = null;

            ClearFormationAllHighlight(formation);
            _allySelectedFormations.Remove(formation);
            SetFocusColor();
            MouseOver(mouseOverFormation);
        }

        //private void Formation_OnUnitCountChanged(Formation formation)
        //{
        //    if (!HighlightEnabled)
        //        return;

        //    var mouseOverFormation = _mouseOverFormation;
        //    _mouseOverFormation = null;
        //    ClearFormationAllContour(formation);
        //    SetFocusContour();
        //    MouseOver(mouseOverFormation);
        //}

        private void OrderController_OnSelectedFormationsChanged()
        {
            if (!ShouldHighlightFormation || ShouldForceHightlightFormation)
                return;
            SetFocusColor();
            if (_orderUiHandler == null)
                return;

            foreach (OrderTroopItemVM troop in ((MissionOrderVM)typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_orderUiHandler)).TroopController.TroopList)
            {
                troop.IsSelectable = PlayerOrderController.IsFormationSelectable(troop.Formation);
                troop.IsSelected = troop.IsSelectable && PlayerOrderController.IsFormationListening(troop.Formation);
            }
        }

        private void Mission_OnPlayerTeamChanged(Team arg1, Team arg2)
        {
            if (!ShouldHighlightFormation && !ShouldForceHightlightFormation)
                return;
            _isShowIndicatorDown = false;
            UpdateColor();
        }

        private void UpdateColor()
        {
            ClearColor();
            SetFocusColor();
        }

        private void SetFocusColor()
        {
            foreach (var formation in PlayerOrderController?.SelectedFormations ?? Enumerable.Empty<Formation>())
            {
                if (HighlightEnabledForSelectedFormation && !_allySelectedFormations.Contains(formation))
                {
                    SetFormationSelectedColor(formation, false);
                }
            }
            foreach (var formation in _allySelectedFormations)
            {
                if (!PlayerOrderController?.SelectedFormations.Contains(formation) ?? true)
                {
                    ClearFormationFocusColor(formation);
                }
            }

            _allySelectedFormations.Clear();
            if (HighlightEnabledForSelectedFormation)
            {
                _allySelectedFormations.AddRange(PlayerOrderController?.SelectedFormations ?? Enumerable.Empty<Formation>());
            }


            var enemyAsTargetFormations = PlayerOrderController?.SelectedFormations
                .Select(formation => formation.TargetFormation).Where(formation => formation != null).ToList() ?? new List<Formation>();

            foreach (var formation in enemyAsTargetFormations)
            {
                if (HighlightEnabledForTargetFormation && !_enemyAsTargetFormations.Contains(formation))
                    SetFormationAsTargetColor(formation, true);
            }
            foreach (var formation in _enemyAsTargetFormations)
            {
                if (!HighlightEnabledForTargetFormation || !enemyAsTargetFormations.Contains(formation))
                {
                    ClearFormationFocusColor(formation);
                }
            }

            _enemyAsTargetFormations.Clear();
            _enemyAsTargetFormations.AddRange(enemyAsTargetFormations);

            if (Utility.IsTeamValid(Mission.Current.PlayerEnemyTeam))
            {
                var allyAsTargetFormations = Mission.Current.PlayerEnemyTeam.FormationsIncludingSpecialAndEmpty
                    .Select(formation => formation.TargetFormation).Where(formation => formation != null).ToList();


                foreach (var formation in allyAsTargetFormations)
                {
                    if (HighlightEnabledForTargetFormation && !_allyAsTargetFormations.Contains(formation))
                        SetFormationAsTargetColor(formation, false);
                }
                foreach (var formation in _allyAsTargetFormations)
                {
                    if (!HighlightEnabledForTargetFormation || !allyAsTargetFormations.Contains(formation))
                    {
                        ClearFormationFocusColor(formation);
                    }
                }

                _allyAsTargetFormations.Clear();
                _allyAsTargetFormations.AddRange(allyAsTargetFormations);
            }

            //_allyAsTargetFormations.Clear();
            //var formations = PlayerOrderController?.SelectedFormations;
            //if (formations == null)
            //    return;
            //foreach (var formation in formations)
            //{
            //    SetFormationSelectedContour(formation, false);
            //    switch (formation.MovementOrder.OrderType)
            //    {
            //        case OrderType.ChargeWithTarget:
            //            {
            //                if (HighlightEnabledForAsTargetFormation)
            //                {
            //                    var enemyFormation = formation.MovementOrder.TargetFormation;
            //                    if (enemyFormation != null)
            //                    {
            //                        SetFormationAsTargetContour(enemyFormation, true);
            //                    }
            //                }

            //                break;
            //            }
            //            //case OrderType.Attach:
            //            //{
            //            //    var allyFormation = formation.MovementOrder.TargetFormation;
            //            //    if (allyFormation != null)
            //            //    {
            //            //        SetFormationAsTargetContour(allyFormation, false);
            //            //    }
            //            //    break;
            //            //}
            //    }
            //}

            //if (Mission.PlayerEnemyTeam == null)
            //    return;
            //foreach (var enemyFormation in Mission.PlayerEnemyTeam.FormationsIncludingSpecial)
            //{
            //    switch (enemyFormation.MovementOrder.OrderType)
            //    {
            //        case OrderType.ChargeWithTarget:
            //            {
            //                if (HighlightEnabledForAsTargetFormation)
            //                {
            //                    var targetFormation = enemyFormation.MovementOrder.TargetFormation;
            //                    if (targetFormation != null)
            //                    {
            //                        SetFormationAsTargetContour(targetFormation, false);
            //                    }
            //                }

            //                break;
            //            }
            //    }
            //}
        }

        private void ClearColor()
        {
            foreach (var formation in _enemyAsTargetFormations)
            {
                ClearFormationAllHighlight(formation);
            }

            _enemyAsTargetFormations.Clear();

            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationAllHighlight(formation);
            }

            _allySelectedFormations.Clear();

            foreach (var formation in _allyAsTargetFormations)
            {
                ClearFormationAllHighlight(formation);
            }

            _allyAsTargetFormations.Clear();

            if (_mouseOverFormation == null)
                return;
            ClearFormationAllHighlight(_mouseOverFormation);
            _mouseOverFormation = null;
        }

        private void ClearEnemyFocusColor()
        {
            foreach (var formation in _enemyAsTargetFormations)
            {
                ClearFormationFocusColor(formation);
            }

            _enemyAsTargetFormations.Clear();
        }

        private void ClearAllySelectedColor()
        {
            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationFocusColor(formation);
            }

            _allySelectedFormations.Clear();
        }

        private void ClearAllyAsTargetColor()
        {
            foreach (var formation in _allyAsTargetFormations)
            {
                ClearFormationFocusColor(formation);
            }
            _allyAsTargetFormations.Clear();
        }

        private void SetFormationMouseOverColor(Formation formation, bool isEnemy)
        {
            _mouseOverFormation = formation;
            _actionQueue.Enqueue(() =>
            {
                formation.ApplyActionOnEachUnit(agent => SetAgentMouseOverColor(agent, isEnemy));
                SetFormationDirty(formation, true);
            });
        }

        private void SetFormationAsTargetColor(Formation formation, bool isEnemy)
        {
            if (isEnemy)
                _enemyAsTargetFormations.Add(formation);
            else
                _allyAsTargetFormations.Add(formation);
            _actionQueue.Enqueue(() =>
            {
                formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetColor(agent, isEnemy));
                SetFormationDirty(formation, true);
            });
        }

        private void SetFormationSelectedColor(Formation formation, bool isEnemy)
        {
            if (!isEnemy)
            {
                _allySelectedFormations.Add(formation);
                _actionQueue.Enqueue(() =>
                {
                    formation.ApplyActionOnEachUnit(agent => SetAgentSelectedColor(agent, isEnemy));
                    SetFormationDirty(formation, true);
                });
            }
        }

        private void SetAgentMouseOverColor(Agent agent, bool enemy)
        {
            SetAgentColor(agent, (int)ColorLevel.MouseOverFormation,
                enemy ? _mouseOverEnemyColor : _mouseOverAllyColor, true, false);
        }

        //private void SetAgentMouseOverContour(Agent agent, bool enemy)
        //{
        //    agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)ColorLevel.MouseOverFormation,
        //        enemy ? _mouseOverEnemyColor : _mouseOverAllyColor, true, false);
        //}

        //private void SetAgentMouseOverGroundMarker(Agent agent, bool enemy)
        //{
        //    agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)ColorLevel.MouseOverFormation,
        //        enemy ? _mouseOverEnemyColor : _mouseOverAllyColor, true, false);
        //}
        private void SetAgentAsTargetColor(Agent agent, bool enemy)
        {
            SetAgentColor(agent, (int)ColorLevel.TargetFormation,
                enemy ? _enemyTargetColor : _allyTargetColor, true, false);
        }

        //private void SetAgentAsTargetContour(Agent agent, bool enemy)
        //{
        //    agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)ColorLevel.TargetFormation,
        //        enemy ? _enemyTargetColor : _allyTargetColor, true, false);
        //}

        //private void SetAgentAsTargetGroundMarker(Agent agent, bool enemy)
        //{
        //    agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)ColorLevel.TargetFormation,
        //        enemy ? _enemyTargetColor : _allyTargetColor, true, false);
        //}
        private void SetAgentSelectedColor(Agent agent, bool enemy)
        {
            SetAgentColor(agent, (int)ColorLevel.SelectedFormation,
                enemy ? _enemySelectedColor : _allySelectedColor, true, false);
        }

        //private void SetAgentSelectedContour(Agent agent, bool enemy)
        //{
        //    agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)ColorLevel.SelectedFormation,
        //        enemy ? _enemySelectedColor : _allySelectedColor, true, false);
        //}

        //private void SetAgentSelectedGroundMarker(Agent agent, bool enemy)
        //{
        //    agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)ColorLevel.SelectedFormation,
        //        enemy ? _enemySelectedColor : _allySelectedColor, true, false);
        //}

        private void ClearFormationMouseOverColor(Formation formation)
        {
            ClearFormationHighlight(formation, ColorLevel.MouseOverFormation);
            _mouseOverFormation = null;
        }

        private void ClearFormationFocusColor(Formation formation)
        {
            _actionQueue.Enqueue(() =>
            {
                ClearTargetOrSelectedFormationColor(formation);
                SetFormationDirty(formation, true);
            });
        }

        private void ClearFormationHighlight(Formation formation, ColorLevel level)
        {
            _actionQueue.Enqueue(() =>
            {
                formation.ApplyActionOnEachUnit(agent =>SetAgentColor(agent, (int)level, null, true, false));
                SetFormationDirty(formation, true);
            });
        }

        private void ClearFormationAllHighlight(Formation formation)
        {
            _actionQueue.Enqueue(() =>
            {
                formation.ApplyActionOnEachUnit(ClearAgentHighlight);
                SetFormationDirty(formation, true);
            });
        }
    }
}
