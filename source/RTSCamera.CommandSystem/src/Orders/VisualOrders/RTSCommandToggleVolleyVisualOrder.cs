using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Orders;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandToggleVolleyVisualOrder: RTSCommandVisualOrder
    {
        private readonly TextObject _volleyEnabledName;
        private readonly TextObject _volleyDisabledName;
        public RTSCommandToggleVolleyVisualOrder(string stringId) : base(stringId)
        {
            _volleyEnabledName = GameTexts.FindText("str_rts_camera_command_system_volley_enabled");
            _volleyDisabledName = GameTexts.FindText("str_rts_camera_command_system_volley_disabled");
        }

        public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
        {
            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations;
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            orderToAdd.CustomOrderType = GetActiveState(orderController) == OrderState.Active ? CustomOrderType.DisableVolley : CustomOrderType.EnableVolley;
            bool volleyEnabled = orderToAdd.CustomOrderType == CustomOrderType.EnableVolley ? true : false;
            Patch_OrderController.LivePreviewFormationChanges.SetFiringOrder(OrderType.FireAtWill, selectedFormations);
            Patch_OrderController.LivePreviewFormationChanges.SetVolleyEnabledOrder(volleyEnabled, selectedFormations);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (queueCommand)
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                foreach (Formation formation in selectedFormations)
                {
                    CommandQueueLogic.SetFormationVolleyEnabled(formation, volleyEnabled);
                }
                orderController.SetOrder(OrderType.FireAtWill);
                CommandQueueLogic.OnCustomOrderIssued(orderToAdd, orderController);
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);

            }
        }

        public override TextObject GetName(OrderController orderController)
        {
            switch (GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    return _volleyEnabledName;
                default:
                    return _volleyDisabledName;
            }
        }

        public override bool IsTargeted()
        {
            return false;
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return Utilities.Utility.DoesFormationHasVolleyOrder(formation);
        }

        protected override string GetIconId()
        {
            var iconId = "order_toggle_fire";
            return _lastActiveState == OrderState.Active ? iconId + "_active" : iconId;
        }
    }
}
