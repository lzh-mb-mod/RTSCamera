using HarmonyLib;
using RTSCamera.CommandSystem.AgentComponents;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic.SubLogic;
using RTSCamera.CommandSystem.Patch;
using RTSCamera.CommandSystem.QuerySystem;
using RTSCameraAgentComponent;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic
{
    public class CommandSystemLogic : MissionLogic, IMissionListener
    {
        private CommandSystemConfig _config = CommandSystemConfig.Get();
        public readonly FormationColorSubLogicV2 OutlineColorSubLogic;
        public readonly FormationColorSubLogicV2 GroundMarkerColorSubLogic;
        private bool _isShowIndicatorsDown = false;
        private bool _followAttack = false;

        public CommandSystemLogic()
        {
            OutlineColorSubLogic = new FormationColorSubLogicV2(
                highlightEnabledInCharacterMode: () =>
                {
                    return _config.TroopHighlightStyleInCharacterMode == TroopHighlightStyle.Outline;
                },
                highlightEnabledInRtsMode: () =>
                {
                    return _config.TroopHighlightStyleInRTSMode == TroopHighlightStyle.Outline;
                },
                mouseOverEnabled: () =>
                {
                    return _config.IsMouseOverEnabled();
                },
                setAgentColor: (Agent agent,int level, uint? color, bool alwaysVisible, bool updateInstantly) =>
                {
                    agent.GetComponent<RTSCameraComponent>()?.SetContourColor((int)level, color, alwaysVisible, updateInstantly);
                },
                clearAgentHighlight: (Agent agent) =>
                {
                    agent.GetComponent<RTSCameraComponent>()?.ClearFormationColor();
                },
                updateAgentColor: (Agent agent) =>
                {
                    agent.GetComponent<RTSCameraComponent>()?.UpdateContour();
                },
                clearTargetOrSelectedFormationColor: (Formation formation) =>
                {
                    formation.ApplyActionOnEachUnit(agent =>
                        agent.GetComponent<RTSCameraComponent>()?.ClearTargetOrSelectedFormationColor());
                },
                updateFormationColor: (Formation formation) =>
                {
                    formation.ApplyActionOnEachUnit(a => a.GetComponent<RTSCameraComponent>()?.UpdateContour());
                });
            GroundMarkerColorSubLogic = new FormationColorSubLogicV2(
                highlightEnabledInCharacterMode: () =>
                {
                    return _config.TroopHighlightStyleInCharacterMode == TroopHighlightStyle.GroundMarker;
                },
                highlightEnabledInRtsMode: () =>
                {
                    return _config.TroopHighlightStyleInRTSMode == TroopHighlightStyle.GroundMarker;
                },
                mouseOverEnabled: () =>
                {
                    return _config.IsMouseOverEnabled();
                },
                setAgentColor: (Agent agent, int level, uint? color, bool alwaysVisible, bool updateInstantly) =>
                {
                    agent.GetComponent<CommandSystemAgentComponent>()?.SetColor((int)level, color, alwaysVisible, updateInstantly);
                },
                clearAgentHighlight: (Agent agent) =>
                {
                    agent.GetComponent<CommandSystemAgentComponent>()?.ClearFormationColor();
                },
                updateAgentColor: (Agent agent) =>
                {
                    agent.GetComponent<CommandSystemAgentComponent>()?.TryUpdateColor();
                },
                clearTargetOrSelectedFormationColor: (Formation formation) =>
                {
                    formation.ApplyActionOnEachUnit(agent =>
                        agent.GetComponent<CommandSystemAgentComponent>()?.ClearTargetOrSelectedFormationColor());
                },
                updateFormationColor: (Formation formation) =>
                {
                    formation.ApplyActionOnEachUnit(a => a.GetComponent<CommandSystemAgentComponent>()?.TryUpdateColor());
                });
        }

        public void OnMovementOrderChanged(Formation formation)
        {
            OutlineColorSubLogic.OnMovementOrderChanged(formation);
            GroundMarkerColorSubLogic.OnMovementOrderChanged(formation);
        }

        public void OnMovementOrderChanged(IEnumerable<Formation> appliedFormations)
        {
            OutlineColorSubLogic.OnMovementOrderChanged(appliedFormations);
            GroundMarkerColorSubLogic.OnMovementOrderChanged(appliedFormations);
        }


        public override void OnAfterMissionCreated()
        {
            base.OnAfterMissionCreated();
            Patch_OrderController.OnAfterMissionCreated();
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();


            Patch_MovementOrder.Patch();
            OutlineColorSubLogic.OnBehaviourInitialize();
            GroundMarkerColorSubLogic.OnBehaviourInitialize();
            Patch_OrderTroopPlacer.OnBehaviorInitialize();
            CommandQueueLogic.OnBehaviorInitialize();
            CommandQuerySystem.OnBehaviorInitialize();

            var config = CommandSystemConfig.Get();
            if (!config.HasHintDisplayed)
            {
                config.HasHintDisplayed = true;
                config.Serialize();
                Utilities.Utility.PrintOrderHint();
            }
        }

        public override void OnRemoveBehavior()
        {
            foreach (var team in Mission.Current.Teams)
            {
                if (team.FormationsIncludingSpecialAndEmpty == null)
                    continue;
                foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
                {
                    formation.OnUnitAdded -= OnUnitAdded;
                    formation.OnUnitRemoved -= OnUnitRemoved;
                }
            }
            OutlineColorSubLogic.OnRemoveBehaviour();
            GroundMarkerColorSubLogic.OnRemoveBehaviour();
            Patch_OrderTroopPlacer.OnRemoveBehavior();
            Patch_OrderController.OnRemoveBehavior();
            CommandQueueLogic.OnRemoveBehavior();
            CommandQuerySystem.OnRemoveBehavior();
        }

        public override void AfterStart()
        {
            base.AfterStart();

            Mission.AddListener(this);
            CommandQueueLogic.AfterStart();
        }

        public override void OnAddTeam(Team team)
        {
            base.OnAddTeam(team);

            Patch_OrderController.OnAddTeam(team);
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);

            OutlineColorSubLogic.OnPreDisplayMissionTick(dt);
            GroundMarkerColorSubLogic.OnPreDisplayMissionTick(dt);

            var combatHotKeyCategory = HotKeyManager.GetCategory("CombatHotKeyCategory");
            combatHotKeyCategory?.GetGameKey(GenericGameKeyContext.ShowIndicators);
            if (Mission.InputManager.IsGameKeyDown(GenericGameKeyContext.ShowIndicators))
            {
                if (!_isShowIndicatorsDown)
                {
                    _isShowIndicatorsDown = true;
                    OutlineColorSubLogic.OnShowIndicatorKeyDownUpdate(_isShowIndicatorsDown);
                    GroundMarkerColorSubLogic.OnShowIndicatorKeyDownUpdate(_isShowIndicatorsDown);
                }
            }
            else
            {
                if (_isShowIndicatorsDown)
                {
                    _isShowIndicatorsDown = false;
                    OutlineColorSubLogic.OnShowIndicatorKeyDownUpdate(_isShowIndicatorsDown);
                    GroundMarkerColorSubLogic.OnShowIndicatorKeyDownUpdate(_isShowIndicatorsDown);
                }
            }

            //if (Input.IsKeyPressed(InputKey.B))
            //{
            //    _followAttack = !_followAttack;
            //}
            //if (_followAttack)
            //{
            //    var lookDirection = Mission.MainAgent.LookDirection;
            //    foreach (var agent in Mission.PlayerTeam.ActiveAgents)
            //    {
            //        if (!agent.IsMainAgent)
            //        {
            //            //agent.Controller = AgentControllerType.None;
            //            //agent.SetScriptedFlags(Agent.AIScriptedFrameFlags.None);
            //            var pos = Mission.MainAgent.GetWorldPosition();
            //            pos.SetVec2(pos.AsVec2 + lookDirection.AsVec2.Normalized() * 2);
            //            var target = pos.GetGroundVec3();
            //            target += lookDirection * 3;
            //            target.z += 3f;
            //            var direction = target - agent.Position;
            //            agent.SetTargetPositionAndDirection(pos.AsVec2, direction);
            //            agent.SetAutomaticTargetSelection(false);
            //            //agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanAttack);
            //            //agent.SetScriptedFlags(agent.GetScriptedFlags() | Agent.AIScriptedFrameFlags.NoAttack);
            //            //var pos = agent.GetWorldPosition();
            //            //agent.AIStateFlags |= Agent.AIStateFlag.UseObjectMoving;
            //            //agent.IsLookDirectionLocked = true;
            //            //agent.SetScriptedPosition(ref pos, false);
            //            agent.SetLookToPointOfInterest(target);
            //            //agent.SetIsAIPaused(true);
            //            //agent.Controller = AgentControllerType.None;
            //            //agent.IsLookDirectionLocked = true;
            //            //agent.LookDirection = direction;
            //            //agent.IsLookDirectionLocked = true;
            //        }
            //    }
            //}
            //else
            //{
            //    foreach (var agent in Mission.PlayerTeam.ActiveAgents)
            //    {
            //        if (!agent.IsMainAgent)
            //        {
            //            agent.SetIsAIPaused(false);
            //            agent.DisableScriptedMovement();
            //            agent.ClearTargetFrame();
            //            agent.SetScriptedFlags(agent.GetScriptedFlags() & ~Agent.AIScriptedFrameFlags.NoAttack);
            //            agent.DisableLookToPointOfInterest();
            //            agent.SetAutomaticTargetSelection(true);
            //            agent.AIStateFlags &= ~Agent.AIStateFlag.UseObjectMoving;
            //            agent.Controller = AgentControllerType.AI;
            //        }
            //    }

            //}
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();

            if (Mission.PlayerTeam != null && CommandSystemConfig.Get().FacingEnemyByDefault)
            {
                foreach (var formation in Mission.PlayerTeam.FormationsIncludingEmpty)
                {
                    if (Mission.PlayerTeam.PlayerOrderController.IsFormationSelectable(formation) && !formation.IsAIControlled && formation.PlayerOwner != null && formation.PlayerOwner == Mission.MainAgent)
                    {
                        formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
                    }
                }
            }
        }

        public override void AfterAddTeam(Team team)
        {
            OutlineColorSubLogic.AfterAddTeam(team);
            GroundMarkerColorSubLogic.AfterAddTeam(team);
            if (team.FormationsIncludingSpecialAndEmpty == null)
                return;
            foreach (var formation in team.FormationsIncludingSpecialAndEmpty)
            {
                formation.OnUnitAdded += OnUnitAdded;
                formation.OnUnitRemoved += OnUnitRemoved;
            }
        }

        private void OnUnitAdded(Formation formation, Agent agent)
        {
            OutlineColorSubLogic.OnUnitAdded(formation, agent);
            GroundMarkerColorSubLogic.OnUnitAdded(formation, agent);
        }

        private void OnUnitRemoved(Formation formation, Agent agent)
        {
            OutlineColorSubLogic.OnUnitRemoved(formation, agent);
            GroundMarkerColorSubLogic.OnUnitRemoved(formation, agent);
            if (formation.CountOfUnits == 0)
            {
                CommandQueueLogic.OnFormationUnitsCleared(formation);
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);

            agent.AddComponent(new CommandSystemAgentComponent(agent));
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            OutlineColorSubLogic.OnAgentBuild(agent, banner);
            GroundMarkerColorSubLogic.OnAgentBuild(agent, banner);
        }

        public override void OnAgentFleeing(Agent affectedAgent)
        {
            base.OnAgentFleeing(affectedAgent);

            OutlineColorSubLogic.OnAgentFleeing(affectedAgent);
            GroundMarkerColorSubLogic.OnAgentFleeing(affectedAgent);
        }

        public void OnEquipItemsFromSpawnEquipmentBegin(Agent agent, Agent.CreationType creationType)
        {
            // called before first equipment
        }

        public void OnEquipItemsFromSpawnEquipment(Agent agent, Agent.CreationType creationType)
        {
            // called after first equipment, and after refreshing equipment such as bearing banner
            agent.GetComponent<CommandSystemAgentComponent>()?.Refresh();
        }

        void IMissionListener.OnEndMission()
        {
            Mission.RemoveListener(this);
        }

        public void OnConversationCharacterChanged()
        {
        }

        public void OnResetMission()
        {

        }

        public void OnDeploymentPlanMade(Team team, bool isFirstPlan)
        {
        }

        protected override void OnAgentControllerChanged(Agent agent, AgentControllerType oldController)
        {
            base.OnAgentControllerChanged(agent, oldController);

            agent.GetComponent<CommandSystemAgentComponent>()?.OnControllerChanged(oldController);
        }
    }
}
