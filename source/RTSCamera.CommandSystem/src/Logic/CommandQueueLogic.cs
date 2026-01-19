using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
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
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Logic
{
    public enum CustomOrderType
    {
        Original,
        FollowMainAgent,
        SetTargetFormation,
        StopUsing,
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

        public bool IsStopUsing { get; set; }

        public bool IsLineShort { get; set; }

        public Dictionary<Formation, bool> ShouldLockFormationInFacingOrder { get; set; } = new Dictionary<Formation, bool>();

        public List<(Formation formation, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction)> ActualFormationChanges { get; set; } = new List<(Formation formation, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction)>();

        public Dictionary<Formation, FormationChange> VirtualFormationChanges { get; set; } = new Dictionary<Formation, FormationChange>();
        public bool ShouldAdjustFormationSpeed { get; set; } = false;

        public Dictionary<Formation, float> FormationSpeedLimits { get; set; } = new Dictionary<Formation, float>();
        public void UpdateMovementSpeed()
        {
            FormationSpeedLimits.Clear();
            if (!ShouldAdjustFormationSpeed)
                return;
            if (CommandSystemConfig.Get().FormationSpeedSyncMode == FormationSpeedSyncMode.Disabled)
                return;
            Dictionary<Formation, float> targetDistances = new Dictionary<Formation, float>();
            Dictionary<Formation, float> originalDurations = new Dictionary<Formation, float>();
            var maxOriginalDuration = float.MinValue;
            var distanceWithMaxDuration = float.MaxValue;
            var maxDistance = float.MinValue;
            var minDistance = float.MaxValue;
            foreach (var formation in SelectedFormations)
            {
                if (formation.CountOfUnits == 0)
                    continue;
                if (!CommandQueueLogic.PendingOrders.TryGetValue(formation, out var otherOrder))
                    continue;
                if (this != otherOrder || CommandQueueLogic.IsMovementOrderCompleted(formation, this))
                    continue;
                if (!VirtualFormationChanges.TryGetValue(formation, out var formationChange))
                    continue;

                var targetPosition = formationChange.WorldPosition;

                if (!targetPosition.HasValue || !targetPosition.Value.IsValid)
                    continue;

                FormationQuerySystem.FormationIntegrityDataGroup formationIntegrityData = formation.QuerySystem.FormationIntegrityData;
                float num2 = formationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents * 3f;
                if (formationIntegrityData.DeviationOfPositionsExcludeFarAgents > num2)
                    return;

                var targetDistance = targetPosition.Value.AsVec2.Distance(formation.CurrentPosition);
                if (targetDistance < 3f)
                    continue;
                if (targetDistance > maxDistance)
                {
                    maxDistance = targetDistance;
                }
                if (targetDistance < minDistance)
                {
                    minDistance = targetDistance;
                }
                var originalSpeed = MathF.Max(0.1f, formation.QuerySystem.MovementSpeed);
                targetDistances[formation] = targetDistance;
                var duration = targetDistance / originalSpeed;
                originalDurations[formation] = duration;
                if (duration > maxOriginalDuration)
                {
                    maxOriginalDuration = duration;
                    distanceWithMaxDuration = targetDistance;
                }
            }

            var distanceError = 1f;
            switch (CommandSystemConfig.Get().FormationSpeedSyncMode)
            {
                case FormationSpeedSyncMode.Linear:
                    {
                        foreach (var pair in targetDistances)
                        {
                            var linearSpeedLimit = pair.Value / maxOriginalDuration;
                            var originalSpeed = MathF.Max(0.1f, pair.Key.QuerySystem.MovementSpeed);
                            FormationSpeedLimits[pair.Key] = MathF.Clamp(linearSpeedLimit, 0.1f, originalSpeed);
                        }
                        break;
                    }
                case FormationSpeedSyncMode.CatchUp:
                    {
                        foreach (var pair in targetDistances)
                        {
                            var linearSpeedLimit = MathF.Max(0.1f, pair.Value / maxOriginalDuration);
                            var originalSpeed = MathF.Max(0.1f, pair.Key.QuerySystem.MovementSpeed);
                            //catch up and do not wait for slower formation
                            FormationSpeedLimits[pair.Key] = MathF.Clamp(MathF.Lerp(linearSpeedLimit, originalSpeed, (pair.Value - distanceWithMaxDuration + distanceError) / (originalSpeed * 2f)), linearSpeedLimit, originalSpeed);
                        }
                        break;
                    }
                case FormationSpeedSyncMode.WaitForLastFormation:
                    {
                        var range = 5f;
                        //foreach (var pair in targetDistances)
                        //{
                        //    var distance = pair.Value;
                        //    var linearSpeedLimit = distance / maxOriginalDuration;
                        //    var originalSpeed = MathF.Max(0.1f, pair.Key.CachedMovementSpeed);
                        //    var originalDuration = originalDurations[pair.Key];
                        //    var maxDistanceSpeed = GetMaxDistanceSpeed(targetDistances, pair.Value, minDistance, maxDistance, maxOriginalDuration, distanceWithMaxDuration, range);
                        //    var minSpeed = MathF.Lerp(linearSpeedLimit, 0.1f, MathF.Pow(MathF.Clamp((maxOriginalDuration - originalDuration) / durationThreshold, 0f, 1f), 10f));
                        //    //var minSpeed = 0.1f;
                        //    var speedLimit = MathF.Clamp(MathF.Lerp(maxDistanceSpeed, minSpeed, MathF.Clamp((maxDistance - distance) / range - 0.1f, 0f, 1f)), minSpeed, originalSpeed);
                        //    FormationSpeedLimits[pair.Key] = speedLimit;
                        //}
                        foreach (var pair in targetDistances)
                        {
                            var distance = pair.Value;
                            var linearSpeedLimit = distance / maxOriginalDuration;
                            var originalSpeed = MathF.Max(0.1f, pair.Key.QuerySystem.MovementSpeed);
                            var originalDuration = originalDurations[pair.Key];
                            var maxDistanceSpeed = GetMaxDistanceSpeed2(targetDistances, pair.Value, minDistance, maxDistance, maxOriginalDuration, distanceWithMaxDuration, range);
                            var minSpeed = 0.1f;
                            var speedLimit = MathF.Clamp(MathF.Lerp(maxDistanceSpeed, minSpeed, MathF.Clamp((maxDistance - distance - distanceError) / range, 0f, 1f)), minSpeed, originalSpeed);
                            FormationSpeedLimits[pair.Key] = speedLimit;
                        }
                        break;
                    }
            }
        }

        private float GetMaxDistanceSpeed(Dictionary<Formation, float> targetDistances, float distance, float minDistance, float maxDistance, float maxOriginalDuration, float distanceWithMaxDuration, float range)
        {
            var speedWeightSum = 0f;
            var speedSum = 0f;
            var minSpeedInRangeOfMaxDistance = float.MaxValue;
            var distanceOfMinSpeedInRange = 0f;
            foreach (var pair in targetDistances)
            {
                var formation = pair.Key;
                var formationDistance = pair.Value;
                var formationOriginalSpeed = formation.QuerySystem.MovementSpeed;
                var diff = maxDistance - formationDistance;
                if (diff < range)
                {
                    if (minSpeedInRangeOfMaxDistance > formationOriginalSpeed)
                    {
                        minSpeedInRangeOfMaxDistance = formationOriginalSpeed;
                        distanceOfMinSpeedInRange = formationDistance;
                    }
                    var weight = 1 - diff / range;
                    speedSum += weight * formationOriginalSpeed;
                    speedWeightSum += weight;
                }
            }
            var weightedAverageSpeed = speedSum / speedWeightSum;
            var result = MathF.Lerp(minSpeedInRangeOfMaxDistance, weightedAverageSpeed, (maxDistance - distanceOfMinSpeedInRange) / range);
            return result;
        }


        private float GetMaxDistanceSpeed2(Dictionary<Formation, float> targetDistances, float distance, float minDistance, float maxDistance, float maxOriginalDuration, float distanceWithMaxDuration, float range)
        {
            var maxDurationInRange = float.MinValue;
            foreach (var pair in targetDistances)
            {
                var formation = pair.Key;
                var formationDistance = pair.Value;
                var originalSpeed = MathF.Max(0.1f, formation.QuerySystem.MovementSpeed);
                var diff = maxDistance - formationDistance;
                if (diff < range)
                {
                    var duration = formationDistance / originalSpeed;
                    if (maxDurationInRange < duration)
                    {
                        maxDurationInRange = duration;
                    }
                }
            }
            return maxDistance / maxDurationInRange;
        }
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
                case OrderType.GuardMe:
                case OrderType.Retreat:
                case OrderType.AdvanceTenPaces:
                case OrderType.FallBackTenPaces:
                case OrderType.Advance:
                case OrderType.FallBack:
                case OrderType.LookAtEnemy:
                case OrderType.LookAtDirection:
                case OrderType.AIControlOn:
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
                case OrderType.Use:
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
                case OrderType.GuardMe:
                case OrderType.Retreat:
                case OrderType.AdvanceTenPaces:
                case OrderType.FallBackTenPaces:
                case OrderType.Advance:
                case OrderType.FallBack:
                case OrderType.LookAtDirection:
                case OrderType.LookAtEnemy:
                case OrderType.AIControlOn:
                case OrderType.AttackEntity:
                case OrderType.PointDefence:
                    return true;
                case OrderType.Use:
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
                case OrderType.AIControlOff:
                case OrderType.Transfer:
                case OrderType.HoldFire:
                case OrderType.FireAtWill:
                case OrderType.RideFree:
                case OrderType.Mount:
                case OrderType.Dismount:
                case OrderType.None:
                default:
                    return false;
            }
        }

        public static bool ShouldCustomOrderClearQueue(OrderInQueue order)
        {
            switch (order.CustomOrderType)
            {
                case CustomOrderType.SetTargetFormation:
                    return true;
                case CustomOrderType.StopUsing:
                    return false;
                default:
                    Utility.DisplayMessage("Error: unexpected order type");
                    break;
            }
            return false;
        }

        public static void OnCustomOrderIssued(OrderInQueue order, OrderController orderController)
        {
            CurrentFormationChanges.SetChanges(Patch_OrderController.LivePreviewFormationChanges.CollectChanges(order.SelectedFormations));
            if (ShouldCustomOrderClearQueue(order))
            {
                ClearOrderInQueue(order.SelectedFormations);
            }

            foreach (var formation in order.SelectedFormations)
            {
                if (GetNextOrderForFormation(formation) == null)
                {
                    LatestOrderInQueueChanges.SetChanges(CurrentFormationChanges.CollectChanges(order.SelectedFormations));
                }
            }
        }

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
            try
            {
                if (TicksToSkip > 0)
                {
                    TicksToSkip--;
                    return;
                }
                var facingTarget = Patch_OrderController.GetFacingEnemyTargetFormation(formation);
                if (facingTarget != null && facingTarget.CountOfUnits == 0)
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
            catch (Exception e)
            {
                Utility.DisplayMessageForced(e.ToString());
            }
        }

        public static bool IsMovementOrderCompleted(Formation formation, OrderInQueue order)
        {
            if (formation.CountOfUnits == 0)
                return true;
            switch (formation.GetReadonlyMovementOrderReference().OrderEnum)
            {
                case MovementOrder.MovementOrderEnum.Charge:
                case MovementOrder.MovementOrderEnum.ChargeToTarget:
                    return formation.TargetFormation == null || formation.TargetFormation.CountOfUnits == 0;
                case MovementOrder.MovementOrderEnum.Follow:
                case MovementOrder.MovementOrderEnum.Guard:
                case MovementOrder.MovementOrderEnum.AttackEntity:
                case MovementOrder.MovementOrderEnum.FollowEntity:
                    if (order == null)
                    {
                        return !formation.GetReadonlyMovementOrderReference().IsApplicable(formation);
                    }
                    else
                    {
                        var usable = order.TargetEntity as UsableMachine;
                        if (usable == null)
                            return !formation.GetReadonlyMovementOrderReference().IsApplicable(formation);
                        return usable.IsDestroyed;
                    }
                case MovementOrder.MovementOrderEnum.FallBack:
                    // fallback is considered complete instantly.
                    return true;
            }
            if (formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Column)
            {
                return Utilities.Utility.GetColumnFormationCurrentPosition(formation).Distance(formation.OrderGroundPosition) < 5f;
            }
            return !formation.OrderPositionIsValid || CommandQuerySystem.GetQueryForFormation(formation).HasCurrentMovementOrderCompleted;
        }

        public static bool IsPendingOrderCompleted(Formation formation)
        {

            if (PendingOrders.TryGetValue(formation, out var order))
            {
                order.UpdateMovementSpeed();
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
                                if (!IsMovementOrderCompleted(otherFormation, order))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    if (otherFormation == formation)
                    {
                        if (!IsMovementOrderCompleted(formation, order))
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
            return IsMovementOrderCompleted(formation, order);
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
                                        if (virtualFormationChange.Width != null && formation.Width != virtualFormationChange.Width && formation.ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Column)
                                        {
                                            formation.FormOrder = FormOrder.FormOrderCustom(virtualFormationChange.Width.Value);
                                        }
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
                                formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
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
                                    var usable = order.TargetEntity as UsableMachine;
                                    if (usable.IsDestroyed)
                                    {
                                        break;
                                    }
                                    if (order.IsStopUsing)
                                    {
                                        //formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                                        formation.StopUsingMachine(usable, true);
                                        var siegeWeapon = usable as SiegeWeapon;
                                        if (siegeWeapon != null)
                                        {
                                            siegeWeapon.SetForcedUse(false);
                                        }
                                        TryPendingOrder(new List<Formation> { formation }, order);
                                        CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                        break;
                                    }
                                    else
                                    {
                                        var siegeWeapon = usable as SiegeWeapon;
                                        if (siegeWeapon != null)
                                        {
                                            siegeWeapon.SetForcedUse(true);
                                        }
                                        GameEntity waitEntity = usable.WaitEntity;
                                        Vec2 direction = waitEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
                                        formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                                        formation.SetMovementOrder(MovementOrder.MovementOrderFollowEntity(waitEntity));
                                        TryPendingOrder(new List<Formation> { formation }, order);
                                        CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                        break;
                                    }
                                }
                            case OrderType.Use:
                                {
                                    var usable = order.TargetEntity as UsableMachine;
                                    if (usable.IsDestroyed)
                                    {
                                        break;
                                    }
                                    if (order.IsStopUsing)
                                    {
                                        formation.StopUsingMachine(usable, true);
                                        var siegeWeapon = usable as SiegeWeapon;
                                        if (siegeWeapon != null)
                                        {
                                            siegeWeapon.SetForcedUse(false);
                                        }
                                    }
                                    else
                                    {
                                        formation.StartUsingMachine(usable, true);
                                        var siegeWeapon = usable as SiegeWeapon;
                                        if (siegeWeapon != null)
                                        {
                                            siegeWeapon.SetForcedUse(true);
                                        }
                                    }
                                    TryPendingOrder(new List<Formation> { formation }, order);
                                    CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                }
                                break;
                            case OrderType.AttackEntity:
                                var missionObject = order.TargetEntity as MissionObject;
                                var gameEntity = missionObject.GameEntity;
                                formation.SetMovementOrder(MovementOrder.MovementOrderAttackEntity(gameEntity, !(missionObject is CastleGate)));
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
                                formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.HoldFire:
                                formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Mount:
                                if (formation.PhysicalClass.IsMounted() || formation.HasAnyMountedUnit)
                                    TryCancelStopOrder(formation);
                                formation.RidingOrder = RidingOrder.RidingOrderMount;
                                TryPendingOrder(new List<Formation> { formation }, order);
                                CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                                break;
                            case OrderType.Dismount:
                                if (formation.PhysicalClass.IsMounted() || formation.HasAnyMountedUnit)
                                    TryCancelStopOrder(formation);
                                formation.RidingOrder = RidingOrder.RidingOrderDismount;
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
                case CustomOrderType.StopUsing:
                    {
                        var usable = order.TargetEntity as UsableMachine;
                        formation.StopUsingMachine(usable, true);
                        var siegeWeapon = usable as SiegeWeapon;
                        if (siegeWeapon != null)
                        {
                            siegeWeapon.SetForcedUse(false);
                        }
                        TryPendingOrder(new List<Formation> { formation }, order);
                        CurrentFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                    }
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
            var formationChanges = order.ActualFormationChanges;
            Patch_OrderController.SetFacingEnemyTargetFormation(formation, null);
            (Formation f, int unitSpacingReduced, float customWidth, WorldPosition position, Vec2 direction) = formationChanges.First(c => c.formation == formation);
            if (order.ShouldLockFormationInFacingOrder.TryGetValue(formation, out var shouldLockFormationInFacingOrder) && shouldLockFormationInFacingOrder)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
            }
            formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
        }

        private static OrderInQueue GetNextOrderForFormation(Formation formation)
        {
            var order = OrderQueue.FirstOrDefault(order => order.RemainingFormations.Contains(formation));
            return order;
        }

        private static void OnOrderExecutedForFormation(OrderInQueue order, Formation formation)
        {

            if (RelatedWithPlayerUI(formation))
            {
                Utilities.Utility.DisplayExecuteOrderMessageInQueue(new List<Formation> { formation }, order);
                var orderController = Mission.Current.PlayerTeam.PlayerOrderController;
                TryTeleportSelectedFormationInDeployment(orderController, new List<Formation> { formation });
            }
            order.RemainingFormations.Remove(formation);
            if (order.RemainingFormations.Count == 0)
            {
                OrderQueue.Remove(order);
            }
            if (GetNextOrderForFormation(formation) == null)
            {
                LatestOrderInQueueChanges.SetChanges(CurrentFormationChanges.CollectChanges(new List<Formation> { formation }));
            }

            if (RelatedWithPlayerUI(formation))
            {
                CommandQueuePreview.IsPreviewOutdated = true;
                Mission.Current?.GetMissionBehavior<CommandSystemLogic>()?.OnMovementOrderChanged(formation);

                Utilities.Utility.UpdateActiveOrders();
            }
        }

        public static bool RelatedWithPlayerUI(Formation formation)
        {
            return formation.Team?.IsPlayerTeam ?? false;
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
                order.UpdateMovementSpeed();
                if (order.ShouldAdjustFormationSpeed && CommandSystemConfig.Get().FormationSpeedSyncMode != FormationSpeedSyncMode.Disabled && order.FormationSpeedLimits.Count > 1)
                {
                    Utilities.Utility.DisplayAdjustFormationSpeedMessage(order.FormationSpeedLimits.Keys);
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
                }
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
                formation.ArrangementOrder = Utilities.Utility.GetArrangementOrder(change.ArrangementOrder.Value);
                formation.SetPositioning(unitSpacing: change.UnitSpacing);
                if (change.Width != null)
                {
                    formation.FormOrder = FormOrder.FormOrderCustom(change.Width.Value);
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
                            agent.UpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
                        }, flag ? Mission.Current.MainAgent : null);
                        Mission.Current.SetRandomDecideTimeOfAgentsWithIndices(formation.CollectUnitIndices());
                    }
                }
                // calls OrderOfBattleFormationItemVM.RefreshMarkerWorldPosition
                Mission.Current.GetMissionBehavior<OrderTroopPlacer>()?.OnUnitDeployed?.Invoke();
            }
        }

        public static void OnFormationUnitsCleared(Formation formation)
        {
            if (formation.Team != null && formation.Team.IsPlayerTeam)
            {
                CurrentFormationChanges.SetChanges(new List<KeyValuePair<Formation, FormationChange>>
                {
                    new KeyValuePair<Formation, FormationChange>(formation, new FormationChange())
                });
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(new List<KeyValuePair<Formation, FormationChange>>
                {
                    new KeyValuePair<Formation, FormationChange>(formation, new FormationChange())
                });
            }
        }
    }
}
