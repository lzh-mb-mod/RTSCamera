using RTSCamera.CommandSystem.Patch;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders
{
     public class RTSCommandOrderItemVM : OrderItemVM
     {
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
            base.OnExecuteAction(executionParameters);
        }
        public void OnEscape()
        {
            Mission.Current.GetMissionBehavior<GauntletOrderUIHandler>()?.OnEscape();
        }
    }

}
