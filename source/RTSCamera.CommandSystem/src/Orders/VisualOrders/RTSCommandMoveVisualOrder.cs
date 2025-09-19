using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandMoveVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName()
        {
            return new TextObject("{=vbAZwibd}Move to Position");
        }
        public RTSCommandMoveVisualOrder(string iconId) : base(iconId)
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
            if (!executionParameters.HasWorldPosition || IsFromClicking)
                return;

            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };

            WorldPosition worldPosition = executionParameters.WorldPosition;
            if (Mission.Current.IsFormationUnitPositionAvailable(ref worldPosition, Mission.Current.PlayerTeam))
            {
                Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(OrderType.MoveToLineSegment, selectedFormations, null, null, null);
                orderToAdd.OrderType = OrderType.MoveToLineSegment;
                orderToAdd.PositionBegin = worldPosition;
                orderToAdd.PositionEnd = worldPosition;
                if (!queueCommand)
                {
                    Patch_OrderController.SimulateNewOrderWithPositionAndDirection(selectedFormations, orderController.simulationFormations, worldPosition, worldPosition, true, out var simulationAgentFrames, false, out _, out var isLineShort, true, true);
                    orderToAdd.IsLineShort = isLineShort;
                }
                else
                {
                    OrderController.SimulateNewOrderWithPositionAndDirection(selectedFormations, orderController.simulationFormations, worldPosition, worldPosition, out var formationChanges, out var isLineShort, true);
                    orderToAdd.IsLineShort = isLineShort;
                    orderToAdd.ActualFormationChanges = formationChanges;
                }
                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                if (!queueCommand)
                {
                    CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                    orderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment, worldPosition, worldPosition);
                }
                else
                {
                    CommandQueueLogic.AddOrderToQueue(orderToAdd);
                }
            }
        }
        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            OrderType activeMovementOrderOf = OrderController.GetActiveMovementOrderOf(formation);
            int num;
            switch (activeMovementOrderOf)
            {
                case OrderType.Move:
                case OrderType.MoveToLineSegment:
                    num = 1;
                    break;
                default:
                    num = activeMovementOrderOf == OrderType.MoveToLineSegmentWithHorizontalLayout ? 1 : 0;
                    break;
            }
            return new bool?(num != 0);
        }

        public override bool IsTargeted() => true;
    }
}
