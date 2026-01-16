using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandToggleFireVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.HoldFire:
                    return new TextObject("{=VyI0rimN}Holding Fire");
                case OrderType.FireAtWill:
                    return new TextObject("{=itoYrj8d}Firing at will");
                default:
                    return TextObject.GetEmpty();
            }
        }
        private readonly TextObject _positiveOrderName;
        private readonly TextObject _negativeOrderName;
        private readonly RTSCommandToggleVolleyVisualOrder _autoVolleyVisualOrder;
        private readonly RTSCommandToggleVolleyVisualOrder _manualVolleyVisualOrder;

        public OrderType PositiveOrder { get; }

        public OrderType NegativeOrder { get; }

        public RTSCommandToggleFireVisualOrder(
            string stringId,
            OrderType positiveOrder,
            OrderType negativeOrder,
            RTSCommandToggleVolleyVisualOrder autoVolleyVisualOrder,
            RTSCommandToggleVolleyVisualOrder manualVolleyVisualOrder)
            : base(stringId)
        {
            PositiveOrder = positiveOrder;
            NegativeOrder = negativeOrder;
            _positiveOrderName = GetName(positiveOrder);
            _negativeOrderName = GetName(negativeOrder);
            _autoVolleyVisualOrder = autoVolleyVisualOrder;
            _manualVolleyVisualOrder = manualVolleyVisualOrder;
        }

        public override TextObject GetName(OrderController orderController)
        {
            switch (GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    switch (_autoVolleyVisualOrder.LastActiveState)
                    {
                        case OrderState.PartiallyActive:
                        case OrderState.Active:
                            return _autoVolleyVisualOrder.GetName(orderController);
                        default:
                            switch (_manualVolleyVisualOrder.LastActiveState)
                            {
                                case OrderState.PartiallyActive:
                                case OrderState.Active:
                                    return _manualVolleyVisualOrder.GetName(orderController);
                            }
                            break;
                    }
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
            Patch_OrderController.LivePreviewFormationChanges.SetVolleyMode(VolleyMode.Disabled, selectedFormations);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (queueCommand)
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
                orderController.SetOrder(orderToAdd.OrderType);
                foreach (var formation in orderController.SelectedFormations)
                {
                    CommandQueueLogic.SetFormationVolleyMode(formation, VolleyMode.Disabled);
                }
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return Utilities.Utility.DoesFormationHasOrderType(formation, PositiveOrder);
        }

        protected override string GetIconId()
        {
            string iconId = base.GetIconId();
            if (_lastActiveState != OrderState.Active || _manualVolleyVisualOrder.LastActiveState == OrderState.Active)
            {
                return iconId;
            }
            return iconId + "_active";
        }
    }
}
