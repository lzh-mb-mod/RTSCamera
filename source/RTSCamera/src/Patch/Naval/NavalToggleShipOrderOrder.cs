using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Config;
using RTSCamera.Logic;
using RTSCamera.Logic.SubLogic;
using RTSCamera.Patch.Fix;
using System.Reflection;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.Patch.Naval
{
    public class NavalToggleShipOrderOrder : VisualOrder
    {
        private readonly TextObject _positiveOrderName;
        private readonly TextObject _negativeOrderName;

        public NavalToggleShipOrderOrder(
          string stringId,
          TextObject positiveOrder,
          TextObject negativeOrder)
          : base(stringId)
        {
            this._positiveOrderName = positiveOrder;
            this._negativeOrderName = negativeOrder;
        }

        public override TextObject GetName(OrderController orderController)
        {
            switch (GetActiveState(orderController))
            {
                case OrderState.PartiallyActive:
                case OrderState.Active:
                    return this._positiveOrderName;
                default:
                    return this._negativeOrderName;
            }
        }
        public override bool IsTargeted() => false;

        protected override bool? OnGetFormationHasOrder(Formation formation)
        {
            if (Agent.Main == null)
                return null;
            if (Agent.Main.Formation == formation)
            {
                return Patch_MissionShip.ShouldAIControlPlayerShipInPlayerMode;
            }
            return null;
        }

        private static MethodInfo _afterSetOrder = AccessTools.Method("TaleWorlds.MountAndBlade.OrderController:AfterSetOrder");

        public override void ExecuteOrder(
          OrderController orderController,
          VisualOrderExecutionParameters executionParameters)
        {
            if (GetActiveState(orderController) == OrderState.Active)
            {
                Patch_MissionShip.ShouldAIControlPlayerShipInPlayerMode = false;
                _afterSetOrder.Invoke(orderController, new object[] { OrderType.AIControlOff });
                Utility.DisplayLocalizedText("str_rts_camera_soldiers_stop_controlling_ship");
                orderController.SetOrder(OrderType.StandYourGround);
                Utilities.Utility.CancelAIPilotPlayerShip(Mission.Current);
            }
            else
            {
                Patch_MissionShip.ShouldAIControlPlayerShipInPlayerMode = true;
                _afterSetOrder.Invoke(orderController, new object[] { OrderType.AIControlOn });
                Utility.DisplayLocalizedText("str_rts_camera_soldiers_start_controlling_ship");
                if (RTSCameraConfig.Get().SteeringModeWhenPlayerStopsPiloting == SteeringMode.DelegateCommand)
                {
                    orderController.SetOrder(OrderType.AIControlOn);
                }
            }
            if (!Input.IsGamepadActive && !(RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false))
                RTSCameraLogic.Instance?.SwitchFreeCameraLogic.RefreshOrders();
        }

        protected override string GetIconId()
        {
            string iconId = base.GetIconId();
            return this._lastActiveState == OrderState.Active ? iconId + "_active" : iconId;
        }
    }
}
