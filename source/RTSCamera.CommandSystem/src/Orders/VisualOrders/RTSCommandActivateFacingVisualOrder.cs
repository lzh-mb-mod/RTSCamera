using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandActivateFacingVisualOrder : RTSCommandVisualOrder
    {
        private OrderType _orderType;
        public static TextObject GetName(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.LookAtEnemy:
                    return new TextObject("{=qWzBa3KT}Facing Enemy");
                case OrderType.LookAtDirection:
                    return new TextObject("{=LWVwNcRA}Facing Direction");
            }
            return new TextObject("");
        }
        public RTSCommandActivateFacingVisualOrder(OrderType orderType, string stringId) : base(stringId)
        {
            _orderType = orderType;
        }
        public override TextObject GetName(OrderController orderController)
        {
            return GetName(_orderType);
        }

        public override void ExecuteOrder(
          OrderController orderController,
          VisualOrderExecutionParameters executionParameters)
        {
            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations,
                ShouldAdjustFormationSpeed = Utilities.Utility.ShouldLockFormation()
            };

            orderToAdd.OrderType = _orderType;
            if (orderToAdd.OrderType == OrderType.LookAtDirection)
            {
                if (IsFromClicking && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickable && CommandSystemConfig.Get().OrderUIClickableExtension)
                {
                    // Allows to click ground to select target to facing to.
                    OrderToSelectTarget = SelectTargetMode.LookAtDirection;
                    return;
                }
            }
            else if (orderToAdd.OrderType == OrderType.LookAtEnemy)
            {
                if (IsSelectTargetForMouseClickingKeyDown && IsFromClicking && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickable && CommandSystemConfig.Get().OrderUIClickableExtension)
                {
                    // Allows to click enemy to select target to facing to.
                    OrderToSelectTarget = SelectTargetMode.LookAtEnemy;
                    return;
                }
            }
            else
            {
                return;
            }
            if (queueCommand)
            {
                if (orderToAdd.OrderType == OrderType.LookAtDirection)
                {
                    Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, orderController, executionParameters.WorldPosition);
                }
                else
                {
                    orderToAdd.TargetFormation = executionParameters.Formation;
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations, orderToAdd.TargetFormation);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                }
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                if (orderToAdd.OrderType == OrderType.LookAtDirection)
                {
                    Patch_OrderController.SetFacingEnemyTargetFormation(selectedFormations, null);
                    // only pending order for formations that is not executing attacking/advance/fallback, etc.
                    orderToAdd.SelectedFormations = orderToAdd.SelectedFormations.Where(f => !Utilities.Utility.IsFormationOrderPositionMoving(f)).ToList();
                    orderController.SetOrderWithPosition(OrderType.LookAtDirection, executionParameters.WorldPosition);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                }
                else
                {
                    orderToAdd.TargetFormation = executionParameters.Formation;
                    // only pending order for formations that is not executing attacking/advance/fallback, etc.
                    orderToAdd.SelectedFormations = orderToAdd.SelectedFormations.Where(f => !Utilities.Utility.IsFormationOrderPositionMoving(f)).ToList();
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations, orderToAdd.TargetFormation);
                    Patch_OrderController.SetFacingEnemyTargetFormation(selectedFormations, orderToAdd.TargetFormation);
                    orderController.SetOrder(OrderType.LookAtEnemy);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                }
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
            }
        }

        public override bool IsTargeted() => false;

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return new bool?(OrderController.GetActiveFacingOrderOf(formation) == _orderType);
        }

        protected override string GetIconId()
        {
            string iconId = base.GetIconId();
            return iconId;
        }
    }
}
