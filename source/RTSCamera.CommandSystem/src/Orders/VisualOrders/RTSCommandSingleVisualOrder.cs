using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandSingleVisualOrder : RTSCommandVisualOrder
    {
        private TextObject _name;
        private OrderType _orderType;
        private bool _useFormationTarget;
        private bool _useWorldPositionTarget;

        public RTSCommandSingleVisualOrder(
          string stringId,
          TextObject name,
          OrderType orderType,
          bool useFormationTarget,
          bool useWorldPositionTarget)
          : base(stringId)
        {
            this._name = name;
            this._orderType = orderType;
            this._useFormationTarget = useFormationTarget;
            this._useWorldPositionTarget = useWorldPositionTarget;
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

            orderToAdd.OrderType = _orderType;
            if (_useFormationTarget || _orderType == OrderType.LookAtEnemy)
            {
                orderToAdd.TargetFormation = executionParameters.Formation;
            }
            if (orderToAdd.OrderType == OrderType.LookAtDirection)
            {
                if (IsFromClicking && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickable && CommandSystemConfig.Get().OrderUIClickableExtension)
                {
                    // Allows to click ground to select target to facing to.
                    OrderToSelectTarget = SelectTargetMode.LookAtDirection;
                    return;
                }
            }
            else
            {
                if (IsSelectTargetForMouseClickingKeyDown && IsFromClicking && Patch_OrderTroopPlacer.IsFreeCamera && CommandSystemConfig.Get().OrderUIClickable && CommandSystemConfig.Get().OrderUIClickableExtension)
                {
                    // Allows to click enemy to select target to facing to.
                    OrderToSelectTarget = SelectTargetMode.LookAtEnemy;
                    return;
                }
            }
            if (orderToAdd.OrderType == OrderType.LookAtDirection)
            {
                Patch_OrderController.FillOrderLookingAtPosition(orderToAdd, orderController, executionParameters.WorldPosition);
            }
            else if (orderToAdd.OrderType == OrderType.LookAtEnemy)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtEnemy, selectedFormations, orderToAdd.TargetFormation);
                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
            }

            if (queueCommand)
            {
                CommandQueueLogic.AddOrderToQueue(orderToAdd);
            }
            else
            {
                // only pending order for formations that should be locked.
                orderToAdd.SelectedFormations = orderToAdd.SelectedFormations.Where(f => !Utilities.Utility.IsFormationOrderPositionMoving(f)).ToList();
                Patch_OrderController.SetFacingEnemyTargetFormation(selectedFormations, orderToAdd.TargetFormation);
                if (executionParameters.HasFormation && _useFormationTarget)
                    orderController.SetOrderWithFormation(_orderType, executionParameters.Formation);
                else if (executionParameters.HasWorldPosition && _useWorldPositionTarget)
                    orderController.SetOrderWithPosition(_orderType, executionParameters.WorldPosition);
                else
                    orderController.SetOrder(_orderType);
                orderToAdd.VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(selectedFormations);
                CommandQueueLogic.TryPendingOrder(orderToAdd.SelectedFormations, orderToAdd);
            }
        }

        public override TextObject GetName(OrderController orderController) => this._name;

        public override bool IsTargeted() => false;

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            return Utilities.Utility.DoesFormationHaveOrderType(formation, _orderType);
        }
    }
}
