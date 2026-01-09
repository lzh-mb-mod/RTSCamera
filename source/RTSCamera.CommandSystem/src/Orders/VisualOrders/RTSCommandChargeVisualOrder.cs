using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandChargeVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName()
        {
            return new TextObject("{=Dxmq32qW}Charge");
        }
        public RTSCommandChargeVisualOrder(string stringId) : base(stringId)
        {
        }

        public override TextObject GetName(OrderController orderController)
        {
            return GetName();
        }

        public override void ExecuteOrder(
          OrderController orderController,
          VisualOrderExecutionParameters executionParameters)
        {
            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            bool shouldIgnoreTarget = CommandSystemConfig.Get().DisableNativeAttack;
            orderToAdd.OrderType = OrderType.Charge;
            orderToAdd.TargetFormation = shouldIgnoreTarget ? null : executionParameters.Formation;
            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Charge, selectedFormations, orderToAdd.TargetFormation, null, null);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                if (executionParameters.HasFormation && !shouldIgnoreTarget)
                    orderController.SetOrderWithFormation(OrderType.Charge, executionParameters.Formation);
                else
                    orderController.SetOrder(OrderType.Charge);
            }
            else
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            OrderType activeMovementOrderOf = OrderController.GetActiveMovementOrderOf(formation);
            return new bool?(activeMovementOrderOf == OrderType.Charge || activeMovementOrderOf == OrderType.ChargeWithTarget);
        }

        public override bool IsTargeted() => true;
    }
}
