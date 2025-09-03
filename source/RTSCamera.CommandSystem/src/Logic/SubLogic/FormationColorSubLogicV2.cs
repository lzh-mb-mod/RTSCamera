using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.AgentComponents;
using RTSCamera.CommandSystem.Config;
using RTSCameraAgentComponent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

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
            OrderType.GuardMe,
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
        private readonly List<Formation> _temporarilyUpdatedFormations = new List<Formation>();
        private OrderController PlayerOrderController => Mission.Current.PlayerTeam?.PlayerOrderController;
        private Formation _mouseOverFormation;
        private MissionGauntletSingleplayerOrderUIHandler _orderUiHandler;
        private readonly CommandSystemConfig _config = CommandSystemConfig.Get();

        private bool _isOrderShown;
        private bool _isFreeCamera;
        //private bool HighlightEnabled => (_config.SelectedFormationHighlightMode >= ShowMode.FreeCameraOnly || _config.TargetFormationHighlightMode >= ShowMode.FreeCameraOnly) && _isOrderShown && _config.ShouldHighlightWithOutline();
        private bool HighlightEnabledForSelectedFormation => _isOrderShown && (!_isFreeCamera && HighlightEnabledInCharacterMode() || (_isFreeCamera && HighlightEnabledInRtsMode()));
        private bool HighlightEnabledForTargetFormation => _isOrderShown && (!_isFreeCamera && HighlightEnabledInCharacterMode() || (_isFreeCamera && HighlightEnabledInRtsMode()));

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
            _temporarilyUpdatedFormations.Clear();
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            Mission.Current.Teams.OnPlayerTeamChanged -= Mission_OnPlayerTeamChanged;
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            _isFreeCamera = freeCamera;
            if (_isOrderShown)
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

                var list = _temporarilyUpdatedFormations.GroupBy(formation => formation).Select(grouping => grouping.Key).ToList();
                var additionalFormationToUpdate = list.FirstOrDefault();

                if (!noAction)
                {
                    foreach (var group in _enemyAsTargetFormations.Concat(_allyAsTargetFormations)
                             .Concat(_allySelectedFormations).GroupBy(formation => formation))
                    {
                        if (group.Key != additionalFormationToUpdate && group.Key != null)
                        {
                            UpdateFormationColor(group.Key);
                        }
                    }
                }

                if (additionalFormationToUpdate != null)
                {
                    _temporarilyUpdatedFormations.RemoveAll(f => f == additionalFormationToUpdate);
                    UpdateFormationColor(additionalFormationToUpdate);
                }
            }
            catch (Exception e)
            {
                Utility.DisplayMessageForced(e.ToString());
            }
        }

        public void AfterAddTeam(Team team)
        {
            team.OnOrderIssued += OnOrderIssued;
            team.OnFormationsChanged += OnFormationsChanged;
            team.PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
            //foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
            //{
            //    formation.OnUnitCountChanged += Formation_OnUnitCountChanged;
            //}
        }

        public void OnAgentBuild(Agent agent, Banner banner)
        {
            if (agent.Formation != null)
            {
                bool isEnemy = Utility.IsEnemy(agent.Formation);
                if (agent.Formation == _mouseOverFormation)
                    SetAgentMouseOverColor(agent, isEnemy);
                if (isEnemy)
                {
                    if (_enemyAsTargetFormations.Contains(agent.Formation))
                        SetAgentAsTargetColor(agent, true);
                }
                else
                {
                    if (_allySelectedFormations.Contains(agent.Formation))
                        SetAgentSelectedColor(agent, false);
                    if (_allyAsTargetFormations.Contains(agent.Formation))
                        SetAgentAsTargetColor(agent, false);
                }
            }
        }

        public void OnAgentFleeing(Agent affectedAgent)
        {
            ClearAgentHighlight(affectedAgent);
            UpdateAgentColor(affectedAgent);
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
            _config.ClickToSelectFormation = enable;
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
            if (!HighlightEnabledForTargetFormation)
            {
                return;
            }

            if (_allySelectedFormations.Contains(formation))
            {
                ClearEnemyFocusColor();
                SetFocusColor();
            }
        }

        private void OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
        {
            if (!HighlightEnabledForTargetFormation || movementOrderTypes.FindIndex(o => o == orderType) == -1)
                return;
            if (!_allySelectedFormations.Intersect(appliedFormations).IsEmpty())
            {
                ClearEnemyFocusColor();
                SetFocusColor();
            }
        }

        private void OnFormationsChanged(Team team, Formation formation)
        {
            if (!ShouldHighlightFormation)
                return;
            var mouseOverFormation = _mouseOverFormation;
            _mouseOverFormation = null;

            ClearFormationAllHighlight(formation);
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
            if (!ShouldHighlightFormation)
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
            if (!ShouldHighlightFormation)
                return;
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
                _temporarilyUpdatedFormations.Add(formation);
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
                _temporarilyUpdatedFormations.Add(formation);
            });
        }

        private void ClearFormationHighlight(Formation formation, ColorLevel level)
        {
            _actionQueue.Enqueue(() =>
            {
                formation.ApplyActionOnEachUnit(agent =>SetAgentColor(agent, (int)level, null, true, false));
                _temporarilyUpdatedFormations.Add(formation);
            });
        }

        private void ClearFormationAllHighlight(Formation formation)
        {
            _actionQueue.Enqueue(() =>
            {
                formation.ApplyActionOnEachUnit(ClearAgentHighlight);
                _temporarilyUpdatedFormations.Add(formation);
            });
        }
    }
}
