using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandToggleFacingVisualOrder : RTSCommandVisualOrder
    {
        public static TextObject GetName(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.LookAtEnemy:
                    return new TextObject("{=qWzBa3KT}Facing Enemy");
                case OrderType.LookAtDirection:
                    return new TextObject("{=LWVwNcRA}Facing Direction");
            }
            return new TextObject("");
        }
        public RTSCommandToggleFacingVisualOrder(string stringId) : base(stringId)
        {
        }
        public override TextObject GetName(OrderController orderController)
        {
            switch (this.GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    return GetName(OrderType.LookAtEnemy);
                default:
                    return GetName(OrderType.LookAtDirection);
            }
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

            orderToAdd.OrderType = IsFacingEnemy(GetActiveState(orderController)) ? OrderType.LookAtDirection : OrderType.LookAtEnemy;
            if (orderToAdd.OrderType == OrderType.LookAtDirection)
            {
                if (IsSelectTargetForMouseClickingKeyDown && OrderToSelectTarget == SelectTargetMode.None && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickableExtension)
                {
                    // Allows to click ground to select target to facing to.
                    OrderToSelectTarget = SelectTargetMode.LookAtDirection;
                    return;
                }
            }
            else
            {
                if (IsSelectTargetForMouseClickingKeyDown && OrderToSelectTarget == SelectTargetMode.None && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickableExtension)
                {
                    // Allows to click enemy to select target to facing to.
                    OrderToSelectTarget = SelectTargetMode.LookAtEnemy;
                    return;
                }
            }
            if (queueCommand)
            {
                if (orderToAdd.OrderType == OrderType.LookAtDirection)
                {
                    Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, orderController, executionParameters.WorldPosition);
                }
                else
                {
                    orderToAdd.TargetFormation = executionParameters.Formation;
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations, orderToAdd.TargetFormation);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                }
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                if (orderToAdd.OrderType == OrderType.LookAtDirection)
                {
                    Patch_OrderController.SetFacingEnemyTargetFormation(selectedFormations, null);
                    // only pending order for formations that is not executing attacking/advance/fallback, etc.
                    orderToAdd.SelectedFormations = orderToAdd.SelectedFormations.Where(f => !Utilities.Utility.IsFormationOrderPositionMoving(f)).ToList();
                    orderController.SetOrderWithPosition(OrderType.LookAtDirection, executionParameters.WorldPosition);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                }
                else
                {
                    orderToAdd.TargetFormation = executionParameters.Formation;
                    // only pending order for formations that is not executing attacking/advance/fallback, etc.
                    orderToAdd.SelectedFormations = orderToAdd.SelectedFormations.Where(f => !Utilities.Utility.IsFormationOrderPositionMoving(f)).ToList();
                    Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations, orderToAdd.TargetFormation);
                    Patch_OrderController.SetFacingEnemyTargetFormation(selectedFormations, orderToAdd.TargetFormation);
                    orderController.SetOrder(OrderType.LookAtEnemy);
                    orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                }
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
            }
        }

        public override bool IsTargeted() => false;

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return new bool?(OrderController.GetActiveFacingOrderOf(formation) == OrderType.LookAtEnemy);
        }

        protected override string GetIconId()
        {
            string iconId = base.GetIconId();
            return this._lastActiveState == OrderState.Active ? iconId + "_active" : iconId;
        }

        private static bool IsFacingEnemy(OrderState activeState) => activeState == OrderState.Active;
    }
}
