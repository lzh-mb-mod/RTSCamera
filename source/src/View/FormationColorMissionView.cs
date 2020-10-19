using System.Collections.Generic;
using System.Linq;
using RTSCamera.Config;
using RTSCamera.Logic.SubLogic.Component;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.View
{
    public class FormationColorMissionView : MissionView
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
            OrderType.Attach,
            OrderType.Retreat,
            OrderType.AdvanceTenPaces,
            OrderType.FallBackTenPaces,
            OrderType.Advance,
            OrderType.FallBack
        };

        private readonly uint _allySelectedColor = new Color(0.5f, 1.0f, 0.5f).ToUnsignedInteger();
        private readonly uint _allyTargetColor = new Color(0.3f, 0.3f, 1.0f).ToUnsignedInteger();
        private readonly uint _mouseOverAllyColor = new Color(0.3f, 1.0f, 1.0f).ToUnsignedInteger();
        private readonly uint _enemySelectedColor = new Color(0.98f, 0.4f, 0.5f).ToUnsignedInteger();
        private readonly uint _enemyTargetColor = new Color(1f, 0.2f, 0.2f).ToUnsignedInteger();
        private readonly uint _mouseOverEnemyColor = new Color(0.98f, 0.6f, 0.5f).ToUnsignedInteger();
        private readonly List<Formation> _enemyAsTargetFormations = new List<Formation>();
        private readonly List<Formation> _allyAsTargetFormations = new List<Formation>();
        private readonly List<Formation> _allySelectedFormations = new List<Formation>();
        private OrderController PlayerOrderController => Mission.PlayerTeam?.PlayerOrderController;
        private Formation _mouseOverFormation;
        private RTSCameraOrderUIHandler _orderUIHandler;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();

        private bool _isOrderShown;
        private bool HighlightEnabled => _isOrderShown && _config.ShouldHighlightWithOutline();
        private bool HighlightEnabledForSelectedFormation => _isOrderShown && _config.ClickToSelectFormation;
        private bool HighlightEnabledForAsTargetFormation => _isOrderShown && _config.AttackSpecificFormation;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            Mission.Teams.OnPlayerTeamChanged += Mission_OnPlayerTeamChanged;
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent);
            _orderUIHandler = Mission.GetMissionBehaviour<RTSCameraOrderUIHandler>();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(OnToggleOrderViewEvent); ;
        }

        public override void AfterAddTeam(Team team)
        {
            base.AfterAddTeam(team);

            team.OnOrderIssued += OnOrderIssued;
            team.OnFormationsChanged += OnFormationsChanged;
            team.PlayerOrderController.OnSelectedFormationsChanged += OrderController_OnSelectedFormationsChanged;
            //foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
            //{
            //    formation.OnUnitCountChanged += Formation_OnUnitCountChanged;
            //}
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);

            if (agent.Formation != null)
            {
                bool isEnemy = Utility.IsEnemy(agent.Formation);
                if (agent.Formation == _mouseOverFormation)
                    SetAgentMouseOverContour(agent, isEnemy);
                if (isEnemy)
                {
                    if (_enemyAsTargetFormations.Contains(agent.Formation))
                        SetAgentAsTargetContour(agent, true);
                }
                else
                {
                    if (_allySelectedFormations.Contains(agent.Formation))
                        SetAgentSelectedContour(agent, false);
                    if (_allyAsTargetFormations.Contains(agent.Formation))
                        SetAgentAsTargetContour(agent, false);
                }
            }

        }

        public override void OnAgentFleeing(Agent affectedAgent)
        {
            base.OnAgentFleeing(affectedAgent);

            ClearAgentFormationContour(affectedAgent);
        }

        public void MouseOver(Formation formation)
        {
            if (!HighlightEnabled || formation == _mouseOverFormation)
                return;
            if (_mouseOverFormation != null)
                ClearFormationMouseOverContour(_mouseOverFormation);
            if (formation != null)
            {
                bool isEnemy = Utility.IsEnemy(formation);
                if (isEnemy ? HighlightEnabledForAsTargetFormation : HighlightEnabledForSelectedFormation)
                    SetFormationMouseOverContour(formation, Utility.IsEnemy(formation));
            }
        }

        public void SetEnableContourForSelectedFormation(bool enable)
        {
            _config.ClickToSelectFormation = enable;
            if (HighlightEnabled)
            {
                SetFocusContour();
            }
            else
            {
                ClearContour();
            }
        }

        private void OnToggleOrderViewEvent(MissionPlayerToggledOrderViewEvent e)
        {
            _isOrderShown = e.IsOrderEnabled;
            if (HighlightEnabled)
            {
                SetFocusContour();
            }
            else
            {
                ClearContour();
            }
        }

        private void OnOrderIssued(OrderType orderType, IEnumerable<Formation> appliedFormations, params object[] delegateParams)
        {
            if (!HighlightEnabledForAsTargetFormation || movementOrderTypes.FindIndex(o => o == orderType) == -1)
                return;
            if (!_allySelectedFormations.Intersect(appliedFormations).IsEmpty())
            {
                ClearEnemyFocusContour();
                SetFocusContour();
            }
        }

        private void OnFormationsChanged(Team team, Formation formation)
        {
            if (!HighlightEnabled)
                return;
            var mouseOverFormation = _mouseOverFormation;
            _mouseOverFormation = null;

            ClearFormationAllContour(formation);
            SetFocusContour();
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
            if (!HighlightEnabled)
                return;
            UpdateContour();
            if (_orderUIHandler == null)
                return;

            foreach (OrderTroopItemVM troop in _orderUIHandler.dataSource.TroopList)
            {
                troop.IsSelectable = PlayerOrderController.IsFormationSelectable(troop.Formation);
                troop.IsSelected = troop.IsSelectable && PlayerOrderController.IsFormationListening(troop.Formation);
            }
        }

        private void Mission_OnPlayerTeamChanged(Team arg1, Team arg2)
        {
            if (!HighlightEnabled)
                return;
            UpdateContour();
        }

        private void UpdateContour()
        {
            ClearContour();
            SetFocusContour();
        }

        private void SetFocusContour()
        {
            ClearEnemyFocusContour();
            ClearAllyFocusContour();
            var formations = PlayerOrderController?.SelectedFormations;
            if (formations == null)
                return;
            foreach (var formation in formations)
            {
                SetFormationSelectedContour(formation, false);
                switch (formation.MovementOrder.OrderType)
                {
                    case OrderType.ChargeWithTarget:
                    {
                        if (HighlightEnabledForAsTargetFormation)
                        {
                            var enemyFormation = formation.MovementOrder.TargetFormation;
                            if (enemyFormation != null)
                            {
                                SetFormationAsTargetContour(enemyFormation, true);
                            }
                        }

                        break;
                    }
                    //case OrderType.Attach:
                    //{
                    //    var allyFormation = formation.MovementOrder.TargetFormation;
                    //    if (allyFormation != null)
                    //    {
                    //        SetFormationAsTargetContour(allyFormation, false);
                    //    }
                    //    break;
                    //}
                }
            }

            if (Mission.PlayerEnemyTeam == null)
                return;
            foreach (var enemyFormation in Mission.PlayerEnemyTeam.FormationsIncludingSpecial)
            {
                switch (enemyFormation.MovementOrder.OrderType)
                {
                    case OrderType.ChargeWithTarget:
                    {
                        if (HighlightEnabledForAsTargetFormation)
                        {
                            var targetFormation = enemyFormation.MovementOrder.TargetFormation;
                            if (targetFormation != null)
                            {
                                SetFormationAsTargetContour(targetFormation, false);
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void ClearContour()
        {
            foreach (var formation in _enemyAsTargetFormations)
            {
                ClearFormationAllContour(formation);
            }

            _enemyAsTargetFormations.Clear();

            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationAllContour(formation);
            }

            _allySelectedFormations.Clear();

            foreach (var formation in _allyAsTargetFormations)
            {
                ClearFormationAllContour(formation);
            }

            _allyAsTargetFormations.Clear();

            if (_mouseOverFormation == null)
                return;
            ClearFormationAllContour(_mouseOverFormation);
            _mouseOverFormation = null;
        }

        private void ClearEnemyFocusContour()
        {
            foreach (var formation in _enemyAsTargetFormations)
            {
                ClearFormationFocusContour(formation);
            }

            _enemyAsTargetFormations.Clear();
        }

        private void ClearAllyFocusContour()
        {
            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationFocusContour(formation);
            }

            _allySelectedFormations.Clear();

            foreach (var formation in _allyAsTargetFormations)
            {
                ClearFormationFocusContour(formation);
            }

            _allyAsTargetFormations.Clear();
        }

        private void SetFormationMouseOverContour(Formation formation, bool isEnemy)
        {
            _mouseOverFormation = formation;
            formation.ApplyActionOnEachUnit(agent => SetAgentMouseOverContour(agent, isEnemy));
        }

        private void SetFormationAsTargetContour(Formation formation, bool isEnemy)
        {
            if (isEnemy)
                _enemyAsTargetFormations.Add(formation);
            else
                _allyAsTargetFormations.Add(formation);
            formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetContour(agent, isEnemy));
        }

        private void SetFormationSelectedContour(Formation formation, bool isEnemy)
        {
            if (!isEnemy)
                _allySelectedFormations.Add(formation);
            if (HighlightEnabledForSelectedFormation)
                formation.ApplyActionOnEachUnit(agent => SetAgentSelectedContour(agent, isEnemy));
        }

        private void SetAgentMouseOverContour(Agent agent, bool enemy)
        {
            agent.GetComponent<RTSCameraAgentComponent>()?.SetContourColor((int)ColorLevel.MouseOverFormation,
                enemy ? _mouseOverEnemyColor : _mouseOverAllyColor, true);
        }

        private void SetAgentAsTargetContour(Agent agent, bool enemy)
        {
            agent.GetComponent<RTSCameraAgentComponent>()?.SetContourColor((int)ColorLevel.TargetFormation,
                enemy ? _enemyTargetColor : _allyTargetColor, true);
        }

        private void SetAgentSelectedContour(Agent agent, bool enemy)
        {
            agent.GetComponent<RTSCameraAgentComponent>()?.SetContourColor((int)ColorLevel.SelectedFormation,
                enemy ? _enemySelectedColor : _allySelectedColor, true);
        }

        private void ClearFormationMouseOverContour(Formation formation)
        {
            ClearFormationContour(formation, ColorLevel.MouseOverFormation);
            _mouseOverFormation = null;
        }

        private void ClearFormationFocusContour(Formation formation)
        {
            formation.ApplyActionOnEachUnit(agent =>
                agent.GetComponent<RTSCameraAgentComponent>()?.ClearTargetOrSelectedFormationColor());
        }

        private void ClearFormationContour(Formation formation, ColorLevel level)
        {
            formation.ApplyActionOnEachUnit(agent => agent.GetComponent<RTSCameraAgentComponent>()?.SetContourColor((int)level, null, true));
        }

        private static void ClearFormationAllContour(Formation formation)
        {
            formation.ApplyActionOnEachUnit(ClearAgentFormationContour);
        }

        private static void ClearAgentFormationContour(Agent agent)
        {
            agent.GetComponent<RTSCameraAgentComponent>()?.ClearFormationColor();
        }
    }
}
