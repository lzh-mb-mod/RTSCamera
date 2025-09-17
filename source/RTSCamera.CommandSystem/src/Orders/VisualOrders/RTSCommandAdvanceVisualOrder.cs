using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandAdvanceVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName()
        {
            return new TextObject("{=A38xbjqm}Engage");
        }
        public RTSCommandAdvanceVisualOrder(string stringId) : base(stringId)
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

            if (IsSelectTargetForMouseClickingKeyDown && OrderToSelectTarget == SelectTargetMode.None && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickableExtension)
            {
                // Allows to click enemy to select target to advance to.
                OrderToSelectTarget = SelectTargetMode.Advance;
                return;
            }
            var selectedFormations = orderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };

            orderToAdd.OrderType = OrderType.Advance;
            orderToAdd.TargetFormation = executionParameters.Formation;
            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.Advance, selectedFormations, orderToAdd.TargetFormation, null, null);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (!queueCommand)
            {
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                if (executionParameters.HasFormation)
                    orderController.SetOrderWithFormation(OrderType.Advance, executionParameters.Formation);
                else
                    orderController.SetOrder(OrderType.Advance);
            }
            else
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return new bool?(OrderController.GetActiveMovementOrderOf(formation) == OrderType.Advance);
        }

        public override bool IsTargeted() => true;
    }
}
