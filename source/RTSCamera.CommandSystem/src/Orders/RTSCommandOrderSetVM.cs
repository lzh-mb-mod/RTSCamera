using RTSCamera.CommandSystem.Patch;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders
{
    public class RTSCommandOrderSetVM : OrderSetVM
    {
        public RTSCommandOrderSetVM(OrderController orderController, VisualOrderSet collection) : base(orderController, collection)
        {
        }
        public void ExecuteClickAction()
        {
            Patch_OrderTroopPlacer.Reset();
            if (OrderSet.IsSoloOrder)
            {
                if (Orders.Count > 0)
                {
                    var vm = Orders[0] as RTSCommandOrderItemVM;
                    vm?.ExecuteClickAction();
                }
            }
            else
            {
                ExecuteAction(new VisualOrderExecutionParameters(Agent.Main, null, null));
            }
        }

        public void OnEscape()
        {
            Mission.Current.GetMissionBehavior<GauntletOrderUIHandler>()?.OnEscape();
        }
    }
}
