using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.HumanAIComponent;

namespace RTSCamera.CommandSystem.Logic.SubLogic
{
    public class AgentAIInputHandler
    {
        private enum VolleyStatus
        {
            WaitingForOrder,
            AimingWhileWaitingForOrder,
            PrepareForShooting,
            ForceDrawing,
            WaitingForLookingForTarget,
            DrawingTheBowUnderShootingOrder,
            ArrowReleased
        }
        public bool IsVolleyEnabled { get; private set; } = false;
        private VolleyStatus _volleyStatus;
        private int _prepareForShootingTick;
        private int _arrowReleasedTick;
        private int _forceDrawingTick;
        private int _waitingForLookingForTargetTick;
        public static bool ForceShootingEnabled = true;
        public void SetVolleyEnabled(Agent agent, bool enabled)
        {
            IsVolleyEnabled = enabled;
            if (IsVolleyEnabled)
            {
                _volleyStatus = VolleyStatus.WaitingForOrder;
                SetWaitingbehavior(agent);
            }
            else
            {
                agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanAttack);
            }
        }

        public void ShootUnderVolley(Agent agent)
        {
            if (IsVolleyEnabled)
            {
                switch (_volleyStatus)
                {
                    case VolleyStatus.WaitingForOrder:
                        _volleyStatus = VolleyStatus.PrepareForShooting;
                        _prepareForShootingTick = (int)(MBRandom.RandomFloat * 240);
                        SetShootingBehavior(agent);
                        break;
                    case VolleyStatus.AimingWhileWaitingForOrder:
                        _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                        break;
                }
            }
        }

        private void SetWaitingbehavior(Agent agent)
        {
            agent.SetAgentFlags(agent.GetAgentFlags() & ~AgentFlag.CanAttack);
            if (agent.Formation != null)
            {
                agent.RefreshBehaviorValues(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
            }
            else
            {
                agent.SetBehaviorValueSet(HumanAIComponent.BehaviorValueSet.Default);
            }
        }

        private void SetShootingBehavior(Agent agent)
        {
            agent.SetAgentFlags(agent.GetAgentFlags() | AgentFlag.CanAttack);

            if (agent.Formation != null)
            {
                agent.RefreshBehaviorValues(agent.Formation.GetReadonlyMovementOrderReference().OrderEnum, agent.Formation.ArrangementOrder.OrderEnum);
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

            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.GoToPos, 1f, 7f, 1f, 20f, 1f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Melee, 1f, 7f, 1f, 20f, 1f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 6f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.ChargeHorseback, 2f, 25f, 5f, 30f, 5f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 2f, 15f, 6.5f, 30f, 5.5f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityMelee, 1f, 12f, 1f, 30f, 1f);
            agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityRanged, 1f, 12f, 1f, 30f, 1f);

            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.GoToPos, 3f, 7f, 5f, 20f, 6f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Melee, 8f, 7f, 4f, 20f, 1f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.Ranged, 2f, 7f, 4f, 20f, 5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.ChargeHorseback, 2f, 25f, 5f, 30f, 5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.RangedHorseback, 2f, 15f, 6.5f, 30f, 5.5f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityMelee, 5f, 12f, 7.5f, 30f, 4f);
            //agent.SetAIBehaviorValues(HumanAIComponent.AISimpleBehaviorKind.AttackEntityRanged, 5.5f, 12f, 8f, 30f, 4.5f);
        }


        public void OnAIInputSet(Agent agent, ref Agent.EventControlFlag eventFlag, ref Agent.MovementControlFlag movementFlag, ref Vec2 inputVector)
        {
            if (IsVolleyEnabled)
            {
                switch (_volleyStatus)
                {
                    case VolleyStatus.WaitingForOrder:
                        if ((movementFlag & Agent.MovementControlFlag.AttackDown) != Agent.MovementControlFlag.None)
                        {
                            //_volleyStatus = VolleyStatus.AimingWhileWaitingForOrder;
                            movementFlag |= Agent.MovementControlFlag.DefendDown;
                        }
                        else
                        {
                            //movementFlag |= Agent.MovementControlFlag.DefendDown;
                        }
                        break;
                    case VolleyStatus.AimingWhileWaitingForOrder:
                        if (movementFlag == Agent.MovementControlFlag.None)
                        {
                            movementFlag |= Agent.MovementControlFlag.DefendDown;
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            SetWaitingbehavior(agent);
                        }
                        break;
                    case VolleyStatus.PrepareForShooting:
                        inputVector = Vec2.Zero;
                        if (--_prepareForShootingTick > 0)
                            break;
                        if (ForceShootingEnabled)
                        {
                            _volleyStatus = VolleyStatus.ForceDrawing;
                            _forceDrawingTick = 60;
                        }
                        else
                        {
                            _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                            _waitingForLookingForTargetTick = 360;
                        }
                        break;
                    case VolleyStatus.ForceDrawing:
                        inputVector = Vec2.Zero;
                        if (--_forceDrawingTick <= 0)
                        {
                            _volleyStatus = VolleyStatus.WaitingForLookingForTarget;
                            _waitingForLookingForTargetTick = 360;
                            break;
                        }
                        movementFlag |= Agent.MovementControlFlag.AttackDown;
                        break;
                    case VolleyStatus.WaitingForLookingForTarget:
                        inputVector = Vec2.Zero;
                        if ((movementFlag & Agent.MovementControlFlag.AttackDown) != Agent.MovementControlFlag.None)
                        {
                            _volleyStatus = VolleyStatus.DrawingTheBowUnderShootingOrder;
                            break;
                        }
                        if (--_waitingForLookingForTargetTick <= 0)
                        {
                            movementFlag |= Agent.MovementControlFlag.DefendDown;
                            _volleyStatus = VolleyStatus.WaitingForOrder;
                            SetWaitingbehavior(agent);
                            break;
                        }
                        if (ForceShootingEnabled)
                        {
                            movementFlag |= Agent.MovementControlFlag.AttackDown;
                        }
                        break;
                    case VolleyStatus.DrawingTheBowUnderShootingOrder:
                        inputVector = Vec2.Zero;
                        if ((movementFlag & Agent.MovementControlFlag.AttackDown) == Agent.MovementControlFlag.None)
                        {
                            _volleyStatus = VolleyStatus.ArrowReleased;
                            _arrowReleasedTick = 60;
                        }
                        break;
                    case VolleyStatus.ArrowReleased:
                        inputVector = Vec2.Zero;
                        if (--_arrowReleasedTick > 0)
                            break;
                        _volleyStatus = VolleyStatus.WaitingForOrder;
                        SetWaitingbehavior(agent);
                        break;
                }
            }
        }
    }
}
