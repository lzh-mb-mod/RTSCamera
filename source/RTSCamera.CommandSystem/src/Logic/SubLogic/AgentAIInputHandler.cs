using RTSCamera.CommandSystem.Config;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.MovementOrder;

namespace RTSCamera.CommandSystem.Logic.SubLogic
{
    public class AgentAIInputHandler
    {
        private enum VolleyStatus
        {
            CancelAttackBeforeWaitingForOrder,
            Reloading,
            WaitingForOrder,
            TryAimingWhileWaitingForOrder,
            AimWhileWaitingForOrder,
            AimingDoneWhileWaitingForOrder,
            PrepareForShooting,
            ForceDrawing,
            WaitingForLookingForTarget,
            DrawingTheBowUnderShootingOrder,
            StandAfterShooted,
            StandAfterCancelShooting,
        }
        public VolleyMode VolleyMode { get; private set; } = VolleyMode.Disabled;
        private VolleyStatus _volleyStatus;


        private bool _cancelAttackOnVolleyDisabled = false;
        public bool IsVolleySuspended { get; private set; } = false;
        private bool _isCandidateForNextFireInAutoVolley  = false;
        private bool _isAvailableForNextFireInAutoVolley = false;
        private MatrixFrame _agentFrame;
        private WorldPosition _aiMoveDestination;
        private WorldPosition _formationPosition;
        private float _distanceToFormationPosition;
        private bool _isMovingToDestination;
        private bool _isAIAtMoveDestination;
        private bool _isTargetAgentOutOfRange = false;
        private bool _isTargetAgentNearby = false;
        private float _minAimingError = float.MaxValue;
        private float _maxAimingError = float.MinValue;
        private bool _encouteredFirstMinimumValue = false;
        private bool _encounteredFirstMaximumValue;
        private bool _tryAimingTimeoutInAutoVolley = false;
        private bool _aimTimeout = false;
        private bool _shouldResetAutoVolleyTimer = false;
        private Timer _cancelAttackeBeforeWaitingForOrder = new Timer(-1, -1, false);
        private Timer _autoVolleyAimingTimer = new Timer(-1, -1, false);
        private Timer _tryAimingWhileWaitingForOrderTimer = new Timer(-1, -1, false);
        private Timer _aimWhileWaitingForOrderTimer = new Timer(-1, -1, false);
        private Timer _prepareForShootingTimer = new Timer(-1, -1, false);
        private Timer _forceDrawingTimer = new Timer(-1, -1, false);
        private Timer _waitingForLookingForTargetTimer = new Timer(-1, -1, false);
        private Timer _allowMovingTimer = new Timer(-1, -1, false);
        private Timer _drawingTheBowUnderShootingOrderTimer = new Timer(-1, -1, false);
        private Timer _standAfterShootedTimer = new Timer(-1, -1, false);
        private Timer _standAfterCancelShootingTimer = new Timer(-1, -1, false);
        public static bool ForceShootingEnabled = false;
        public bool AllowPreAiming => CommandSystemConfig.Get().VolleyPreAimingMode == VolleyPreAimingMode.BothAutoAndManualVolley || VolleyMode == VolleyMode.Auto;

        public bool IsPreAimingEnabled(Agent agent)
        {
            return AllowPreAiming && (!_formationPosition.IsValid || _distanceToFormationPosition < 7f);
        }
        public void SetVolleyMode(Agent agent, VolleyMode volleyMode)
        {
            if (VolleyMode == volleyMode)
                return;
            VolleyMode = volleyMode;
            if (VolleyMode == VolleyMode.Disabled)
            {
                if (agent.GetFiringOrder() == (int)FiringOrder.RangedWeaponUsageOrderEnum.HoldYourFire && IsVolleyStatusDrawing(agent))
                {
                    _cancelAttackOnVolleyDisabled = true;
                }
                IsVolleySuspended = false;
                _volleyStatus = VolleyStatus.WaitingForOrder;
                OnVolleyDisabled(agent);
                return;
            }
            if (VolleyMode == VolleyMode.Manual)
            {
                _cancelAttackeBeforeWaitingForOrder.Reset(Mission.Current.CurrentTime, MBRandom.RandomFloat * 0.6f);
                _volleyStatus = VolleyStatus.CancelAttackBeforeWaitingForOrder;
            }
            else if (VolleyMode == VolleyMode.Auto)
            {
                ShootUnderVolley(agent);
            }
        }

        private void OnVolleyWait(Agent agent)
        {
            if (!agent.HasMount)
            {
                SetCanAttack(agent, false);
            }
            SetWaitingBehavior(agent);
        }

        private void OnVolleyDisabled(Agent agent)
        {
            SetCanAttack(agent, true);
            SetNoVolleyBehavior(agent);
        }

        private void OnShootingEnabled(Agent agent)
        {
            SetCanAttack(agent, true);
            SetShootingBehavior(agent);
        }

        public bool ShootUnderVolley(Agent agent)
        {
            if (VolleyMode != VolleyMode.Disabled && !IsVolleySuspended)
            {
                _shouldResetAutoVolleyTimer = true;
                switch (_volleyStatus)
                {
                    case VolleyStatus.CancelAttackBeforeWaitingForOrder:
                    case VolleyStatus.WaitingForOrder:
                    case VolleyStatus.StandAfterShooted:
                    case VolleyStatus.StandAfterCancelShooting:
                    case VolleyStatus.Reloading:
                        _volleyStatus = VolleyStatus.PrepareForShooting;
                        if (VolleyMode == VolleyMode.Auto)
                        {
                            _prepareForShootingTimer.Reset(Mission.Current.CurrentTime, 0);
                        }
                        else
                        {
                            _prepareForShootingTimer.Reset(Mission.Current.CurrentTime, MBRandom.RandomFloat * 0.6f);
                        }
                        _isCandidateForNextFireInAutoVolley = false;
                        _isAvailableForNextFireInAutoVolley = false;
                        //_prepareForShootingTick = (int)(MBRandom.RandomFloat * 60);
                        break;
                    case VolleyStatus.TryAimingWhileWaitingForOrder:
                        _waitingForLookingForTargetTimer.Reset(Mission.Current.CurrentTime, 2f);
                        _isCandidateForNextFireInAutoVolley = false;
                        _isAvailableForNextFireInAutoVolley = false;
                        _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                        break;
                    case VolleyStatus.AimWhileWaitingForOrder:
                    case VolleyStatus.AimingDoneWhileWaitingForOrder:
                        _drawingTheBowUnderShootingOrderTimer.Reset(Mission.Current.CurrentTime, agent.HasMount ? 15f : 7.5f);
                        _isCandidateForNextFireInAutoVolley = true;
                        _isAvailableForNextFireInAutoVolley = false;
                        _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                        break;
                    case VolleyStatus.WaitingForLookingForTarget:
                        _waitingForLookingForTargetTimer.Reset(Mission.Current.CurrentTime, 1f);
                        _isCandidateForNextFireInAutoVolley = true;
                        _isAvailableForNextFireInAutoVolley = false;
                        break;
                    case VolleyStatus.DrawingTheBowUnderShootingOrder:
                        _drawingTheBowUnderShootingOrderTimer.Reset(Mission.Current.CurrentTime, 4f);
                        _isCandidateForNextFireInAutoVolley = true;
                        _isAvailableForNextFireInAutoVolley = false;
                        break;
                    default:
                        _isCandidateForNextFireInAutoVolley = false;
                        _isAvailableForNextFireInAutoVolley = false;
                        return false;
                }
                return true;
            }
            return false;
        }

        public void OnFormationSet(Agent agent)
        {
            VolleyMode newVolleyMode = VolleyMode.Disabled;
            if (agent.Formation != null)
            {
                newVolleyMode = CommandQueueLogic.GetFormationVolleyMode(agent.Formation);
            }
            if (newVolleyMode != VolleyMode)
            {
                SetVolleyMode(agent, newVolleyMode);
            }
        }

        public void OnHit(Agent affectedAgent, Agent affectorAgent, int damage, in MissionWeapon affectorWeapon, in Blow b, in AttackCollisionData collisionData)
        {
            if (VolleyMode != VolleyMode.Disabled && !IsVolleySuspended)
            {
                switch (_volleyStatus)
                {
                    case VolleyStatus.AimWhileWaitingForOrder:
                    case VolleyStatus.AimingDoneWhileWaitingForOrder:
                        //_aimTimeout = false;
                        //_autoVolleyAimingTimer.Reset(Mission.Current.CurrentTime, CommandSystemConfig.Get().MaxAimingTime);
                        //_tryAimingWhileWaitingForOrderTimer.Reset(Mission.Current.CurrentTime, 2f);
                        _volleyStatus = VolleyStatus.TryAimingWhileWaitingForOrder;
                        break;
                    case VolleyStatus.DrawingTheBowUnderShootingOrder:
                        _allowMovingTimer.Reset(Mission.Current.CurrentTime, 0.6f);
                        _waitingForLookingForTargetTimer.Reset(Mission.Current.CurrentTime, 3f);
                        _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                        break;
                }
            }
        }

        public void OnControllerChanged(Agent agent, AgentControllerType oldController)
        {
            VolleyMode newVolleyMode = VolleyMode.Disabled;
            if (agent.Controller == AgentControllerType.AI && agent.Formation != null)
            {
                newVolleyMode = CommandQueueLogic.GetFormationVolleyMode(agent.Formation);
            }
            if (newVolleyMode != VolleyMode)
            {
                SetVolleyMode(agent, newVolleyMode);
            }
        }

        public bool IsCandidateForNextFireInAutoVolley(Agent agent)
        {
            return agent.IsAIControlled && !IsVolleySuspended && (
                _volleyStatus == VolleyStatus.TryAimingWhileWaitingForOrder && !_tryAimingTimeoutInAutoVolley ||
                _volleyStatus == VolleyStatus.AimWhileWaitingForOrder && !_aimTimeout ||
                _volleyStatus == VolleyStatus.AimingDoneWhileWaitingForOrder)/* && _isCandidateForNextFireInAutoVolley*/;
        }

        public bool IsReadyForNextFire(Agent agent)
        {
            //if (_isAvailableForNextFireInAutoVolley)
            //    return _volleyStatus != VolleyStatus.Reloading;
            switch (_volleyStatus)
            {
                case VolleyStatus.Reloading:
                    return false;
                case VolleyStatus.CancelAttackBeforeWaitingForOrder:
                    return false;
                case VolleyStatus.WaitingForOrder:
                    return false;
                case VolleyStatus.TryAimingWhileWaitingForOrder:
                    return false;
                case VolleyStatus.AimWhileWaitingForOrder:
                    return false;
                case VolleyStatus.AimingDoneWhileWaitingForOrder:
                    return true;
                case VolleyStatus.PrepareForShooting:
                    return false;
                case VolleyStatus.ForceDrawing:
                    return false;
                case VolleyStatus.WaitingForLookingForTarget:
                    {
                        //if (!_waitingForLookingForTargetTimer.Check(Mission.Current.CurrentTime))
                        //{
                        //    if (_waitingForLookingForTargetTimer.ElapsedTime() > 3)
                        //    {
                        //        return true;
                        //    }
                        //    return false;
                        //}
                        //return true;
                        return false;
                    }
                case VolleyStatus.DrawingTheBowUnderShootingOrder:
                    {
                        //if (!_drawingTheBowUnderShootingOrderTimer.Check(Mission.Current.CurrentTime))
                        //{
                        //    if (_drawingTheBowUnderShootingOrderTimer.ElapsedTime() > 6)
                        //    {
                        //        return true;
                        //    }
                        //    return false;
                        //}
                        //return true;
                        return false;
                    }
                case VolleyStatus.StandAfterShooted:
                    return false;
                case VolleyStatus.StandAfterCancelShooting:
                    return false;
            }
            return false;
        }

        private void SetCanAttack(Agent agent, bool canAttack)
        {
            if (canAttack)
            {
                agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanAttack);
            }
            else
            {
                agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanAttack);
            }
        }

        private void SetNoVolleyBehavior(Agent agent)
        {
            //SetCanAttack(agent, false);
            if (!agent.IsAIControlled)
                return;
            if (agent.Formation != null)
            {
                agent.RefreshBehaviorValues(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
            }
            else
            {
                agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            }
            //agent.HumanAIComponent?.SyncBehaviorParamsIfNecessary();
            //agent.ForceAiBehaviorSelection();
        }

        private void SetWaitingBehavior(Agent agent)
        {
            //SetCanAttack(agent, false);
            if (!agent.IsAIControlled)
                return;
            //if (VolleyMode == VolleyMode.Auto)
            //{
            //    agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            //    return;
            //}
            if (agent.Formation != null)
            {
                var movementOrderEnum = agent.Formation.GetReadonlyMovementOrderReference().OrderEnum;
                agent.RefreshBehaviorValues(movementOrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
                if (movementOrderEnum == MovementOrder.MovementOrderEnum.Charge || movementOrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget)
                    return;
            }
            else
            {
                agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            }
            if (VolleyMode == VolleyMode.Auto)
                return;
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Ranged, 0, 7f, 0, 20f, 0);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 0, 15f, 0, 30f, 0);
            //agent.HumanAIComponent?.SyncBehaviorParamsIfNecessary();
            //agent.ForceAiBehaviorSelection();
        }

        private void SetShootingBehavior(Agent agent)
        {
            if (!agent.IsAIControlled)
                return;
            if (agent.Formation != null)
            {
                var movementOrderEnum = agent.Formation.GetReadonlyMovementOrderReference().OrderEnum;
                agent.RefreshBehaviorValues(movementOrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
                if (movementOrderEnum == MovementOrder.MovementOrderEnum.Charge || movementOrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget)
                    return;
            }
            else
            {
                agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            }
            if (VolleyMode == VolleyMode.Auto)
                return;

            //agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.DefaultMove);
            //agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Charge);

            //agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
            ////agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.GoToPos, 1f, 7f, 1f, 20f, 1f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Melee, 1f, 7f, 1f, 20f, 1f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Ranged, 2f, 7f, 2f, 20f, 2f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.ChargeHorseback, 2f, 25f, 5f, 30f, 5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 2f, 15f, 6.5f, 30f, 5.5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityRanged, 5.5f, 12f, 8f, 30f, 4.5f);

            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.GoToPos, 3f, 7f, 1f, 20f, 0.5f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Melee, 0f, 7f, 0f, 20f, 0f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Ranged, 1f, 7f, 1f, 20f, 1f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.ChargeHorseback, 0f, 25f, 0f, 30f, 0f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 0.7f, 15f, 0.7f, 30f, 0.7f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityMelee, 0f, 12f, 0f, 30f, 0f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityRanged, 1f, 12f, 1f, 30f, 1f);

            //agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 1f, 7f, 1f, 20f, 2f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 7f, 5f, 20f, 0.01f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 2f, 7f, 2f, 20f, 2f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 100f, 3f, 10f, 15f, 0.1f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 9f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);

            //agent.RefreshBehaviorValues(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum, agent.Formation.ArrangementOrder.OrderEnum);

            //default move:
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.Melee, 8f, 7f, 5f, 20f, 0.01f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.Ranged, 0.02f, 7f, 0.04f, 20f, 0.03f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.ChargeHorseback, 100f, 3f, 10f, 15f, 0.1f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.RangedHorseback, 0.02f, 15f, 0.065f, 30f, 0.055f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 9f);
            //agent.SetAIBehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, 0.55f, 12f, 0.8f, 30f, 0.45f);

            // default:
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Melee, 8f, 7f, 4f, 20f, 1f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.ChargeHorseback, 2f, 25f, 5f, 30f, 5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 2f, 15f, 6.5f, 30f, 5.5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityRanged, 5.5f, 12f, 8f, 30f, 4.5f);

            //agent.HumanAIComponent?.SyncBehaviorParamsIfNecessary();
            //agent.ForceAiBehaviorSelection();
        }


        public void OnAIInputSet(Agent agent, ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
        {
            if (VolleyMode != VolleyMode.Disabled && agent.IsAIControlled)
            {
                UpdateAITarget(agent);
                bool shouldDisableAttackOnWait = !AllowPreAiming;
                var wieldedWeapon = agent.WieldedWeapon;
                if (agent.Formation != null && (agent.Formation.GetMovementState() == MovementOrder.MovementStateEnum.Charge && shouldDisableAttackOnWait || agent.Formation.FiringOrder == FiringOrder.FiringOrderHoldYourFire) ||
                    !wieldedWeapon.IsEmpty && !wieldedWeapon.CurrentUsageItem.IsRangedWeapon ||
                    // handle edge case, where there's only the last ammo on ranged weapon, HasAnyRangedWeaponCached will be false.
                    // and isReloading will be also false.
                    (!agent.HasAnyRangedWeaponCached && !wieldedWeapon.IsEmpty && wieldedWeapon.CurrentUsageItem.IsRangedWeapon && wieldedWeapon.IsReloading) ||
                    agent.IsDetachedFromFormation || agent.IsUsingGameObject || agent.AIMoveToGameObjectIsEnabled() ||
                    _isTargetAgentNearby && shouldDisableAttackOnWait)
                {
                    if (!IsVolleySuspended)
                    {
                        IsVolleySuspended = true;
                        OnVolleyDisabled(agent);
                        if (_volleyStatus != VolleyStatus.PrepareForShooting)
                        {
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                        }
                        return;
                    }
                }
                else
                {
                    if (IsVolleySuspended)
                    {
                        IsVolleySuspended = false;
                        OnVolleyWait(agent);
                    }
                }
                if (!wieldedWeapon.IsEmpty && wieldedWeapon.CurrentUsageItem.IsRangedWeapon && !IsVolleySuspended)
                {
                    UpdateAIDestination(agent, inputVector);
                    var currentTime = Mission.Current.CurrentTime;
                    var targetVisibilityState = agent.GetLastTargetVisibilityState();
                    switch (_volleyStatus)
                    {
                        case VolleyStatus.CancelAttackBeforeWaitingForOrder:
                            if (IsAttacking(movementFlag) && !agent.WieldedWeapon.IsReloading)
                            {
                                SetCancelAttack(ref movementFlag);
                            }
                            if (!_cancelAttackeBeforeWaitingForOrder.Check(currentTime))
                            {
                                break;
                            }
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            OnVolleyWait(agent);
                            break;
                        case VolleyStatus.WaitingForOrder:
                            if (agent.WieldedWeapon.IsReloading)
                            {
                                _volleyStatus = VolleyStatus.Reloading;
                                OnShootingEnabled(agent);
                                break;
                            }
                            if (IsPreAimingEnabled(agent))
                            {
                                OnShootingEnabled(agent);
                                if (IsAttacking(movementFlag))
                                {
                                    GoToAimingWhileWaitingForOrderState(agent, currentTime);
                                    break;
                                }
                                _tryAimingTimeoutInAutoVolley = false;
                                if (_shouldResetAutoVolleyTimer)
                                {
                                    _autoVolleyAimingTimer.Reset(currentTime, CommandSystemConfig.Get().MaxAimingTime);
                                    _shouldResetAutoVolleyTimer = false;
                                }
                                //_tryAimingWhileWaitingForOrderTimer.Reset(currentTime, 2f);
                                _volleyStatus = VolleyStatus.TryAimingWhileWaitingForOrder;
                                break;
                            }
                            else
                            {
                                if (IsAttacking(movementFlag))
                                {
                                    _shouldResetAutoVolleyTimer = true;
                                    SetCancelAttack(ref movementFlag);
                                    break;
                                }
                            }
                            break;
                        case VolleyStatus.TryAimingWhileWaitingForOrder:
                            if (IsPreAimingEnabled(agent))
                            {
                                if (IsAttacking(movementFlag))
                                {
                                    GoToAimingWhileWaitingForOrderState(agent, currentTime);
                                    break;
                                }

                                if (!_tryAimingTimeoutInAutoVolley && _autoVolleyAimingTimer.Check(currentTime))
                                {
                                    _tryAimingTimeoutInAutoVolley = true;
                                }
                                break;
                            }
                            else
                            {
                                // Aiming Disabled
                                if (IsAttacking(movementFlag))
                                {
                                    SetCancelAttack(ref movementFlag);
                                }
                                OnVolleyWait(agent);
                                _isCandidateForNextFireInAutoVolley = false;
                                _isAvailableForNextFireInAutoVolley = false;
                                _shouldResetAutoVolleyTimer = true;
                                _volleyStatus = VolleyStatus.WaitingForOrder;
                            }
                            break;
                        case VolleyStatus.AimWhileWaitingForOrder:
                            if (IsPreAimingEnabled(agent))
                            {
                                bool isAttackCancelled = IsCancelAttack(movementFlag);
                                if (isAttackCancelled)
                                {
                                    _isCandidateForNextFireInAutoVolley = false;
                                    _isAvailableForNextFireInAutoVolley = false;
                                    //_tryAimingTimeoutInAutoVolley = false;
                                    // attack may be cancelled because of friend in way, etc.
                                    //_autoVolleyAimingTimer.Reset(currentTime, CommandSystemConfig.Get().MaxAimingTime);
                                    //_tryAimingWhileWaitingForOrderTimer.Reset(currentTime, 2f);
                                    _volleyStatus = VolleyStatus.TryAimingWhileWaitingForOrder;
                                    break;
                                }
                                // Keep Aiming
                                if (!IsAttacking(movementFlag))
                                {
                                    _isAvailableForNextFireInAutoVolley = true;
                                    SetAttack(ref movementFlag, true);
                                    var aimingError = agent.CurrentAimingError;
                                    _minAimingError = aimingError;
                                    _volleyStatus = VolleyStatus.AimingDoneWhileWaitingForOrder;
                                    break;
                                }
                                else
                                {
                                    if (!_aimTimeout && _autoVolleyAimingTimer.Check(currentTime))
                                    {
                                        _aimTimeout = true;
                                    }
                                }
                                break;
                            }
                            else
                            {
                                // Aiming Disabled
                                SetCancelAttack(ref movementFlag);
                                OnVolleyWait(agent);
                                _isCandidateForNextFireInAutoVolley = false;
                                _isAvailableForNextFireInAutoVolley = false;
                                _shouldResetAutoVolleyTimer = true;
                                _volleyStatus = VolleyStatus.WaitingForOrder;
                            }
                            break;
                        case VolleyStatus.AimingDoneWhileWaitingForOrder:
                            if (IsPreAimingEnabled(agent))
                            {
                                // Keep Aiming
                                if (!IsAttacking(movementFlag))
                                {
                                    SetAttack(ref movementFlag, true);
                                    break;
                                }
                                bool isAttackCancelled = IsCancelAttack(movementFlag);
                                if (!isAttackCancelled)
                                {
                                    var aimingError = agent.CurrentAimingError;
                                    var aimingTurbulance = agent.CurrentAimingTurbulance;
                                    _minAimingError = MathF.Min(_minAimingError, aimingError);
                                    if (_minAimingError < aimingError && !agent.HasMount && _isAIAtMoveDestination && inputVector == Vec2.Zero)
                                    {
                                        SetCancelAttack(ref movementFlag);
                                        isAttackCancelled = true;
                                    }
                                }
                                if (isAttackCancelled)
                                {
                                    _isCandidateForNextFireInAutoVolley = false;
                                    _isAvailableForNextFireInAutoVolley = false;
                                    // attack may be cancelled because of friend in way, etc.
                                    //_tryAimingTimeoutInAutoVolley = false;
                                    //_autoVolleyAimingTimer.Reset(currentTime, CommandSystemConfig.Get().MaxAimingTime);
                                    //_tryAimingWhileWaitingForOrderTimer.Reset(currentTime, 2f);
                                    _volleyStatus = VolleyStatus.TryAimingWhileWaitingForOrder;
                                    break;
                                }
                            }
                            else
                            {
                                // Aiming Disabled
                                SetCancelAttack(ref movementFlag);
                                OnVolleyWait(agent);
                                _isCandidateForNextFireInAutoVolley = false;
                                _isAvailableForNextFireInAutoVolley = false;
                                _shouldResetAutoVolleyTimer = true;
                                _volleyStatus = VolleyStatus.WaitingForOrder;
                                break;
                            }
                            break;
                        case VolleyStatus.PrepareForShooting:
                            //if (--_prepareForShootingTick > 0)
                            if (!_prepareForShootingTimer.Check(currentTime))
                            {
                                break;
                            }
                            //SetStand(agent, ref inputVector);
                            OnShootingEnabled(agent);
                            if (ForceShootingEnabled)
                            {
                                _volleyStatus = VolleyStatus.ForceDrawing;
                                //_forceDrawingTick = 60;
                                _forceDrawingTimer.Reset(currentTime, 0.0f);
                            }
                            else
                            {
                                _isCandidateForNextFireInAutoVolley = true;
                                _isAvailableForNextFireInAutoVolley = false;
                                _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                                //_allowMovingTick = 100;
                                _allowMovingTimer.Reset(currentTime, 0.6f);
                                //_waitingForLookingForTargetTick = 360;
                                _waitingForLookingForTargetTimer.Reset(currentTime, 3f);
                            }
                            break;
                        case VolleyStatus.ForceDrawing:
                            SetStand(agent, ref inputVector); 
                            if (IsAttacking(movementFlag))
                            {
                                //_drawingTheBowUnderShootingOrderTick = 360;
                                _drawingTheBowUnderShootingOrderTimer.Reset(currentTime, 3.6f);
                                _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                            }
                            //if (--_forceDrawingTick <= 0)
                            if (_forceDrawingTimer.Check(currentTime))
                            {
                                _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                                //_allowMovingTick = 60;
                                _allowMovingTimer.Reset(currentTime, 0.6f);
                                //_waitingForLookingForTargetTick = 360;
                                _waitingForLookingForTargetTimer.Reset(currentTime, 1f);
                                break;
                            }
                            SetAttack(ref movementFlag, true);
                            break;
                        case VolleyStatus.WaitingForLookingForTarget:
                            //SetStand(agent, ref inputVector);
                            // For crossbow, it may be reloading here.
                            if (agent.WieldedWeapon.IsReloading)
                            {
                                break;
                            }
                            if (_allowMovingTimer.Check(currentTime))
                            {
                                SetStand(agent, ref inputVector);
                            }
                            if (IsAttacking(movementFlag))
                            {
                                _drawingTheBowUnderShootingOrderTimer.Reset(currentTime, agent.HasMount ? 15f : 7.2f);
                                _isCandidateForNextFireInAutoVolley = true;
                                _isAvailableForNextFireInAutoVolley = false;
                                _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                                break;
                            }

                            //if (agent.WieldedWeapon.IsReloading)
                            //{
                            //    _volleyStatus = VolleyStatus.Reloading;
                            //    SetWaitingBehavior(agent);
                            //    break;
                            //}

                            if (_waitingForLookingForTargetTimer.Check(currentTime))
                            {
                                if (ForceShootingEnabled && !agent.WieldedWeapon.IsReloading)
                                {
                                    SetCancelAttack(ref movementFlag);
                                }

                                _isCandidateForNextFireInAutoVolley = false;
                                _isAvailableForNextFireInAutoVolley = false;
                                _volleyStatus = VolleyStatus.StandAfterCancelShooting;
                                _standAfterCancelShootingTimer.Reset(currentTime, 0f);
                                break;
                            }
                            else
                            {

                                if (_waitingForLookingForTargetTimer.ElapsedTime() > 2)
                                {
                                    _isCandidateForNextFireInAutoVolley = false;
                                }
                            }
                            if (ForceShootingEnabled)
                            {
                                SetAttack(ref movementFlag, true);
                                break;
                            }
                            break;
                        case VolleyStatus.DrawingTheBowUnderShootingOrder:
                            SetStand(agent, ref inputVector);
                            if (IsCancelAttack(movementFlag))
                            {
                                _isCandidateForNextFireInAutoVolley = false;
                                _isAvailableForNextFireInAutoVolley = false;
                                _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                                //_allowMovingTick = 60;
                                _allowMovingTimer.Reset(currentTime, 0.6f);
                                //_waitingForLookingForTargetTick = 360;
                                _waitingForLookingForTargetTimer.Reset(currentTime, 2f);
                                break;
                            }
                            if (!IsAttacking(movementFlag))
                            {
                                _volleyStatus = VolleyStatus.StandAfterShooted;
                                // It takes max to 2 seconds to throw.
                                _standAfterShootedTimer.Reset(currentTime, 2f);
                                SetWaitingBehavior(agent);
                                break;
                            }
                            // TODO: attacking and reloading?
                            if (agent.WieldedWeapon.IsReloading)
                            {
                                SetCancelAttack(ref movementFlag);
                                _volleyStatus = VolleyStatus.Reloading;
                                SetWaitingBehavior(agent);
                                break;
                            }
                            //if (--_drawingTheBowUnderShootingOrderTick <= 0)
                            if (_drawingTheBowUnderShootingOrderTimer.Check(currentTime))
                            {
                                SetCancelAttack(ref movementFlag);
                                _isCandidateForNextFireInAutoVolley = false;
                                _isAvailableForNextFireInAutoVolley = false;
                                _volleyStatus = VolleyStatus.StandAfterCancelShooting;
                                _standAfterCancelShootingTimer.Reset(currentTime, 0f);
                                SetWaitingBehavior(agent);
                                break;
                            }
                            if (_drawingTheBowUnderShootingOrderTimer.ElapsedTime() > 3)
                            {
                                _isCandidateForNextFireInAutoVolley = false;
                            }
                            break;
                        case VolleyStatus.StandAfterShooted:
                            SetStand(agent, ref inputVector);
                            //SetCancelAttack(ref movementFlag);
                            //SetAttack(ref movementFlag, false);
                            //if (--_standBeforeWaitingForOrderTick <= 0)
                            SetAttack(ref movementFlag, false);
                            // for throwing weapon, after thrown, the wielded weapon will become empty.
                            if (agent.WieldedWeapon.IsReloading || agent.WieldedWeapon.IsEmpty)
                            {
                                _volleyStatus = VolleyStatus.Reloading;
                                SetWaitingBehavior(agent);
                                break;
                            }
                            // for throwing (consumable) weapon, it takes time to throw.
                            if (!_standAfterShootedTimer.Check(currentTime) && (_isTargetAgentOutOfRange || IsHoldingThrownWeapon(agent)))
                            {
                                break;
                            }

                            // TODO: out of ammo?
                            _isCandidateForNextFireInAutoVolley = false;
                            _isAvailableForNextFireInAutoVolley = false;
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            OnVolleyWait(agent);
                            break;
                        case VolleyStatus.StandAfterCancelShooting:
                            SetStand(agent, ref inputVector);
                            SetAttack(ref movementFlag, false);
                            if (!_standAfterCancelShootingTimer.Check(currentTime) && _isTargetAgentOutOfRange)
                            {
                                break;
                            }

                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            OnVolleyWait(agent);
                            break;
                        case VolleyStatus.Reloading:
                            if (!agent.WieldedWeapon.IsReloading && !agent.WieldedWeapon.IsEmpty)
                            {
                                _volleyStatus = VolleyStatus.WaitingForOrder;
                                OnVolleyWait(agent);
                            }
                            //SetStand(agent, ref inputVector);
                            break;
                    }
                }
            }
            else if (_cancelAttackOnVolleyDisabled)
            {
                _cancelAttackOnVolleyDisabled = false;
                SetCancelAttack(ref movementFlag);
            }
        }

        private void GoToAimingWhileWaitingForOrderState(Agent agent, float currentTime)
        {
            _minAimingError = float.MaxValue;
            _maxAimingError = float.MinValue;
            _encouteredFirstMinimumValue = false;
            _encounteredFirstMaximumValue = false;
            _isCandidateForNextFireInAutoVolley = true;
            _isAvailableForNextFireInAutoVolley = false;
            _aimWhileWaitingForOrderTimer.Reset(currentTime, CommandSystemConfig.Get().MaxAimingTime);
            _aimTimeout = false;
            _volleyStatus = VolleyStatus.AimWhileWaitingForOrder;
        }

        private static bool IsHoldingThrownWeapon(Agent agent)
        {
            return !agent.WieldedWeapon.IsEmpty && !agent.WieldedWeapon.IsReloading && agent.WieldedWeapon.CurrentUsageItem.IsConsumable;
        }

        private static bool IsAttacking(Agent.MovementControlFlag movementFlag)
        {
            return (movementFlag & Agent.MovementControlFlag.AttackMask) != Agent.MovementControlFlag.None;
        }

        private static void SetAttack(ref Agent.MovementControlFlag movementFlag, bool attack)
        {
            if (attack)
            {
                movementFlag |= Agent.MovementControlFlag.AttackDown;
            }
            else
            {
                movementFlag &= ~Agent.MovementControlFlag.AttackMask;
            }
        }

        private static bool IsCancelAttack(Agent.MovementControlFlag movementFlag)
        {
            return (movementFlag & Agent.MovementControlFlag.DefendMask) != Agent.MovementControlFlag.None;
        }

        private static void SetCancelAttack(ref Agent.MovementControlFlag movementFlag)
        {
            movementFlag &= ~Agent.MovementControlFlag.AttackMask;
            movementFlag |= Agent.MovementControlFlag.DefendDown;
        }

        private void SetStand(Agent agent, ref Vec2 inputVector)
        {
            if (agent.HasMount || VolleyMode == VolleyMode.Auto)
                return;
            if (_isTargetAgentOutOfRange && !_isAIAtMoveDestination && !_isMovingToDestination)
            {
                inputVector = Vec2.Zero;
            }
        }

        private void UpdateAIDestination(Agent agent, Vec2 inputVector)
        {
            _aiMoveDestination = agent.GetAIMoveDestination();
            _isAIAtMoveDestination = IsAIAtMoveDestination(agent);
            if (agent.Formation == null)
            {
                _isMovingToDestination = true;
                return;
            }
            _formationPosition = agent.Formation.GetOrderPositionOfUnit(agent);
            if (!_formationPosition.IsValid)
            {
                _isMovingToDestination = true;
                return;
            }
            _agentFrame = agent.Frame;
            var vecToFormationPosition = _formationPosition.GetGroundVec3() - agent.Position;
            _distanceToFormationPosition = vecToFormationPosition.Normalize();
            if (inputVector.LengthSquared < 0.1 || _isAIAtMoveDestination)
            {
                _isMovingToDestination = false;
            }
            else
            {
                var movementVector = _agentFrame.rotation.TransformToParent(inputVector.ToVec3(0)).NormalizedCopy();
                var cos = Vec3.DotProduct(movementVector, vecToFormationPosition);
                _isMovingToDestination = cos > 0.2;
            }
        }

        private void UpdateAITarget(Agent agent)
        {
            var targetAgent = agent.GetTargetAgent() ?? agent.ImmediateEnemy;
            if (targetAgent == null)
            {
                _isTargetAgentOutOfRange = true;
                _isTargetAgentNearby = false;
                return;
            }

            var distanceSquared = targetAgent.Position.DistanceSquared(agent.Position);
            var range = agent.GetMissileRangeWithHeightDifferenceAux(targetAgent.Position.z);

            _isTargetAgentOutOfRange = distanceSquared > range * range;
            _isTargetAgentNearby = distanceSquared < 25;
        }

        private bool IsAIAtMoveDestination(Agent agent)
        {
            float moveStartTolerance = agent.GetAIMoveStartTolerance();
            return (double)_aiMoveDestination.AsVec2.DistanceSquared(agent.Position.AsVec2) <= (double)moveStartTolerance * (double)moveStartTolerance;
        }

        private bool IsVolleyStatusDrawing(Agent agent)
        {
            switch (_volleyStatus)
            {
                case VolleyStatus.AimWhileWaitingForOrder:
                case VolleyStatus.DrawingTheBowUnderShootingOrder:
                case VolleyStatus.ForceDrawing:
                    return true;
            }
            return false;
        }

    }
}
