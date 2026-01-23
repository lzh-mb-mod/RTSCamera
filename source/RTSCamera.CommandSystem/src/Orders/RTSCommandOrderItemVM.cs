using RTSCamera.CommandSystem.Patch;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders
{
     public class RTSCommandOrderItemVM : OrderItemVM
    {
        // hide OnExecuteOrder in base class because we cannot trigger it.
        public static new event Action<OrderItemVM> OnExecuteOrder;

        public static void RegisterEvent(MissionOrderVM missionOrderVM)
        {
            if (missionOrderVM == null)
                return;
            OnExecuteOrder += missionOrderVM.OnOrderExecuted;
        }

        public static void ClearEvent()
        {
            OnExecuteOrder = null;
        }

        public RTSCommandOrderItemVM(OrderController orderController, VisualOrder order)
          : base(orderController, order)
        {
        }

        public void ExecuteClickAction()
        {
            Patch_OrderTroopPlacer.Reset();
            RTSCommandVisualOrder.IsFromClicking = true;
            ExecuteAction(new VisualOrderExecutionParameters(Agent.Main, null, null));
            RTSCommandVisualOrder.IsFromClicking = false;
        }

        protected override void OnExecuteAction(VisualOrderExecutionParameters executionParameters)
        {
            if (RTSCommandVisualOrder.IsFromClicking && Order.StringId == "order_movement_move")
                return;
            this.Order.BeforeExecuteOrder(this._orderController, executionParameters);
            this.Order.ExecuteOrder(this._orderController, executionParameters);
            if (RTSCommandVisualOrder.OrderToSelectTarget == SelectTargetMode.None)
            {
                OnExecuteOrder?.Invoke(this);
            }
        }
        public void OnEscape()
        {
            Mission.Current.GetMissionBehavior<GauntletOrderUIHandler>()?.OnEscape();
        }
    }

}
