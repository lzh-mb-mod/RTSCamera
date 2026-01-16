using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandToggleVolleyVisualOrder: RTSCommandVisualOrder
    {
        private readonly TextObject _positiveOrderName;
        private readonly TextObject _negativeOrderName;
        private readonly VolleyMode _volleyMode;

        public OrderState LastActiveState => _lastActiveState;
        public RTSCommandToggleVolleyVisualOrder(string stringId, TextObject positiveOrderName, TextObject negativeOrderName, VolleyMode volleyMode) : base(stringId)
        {
            _positiveOrderName = positiveOrderName;
            _negativeOrderName = negativeOrderName;
            _volleyMode = volleyMode;
        }

        public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
        {
            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations;
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            var volleyModeToSet = GetActiveState(orderController) == OrderState.Active ? VolleyMode.Disabled : _volleyMode;
            orderToAdd.CustomOrderType = GetActiveState(orderController) == OrderState.Active ? CustomOrderType.DisableVolley : _volleyMode == VolleyMode.Auto ? CustomOrderType.AutoVolley : CustomOrderType.ManualVolley;
            Patch_OrderController.LivePreviewFormationChanges.SetFiringOrder(OrderType.FireAtWill, selectedFormations);
            Patch_OrderController.LivePreviewFormationChanges.SetVolleyMode(volleyModeToSet, selectedFormations);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (queueCommand)
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                foreach (Formation formation in selectedFormations)
                {
                    formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
                    CommandQueueLogic.SetFormationVolleyMode(formation, volleyModeToSet);
                }
                Utilities.Utility.CallAfterSetOrder(orderController, volleyModeToSet == VolleyMode.Manual ? OrderType.HoldFire : OrderType.FireAtWill);
                CommandQueueLogic.OnCustomOrderIssued(orderToAdd, orderController);
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
            }
        }

        public override TextObject GetName(OrderController orderController)
        {
            switch (this.GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    return _positiveOrderName;
                default:
                    return _negativeOrderName;
            }
        }

        public override bool IsTargeted()
        {
            return false;
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return Utilities.Utility.DoesFormationHasVolleyOrder(formation, _volleyMode);
        }

        protected override string GetIconId()
        {
            var iconId = "order_toggle_fire";
            return _volleyMode == VolleyMode.Manual && _lastActiveState == OrderState.Active ? iconId : iconId + "_active";
        }
    }
}
