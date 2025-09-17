using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandRetreatVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName()
        {
            return new TextObject("{=VbeHEAsa}Retreat");
        }
        public RTSCommandRetreatVisualOrder(string stringId) : base(stringId)
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
            orderToAdd.OrderType = OrderType.Retreat;
            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Retreat, selectedFormations, null, null, null);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                orderController.SetOrder(OrderType.Retreat);
            }
            else
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return new bool?(OrderController.GetActiveMovementOrderOf(formation) == OrderType.Retreat);
        }

        public override bool IsTargeted() => true;
    }
}
