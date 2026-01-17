using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders.VisualOrders
{
    public class RTSCommandGenericVisualOrderSet : VisualOrderSet
    {
        private readonly TextObject _name;
        private readonly string _stringId;
        private readonly bool _useActiveOrderForIconId;
        private readonly bool _useActiveOrderForName;
        private readonly VisualOrder _otherOrder;

        public override bool IsSoloOrder => false;

        public override string StringId => this._stringId;

        public override string IconId
        {
            get
            {
                if (this._useActiveOrderForIconId)
                {
                    for (int index = 0; index < this.Orders.Count; ++index)
                    {
                        if (this.Orders[index].GetActiveState(Mission.Current.PlayerTeam.PlayerOrderController) == OrderState.Active)
                            return this.Orders[index].IconId;
                    }
                }
                if (_otherOrder != null)
                {
                    return _otherOrder.IconId;
                }
                return this._stringId;
            }
        }

        public override TextObject GetName(OrderController orderController)
        {
            if (this._useActiveOrderForName)
            {
                for (int index = 0; index < this.Orders.Count; ++index)
                {
                    if (this.Orders[index].GetActiveState(orderController) == OrderState.Active)
                        return this.Orders[index].GetName(orderController);
                }
            }
            if (_otherOrder != null)
            {
                return _otherOrder.GetName(orderController);
            }
            return this._name;
        }

        public RTSCommandGenericVisualOrderSet(
          string stringId,
          TextObject name,
          bool useActiveOrderForIconId,
          bool useActiveOrderForName,
          VisualOrder otherOrder)
        {
            this._stringId = stringId;
            this._name = name;
            this._useActiveOrderForIconId = useActiveOrderForIconId;
            this._useActiveOrderForName = useActiveOrderForName;
            _otherOrder = otherOrder;
        }
    }
}
