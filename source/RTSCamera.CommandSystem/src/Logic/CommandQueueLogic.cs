using MissionSharedLibrary.Utilities;
using NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Logic
{
    public enum CustomOrderType
    {
        Original,
        MoveToLineSegment,
        MoveToLineSegmentWithHorizontalLayout,
        ToggleFire,
        ToggleFacing,
        ToggleMount,
        ToggleAI,
        FollowMainAgent,
        SetTargetFormation
    }
    public class OrderInQueue
    {
        private List<Formation> _selectedFormation;
        // Formations that will or is executing this order.
        // If the formation cancels the order, it will be removed from this list.
        public List<Formation> SelectedFormations
        {
            get => _selectedFormation;
            set
            {
                _selectedFormation = value.ToList();
                RemainingFormations = value.ToList();
            }
        }

        // Formations that have not started executing yet.
        // If the formtation starts executing the order, it will be removed from this list.
        public List<Formation> RemainingFormations { get; set; }
        public CustomOrderType CustomOrderType { get; set; } = CustomOrderType.Original;
        public OrderType OrderType { get; set; }
        public WorldPosition PositionBegin { get; set; }
        public WorldPosition PositionEnd { get; set; }
        public Formation TargetFormation { get; set; }
        public Agent TargetAgent { get; set; }

        public bool IsLineShort { get; set; }

        public bool ShouldLockFormationInFacingOrder { get; set; }

        public List<(Formation formation, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction)> FormationChanges { get; set; }

        public Dictionary<Formation, Vec2> VirtualPositions { get; set; } = new Dictionary<Formation, Vec2>();

        public Dictionary<Formation, Vec2> VirtualDirections { get; set; } = new Dictionary<Formation, Vec2>();
    }

    public static class CommandQueueLogic
    {
        public static List<OrderInQueue> OrderQueue = new List<OrderInQueue>();
        // Orders that formation is pending on.
        // Formation will continue when all the selected formations complete the order.
        public static Dictionary<Formation, OrderInQueue> PendingOrders = new Dictionary<Formation, OrderInQueue>();
        public static Dictionary<Formation, bool> ShouldSkipCurrentOrders = new Dictionary<Formation, bool>();

        // virtual positions of last executed order.
        public static Dictionary<Formation, Vec2> VirtualPositions = new Dictionary<Formation, Vec2>();
        public static Dictionary<Formation, Vec2> VirtualDirections = new Dictionary<Formation, Vec2>();

        public static void OnBehaviorInitialize()
        {
            OrderQueue = new List<OrderInQueue>();
            PendingOrders = new Dictionary<Formation, OrderInQueue>();
            ShouldSkipCurrentOrders = new Dictionary<Formation, bool>();
            VirtualPositions = new Dictionary<Formation, Vec2>();
            VirtualDirections = new Dictionary<Formation, Vec2>();
        }

        public static void OnRemoveBehavior()
        {
            OrderQueue = null;
            PendingOrders = null;
            ShouldSkipCurrentOrders = null;
            VirtualPositions = null;
            VirtualDirections = null;
        }

        public static void AddOrderToQueue(OrderInQueue order)
        {
            OrderQueue.Add(order);
            Utility.DisplayMessage($"Added command to queue: {order.CustomOrderType}, {order.OrderType}");
        }

        public static void ClearOrderInQueue(IEnumerable<Formation> formations)
        {
            foreach (var order in OrderQueue.ToList())
            {
                order.SelectedFormations.RemoveAll(f => formations.Contains(f));
                order.RemainingFormations.RemoveAll(f => formations.Contains(f));
                if (order.SelectedFormations.Count == 0)
                    OrderQueue.Remove(order);
            }
            foreach (var formation in formations)
            {
                PendingOrders.Remove(formation);
            }
        }

        public static void SkipCurrentOrderForFormations(IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                ShouldSkipCurrentOrders[formation] = true;
            }
        }
        public static void SetVirtualPositions(IEnumerable<KeyValuePair<Formation, Vec2>> virtualPositions)
        {
            foreach (var pair in virtualPositions)
            {
                VirtualPositions[pair.Key] = pair.Value;
            }
        }

        private static void SetVirtualDirections(IEnumerable<KeyValuePair<Formation, Vec2>> virtualDirections)
        {
            foreach (var pair in virtualDirections)
            {
                VirtualDirections[pair.Key] = pair.Value;
            }
        }

        public static IEnumerable<KeyValuePair<Formation, Vec2>> CollectVirtualPositions(IEnumerable<Formation> formations)
        {
            return VirtualPositions.Where(pair => formations.Contains(pair.Key));
        }

        public static IEnumerable<KeyValuePair<Formation, Vec2>> CollectVirtualDirections(IEnumerable<Formation> formations)
        {
            return VirtualDirections.Where(pair => formations.Contains(pair.Key));
        }

        public static void UpdateFormation(Formation formation)
        {
            while (!formation.GetReadonlyMovementOrderReference().IsApplicable(formation) ||
                IsPendingOrderCompleted(formation))
            {
                var order = GetOrderForFormation(formation);
                if (order == null)
                    return;

                ExecuteOrderForFormation(order, formation);
                OnOrderExecutedForFormation(order, formation);
            }
        }

        public static bool IsMovementOrderCompleted(Formation formation)
        {
            return formation.OrderPositionIsValid ? formation.CurrentPosition.Distance(formation.OrderPosition) < 2 : false;
        }

        public static bool IsFacingOrderCompleted(Formation formation)
        {
            return formation.CurrentDirection.DotProduct(formation.Direction) > 0.99;
        }

        public static bool IsPendingOrderCompleted(Formation formation)
        {
            if (PendingOrders.TryGetValue(formation, out var order))
            {
                foreach (var otherFormation in order.SelectedFormations)
                {
                    if (otherFormation != formation)
                    {
                        if (PendingOrders.TryGetValue(otherFormation, out var otherOrder))
                        {
                            // other formations are executing other orders.
                            // waiting for them to execute this order.
                            if (otherOrder != order)
                            {
                                return false;
                            }
                            else
                            {
                                if (!IsMovementOrderCompleted(otherFormation) || !IsFacingOrderCompleted(otherFormation))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    if (otherFormation == formation)
                    {
                        if (!IsMovementOrderCompleted(formation) || !IsFacingOrderCompleted(formation))
                        {
                            return false;
                        }
                    }
                }

                // All formations in the order completed the order.
                foreach (var otherFormation in order.SelectedFormations)
                {
                    if (PendingOrders.TryGetValue(otherFormation, out var otherOrder))
                    {
                        PendingOrders.Remove(otherFormation);
                    }
                }
            }
            if (ShouldSkipCurrentOrders.TryGetValue(formation, out var result) && result)
            {
                ShouldSkipCurrentOrders[formation] = false;
                return true;
            }
            return IsMovementOrderCompleted(formation) && IsFacingOrderCompleted(formation);
        }

        public static void ExecuteOrderForFormation(OrderInQueue order, Formation formation)
        {
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.Charge:
                                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                                if (order.TargetFormation != null)
                                {
                                    formation.SetTargetFormation(order.TargetFormation);
                                }
                                break;
                            case OrderType.ChargeWithTarget:
                                formation.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(formation));
                                if (order.TargetFormation != null)
                                {
                                    Utilities.Utility.DisplayFormationChargeMessage(formation);
                                    formation.SetTargetFormation(order.TargetFormation);
                                }
                                break;
                            case OrderType.LookAtDirection:
                                FacingOrderLookAtDirection(order, formation);
                                FormationPendingOrder(formation, order);
                                SetVirtualPositions(order.VirtualPositions.Where(pair => pair.Key == formation));
                                SetVirtualDirections(order.VirtualDirections.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.LookAtEnemy:
                                TryCancelStopOrder(formation);
                                formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                                FormationPendingOrder(formation, order);
                                break;
                            case OrderType.FollowMe:
                                formation.SetMovementOrder(MovementOrder.MovementOrderFollow(order.TargetAgent));
                                break;
                            case OrderType.Advance:
                                formation.SetMovementOrder(MovementOrder.MovementOrderAdvance);
                                if (order.TargetFormation != null)
                                {
                                    formation.SetTargetFormation(order.TargetFormation);
                                }
                                FormationPendingOrder(formation, order);
                                break;
                            case OrderType.FallBack:
                                formation.SetMovementOrder(MovementOrder.MovementOrderFallBack);
                                break;
                            case OrderType.StandYourGround:
                                formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                                break;
                            case OrderType.Retreat:
                                formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
                                break;
                            case OrderType.ArrangementLine:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                                break;
                            case OrderType.ArrangementCloseOrder:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
                                break;
                            case OrderType.ArrangementLoose:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                                break;
                            case OrderType.ArrangementCircular:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
                                break;
                            case OrderType.ArrangementSchiltron:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSquare;
                                break;
                            case OrderType.ArrangementVee:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSkein;
                                break;
                            case OrderType.ArrangementColumn:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderColumn;
                                break;
                            case OrderType.ArrangementScatter:
                                TryCancelStopOrder(formation);
                                formation.ArrangementOrder = ArrangementOrder.ArrangementOrderScatter;
                                break;
                            default:
                                Utility.DisplayMessage("Error: unexpected order type");
                                break;
                        }
                        break;
                    }
                case CustomOrderType.MoveToLineSegment:
                case CustomOrderType.MoveToLineSegmentWithHorizontalLayout:
                    {
                        var formationChanges = order.FormationChanges;
                        (Formation f, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction) = formationChanges.First(c => c.formation == formation);
                        if (order.IsLineShort)
                        {
                            switch (OrderController.GetActiveFacingOrderOf(formation))
                            {
                                case OrderType.LookAtEnemy:
                                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                                    break;
                                case OrderType.LookAtDirection:
                                    formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                                    formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                                    break;
                            }
                        }
                        else
                        {
                            formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                            formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                            formation.FormOrder = FormOrder.FormOrderCustom(customWidth);
                        }
                        FormationPendingOrder(formation, order);
                        SetVirtualPositions(order.VirtualPositions.Where(pair => pair.Key == formation));
                        SetVirtualDirections(order.VirtualDirections.Where(pair => pair.Key == formation));
                        break;
                    }
                case CustomOrderType.ToggleFacing:
                    if (order.SelectedFormations.Any(f => f.FacingOrder.OrderType == OrderType.LookAtDirection))
                    {
                        TryCancelStopOrder(formation);
                        formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                    }
                    else
                    {
                        FacingOrderLookAtDirection(order, formation);
                    }
                    FormationPendingOrder(formation, order);
                    SetVirtualPositions(order.VirtualPositions.Where(pair => pair.Key == formation));
                    SetVirtualDirections(order.VirtualDirections.Where(pair => pair.Key == formation));
                    break;
                case CustomOrderType.ToggleFire:
                    if (order.SelectedFormations.Any(f => f.FiringOrder.OrderType == OrderType.FireAtWill))
                    {
                        formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                    }
                    else
                    {
                        formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                    }
                    break;
                case CustomOrderType.ToggleMount:
                    if (order.SelectedFormations.Any(f => f.RidingOrder.OrderType == OrderType.Dismount))
                    {
                        if (formation.PhysicalClass.IsMounted() || formation.HasAnyMountedUnit)
                            TryCancelStopOrder(formation);
                        formation.RidingOrder = RidingOrder.RidingOrderMount;
                    }
                    else
                    {
                        if (formation.PhysicalClass.IsMounted() || formation.HasAnyMountedUnit)
                            TryCancelStopOrder(formation);
                        formation.RidingOrder = RidingOrder.RidingOrderDismount;
                    }
                    break;
                case CustomOrderType.ToggleAI:
                    if (order.SelectedFormations.Any(f => f.IsAIControlled))
                    {
                        formation.SetControlledByAI(false);
                    }
                    else
                    {
                        formation.SetControlledByAI(true);
                        ClearOrderInQueue(new List<Formation> { formation });
                    }
                    break;
                case CustomOrderType.FollowMainAgent:
                            break;
                case CustomOrderType.SetTargetFormation:
                    Utilities.Utility.DisplayFocusAttackMessage(formation, order.TargetFormation);
                    formation.SetTargetFormation(order.TargetFormation);
                    break;
            }
        }
        private static void TryCancelStopOrder(Formation formation)
        {
            if (GameNetwork.IsClientOrReplay || formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Stop)
                return;
            WorldPosition orderWorldPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
            if (!orderWorldPosition.IsValid)
                return;
            formation.SetMovementOrder(MovementOrder.MovementOrderMove(orderWorldPosition));
        }

        private static void FacingOrderLookAtDirection(OrderInQueue order, Formation formation)
        {
            if (order.ShouldLockFormationInFacingOrder)
            {
                var formationChanges = order.FormationChanges;
                (Formation f, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction) = formationChanges.First(c => c.formation == formation);
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
            }
            else
            {
                FacingOrder facingOrder = FacingOrder.FacingOrderLookAtDirection(OrderController.GetOrderLookAtDirection(order.SelectedFormations, order.PositionBegin.AsVec2));
            }
        }

        private static OrderInQueue GetOrderForFormation(Formation formation)
        {
            var order = OrderQueue.FirstOrDefault(order => order.RemainingFormations.Contains(formation));
            return order;
        }

        private static void OnOrderExecutedForFormation(OrderInQueue order, Formation formation)
        {
            order.RemainingFormations.Remove(formation);
            if (order.RemainingFormations.Count == 0)
            {
                OrderQueue.Remove(order);
            }
        }

        private static void FormationPendingOrder(Formation formation, OrderInQueue order)
        {
            PendingOrders[formation] = order;
        }
    }
}
