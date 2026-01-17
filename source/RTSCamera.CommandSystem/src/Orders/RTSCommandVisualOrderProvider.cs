using RTSCamera.CommandSystem.Orders.VisualOrders;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;

namespace RTSCamera.CommandSystem.Orders
{
    public class RTSCommandVisualOrderProvider: VisualOrderProvider
    {
        public override bool IsAvailable()
        {
            return Mission.Current != null && !Mission.Current.IsFriendlyMission && !Mission.Current.IsNavalBattle;
        }

        public override MBReadOnlyList<VisualOrderSet> GetOrders()
        {
            return BannerlordConfig.OrderLayoutType == 1 ? GetLegacyOrders() : GetDefaultOrders();
        }

        private MBReadOnlyList<VisualOrderSet> GetDefaultOrders()
        {
            MBList<VisualOrderSet> defaultOrders = new MBList<VisualOrderSet>();
            RTSCommandGenericVisualOrderSet genericVisualOrderSet1 = new RTSCommandGenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), false, true, null);
            genericVisualOrderSet1.AddOrder(new RTSCommandMoveVisualOrder("order_movement_move"));
            genericVisualOrderSet1.AddOrder(new RTSCommandFollowMeVisualOrder("order_movement_follow"));
            genericVisualOrderSet1.AddOrder(new RTSCommandChargeVisualOrder("order_movement_charge"));
            genericVisualOrderSet1.AddOrder(new RTSCommandAdvanceVisualOrder("order_movement_advance"));
            genericVisualOrderSet1.AddOrder(new RTSCommandFallbackVisualOrder("order_movement_fallback"));
            genericVisualOrderSet1.AddOrder(new RTSCommandStopVisualOrder("order_movement_stop"));
            genericVisualOrderSet1.AddOrder(new RTSCommandRetreatVisualOrder("order_movement_retreat"));
            genericVisualOrderSet1.AddOrder(new ReturnVisualOrder());
            GenericVisualOrderSet genericVisualOrderSet2 = new GenericVisualOrderSet("order_type_form", new TextObject("{=iBk2wbn3}Form"), true, true);
            RTSCommandArrangementVisualOrder order1 = new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Line, "order_form_line");
            RTSCommandArrangementVisualOrder order2 = new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.ShieldWall, "order_form_close");
            genericVisualOrderSet2.AddOrder(order1);
            genericVisualOrderSet2.AddOrder(order2);
            genericVisualOrderSet2.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Loose, "order_form_loose"));
            genericVisualOrderSet2.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Circle, "order_form_circular"));
            genericVisualOrderSet2.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Square, "order_form_schiltron"));
            genericVisualOrderSet2.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Skein, "order_form_v"));
            genericVisualOrderSet2.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Column, "order_form_column"));
            genericVisualOrderSet2.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Scatter, "order_form_scatter"));
            genericVisualOrderSet2.AddOrder(new ReturnVisualOrder());
            GenericVisualOrderSet genericVisualOrderSet3 = new GenericVisualOrderSet("order_type_toggle", new TextObject("{=0HTNYQz2}Toggle"), false, false);
            RTSCommandToggleFacingVisualOrder order3 = new RTSCommandToggleFacingVisualOrder("order_toggle_facing");
            var autoVolleyVisualOrder = new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto);
            var manualVolleyVisualOrder = new RTSCommandToggleVolleyVisualOrder("order_manual_volley", GameTexts.FindText("str_rts_camera_command_system_manual_volley"), GameTexts.FindText("str_rts_camera_command_system_manual_volley_off"), Logic.VolleyMode.Manual);
            RTSCommandToggleFireVisualOrder order4 = new RTSCommandToggleFireVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire, autoVolleyVisualOrder, manualVolleyVisualOrder);
            RTSCommandGenericToggleVisualOrder order5 = new RTSCommandGenericToggleVisualOrder("order_toggle_mount", OrderType.Mount, OrderType.Dismount);
            RTSCommandGenericToggleVisualOrder order6 = GameNetwork.IsMultiplayer ? null : new RTSCommandGenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff);
            TransferTroopsVisualOrder order7 = GameNetwork.IsMultiplayer ? null : new TransferTroopsVisualOrder();
            RTSCommandActivateFacingVisualOrder order8 = new RTSCommandActivateFacingVisualOrder(OrderType.LookAtDirection, "order_toggle_facing");
            genericVisualOrderSet3.AddOrder(order3);
            genericVisualOrderSet3.AddOrder(order4);
            genericVisualOrderSet3.AddOrder(order5);
            if (order6 != null)
                genericVisualOrderSet3.AddOrder(order6);
            if (order7 != null)
                genericVisualOrderSet3.AddOrder(order7);

            genericVisualOrderSet3.AddOrder(autoVolleyVisualOrder);
            genericVisualOrderSet3.AddOrder(manualVolleyVisualOrder);
            genericVisualOrderSet3.AddOrder(new RTSCommandVolleyFireVisualOrder("order_volley_fire"));
            genericVisualOrderSet3.AddOrder(new ReturnVisualOrder());
            defaultOrders.Add(genericVisualOrderSet1);
            defaultOrders.Add(genericVisualOrderSet2);
            defaultOrders.Add(genericVisualOrderSet3);
            if (!Input.IsGamepadActive)
            {
                defaultOrders.Add(new SingleVisualOrderSet(order4));
                defaultOrders.Add(new SingleVisualOrderSet(order5));
                if (order6 != null)
                    defaultOrders.Add(new SingleVisualOrderSet(order6));
                defaultOrders.Add(new SingleVisualOrderSet(order8));
                defaultOrders.Add(new SingleVisualOrderSet(order2));
                defaultOrders.Add(new SingleVisualOrderSet(order1));
            }
            defaultOrders.Add(new SingleVisualOrderSet(new ReturnVisualOrder()));
            return defaultOrders;
        }

        private MBList<VisualOrderSet> GetLegacyOrders()
        {
            MBList<VisualOrderSet> legacyOrders = new MBList<VisualOrderSet>();
            RTSCommandGenericVisualOrderSet genericVisualOrderSet1 = new RTSCommandGenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), false, false, null);
            genericVisualOrderSet1.AddOrder(new RTSCommandMoveVisualOrder("order_movement_move"));
            genericVisualOrderSet1.AddOrder(new RTSCommandFollowMeVisualOrder("order_movement_follow"));
            genericVisualOrderSet1.AddOrder(new RTSCommandChargeVisualOrder("order_movement_charge"));
            genericVisualOrderSet1.AddOrder(new RTSCommandAdvanceVisualOrder("order_movement_advance"));
            genericVisualOrderSet1.AddOrder(new RTSCommandFallbackVisualOrder("order_movement_fallback"));
            genericVisualOrderSet1.AddOrder(new RTSCommandStopVisualOrder("order_movement_stop"));
            genericVisualOrderSet1.AddOrder(new RTSCommandRetreatVisualOrder("order_movement_retreat"));
            genericVisualOrderSet1.AddOrder(new ReturnVisualOrder());
            RTSCommandGenericVisualOrderSet genericVisualOrderSet2 = new RTSCommandGenericVisualOrderSet("order_type_facing", new TextObject("{=psynaDsM}Facing"), false, false, null);
            RTSCommandSingleVisualOrder order1 = new RTSCommandSingleVisualOrder("order_toggle_facing", new TextObject("{=MH9Pi3ao}Face Direction"), OrderType.LookAtDirection, false, true);
            RTSCommandSingleVisualOrder order2 = new RTSCommandSingleVisualOrder("order_toggle_facing_active", new TextObject("{=u8j8nN5U}Face Enemy"), OrderType.LookAtEnemy, true, false);
            genericVisualOrderSet2.AddOrder(order1);
            genericVisualOrderSet2.AddOrder(order2);
            var autoVolleyVisualOrder = new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto);
            var manualVolleyVisualOrder = new RTSCommandToggleVolleyVisualOrder("order_manual_volley", GameTexts.FindText("str_rts_camera_command_system_manual_volley"), GameTexts.FindText("str_rts_camera_command_system_manual_volley_off"), Logic.VolleyMode.Manual);
            GenericVisualOrderSet genericVisualOrderSet3 = new GenericVisualOrderSet("order_type_form", new TextObject("{=iBk2wbn3}Form"), true, true);
            RTSCommandArrangementVisualOrder order3 = new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Line, "order_form_line");
            RTSCommandArrangementVisualOrder order4 = new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.ShieldWall, "order_form_close");
            genericVisualOrderSet3.AddOrder(order3);
            genericVisualOrderSet3.AddOrder(order4);
            genericVisualOrderSet3.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Loose, "order_form_loose"));
            genericVisualOrderSet3.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Circle, "order_form_circular"));
            genericVisualOrderSet3.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Square, "order_form_schiltron"));
            genericVisualOrderSet3.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Skein, "order_form_v"));
            genericVisualOrderSet3.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Column, "order_form_column"));
            genericVisualOrderSet3.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Scatter, "order_form_scatter"));
            genericVisualOrderSet3.AddOrder(new ReturnVisualOrder());
            legacyOrders.Add(genericVisualOrderSet1);
            legacyOrders.Add(genericVisualOrderSet2);
            legacyOrders.Add(genericVisualOrderSet3);
            RTSCommandToggleFireVisualOrder order5 = new RTSCommandToggleFireVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire, autoVolleyVisualOrder, manualVolleyVisualOrder);
            RTSCommandGenericToggleVisualOrder order6 = new RTSCommandGenericToggleVisualOrder("order_toggle_mount", OrderType.Mount, OrderType.Dismount);
            RTSCommandGenericToggleVisualOrder order7 = GameNetwork.IsMultiplayer ? null : new RTSCommandGenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff);
            TransferTroopsVisualOrder order8 = GameNetwork.IsMultiplayer ? null : new TransferTroopsVisualOrder();

            if (!Input.IsGamepadActive)
            {
                legacyOrders.Add(new SingleVisualOrderSet(order5));
                legacyOrders.Add(new SingleVisualOrderSet(order6));
                if (order7 != null)
                    legacyOrders.Add(new SingleVisualOrderSet(order7));
                if (order8 != null)
                    legacyOrders.Add(new SingleVisualOrderSet(order8));
            }
            RTSCommandGenericVisualOrderSet volleyVisualOrderSet = new RTSCommandGenericVisualOrderSet("order_type_volley", GameTexts.FindText("str_rts_camera_command_system_volley_order"), true, true, autoVolleyVisualOrder);
            volleyVisualOrderSet.AddOrder(autoVolleyVisualOrder);
            volleyVisualOrderSet.AddOrder(manualVolleyVisualOrder);
            volleyVisualOrderSet.AddOrder(new RTSCommandVolleyFireVisualOrder("order_volley_fire"));
            volleyVisualOrderSet.AddOrder(new ReturnVisualOrder());
            legacyOrders.Add(volleyVisualOrderSet);
            if (!Input.IsGamepadActive)
            {
                legacyOrders.Add(new SingleVisualOrderSet(new ReturnVisualOrder()));
            }
            return legacyOrders;
        }
    }
}
