using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandArrangementVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName(ArrangementOrder.ArrangementOrderEnum order)
        {
            switch (order)
            {
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Circle:
                    return new TextObject("{=9TGLirQf}Circle");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Column:
                    return new TextObject("{=WsmZzaOq}Column");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Line:
                    return new TextObject("{=9aboazgu}Line");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Loose:
                    return new TextObject("{=iJXH3841}Loose");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Scatter:
                    return new TextObject("{=eEf7hE4r}Scatter");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.ShieldWall:
                    return new TextObject("{=rTPnyeJ3}Shield Wall");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Skein:
                    return new TextObject("{=uCyQNvq1}Skein");
                case TaleWorlds.MountAndBlade.ArrangementOrder.ArrangementOrderEnum.Square:
                    return new TextObject("{=E3tCWX7w}Square");
                default:
                    return TextObject.GetEmpty();
            }
        }
        public ArrangementOrder.ArrangementOrderEnum ArrangementOrder { get; }

        public RTSCommandArrangementVisualOrder(
          ArrangementOrder.ArrangementOrderEnum arrangementOrder,
          string iconId)
          : base(iconId)
        {
            ArrangementOrder = arrangementOrder;
        }

        public override TextObject GetName(OrderController orderController)
        {
            return GetName(this.ArrangementOrder);
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
            orderToAdd.OrderType = Utilities.Utility.ArrangementOrderEnumToOrderType(ArrangementOrder);
            bool fadeOut = Utilities.Utility.ShouldFadeOut();
            Patch_OrderController.SimulateNewArrangementOrder(selectedFormations, orderController.simulationFormations, ArrangementOrder, fadeOut, out var simulationAgentFrames, true, out _);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (fadeOut)
            {
                Patch_OrderTroopPlacer.AddOrderPositionEntities(simulationAgentFrames, true);
            }
            if (!queueCommand)
            {
                ExecuteArrangementOrder(orderController, orderToAdd);
            }
            else
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return OrderController.GetActiveArrangementOrderOf(formation) == Utilities.Utility.ArrangementOrderEnumToOrderType(ArrangementOrder);
        }

        public override bool IsTargeted() => false;

        private static void ExecuteArrangementOrder(OrderController orderController, OrderInQueue order)
        {
            //Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(order.SelectedFormations));
            Patch_OrderController.LivePreviewFormationChanges.SetChanges(order.VirtualFormationChanges);
            orderController.SetOrder(order.OrderType);
            foreach (var pair in order.VirtualFormationChanges)
            {
                var formation = pair.Key;
                var change = pair.Value;
                formation.SetPositioning(unitSpacing: change.UnitSpacing);
                if (change.Width != null)
                {
                    formation.SetFormOrder(FormOrder.FormOrderCustom(change.Width.Value));
                    formation.SetPositioning(unitSpacing: change.UnitSpacing);
                }
            }
            CommandQueueLogic.TryTeleportSelectedFormationInDeployment(orderController, order.SelectedFormations);
            CommandQueueLogic.CurrentFormationChanges.SetChanges(order.VirtualFormationChanges);
            // for column formation, the direciton in VirtualFormationChanges is the heading direction.
            foreach (var pair in order.VirtualFormationChanges)
            {
                var formation = pair.Key;
                CommandQueueLogic.CurrentFormationChanges.UpdateFormationChange(formation, null, formation.Direction, null, null);
            }
        }
    }
}
