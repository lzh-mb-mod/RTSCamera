using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic.SubLogic
{
    public class AgentAIInputHandler
    {
        private enum VolleyStatus
        {
            CancelAttackBeforeWaitingForOrder,
            Reloading,
            WaitingForOrder,
            AimingWhileWaitingForOrder,
            PrepareForShooting,
            ForceDrawing,
            WaitingForLookingForTarget,
            DrawingTheBowUnderShootingOrder,
            StandBeforeWaitingForOrder
        }
        public bool IsVolleyEnabled { get; private set; } = false;
        private VolleyStatus _volleyStatus;

        private bool _volleySuspended = false;
        private Timer _cancelAttackeBeforeWaitingForOrder = new Timer(-1, -1, false);
        private Timer _prepareForShootingTimer = new Timer(-1, -1, false);
        private Timer _forceDrawingTimer = new Timer(-1, -1, false);
        private Timer _waitingForLookingForTargetTimer = new Timer(-1, -1, false);
        private Timer _allowMovingTimer = new Timer(-1, -1, false);
        private Timer _drawingTheBowUnderShootingOrderTimer = new Timer(-1, -1, false);
        private Timer _standBeforeWaitingForOrderTimer = new Timer(-1, -1, false);
        private int _prepareForShootingTick;
        private int _forceDrawingTick;
        private int _waitingForLookingForTargetTick;
        private int _allowMovingTick;
        private int _drawingTheBowUnderShootingOrderTick;
        private int _standBeforeWaitingForOrderTick;
        public static bool ForceShootingEnabled = false;
        public void SetVolleyEnabled(Agent agent, bool enabled)
        {
            if (IsVolleyEnabled == enabled)
                return;
            IsVolleyEnabled = enabled;
            if (IsVolleyEnabled)
            {
                _cancelAttackeBeforeWaitingForOrder.Reset(Mission.Current.CurrentTime, MBRandom.RandomFloat * 0.6f);
                _volleyStatus = VolleyStatus.CancelAttackBeforeWaitingForOrder;
            }
            else
            {
                _volleySuspended = false;
                SetCanAttack(agent, true);
                SetWaitingBehavior(agent);
            }
        }

        public void ShootUnderVolley(Agent agent)
        {
            if (IsVolleyEnabled)
            {
                switch (_volleyStatus)
                {
                    case VolleyStatus.CancelAttackBeforeWaitingForOrder:
                    case VolleyStatus.WaitingForOrder:
                    case VolleyStatus.StandBeforeWaitingForOrder:
                    case VolleyStatus.Reloading:
                        _volleyStatus = VolleyStatus.PrepareForShooting;
                        _prepareForShootingTimer.Reset(Mission.Current.CurrentTime, MBRandom.RandomFloat * 0.6f);
                        //_prepareForShootingTick = (int)(MBRandom.RandomFloat * 60);
                        break;
                    case VolleyStatus.AimingWhileWaitingForOrder:
                        _drawingTheBowUnderShootingOrderTimer.Reset(Mission.Current.CurrentTime, 7.2f);
                        _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                        break;
                }
            }
        }

        public void OnFormationSet(Agent agent)
        {
            bool newVolleyEnabled = false;
            if (agent.Formation != null)
            {
                newVolleyEnabled = CommandQueueLogic.IsFormationVolleyEnabled(agent.Formation);
            }
            if (newVolleyEnabled != IsVolleyEnabled)
            {
                SetVolleyEnabled(agent, newVolleyEnabled);
            }
        }

        private static void SetCanAttack(Agent agent, bool canAttack)
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

        private void SetWaitingBehavior(Agent agent)
        {
            //SetCanAttack(agent, false);
            if (agent.Formation != null)
            {
                agent.RefreshBehaviorValues(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
            }
            else
            {
                agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            }
            //agent.HumanAIComponent?.SyncBehaviorParamsIfNecessary();
        }

        private void SetShootingBehavior(Agent agent)
        {
            if (agent.Formation != null)
            {
                //agent.RefreshBehaviorValues(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
            }
            else
            {
                agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            }

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
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 0f, 15f, 1f, 30f, 1f);
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
        }


        public void OnAIInputSet(Agent agent, ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
        {
            if (IsVolleyEnabled)
            {
                var wieldedWeapon = agent.WieldedWeapon;
                if (agent.Formation != null && (agent.Formation.GetMovementState() == MovementOrder.MovementStateEnum.Charge || agent.Formation.FiringOrder == FiringOrder.FiringOrderHoldYourFire) ||
                    !wieldedWeapon.IsEmpty && wieldedWeapon.CurrentUsageItem.IsMeleeWeapon ||
                    !agent.HasAnyRangedWeaponCached)
                {
                    if (!_volleySuspended)
                    {
                        _volleySuspended = true;
                        SetCanAttack(agent, true);
                        SetWaitingBehavior(agent);
                        if (_volleyStatus != VolleyStatus.PrepareForShooting)
                        {
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                        }
                        return;
                    }
                }
                else
                {
                    if (_volleySuspended)
                    {
                        _volleySuspended = false;
                    }
                }
                if (!wieldedWeapon.IsEmpty && wieldedWeapon.CurrentUsageItem.IsRangedWeapon)
                {
                    var currentTime = Mission.Current.CurrentTime;
                    switch (_volleyStatus)
                    {
                        case VolleyStatus.CancelAttackBeforeWaitingForOrder:
                            if (!_cancelAttackeBeforeWaitingForOrder.Check(currentTime))
                            {
                                break;
                            }
                            if (IsAttacking(movementFlag))
                            {
                                SetCancelAttack(ref movementFlag);
                            }
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            SetCanAttack(agent, false);
                            SetWaitingBehavior(agent);
                            break;
                        case VolleyStatus.WaitingForOrder:
                            if (IsAttacking(movementFlag) && !agent.WieldedWeapon.IsReloading)
                            {
                                //_volleyStatus = VolleyStatus.AimingWhileWaitingForOrder;
                                SetAttack(ref movementFlag, false);
                            }
                            else
                            {
                                //movementFlag |= Agent.MovementControlFlag.DefendDown;
                            }
                            break;
                        case VolleyStatus.AimingWhileWaitingForOrder:
                            if (movementFlag == Agent.MovementControlFlag.None)
                            {
                                SetCancelAttack(ref movementFlag);
                                _volleyStatus = VolleyStatus.WaitingForOrder;
                                SetWaitingBehavior(agent);
                                SetCanAttack(agent, false);
                            }
                            break;
                        case VolleyStatus.PrepareForShooting:
                            //if (--_prepareForShootingTick > 0)
                            if (!_prepareForShootingTimer.Check(currentTime))
                            {
                                break;
                            }
                            //SetStand(agent, ref inputVector);
                            SetCanAttack(agent, true);
                            SetShootingBehavior(agent);
                            if (ForceShootingEnabled)
                            {
                                _volleyStatus = VolleyStatus.ForceDrawing;
                                //_forceDrawingTick = 60;
                                _forceDrawingTimer.Reset(currentTime, 0.0f);
                            }
                            else
                            {
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
                            //if (_allowMovingTick <= 0)
                            if (_allowMovingTimer.Check(currentTime))
                            {
                                SetStand(agent, ref inputVector);
                            }
                            else
                            {
                                //--_allowMovingTick;
                            }
                            if (IsAttacking(movementFlag))
                            {
                                //_drawingTheBowUnderShootingOrderTick = 720;
                                _drawingTheBowUnderShootingOrderTimer.Reset(currentTime, agent.HasMount ? 15f : 7.2f);
                                _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                                break;
                            }
                            //if (--_waitingForLookingForTargetTick <= 0)
                            if (_waitingForLookingForTargetTimer.Check(currentTime))
                            {
                                //SetCancelAttack(ref movementFlag);

                                _volleyStatus = VolleyStatus.StandBeforeWaitingForOrder;
                                //_standBeforeWaitingForOrderTick = 0;
                                //SetCanAttack(agent, false);
                                //SetWaitingBehavior(agent);
                                _standBeforeWaitingForOrderTimer.Reset(currentTime, 0.5f);
                                break;
                            }
                            if (ForceShootingEnabled)
                            {
                                SetAttack(ref movementFlag, true);
                                break;
                            }
                            break;
                        case VolleyStatus.DrawingTheBowUnderShootingOrder:
                            SetStand(agent, ref inputVector);
                            if (!IsAttacking(movementFlag))
                            {
                                _volleyStatus = VolleyStatus.StandBeforeWaitingForOrder;
                                //_standBeforeWaitingForOrderTick = 60;
                                _standBeforeWaitingForOrderTimer.Reset(currentTime, 0.5f);
                                SetWaitingBehavior(agent);
                                break;
                            }
                            //if (--_drawingTheBowUnderShootingOrderTick <= 0)
                            if (_drawingTheBowUnderShootingOrderTimer.Check(currentTime))
                            {
                                SetCancelAttack(ref movementFlag);
                                _volleyStatus = VolleyStatus.StandBeforeWaitingForOrder;
                                _standBeforeWaitingForOrderTimer.Reset(currentTime, 0.6f);
                                SetWaitingBehavior(agent);
                                break;
                            }
                            break;
                        case VolleyStatus.StandBeforeWaitingForOrder:
                            SetStand(agent, ref inputVector);
                            //if (agent.WieldedWeapon.IsReloading)
                            //{
                            //    _volleyStatus = VolleyStatus.Reloading;
                            //    SetWaitingbehavior(agent);
                            //    break;
                            //}
                            //SetCancelAttack(ref movementFlag);
                            //SetAttack(ref movementFlag, false);
                            //if (--_standBeforeWaitingForOrderTick <= 0)
                            if (!_standBeforeWaitingForOrderTimer.Check(currentTime))
                                break;
                            if (agent.WieldedWeapon.IsReloading)
                            {
                                _volleyStatus = VolleyStatus.Reloading;
                                SetWaitingBehavior(agent);
                                break;
                            }

                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            SetCanAttack(agent, false);
                            SetWaitingBehavior(agent);
                            break;
                        case VolleyStatus.Reloading:
                            if (!agent.WieldedWeapon.IsReloading)
                            {
                                _volleyStatus = VolleyStatus.WaitingForOrder;
                                SetCanAttack(agent, false);
                                SetWaitingBehavior(agent);
                            }
                            //SetStand(agent, ref inputVector);
                            break;
                    }
                }
            }
        }

        private static bool IsAttacking(Agent.MovementControlFlag movementFlag)
        {
            return (movementFlag & Agent.MovementControlFlag.AttackDown) != Agent.MovementControlFlag.None;
        }

        private static void SetAttack(ref Agent.MovementControlFlag movementFlag, bool attack)
        {
            if (attack)
            {
                movementFlag |= Agent.MovementControlFlag.AttackDown;
            }
            else
            {
                movementFlag &= ~Agent.MovementControlFlag.AttackDown;
            }
        }

        private static void SetCancelAttack(ref Agent.MovementControlFlag movementFlag)
        {
            movementFlag |= Agent.MovementControlFlag.DefendDown;
        }

        private static void SetStand(Agent agent, ref Vec2 inputVector)
        {
            if (agent.HasMount)
                return;
            inputVector = Vec2.Zero;
        }
    }
}
