using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using RTSCamera.src;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera
{
    public class AgentContourMissionView : MissionView
    {
        private readonly uint _allySelectedColor = new Color(0.5f, 1.0f, 0.5f).ToUnsignedInteger();
        private readonly uint _allyTargetColor = new Color(0.2f, 0.7f, 1.0f).ToUnsignedInteger();
        private readonly uint _enemySelectedColor = new Color(0.98f, 0.4f, 0.5f).ToUnsignedInteger();
        private readonly uint _enemyTargetColor = new Color(1f, 0.2f, 0.2f).ToUnsignedInteger();
        private readonly uint _mouseOverAllyColor = new Color(0.3f, 1.0f, 1.0f).ToUnsignedInteger();
        private readonly uint _mouseOverEnemyColor = new Color(0.98f, 0.6f, 0.5f).ToUnsignedInteger();
        private readonly List<Formation> _enemyAsTargetFormations = new List<Formation>();
        private readonly List<Formation> _allyAsTargetFormations = new List<Formation>();
        private readonly List<Formation> _allySelectedFormations = new List<Formation>();
        private OrderController PlayerOrderController => Mission.PlayerTeam?.PlayerOrderController;
        private Formation _mouseOverFormation;
        private RTSCameraOrderUIHandler _orderUIHandler;
        private readonly RTSCameraConfig _config = RTSCameraConfig.Get();

        private bool _isOrderShown;
        private bool ContourEnabled => _isOrderShown && _config.ShowContour;

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
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);

            agent.AddComponent(new ContourAgentComponent(agent));
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnEarlyAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            ClearAgentContour(affectedAgent);
        }

        public override void OnAgentMount(Agent agent)
        {
            base.OnAgentMount(agent);

            if (!ContourEnabled)
                return;
            var formation = agent.Formation;
            if (formation == null)
                return;
            bool isEnemy = Utility.IsEnemy(agent);
            if (formation == _mouseOverFormation)
                SetAgentMouseOverContour(agent, isEnemy);
            else if (isEnemy)
            {
                if (_enemyAsTargetFormations.Contains(formation))
                    SetAgentAsTargetContour(agent, true);
            }
            else if (_allySelectedFormations.Contains(formation))
                SetAgentSelectedContour(agent, false);
            else if (_allyAsTargetFormations.Contains(formation))
                SetAgentAsTargetContour(agent, false);
        }

        public void MouseOver(Formation formation)
        {
            if (!ContourEnabled)
                return;
            if (formation == _mouseOverFormation)
                return;
            if (_mouseOverFormation != null)
            {
                ClearFormationMouseOverContour(_mouseOverFormation, Utility.IsEnemy(_mouseOverFormation));
                _mouseOverFormation = null;
            }
            if (formation != null)
                SetFormationMouseOverContour(formation, Utility.IsEnemy(formation));
        }

        public void Select(Formation formation, bool isEnemy)
        {
            if (!ContourEnabled)
                return;

            // not implemented.
        }

        public void SetEnableContour(bool enable)
        {
            _config.ShowContour = enable;
            if (ContourEnabled)
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
            if (ContourEnabled)
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
            if (!ContourEnabled)
                return;
            if (!_allySelectedFormations.Intersect(appliedFormations).IsEmpty())
            {
                ClearEnemyFocusContour();
                SetFocusContour();
            }
        }

        private void OnFormationsChanged(Team team, Formation formation)
        {
            if (!ContourEnabled)
                return;
            ClearFormationContour(formation);
            SetFocusContour();
            MouseOver(_mouseOverFormation);
        }

        private void OrderController_OnSelectedFormationsChanged()
        {
            if (!ContourEnabled)
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
            if (!ContourEnabled)
                return;
            UpdateContour();
        }

        private void UpdateContour()
        {
            var mouseOverFormation = _mouseOverFormation;
            ClearEnemyFocusContour();
            ClearAllyFocusContour();
            SetFocusContour();
        }

        private void SetFocusContour()
        {
            _enemyAsTargetFormations.Clear();
            _allyAsTargetFormations.Clear();
            _allySelectedFormations.Clear();
            foreach (var formation in PlayerOrderController.SelectedFormations)
            {
                SetFormationSelectedContour(formation, false);
                switch (formation.MovementOrder.OrderType)
                {
                    case OrderType.ChargeWithTarget:
                    {
                        var enemyFormation = formation.MovementOrder.TargetFormation;
                        if (enemyFormation != null)
                        {
                            SetFormationAsTargetContour(enemyFormation, true);
                        }

                        break;
                    }
                    case OrderType.Attach:
                    {
                        var allyFormation = formation.MovementOrder.TargetFormation;
                        if (allyFormation != null)
                        {
                            SetFormationAsTargetContour(allyFormation, false);
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
                ClearFormationContour(formation);
            }

            _enemyAsTargetFormations.Clear();

            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationContour(formation);
            }

            _allySelectedFormations.Clear();

            foreach (var formation in _allyAsTargetFormations)
            {
                ClearFormationContour(formation);
            }

            _allyAsTargetFormations.Clear();

            if (_mouseOverFormation == null)
                return;
            ClearFormationContour(_mouseOverFormation);
            _mouseOverFormation = null;
        }

        private void ClearEnemyFocusContour()
        {
            foreach (var formation in _enemyAsTargetFormations)
            {
                ClearFormationFocusContour(formation, true);
            }

            _enemyAsTargetFormations.Clear();
        }

        private void ClearAllyFocusContour()
        {
            foreach (var formation in _allySelectedFormations)
            {
                ClearFormationFocusContour(formation, false);
            }

            _allySelectedFormations.Clear();

            foreach (var formation in _allyAsTargetFormations)
            {
                ClearFormationFocusContour(formation, false);
            }

            _allyAsTargetFormations.Clear();
        }

        private void SetFormationMouseOverContour(Formation formation, bool isEnemy)
        {
            _mouseOverFormation = formation;
            formation.ApplyActionOnEachUnit(agent => SetAgentMouseOverContour(agent, isEnemy));
        }

        private void ClearFormationMouseOverContour(Formation formation, bool isEnemy)
        {
            if (isEnemy)
            {
                if (_enemyAsTargetFormations.Contains(formation))
                    formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetContour(agent, true));
                else
                    ClearFormationContour(formation);
            }
            else
            {
                if (_allySelectedFormations.Contains(formation))
                    formation.ApplyActionOnEachUnit(agent => SetAgentSelectedContour(agent, false));
                else if (_allyAsTargetFormations.Contains(formation))
                    formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetContour(agent, false));
                else
                    ClearFormationContour(formation);
            }
        }

        private void SetFormationAsTargetContour(Formation formation, bool isEnemy)
        {
            if (isEnemy)
                _enemyAsTargetFormations.Add(formation);
            else
                _allyAsTargetFormations.Add(formation);
            if (_mouseOverFormation == formation)
                return;
            formation.ApplyActionOnEachUnit(agent => SetAgentAsTargetContour(agent, isEnemy));
        }

        private void SetFormationSelectedContour(Formation formation, bool isEnemy)
        {
            if (!isEnemy)
                _allySelectedFormations.Add(formation);

            if (_mouseOverFormation == formation)
                return;
            formation.ApplyActionOnEachUnit(agent => SetAgentSelectedContour(agent, isEnemy));
        }

        private void ClearFormationFocusContour(Formation formation, bool isEnemy)
        {
            if (_mouseOverFormation == formation)
                return;
            ClearFormationContour(formation);
        }

        private static void ClearFormationContour(Formation formation)
        {
            formation.ApplyActionOnEachUnit(ClearAgentContour);
        }

        private void SetAgentMouseOverContour(Agent agent, bool enemy)
        {
            SetAgentContour(agent, enemy ? _mouseOverEnemyColor : _mouseOverAllyColor);
        }

        private void SetAgentAsTargetContour(Agent agent, bool enemy)
        {
            SetAgentContour(agent, enemy ? _enemyTargetColor : _allyTargetColor);
        }

        private void SetAgentSelectedContour(Agent agent, bool enemy)
        {
            SetAgentContour(agent, enemy ? _enemySelectedColor : _allySelectedColor);
        }

        private static void ClearAgentContour(Agent agent)
        {
            SetAgentContour(agent, new uint?());
        }

        private static void SetAgentContour(Agent agent, uint? color)
        {
            agent.AgentVisuals?.SetContourColor(color);
            if (agent.HasMount)
                agent.MountAgent.AgentVisuals?.SetContourColor(color);
        }
    }
}
