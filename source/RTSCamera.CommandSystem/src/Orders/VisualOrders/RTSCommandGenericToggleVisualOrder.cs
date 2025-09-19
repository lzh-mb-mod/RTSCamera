using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandGenericToggleVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.LookAtEnemy:
                    return new TextObject("{=u8j8nN5U}Face Enemy");
                case OrderType.LookAtDirection:
                    return new TextObject("{=1gC25EMb}Face this Direction");
                case OrderType.HoldFire:
                    return new TextObject("{=VyI0rimN}Holding Fire");
                case OrderType.FireAtWill:
                    return new TextObject("{=itoYrj8d}Firing at will");
                case OrderType.Mount:
                    return new TextObject("{=ubTGIdcv}Mounted");
                case OrderType.Dismount:
                    return new TextObject("{=Ema5Vd6o}Dismounted");
                case OrderType.AIControlOn:
                    return new TextObject("{=zatDiaEI}Delegate Command On");
                case OrderType.AIControlOff:
                    return new TextObject("{=JceqNdWx}Delegate Command Off");
                default:
                    return TextObject.GetEmpty();
            }
        }
        private readonly TextObject _positiveOrderName;
        private readonly TextObject _negativeOrderName;

        public OrderType PositiveOrder { get; }

        public OrderType NegativeOrder { get; }

        public RTSCommandGenericToggleVisualOrder(
            string stringId,
            OrderType positiveOrder,
            OrderType negativeOrder)
            : base(stringId)
        {
            PositiveOrder = positiveOrder;
            NegativeOrder = negativeOrder;
            _positiveOrderName = GetName(positiveOrder);
            _negativeOrderName = GetName(negativeOrder);
        }

        public override TextObject GetName(OrderController orderController)
        {
            switch (GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    return _positiveOrderName;
                default:
                    return _negativeOrderName;
            }
        }

        public override bool IsTargeted() => false;

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
            orderToAdd.OrderType = GetActiveState(orderController) == OrderState.Active ? NegativeOrder : PositiveOrder;
            Patch_OrderController.LivePreviewFormationChanges.SetToggleOrder(orderToAdd.OrderType, selectedFormations);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (queueCommand)
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                orderController.SetOrder(orderToAdd.OrderType);
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return Utilities.Utility.DoesFormationHaveOrderType(formation, PositiveOrder);
        }

        protected override string GetIconId()
        {
            string iconId = base.GetIconId();
            return _lastActiveState == OrderState.Active ? iconId + "_active" : iconId;
        }
    }
}
