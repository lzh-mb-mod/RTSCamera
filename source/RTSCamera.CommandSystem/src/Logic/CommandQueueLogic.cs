using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Patch;
using RTSCamera.CommandSystem.QuerySystem;
using RTSCamera.CommandSystem.View;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;

namespace RTSCamera.CommandSystem.Logic
{
    public enum CustomOrderType
    {
        Original,
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
            }
        }

        // Formations that have not started executing yet.
        // If the formtation starts executing the order, it will be removed from this list.
        public List<Formation> RemainingFormations { get; set; } = new List<Formation>();
        public CustomOrderType CustomOrderType { get; set; } = CustomOrderType.Original;
        public OrderType OrderType { get; set; }
        public WorldPosition PositionBegin { get; set; }
        public WorldPosition PositionEnd { get; set; }
        public Formation TargetFormation { get; set; }
        public Agent TargetAgent { get; set; }

        public IOrderable TargetEntity { get; set; }

        public bool IsLineShort { get; set; }

        public Dictionary<Formation, bool> ShouldLockFormationInFacingOrder { get; set; } = new Dictionary<Formation, bool>();

        public List<(Formation formation, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction)> ActualFormationChanges { get; set; } = new List<(Formation formation, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction)>();

        public Dictionary<Formation, FormationChange> VirtualFormationChanges { get; set; } = new Dictionary<Formation, FormationChange>();
    }

    public static class CommandQueueLogic
    {
        public static List<OrderInQueue> OrderQueue = new List<OrderInQueue>();
        // Orders that formation is pending on.
        // Formation will continue when all the selected formations complete the order.
        public static Dictionary<Formation, OrderInQueue> PendingOrders = new Dictionary<Formation, OrderInQueue>();
        public static Dictionary<Formation, bool> ShouldSkipCurrentOrders = new Dictionary<Formation, bool>();

        // virtual positions of last executed order.
        public static FormationChanges CurrentFormationChanges = new FormationChanges();
        public static FormationChanges LatestOrderInQueueChanges = new FormationChanges();
        private static int TicksToSkip = 0;

        public static void OnBehaviorInitialize()
        {
            OrderQueue = new List<OrderInQueue>();
            PendingOrders = new Dictionary<Formation, OrderInQueue>();
            ShouldSkipCurrentOrders = new Dictionary<Formation, bool>();
            CurrentFormationChanges = new FormationChanges();
            LatestOrderInQueueChanges = new FormationChanges();
        }

        public static void OnRemoveBehavior()
        {
            OrderQueue = null;
            PendingOrders = null;
            ShouldSkipCurrentOrders = null;
            CurrentFormationChanges = null;
            LatestOrderInQueueChanges = null;

            var orderController = Mission.Current?.PlayerTeam?.PlayerOrderController;
            if (orderController != null)
            {
                orderController.OnOrderIssued -= OnOrderIssued;
            }
        }

        public static void AfterStart()
        {
            var orderController = Mission.Current?.PlayerTeam?.PlayerOrderController;
            if (orderController != null)
            {
                orderController.OnOrderIssued += OnOrderIssued;
            }
        }

        public static bool ShouldClearQueue(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Move:
                case OrderType.MoveToLineSegment:
                case OrderType.MoveToLineSegmentWithHorizontalLayout:
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                case OrderType.StandYourGround:
                case OrderType.FollowMe:
                case OrderType.FollowEntity:
                case OrderType.Retreat:
                case OrderType.AdvanceTenPaces:
                case OrderType.FallBackTenPaces:
                case OrderType.Advance:
                case OrderType.FallBack:
                case OrderType.LookAtEnemy:
                case OrderType.LookAtDirection:
                case OrderType.AIControlOn:
                case OrderType.Use:
                case OrderType.AttackEntity:
                case OrderType.PointDefence:
                case OrderType.ArrangementLine:
                case OrderType.ArrangementCloseOrder:
                case OrderType.ArrangementLoose:
                case OrderType.ArrangementCircular:
                case OrderType.ArrangementSchiltron:
                case OrderType.ArrangementVee:
                case OrderType.ArrangementColumn:
                case OrderType.ArrangementScatter:
                case OrderType.FormCustom:
                case OrderType.FormDeep:
                case OrderType.FormWide:
                case OrderType.FormWider:
                case OrderType.CohesionHigh:
                case OrderType.CohesionMedium:
                case OrderType.CohesionLow:
                case OrderType.None:
                case OrderType.HoldFire:
                case OrderType.FireAtWill:
                case OrderType.RideFree:
                case OrderType.Mount:
                case OrderType.Dismount:
                    return true;
                case OrderType.AIControlOff:
                case OrderType.Transfer:
                default:
                    return false;
            }
        }

        public static bool ShouldClearPendingOrder(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Move:
                case OrderType.MoveToLineSegment:
                case OrderType.MoveToLineSegmentWithHorizontalLayout:
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                case OrderType.StandYourGround:
                case OrderType.FollowMe:
                case OrderType.FollowEntity:
                case OrderType.Retreat:
                case OrderType.AdvanceTenPaces:
                case OrderType.FallBackTenPaces:
                case OrderType.Advance:
                case OrderType.FallBack:
                case OrderType.LookAtDirection:
                case OrderType.LookAtEnemy:
                case OrderType.AIControlOn:
                case OrderType.Use:
                case OrderType.AttackEntity:
                case OrderType.PointDefence:
                case OrderType.ArrangementLine:
                case OrderType.ArrangementCloseOrder:
                case OrderType.ArrangementLoose:
                case OrderType.ArrangementCircular:
                case OrderType.ArrangementSchiltron:
                case OrderType.ArrangementVee:
                case OrderType.ArrangementColumn:
                case OrderType.ArrangementScatter:
                case OrderType.FormCustom:
                case OrderType.FormDeep:
                case OrderType.FormWide:
                case OrderType.FormWider:
                case OrderType.CohesionHigh:
                case OrderType.CohesionMedium:
                case OrderType.CohesionLow:
                case OrderType.None:
                case OrderType.HoldFire:
                case OrderType.FireAtWill:
                case OrderType.RideFree:
                case OrderType.Mount:
                case OrderType.Dismount:
                    return true;
                case OrderType.AIControlOff:
                case OrderType.Transfer:
                default:
                    return false;
            }
        }

        //public static bool ShouldClearQueueAndPendingOrder(OrderInQueue order)
        //{
        //    switch (order.CustomOrderType)
        //    {
        //        case CustomOrderType.Original:
        //            return ShouldClearQueueAndPendingOrder(order.OrderType);
        //        case CustomOrderType.FollowMainAgent:
        //            return true;
        //        case CustomOrderType.SetTargetFormation:
        //        default:
        //            return false;
        //    }
        //}

        private static void OnOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations, OrderController orderController, params object[] delegateParams)
        {
            CurrentFormationChanges.SetChanges(Patch_OrderController.LivePreviewFormationChanges.CollectChanges(appliedFormations));
            if (ShouldClearQueue(orderType))
            {
                ClearOrderInQueue(appliedFormations);
            }

            foreach (var formation in appliedFormations)
            {
                if (GetNextOrderForFormation(formation) == null)
                {
                    LatestOrderInQueueChanges.SetChanges(CurrentFormationChanges.CollectChanges(appliedFormations));
                }
            }
        }

        public static void AddOrderToQueue(OrderInQueue order)
        {
            if (order.CustomOrderType == CustomOrderType.Original && order.OrderType == OrderType.None)
                return;
            order.RemainingFormations = order.SelectedFormations.ToList();
            LatestOrderInQueueChanges.SetChanges(Patch_OrderController.LivePreviewFormationChanges.CollectChanges(order.SelectedFormations));
            OrderQueue.Add(order);
            Utilities.Utility.DisplayAddOrderToQueueMessage();
            CommandQueuePreview.IsPreviewOutdated = true;
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
            //CurrentFormationChanges.ClearFiringOrder(formations);
            //CurrentFormationChanges.ClearRidingOrder(formations);
            CommandQueuePreview.IsPreviewOutdated = true;
        }

        public static void UpdateFormation(Formation formation)
        {
            if (TicksToSkip > 0)
            {
                TicksToSkip--;
                return;
            }
            var facingTarget = Patch_OrderController.GetFacingEnemyTargetFormation(formation);
            if (facingTarget != null &&facingTarget.CountOfUnits == 0)
            {
                Patch_OrderController.SetFacingEnemyTargetFormation(formation, null);
            }
            var order = GetNextOrderForFormation(formation);
            bool isApplicable = formation.GetReadonlyMovementOrderReference().IsApplicable(formation);
            bool isPendingOrderCompleted = IsPendingOrderCompleted(formation);
            while (TicksToSkip <= 0 && order != null &&
                (!isApplicable || isPendingOrderCompleted))
            {
                ExecuteOrderForFormation(order, formation);
                OnOrderExecutedForFormation(order, formation);
                order = GetNextOrderForFormation(formation);
            }
        }

        public static bool IsMovementOrderCompleted(Formation formation)
        {
            if (formation.CountOfUnits == 0)
                return true;
            switch (formation.GetReadonlyMovementOrderReference().OrderEnum)
            {
                case MovementOrder.MovementOrderEnum.Charge:
                case MovementOrder.MovementOrderEnum.ChargeToTarget:
                    return formation.TargetFormation == null || formation.TargetFormation.CountOfUnits == 0;
                case MovementOrder.MovementOrderEnum.Follow:
                case MovementOrder.MovementOrderEnum.AttackEntity:
                case MovementOrder.MovementOrderEnum.FollowEntity:
                    return !formation.GetReadonlyMovementOrderReference().IsApplicable(formation);
                case MovementOrder.MovementOrderEnum.FallBack:
                    // fallback is considered complete instantly.
                    return true;
            }
            return !formation.OrderPositionIsValid || CommandQuerySystem.GetQueryForFormation(formation).HasCurrentMovementOrderCompleted;
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
                                if (!IsMovementOrderCompleted(otherFormation))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    if (otherFormation == formation)
                    {
                        if (!IsMovementOrderCompleted(formation))
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
            return IsMovementOrderCompleted(formation);
        }

        public static void ExecuteOrderForFormation(OrderInQueue order, Formation formation)
        {
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.MoveToLineSegment:
                            case OrderType.MoveToLineSegmentWithHorizontalLayout:
                                {
                                    var formationChanges = order.ActualFormationChanges;
                                    (Formation f, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction) = formationChanges.First(c => c.formation == formation);
                                    var virtualFormationChange = order.VirtualFormationChanges[formation];
                                    if (formation.UnitSpacing != virtualFormationChange.UnitSpacing)
                                    {
                                        formation.SetPositioning(unitSpacing: virtualFormationChange.UnitSpacing);
                                    }
                                    if (order.IsLineShort)
                                    {
                                        if (virtualFormationChange.Width != null && formation.Width != virtualFormationChange.Width)
                                        {
                                            formation.SetFormOrder(FormOrder.FormOrderCustom(virtualFormationChange.Width.Value));
                                        }
                                        switch (OrderController.GetActiveFacingOrderOf(formation))
                                        {
                                            case OrderType.LookAtEnemy:
                                                formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                                                break;
                                            case OrderType.LookAtDirection:
                                                formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                                                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(direction));
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                                        formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(direction));
                                        formation.SetFormOrder(FormOrder.FormOrderCustom(customWidth));
                                    }
                                    TryPendingOrder(new List<Formation> { formation }, order);
                                    CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                    break;
                                }
                            case OrderType.Move:
                                formation.SetMovementOrder(MovementOrder.MovementOrderMove(order.PositionBegin));
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Charge:
                                formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                                if (order.TargetFormation != null)
                                {
                                    formation.SetTargetFormation(order.TargetFormation);
                                    TryPendingOrder(new List<Formation> { formation }, order);
                                }
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.ChargeWithTarget:
                                formation.SetMovementOrder(MovementOrder.MovementOrderChargeToTarget(formation));
                                if (order.TargetFormation != null)
                                {
                                    Utilities.Utility.DisplayFormationChargeMessage(formation);
                                    formation.SetTargetFormation(order.TargetFormation);
                                    TryPendingOrder(new List<Formation> { formation }, order);
                                }
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.LookAtDirection:
                                var movementOrder = order.VirtualFormationChanges[formation].MovementOrderType ?? formation.GetReadonlyMovementOrderReference().OrderType;
                                var shouldBePended = !Utilities.Utility.IsMovementOrderMoving(movementOrder);
                                FacingOrderLookAtDirection(order, formation);
                                if (shouldBePended)
                                {
                                    TryPendingOrder(new List<Formation> { formation }, order);
                                }
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.LookAtEnemy:
                                TryCancelStopOrder(formation);
                                Patch_OrderController.SetFacingEnemyTargetFormation(formation, order.TargetFormation);
                                formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy);
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.FollowMe:
                                formation.SetMovementOrder(MovementOrder.MovementOrderFollow(order.TargetAgent));
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.FollowEntity:
                                {
                                    GameEntity waitEntity = (order.TargetEntity as UsableMachine).WaitEntity;
                                    Vec2 direction = waitEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
                                    formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(direction));
                                    formation.SetMovementOrder(MovementOrder.MovementOrderFollowEntity(waitEntity));
                                    TryPendingOrder(new List<Formation> { formation }, order);
                                    CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                    break;
                                }
                            case OrderType.AttackEntity:
                                var missionObject = order.TargetEntity as MissionObject;
                                var gameEntity = missionObject.GameEntity;
                                formation.SetMovementOrder(MovementOrder.MovementOrderAttackEntity(GameEntity.CreateFromWeakEntity(gameEntity), !(missionObject is CastleGate)));
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.PointDefence:
                                var pointDefendable = order.TargetEntity as IPointDefendable;
                                formation.SetMovementOrder(MovementOrder.MovementOrderMove(pointDefendable.MiddleFrame.Origin));
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Advance:
                                formation.SetMovementOrder(MovementOrder.MovementOrderAdvance);
                                if (order.TargetFormation != null)
                                {
                                    formation.SetTargetFormation(order.TargetFormation);
                                }
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.FallBack:
                                formation.SetMovementOrder(MovementOrder.MovementOrderFallBack);
                                break;
                            case OrderType.StandYourGround:
                                formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Retreat:
                                formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.ArrangementLine:
                            case OrderType.ArrangementCloseOrder:
                            case OrderType.ArrangementLoose:
                            case OrderType.ArrangementCircular:
                            case OrderType.ArrangementSchiltron:
                            case OrderType.ArrangementVee:
                            case OrderType.ArrangementColumn:
                            case OrderType.ArrangementScatter:
                                ExecuteArrangementOrder(order);
                                break;
                            case OrderType.FireAtWill:
                                formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.HoldFire:
                                formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Mount:
                                if (formation.PhysicalClass.IsMounted() || formation.HasAnyMountedUnit)
                                    TryCancelStopOrder(formation);
                                formation.SetRidingOrder(RidingOrder.RidingOrderMount);
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Dismount:
                                if (formation.PhysicalClass.IsMounted() || formation.HasAnyMountedUnit)
                                    TryCancelStopOrder(formation);
                                formation.SetRidingOrder(RidingOrder.RidingOrderDismount);
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.AIControlOn:
                                formation.SetControlledByAI(true);
                                Patch_OrderController.SetFacingEnemyTargetFormation(formation, null);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                ClearOrderInQueue(new List<Formation> { formation });
                                break;
                            case OrderType.AIControlOff:
                                formation.SetControlledByAI(false);
                                Patch_OrderController.SetFacingEnemyTargetFormation(formation, null);
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            default:
                                Utility.DisplayMessage("Error: unexpected order type");
                                break;
                        }
                        break;
                    }
                case CustomOrderType.FollowMainAgent:
                    break;
                case CustomOrderType.SetTargetFormation:
                    formation.SetTargetFormation(order.TargetFormation);
                    break;
            }
        }
        private static void TryCancelStopOrder(Formation formation)
        {
            if (GameNetwork.IsClientOrReplay || formation.GetReadonlyMovementOrderReference().OrderEnum != MovementOrder.MovementOrderEnum.Stop)
                return;
            WorldPosition orderWorldPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
            if (!orderWorldPosition.IsValid)
                return;
            formation.SetMovementOrder(MovementOrder.MovementOrderMove(orderWorldPosition));
        }

        private static void FacingOrderLookAtDirection(OrderInQueue order, Formation formation)
        {
            var formationChanges = order.ActualFormationChanges;
            Patch_OrderController.SetFacingEnemyTargetFormation(formation, null);
            (Formation f, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction) = formationChanges.First(c => c.formation == formation);
            if (order.ShouldLockFormationInFacingOrder.TryGetValue(formation, out var shouldLockFormationInFacingOrder) && shouldLockFormationInFacingOrder)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
            }
            formation.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(direction));
        }

        private static OrderInQueue GetNextOrderForFormation(Formation formation)
        {
            var order = OrderQueue.FirstOrDefault(order => order.RemainingFormations.Contains(formation));
            return order;
        }

        private static void OnOrderExecutedForFormation(OrderInQueue order, Formation formation)
        {
            Utilities.Utility.DisplayExecuteOrderMessage(new List<Formation> { formation }, order);
            var orderController = Mission.Current.PlayerTeam.PlayerOrderController;
            TryTeleportSelectedFormationInDeployment(orderController, new List<Formation> { formation });
            order.RemainingFormations.Remove(formation);
            if (order.RemainingFormations.Count == 0)
            {
                OrderQueue.Remove(order);
            }
            if (GetNextOrderForFormation(formation) == null)
            {
                LatestOrderInQueueChanges.SetChanges(CurrentFormationChanges.CollectChanges(new List<Formation> { formation }));
            }
            CommandQueuePreview.IsPreviewOutdated = true;
            Mission.Current?.GetMissionBehavior<CommandSystemLogic>()?.OnMovementOrderChanged(formation);

            Utilities.Utility.UpdateActiveOrders();
        }

        public static bool CanBePended(OrderInQueue order)
        {
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.Charge:
                            case OrderType.ChargeWithTarget:
                                {
                                    return order.TargetFormation != null;
                                }
                            case OrderType.MoveToLineSegment:
                            case OrderType.MoveToLineSegmentWithHorizontalLayout:
                            case OrderType.Move:
                            case OrderType.FollowMe:
                            case OrderType.FollowEntity:
                            case OrderType.AttackEntity:
                            case OrderType.PointDefence:
                            case OrderType.Advance:
                            case OrderType.LookAtDirection:
                            case OrderType.LookAtEnemy:
                                {
                                    return true;
                                }
                        }
                        break;
                    }
            }
            return false;
        }

        public static void TryPendingOrder(IEnumerable<Formation> formations, OrderInQueue order)
        {
            if (CanBePended(order))
            {
                CancelPendingOrder(formations);
                foreach (var formation in formations)
                {
                    FormationPendingOrder(formation, order);
                }
            }
            else
            {
                if (ShouldClearPendingOrder(order.OrderType))
                {
                    CancelPendingOrder(formations);
                }
            }
        }

        public static void CancelPendingOrder(IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (PendingOrders.TryGetValue(formation, out var order))
                {
                    order.SelectedFormations.Remove(formation);
                };
                PendingOrders.Remove(formation);
            }
        }

        private static void FormationPendingOrder(Formation formation, OrderInQueue order)
        {
            PendingOrders[formation] = order;
            CommandQuerySystem.GetQueryForFormation(formation).ExpireAllQueries();
            TicksToSkip = 1;
        }

        private static void ExecuteArrangementOrder(OrderInQueue order)
        {
            foreach (var pair in order.VirtualFormationChanges)
            {
                var formation = pair.Key;
                var change = pair.Value;
                TryCancelStopOrder(formation);
                formation.SetArrangementOrder(Utilities.Utility.GetArrangementOrder(change.ArrangementOrder.Value));
                formation.SetPositioning(unitSpacing: change.UnitSpacing);
                if (change.Width != null)
                {
                    formation.SetFormOrder(FormOrder.FormOrderCustom(change.Width.Value));
                }
                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
            }
        }

        public static void TryTeleportSelectedFormationInDeployment(OrderController orderController, IEnumerable<Formation> formations)
        {
            // Copied from OrderController.AfterSetOrder
            if (Mission.Current.Mode == MissionMode.Deployment && orderController.FormationUpdateEnabledAfterSetOrder)
            {
                // Fix the issue that in Deployment mode, after switching to loose formation, the units are not teleported to correct position.
                foreach (Formation formation in formations)
                {
                    if (formation.CountOfUnits > 0 && (orderController == null || orderController.FormationUpdateEnabledAfterSetOrder))
                    {
                        bool flag = false;
                        if (formation.IsPlayerTroopInFormation)
                        {
                            flag = formation.GetReadonlyMovementOrderReference().OrderEnum == MovementOrder.MovementOrderEnum.Follow;
                        }
                        // update direction instantly in deployment mode
                        var target = formation.GetReadonlyMovementOrderReference()._targetAgent;
                        formation.SetPositioning(direction: formation.FacingOrder.GetDirection(formation, flag && target == Mission.Current.MainAgent ? null : target));
                        formation.ApplyActionOnEachUnit(delegate (Agent agent)
                        {
                            agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
                        }, flag ? Mission.Current.MainAgent : null);
                        formation.SetHasPendingUnitPositions(false);
                        Mission.Current.SetRandomDecideTimeOfAgentsWithIndices(formation.CollectUnitIndices());
                    }
                }
                // calls OrderOfBattleFormationItemVM.RefreshMarkerWorldPosition
                Mission.Current.GetMissionBehavior<OrderTroopPlacer>()?.OnUnitDeployed?.Invoke();
            }
        }
    }
}
