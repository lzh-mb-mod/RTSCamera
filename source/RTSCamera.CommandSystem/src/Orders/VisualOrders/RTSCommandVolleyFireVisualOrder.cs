using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandVolleyFireVisualOrder : RTSCommandVisualOrder
    {
        private readonly TextObject _volleyFireName;
        public RTSCommandVolleyFireVisualOrder(string stringId) : base(stringId)
        {
            _volleyFireName = GameTexts.FindText("str_rts_camera_command_system_volley_fire");
        }

        public override void ExecuteOrder(OrderController orderController, VisualOrderExecutionParameters executionParameters)
        {
            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations;
            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            orderToAdd.CustomOrderType = CustomOrderType.VolleyFire;
            Patch_OrderController.LivePreviewFormationChanges.SetFiringOrder(OrderType.FireAtWill, selectedFormations);
            Patch_OrderController.LivePreviewFormationChanges.SetVolleyMode(VolleyMode.Manual, selectedFormations);
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
                    CommandQueueLogic.SetFormationVolleyMode(formation, VolleyMode.Manual);
                    CommandQueueLogic.FormationVolleyFire(formation);
                }
                Utilities.Utility.CallAfterSetOrder(orderController, OrderType.FireAtWill);
                CommandQueueLogic.OnCustomOrderIssued(orderToAdd, orderController);
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
            }
        }

        public override TextObject GetName(OrderController orderController)
        {
            return _volleyFireName;
        }

        public override bool IsTargeted()
        {
            return false;
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return false;
        }
        protected override string GetIconId()
        {
            var iconId = "order_toggle_fire";
            return iconId + "_active";
        }
    }
}
