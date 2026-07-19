using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandToggleDefensiveHoldVisualOrder : RTSCommandVisualOrder
    {
        private string _iconId;
        public static TextObject GetName(DefensiveHoldMode defensiveHoldMode)
        {
            switch (defensiveHoldMode)
            {
                case DefensiveHoldMode.Enabled:
                    return GameTexts.FindText("str_rts_camera_command_system_defensive_hold_on");
                case DefensiveHoldMode.Disabled:
                    return GameTexts.FindText("str_rts_camera_command_system_defensive_hold_off");
            }
            return new TextObject("");
        }
        public RTSCommandToggleDefensiveHoldVisualOrder(string stringId, string iconId) : base(stringId)
        {
            _iconId = iconId;
        }

        protected override string GetIconId()
        {
            return _iconId;
        }

        public override TextObject GetName(OrderController orderController)
        {
            switch (GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    return GetName(DefensiveHoldMode.Enabled);
                default:
                    return GetName(DefensiveHoldMode.Disabled);
            }
        }

        public override void ExecuteOrder(
            OrderController orderController,
            VisualOrderExecutionParameters executionParameters)
        {
            bool queueCommand = OnBeforeExecuteOrder(orderController, executionParameters);
            var selectedFormations = orderController.SelectedFormations.ToList();
            if (selectedFormations.Count == 0)
                return;

            var orderToAdd = new OrderInQueue
            {
                SelectedFormations = selectedFormations
            };
            var defensiveHoldModeToSet = GetActiveState(orderController) == OrderState.Active ? DefensiveHoldMode.Disabled : DefensiveHoldMode.Enabled;
            orderToAdd.CustomOrderType = GetActiveState(orderController) == OrderState.Active ? CustomOrderType.DisableDefensiveHold : CustomOrderType.EnableDefensiveHold;
            Patch_OrderController.LivePreviewFormationChanges.SetDefensiveHoldMode(defensiveHoldModeToSet, selectedFormations);
            orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);

            if (queueCommand)
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                foreach (Formation formation in selectedFormations)
                {
                    CommandQueueLogic.SetFormationDefensiveHoldMode(formation, defensiveHoldModeToSet);
                }
                Utilities.Utility.CallAfterSetOrder(orderController, defensiveHoldModeToSet == DefensiveHoldMode.Enabled ? OrderType.StandYourGround : OrderType.Move);
                CommandQueueLogic.OnCustomOrderIssued(orderToAdd, orderController);
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
            }
        }

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return CommandQueueLogic.GetFormationDefensiveHoldMode(formation) == DefensiveHoldMode.Enabled;
        }

        public override bool IsTargeted() => false;
    }

}
