//using MissionLibrary.Event;
//using MissionSharedLibrary.Utilities;
//using RTSCamera.CommandSystem.AgentComponents;
//using RTSCamera.CommandSystem.Config;
//using RTSCameraAgentComponent;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using TaleWorlds.Core;
//using TaleWorlds.Library;
//using TaleWorlds.MountAndBlade;
//using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
//using TaleWorlds.MountAndBlade.View;
//using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

//namespace RTSCamera.CommandSystem.Logic.SubLogic
//{
//    public class FormationColorSubLogic
//    {
//        private static readonly OrderType[] movementOrderTypes =
//        {
//            OrderType.Move,
//            OrderType.MoveToLineSegment,
//            OrderType.MoveToLineSegmentWithHorizontalLayout,
//            OrderType.Charge,
//            OrderType.ChargeWithTarget,
//            OrderType.StandYourGround,
//            OrderType.FollowMe,
//            OrderType.FollowEntity,
//            OrderType.GuardMe,
//            OrderType.Retreat,
//            OrderType.AdvanceTenPaces,
//            OrderType.FallBackTenPaces,
//            OrderType.Advance,
//            OrderType.FallBack,
//            OrderType.AttackEntity
//        };

//        public uint _invisibleGroundMarkerColor = new Color(0.0f, 0.0f, 0.0f, 0.0f).ToUnsignedInteger();

//        public uint _allySelectedColor = new Color(0.5f, 1.0f, 0.5f).ToUnsignedInteger();
//        public uint _allyTargetColor = new Color(0.3f, 0.3f, 1.0f).ToUnsignedInteger();
//        public uint _mouseOverAllyColor = new Color(0.3f, 0.8f, 1.0f).ToUnsignedInteger();
//        public uint _enemySelectedColor = new Color(0.98f, 0.3f, 0.9f).ToUnsignedInteger();
//        public uint _enemyTargetColor = new Color(1f, 0.1f, 0.2f).ToUnsignedInteger();
//        public uint _mouseOverEnemyColor = new Color(0.98f, 0.6f, 0.5f).ToUnsignedInteger();
//        private readonly List<Formation> _enemyAsTargetFormations = new List<Formation>();
//        private readonly List<Formation> _allyAsTargetFormations = new List<Formation>();
//        private readonly List<Formation> _allySelectedFormations = new List<Formation>();
//        private readonly List<Formation> _temporarilyUpdatedFormations = new List<Formation>();
//        private OrderController PlayerOrderController => Mission.Current.PlayerTeam?.PlayerOrderController;
//        private Formation _mouseOverFormation;
//        private MissionGauntletSingleplayerOrderUIHandler _orderUiHandler;
//        private readonly CommandSystemConfig _config = CommandSystemConfig.Get();

//        private bool _isOrderShown;
//        private bool _isFreeCamera;
//        //private bool HighlightEnabled => (_config.SelectedFormationHighlightMode >= ShowMode.FreeCameraOnly || _config.TargetFormationHighlightMode >= ShowMode.FreeCameraOnly) && _isOrderShown && _config.ShouldHighlightWithOutline();


//        private bool ContourStyleEnabled => _isOrderShown && (_config.TroopHighlightStyleInCharacterMode == TroopHighlightStyle.Outline || _isFreeCamera && _config.TroopHighlightStyleInRTSMode == TroopHighlightStyle.Outline);

//        private bool GroundMarkerStyleEnabled => _isOrderShown && (_config.TroopHighlightStyleInCharacterMode == TroopHighlightStyle.GroundMarker || _isFreeCamera && _config.TroopHighlightStyleInRTSMode == TroopHighlightStyle.GroundMarker);
//        //private bool HighlightEnabledForSelectedFormation => _isOrderShown && (_config.TroopHighlightStyleInCharacterMode > TroopHighlightStyle.No || (_isFreeCamera && _config.TroopHighlightStyleInCharacterMode > TroopHighlightStyle.No));
//        //private bool HighlightEnabledForTargetFormation => _isOrderShown && (_config.TroopHighlightStyleInCharacterMode > TroopHighlightStyle.No || (_isFreeCamera && _config.TroopHighlightStyleInCharacterMode > TroopHighlightStyle.No));
//        private bool ContourStyleMouseOverEnabled => ContourStyleEnabled && _config.IsMouseOverEnabled();

//        private bool GroundMarkerStyleMouseOverEnabled => GroundMarkerStyleEnabled && _config.IsMouseOverEnabled();

//        private readonly Queue<Action> _actionQueue = new Queue<Action>();

//        public void OnBehaviourInitialize()
//        {
//            Mission.Current.Teams.OnPlayerTeamChanged += Mission_OnPlayerTeamChanged;
//            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
//            _orderUiHandler = Mission.Current.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
//            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
//        }

//        public void OnRemoveBehaviour()
//        {
//            _actionQueue.Clear();
//            _enemyAsTargetFormations.Clear();
//            _allyAsTargetFormations.Clear();
//            _allySelectedFormations.Clear();
//            _temporarilyUpdatedFormations.Clear();
//            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
//            Mission.Current.Teams.OnPlayerTeamChanged -= Mission_OnPlayerTeamChanged;
//            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
//        }

//        private void OnToggleFreeCamera(bool freeCamera)
//        {
//            _isFreeCamera = freeCamera;
//            if (_isOrderShown)
//            {
//                if (_isFreeCamera)
//                {
//                    SetFocusColor();
//                }
//                else
//                {
//                    if (_config.TroopHighlightStyleInCharacterMode == TroopHighlightStyle.Outline && _config.TroopHighlightStyleInRTSMode != TroopHighlightStyle.Outline)
//                    {
//                        ClearAllySelectedColor();
//                        ClearEnemyFocusColor();
//                        ClearAllyAsTargetColor();
//                    }
//                }
//            }
//        }

//        public void OnPreDisplayMissionTick(float dt)
//        {
//            try
//            {
//                bool noAction = _actionQueue.IsEmpty();
//                while (!_actionQueue.IsEmpty())
//                    _actionQueue.Dequeue()?.Invoke();

//                var list = _temporarilyUpdatedFormations.GroupBy(formation => formation).Select(grouping => grouping.Key).ToList();
//                var additionalFormationToUpdate = list.FirstOrDefault();

//                if (!noAction)
//                {
//                    foreach (var group in _enemyAsTargetFormations.Concat(_allyAsTargetFormations)
//                             .Concat(_allySelectedFormations).GroupBy(formation => formation))
//                    {
//                        if (group.Key != additionalFormationToUpdate)
//                        {

//                            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//                            {
//                                group.Key?.ApplyActionOnEachUnit(a => a.GetComponent<RTSCameraComponent>()?.UpdateContour());
//                            }
//                            else
//                            {
//                                group.Key?.ApplyActionOnEachUnit(a => a.GetComponent<CommandSystemAgentComponent>()?.TryUpdateColor());
//                            }
//                        }
//                    }
//                }

//                if (additionalFormationToUpdate != null)
//                {
//                    _temporarilyUpdatedFormations.RemoveAll(f => f == additionalFormationToUpdate);
//                    if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//                    {
//                        additionalFormationToUpdate.ApplyActionOnEachUnit(a => a.GetComponent<RTSCameraComponent>()?.UpdateContour());
//                    }
//                    else
//                    {
//                        additionalFormationToUpdate.ApplyActionOnEachUnit(a => a.GetComponent<CommandSystemAgentComponent>()?.TryUpdateColor());
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Utility.DisplayMessageForced(e.ToString());
//            }
//        }

//        public void AfterAddTeam(Team team)
//        {
//            team.OnOrderIssued += OnOrderIssued;
//            team.OnFormationsChanged += OnFormationsChanged;
//            team.PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
//            //foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
//            //{
//            //    formation.OnUnitCountChanged += Formation_OnUnitCountChanged;
//            //}
//        }

//        public void OnAgentBuild(Agent agent, Banner banner)
//        {
//            if (agent.Formation != null)
//            {
//                if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//                {
//                    bool isEnemy = Utility.IsEnemy(agent.Formation);
//                    if (agent.Formation == _mouseOverFormation)
//                        SetAgentMouseOverContour(agent, isEnemy);
//                    if (isEnemy)
//                    {
//                        if (_enemyAsTargetFormations.Contains(agent.Formation))
//                            SetAgentAsTargetContour(agent, true);
//                    }
//                    else
//                    {
//                        if (_allySelectedFormations.Contains(agent.Formation))
//                            SetAgentSelectedContour(agent, false);
//                        if (_allyAsTargetFormations.Contains(agent.Formation))
//                            SetAgentAsTargetContour(agent, false);
//                    }
//                }
//                else
//                {
//                    bool isEnemy = Utility.IsEnemy(agent.Formation);
//                    if (agent.Formation == _mouseOverFormation)
//                        SetAgentMouseOverGroundMarker(agent, isEnemy);
//                    if (isEnemy)
//                    {
//                        if (_enemyAsTargetFormations.Contains(agent.Formation))
//                            SetAgentAsTargetGroundMarker(agent, true);
//                    }
//                    else
//                    {
//                        if (_allySelectedFormations.Contains(agent.Formation))
//                            SetAgentSelectedGroundMarker(agent, false);
//                        if (_allyAsTargetFormations.Contains(agent.Formation))
//                            SetAgentAsTargetGroundMarker(agent, false);
//                    }
//                }
//            }
//        }

//        public void OnAgentFleeing(Agent affectedAgent)
//        {
//            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//            {
//                ClearAgentFormationContour(affectedAgent);
//                affectedAgent.GetComponent<RTSCameraComponent>()?.UpdateContour();
//            }
//            else
//            {
//                ClearAgentFormationGroundMarker(affectedAgent);
//                affectedAgent.GetComponent<CommandSystemAgentComponent>()?.TryUpdateColor();
//            }
//        }

//        public void MouseOver(Formation formation)
//        {
//            if (formation == _mouseOverFormation)
//                return;
//            if (_mouseOverFormation != null)
//                ClearFormationMouseOverColor(_mouseOverFormation);
//            if (!HighlightEnabled)
//                return;
//            if (formation != null)
//            {
//                bool isEnemy = Utility.IsEnemy(formation);
//                if (isEnemy ? HighlightEnabledForTargetFormation : HighlightEnabledForSelectedFormation)
//                    SetFormationMouseOverColor(formation, Utility.IsEnemy(formation));
//            }
//        }

//        public void SetEnableColorForSelectedFormation(bool enable)
//        {
//            _config.ClickToSelectFormation = enable;
//            if (HighlightEnabled)
//            {
//                SetFocusColor();
//            }
//            else
//            {
//                ClearColor();
//            }
//        }

//        private void OnToggleOrderViewEvent(MissionPlayerToggledOrderViewEvent e)
//        {
//            _isOrderShown = e.IsOrderEnabled;
//            if (HighlightEnabled)
//            {
//                SetFocusColor();
//            }
//            else
//            {
//                ClearColor();
//            }
//        }

//        public void OnMovementOrderChanged(Formation formation)
//        {
//            if (!HighlightEnabledForTargetFormation)
//            {
//                return;
//            }

//            if (_allySelectedFormations.Contains(formation))
//            {
//                ClearEnemyFocusColor();
//                SetFocusColor();
//            }
//        }

//        private void OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
//        {
//            if (!HighlightEnabledForTargetFormation || movementOrderTypes.FindIndex(o => o == orderType) == -1)
//                return;
//            if (!_allySelectedFormations.Intersect(appliedFormations).IsEmpty())
//            {
//                ClearEnemyFocusColor();
//                SetFocusColor();
//            }
//        }

//        private void OnFormationsChanged(Team team, Formation formation)
//        {
//            if (!HighlightEnabled)
//                return;
//            var mouseOverFormation = _mouseOverFormation;
//            _mouseOverFormation = null;

//            ClearFormationAllHighlight(formation);
//            SetFocusColor();
//            MouseOver(mouseOverFormation);
//        }

//        //private void Formation_OnUnitCountChanged(Formation formation)
//        //{
//        //    if (!HighlightEnabled)
//        //        return;

//        //    var mouseOverFormation = _mouseOverFormation;
//        //    _mouseOverFormation = null;
//        //    ClearFormationAllContour(formation);
//        //    SetFocusContour();
//        //    MouseOver(mouseOverFormation);
//        //}

//        private void OrderController_OnSelectedFormationsChanged()
//        {
//            if (!HighlightEnabled)
//                return;
//            SetFocusColor();
//            if (_orderUiHandler == null)
//                return;

//            foreach (OrderTroopItemVM troop in ((MissionOrderVM)typeof(MissionGauntletSingleplayerOrderUIHandler).GetField("_dataSource", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_orderUiHandler)).TroopController.TroopList)
//            {
//                troop.IsSelectable = PlayerOrderController.IsFormationSelectable(troop.Formation);
//                troop.IsSelected = troop.IsSelectable && PlayerOrderController.IsFormationListening(troop.Formation);
//            }
//        }

//        private void Mission_OnPlayerTeamChanged(Team arg1, Team arg2)
//        {
//            if (!HighlightEnabled)
//                return;
//            UpdateColor();
//        }

//        private void UpdateColor()
//        {
//            ClearColor();
//            SetFocusColor();
//        }

//        private void SetFocusColor()
//        {
//            foreach (var formation in PlayerOrderController?.SelectedFormations ?? Enumerable.Empty<Formation>())
//            {
//                if (HighlightEnabledForSelectedFormation && !_allySelectedFormations.Contains(formation))
//                {
//                    SetFormationSelectedColor(formation, false);
//                }
//            }
//            foreach (var formation in _allySelectedFormations)
//            {
//                if (!PlayerOrderController?.SelectedFormations.Contains(formation) ?? true)
//                {
//                    ClearFormationFocusColor(formation);
//                }
//            }

//            _allySelectedFormations.Clear();
//            if (HighlightEnabledForSelectedFormation)
//            {
//                _allySelectedFormations.AddRange(PlayerOrderController?.SelectedFormations ?? Enumerable.Empty<Formation>());
//            }


//            var enemyAsTargetFormations = PlayerOrderController?.SelectedFormations
//                .Select(formation => formation.TargetFormation).Where(formation => formation != null).ToList() ?? new List<Formation>();

//            foreach (var formation in enemyAsTargetFormations)
//            {
//                if (HighlightEnabledForTargetFormation && !_enemyAsTargetFormations.Contains(formation))
//                    SetFormationAsTargetColor(formation, true);
//            }
//            foreach (var formation in _enemyAsTargetFormations)
//            {
//                if (!HighlightEnabledForTargetFormation || !enemyAsTargetFormations.Contains(formation))
//                {
//                    ClearFormationFocusColor(formation);
//                }
//            }

//            _enemyAsTargetFormations.Clear();
//            _enemyAsTargetFormations.AddRange(enemyAsTargetFormations);

//            if (Utility.IsTeamValid(Mission.Current.PlayerEnemyTeam))
//            {
//                var allyAsTargetFormations = Mission.Current.PlayerEnemyTeam.FormationsIncludingSpecialAndEmpty
//                    .Select(formation => formation.TargetFormation).Where(formation => formation != null).ToList();


//                foreach (var formation in allyAsTargetFormations)
//                {
//                    if (HighlightEnabledForTargetFormation && !_allyAsTargetFormations.Contains(formation))
//                        SetFormationAsTargetColor(formation, false);
//                }
//                foreach (var formation in _allyAsTargetFormations)
//                {
//                    if (!HighlightEnabledForTargetFormation || !allyAsTargetFormations.Contains(formation))
//                    {
//                        ClearFormationFocusColor(formation);
//                    }
//                }

//                _allyAsTargetFormations.Clear();
//                _allyAsTargetFormations.AddRange(allyAsTargetFormations);
//            }

//            //_allyAsTargetFormations.Clear();
//            //var formations = PlayerOrderController?.SelectedFormations;
//            //if (formations == null)
//            //    return;
//            //foreach (var formation in formations)
//            //{
//            //    SetFormationSelectedContour(formation, false);
//            //    switch (formation.MovementOrder.OrderType)
//            //    {
//            //        case OrderType.ChargeWithTarget:
//            //            {
//            //                if (HighlightEnabledForAsTargetFormation)
//            //                {
//            //                    var enemyFormation = formation.MovementOrder.TargetFormation;
//            //                    if (enemyFormation != null)
//            //                    {
//            //                        SetFormationAsTargetContour(enemyFormation, true);
//            //                    }
//            //                }

//            //                break;
//            //            }
//            //            //case OrderType.Attach:
//            //            //{
//            //            //    var allyFormation = formation.MovementOrder.TargetFormation;
//            //            //    if (allyFormation != null)
//            //            //    {
//            //            //        SetFormationAsTargetContour(allyFormation, false);
//            //            //    }
//            //            //    break;
//            //            //}
//            //    }
//            //}

//            //if (Mission.PlayerEnemyTeam == null)
//            //    return;
//            //foreach (var enemyFormation in Mission.PlayerEnemyTeam.FormationsIncludingSpecial)
//            //{
//            //    switch (enemyFormation.MovementOrder.OrderType)
//            //    {
//            //        case OrderType.ChargeWithTarget:
//            //            {
//            //                if (HighlightEnabledForAsTargetFormation)
//            //                {
//            //                    var targetFormation = enemyFormation.MovementOrder.TargetFormation;
//            //                    if (targetFormation != null)
//            //                    {
//            //                        SetFormationAsTargetContour(targetFormation, false);
//            //                    }
//            //                }

//            //                break;
//            //            }
//            //    }
//            //}
//        }

//        private void ClearColor()
//        {
//            foreach (var formation in _enemyAsTargetFormations)
//            {
//                ClearFormationAllHighlight(formation);
//            }

//            _enemyAsTargetFormations.Clear();

//            foreach (var formation in _allySelectedFormations)
//            {
//                ClearFormationAllHighlight(formation);
//            }

//            _allySelectedFormations.Clear();

//            foreach (var formation in _allyAsTargetFormations)
//            {
//                ClearFormationAllHighlight(formation);
//            }

//            _allyAsTargetFormations.Clear();

//            if (_mouseOverFormation == null)
//                return;
//            ClearFormationAllHighlight(_mouseOverFormation);
//            _mouseOverFormation = null;
//        }

//        private void ClearEnemyFocusColor()
//        {
//            foreach (var formation in _enemyAsTargetFormations)
//            {
//                ClearFormationFocusColor(formation);
//            }

//            _enemyAsTargetFormations.Clear();
//        }

//        private void ClearAllySelectedColor()
//        {
//            foreach (var formation in _allySelectedFormations)
//            {
//                ClearFormationFocusColor(formation);
//            }

//            _allySelectedFormations.Clear();
//        }

//        private void ClearAllyAsTargetColor()
//        {
//            foreach (var formation in _allyAsTargetFormations)
//            {
//                ClearFormationFocusColor(formation);
//            }
//            _allyAsTargetFormations.Clear();
//        }

//        private void SetFormationMouseOverColor(Formation formation, bool isEnemy)
//        {
//            _mouseOverFormation = formation;
//            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent => SetAgentMouseOverContour(agent, isEnemy));
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//            else
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent => SetAgentMouseOverGroundMarker(agent, isEnemy));
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//        }

//        private void SetFormationAsTargetColor(Formation formation, bool isEnemy)
//        {
//            if (isEnemy)
//                _enemyAsTargetFormations.Add(formation);
//            else
//                _allyAsTargetFormations.Add(formation);
//            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetContour(agent, isEnemy));
//                });
//            }
//            else
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetGroundMarker(agent, isEnemy));
//                });
//            }
//        }

//        private void SetFormationSelectedColor(Formation formation, bool isEnemy)
//        {
//            if (!isEnemy)
//            {
//                _allySelectedFormations.Add(formation);
//                if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//                {
//                    _actionQueue.Enqueue(() =>
//                    {
//                        formation.ApplyActionOnEachUnit(agent => SetAgentSelectedContour(agent, isEnemy));
//                    });
//                }
//                else
//                {
//                    _actionQueue.Enqueue(() =>
//                    {
//                        formation.ApplyActionOnEachUnit(agent => SetAgentSelectedGroundMarker(agent, isEnemy));
//                    });
//                }
//            }
//        }

//        private void SetAgentMouseOverContour(Agent agent, bool enemy)
//        {
//            agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)ColorLevel.MouseOverFormation,
//                enemy ? _mouseOverEnemyColor : _mouseOverAllyColor, true, false);
//        }

//        private void SetAgentMouseOverGroundMarker(Agent agent, bool enemy)
//        {
//            agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)ColorLevel.MouseOverFormation,
//                enemy ? _mouseOverEnemyColor : _mouseOverAllyColor, true, false);
//        }

//        private void SetAgentAsTargetContour(Agent agent, bool enemy)
//        {
//            agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)ColorLevel.TargetFormation,
//                enemy ? _enemyTargetColor : _allyTargetColor, true, false);
//        }

//        private void SetAgentAsTargetGroundMarker(Agent agent, bool enemy)
//        {
//            agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)ColorLevel.TargetFormation,
//                enemy ? _enemyTargetColor : _allyTargetColor, true, false);
//        }

//        private void SetAgentSelectedContour(Agent agent, bool enemy)
//        {
//            agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)ColorLevel.SelectedFormation,
//                enemy ? _enemySelectedColor : _allySelectedColor, true, false);
//        }

//        private void SetAgentSelectedGroundMarker(Agent agent, bool enemy)
//        {
//            agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)ColorLevel.SelectedFormation,
//                enemy ? _enemySelectedColor : _allySelectedColor, true, false);
//        }

//        private void ClearFormationMouseOverColor(Formation formation)
//        {
//            ClearFormationHighlight(formation, ColorLevel.MouseOverFormation);
//            _mouseOverFormation = null;
//        }

//        private void ClearFormationFocusColor(Formation formation)
//        {
//            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent =>
//                        agent.GetComponent<RTSCameraComponent>()?.ClearTargetOrSelectedFormationColor());
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//            else
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent =>
//                        agent.GetComponent<CommandSystemAgentComponent>()?.ClearTargetOrSelectedFormationColor());
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//        }

//        private void ClearFormationHighlight(Formation formation, ColorLevel level)
//        {
//            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent =>
//                        agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)level, null, true, false));
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//            else
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(agent =>
//                        agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)level, null, true, false));
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//        }

//        private void ClearFormationAllHighlight(Formation formation)
//        {
//            if (_config.TroopHighlightStyle == TroopHighlightStyle.Outline)
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(ClearAgentFormationContour);
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//            else
//            {
//                _actionQueue.Enqueue(() =>
//                {
//                    formation.ApplyActionOnEachUnit(ClearAgentFormationGroundMarker);
//                    _temporarilyUpdatedFormations.Add(formation);
//                });
//            }
//        }

//        private static void ClearAgentFormationContour(Agent agent)
//        {
//            agent.GetComponent<RTSCameraComponent>()?.ClearFormationColor();
//        }
//        private static void ClearAgentFormationGroundMarker(Agent agent)
//        {
//            agent.GetComponent<CommandSystemAgentComponent>()?.ClearFormationColor();
//        }
//    }
//}
