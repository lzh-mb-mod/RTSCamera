using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Orders.VisualOrders;
using SandBox.Missions.MissionLogics.Hideout;
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
        private bool IsHideOut => Mission.Current?.HasMissionBehavior<HideoutMissionController>() ?? false;
        private bool IsNavalRaid = Mission.Current?.IsNavalRaidBattle ?? false;
        public override bool IsAvailable()
        {
            return Mission.Current != null && !Mission.Current.IsFriendlyMission && !Mission.Current.IsNavalBattle;
        }

        public override MBReadOnlyList<VisualOrderSet> GetOrders()
        {
            return BannerlordConfig.OrderLayoutType == 1 && !IsNavalRaid ? GetLegacyOrders() : GetDefaultOrders();
        }

        private MBReadOnlyList<VisualOrderSet> GetDefaultOrders()
        {
            MBList<VisualOrderSet> defaultOrders = new MBList<VisualOrderSet>();
            GenericVisualOrderSet movementVisualOrderSet = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), false, true);
            movementVisualOrderSet.AddOrder(new RTSCommandMoveVisualOrder("order_movement_move"));
            movementVisualOrderSet.AddOrder(new RTSCommandFollowMeVisualOrder("order_movement_follow"));
            movementVisualOrderSet.AddOrder(new RTSCommandChargeVisualOrder("order_movement_charge"));
            if (!IsHideOut)
            {
                movementVisualOrderSet.AddOrder(new RTSCommandAdvanceVisualOrder("order_movement_advance"));
            }
            movementVisualOrderSet.AddOrder(new RTSCommandFallbackVisualOrder("order_movement_fallback"));
            movementVisualOrderSet.AddOrder(new RTSCommandStopVisualOrder("order_movement_stop"));
            movementVisualOrderSet.AddOrder(new RTSCommandRetreatVisualOrder("order_movement_retreat"));
            movementVisualOrderSet.AddOrder(new ReturnVisualOrder());
            GenericVisualOrderSet formVisualOrderSet = new GenericVisualOrderSet("order_type_form", new TextObject("{=iBk2wbn3}Form"), true, true);
            RTSCommandArrangementVisualOrder lineFormationOrder = new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Line, "order_form_line");
            RTSCommandArrangementVisualOrder shieldWallFormationOrder = new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.ShieldWall, "order_form_close");
            formVisualOrderSet.AddOrder(lineFormationOrder);
            formVisualOrderSet.AddOrder(shieldWallFormationOrder);
            formVisualOrderSet.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Loose, "order_form_loose"));
            formVisualOrderSet.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Circle, "order_form_circular"));
            formVisualOrderSet.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Square, "order_form_schiltron"));
            formVisualOrderSet.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Skein, "order_form_v"));
            formVisualOrderSet.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Column, "order_form_column"));
            formVisualOrderSet.AddOrder(new RTSCommandArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Scatter, "order_form_scatter"));
            formVisualOrderSet.AddOrder(new ReturnVisualOrder());
            GenericVisualOrderSet toggleVisualOrderSet = new GenericVisualOrderSet("order_type_toggle", new TextObject("{=0HTNYQz2}Toggle"), false, false);
            RTSCommandToggleFacingVisualOrder toggleFacingOrder = new RTSCommandToggleFacingVisualOrder("order_toggle_facing");
            RTSCommandToggleFireVisualOrder toggleFireOrder = new RTSCommandToggleFireVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire, new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto), new RTSCommandToggleVolleyVisualOrder("order_manual_volley", GameTexts.FindText("str_rts_camera_command_system_manual_volley"), GameTexts.FindText("str_rts_camera_command_system_manual_volley_off"), Logic.VolleyMode.Manual));
            RTSCommandGenericToggleVisualOrder toggleMountOrder = new RTSCommandGenericToggleVisualOrder("order_toggle_mount", OrderType.Mount, OrderType.Dismount);
            RTSCommandGenericToggleVisualOrder toggleAIOrder = GameNetwork.IsMultiplayer ? null : new RTSCommandGenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff);
            TransferTroopsVisualOrder transferOrder = GameNetwork.IsMultiplayer ? null : new TransferTroopsVisualOrder();
            RTSCommandActivateFacingVisualOrder activateFacingOrder = new RTSCommandActivateFacingVisualOrder(OrderType.LookAtDirection, "order_toggle_facing");
            var defensiveHoldOrder = new RTSCommandToggleDefensiveHoldVisualOrder("order_defensive_hold", "order_movement_stop");
            toggleVisualOrderSet.AddOrder(toggleFacingOrder);
            toggleVisualOrderSet.AddOrder(toggleFireOrder);
            if (!Input.IsGamepadActive && CommandSystemConfig.Get().AddDefensiveHoldOrder)
            {
                toggleVisualOrderSet.AddOrder(defensiveHoldOrder);
            }
            else if (!IsNavalRaid)
            {
                toggleVisualOrderSet.AddOrder(toggleMountOrder);
            }
            if (toggleAIOrder != null)
                toggleVisualOrderSet.AddOrder(toggleAIOrder);
            if (transferOrder != null)
                toggleVisualOrderSet.AddOrder(transferOrder);

            toggleVisualOrderSet.AddOrder(new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto));
            toggleVisualOrderSet.AddOrder(new RTSCommandToggleVolleyVisualOrder("order_manual_volley", GameTexts.FindText("str_rts_camera_command_system_manual_volley"), GameTexts.FindText("str_rts_camera_command_system_manual_volley_off"), Logic.VolleyMode.Manual));
            toggleVisualOrderSet.AddOrder(new RTSCommandVolleyFireVisualOrder("order_volley_fire"));
            toggleVisualOrderSet.AddOrder(new ReturnVisualOrder());
            defaultOrders.Add(movementVisualOrderSet);
            defaultOrders.Add(formVisualOrderSet);
            defaultOrders.Add(toggleVisualOrderSet);
            if (!Input.IsGamepadActive)
            {
                defaultOrders.Add(new SingleVisualOrderSet(toggleFireOrder));
                defaultOrders.Add(new SingleVisualOrderSet(toggleMountOrder));
                if (toggleAIOrder != null)
                    defaultOrders.Add(new SingleVisualOrderSet(toggleAIOrder));
                defaultOrders.Add(new SingleVisualOrderSet(activateFacingOrder));
                defaultOrders.Add(new SingleVisualOrderSet(shieldWallFormationOrder));
                defaultOrders.Add(new SingleVisualOrderSet(lineFormationOrder));
            }
            if (Input.IsGamepadActive && CommandSystemConfig.Get().AddDefensiveHoldOrder)
            {
                defaultOrders.Add(new SingleVisualOrderSet(defensiveHoldOrder));
            }
            return defaultOrders;
        }

        private MBList<VisualOrderSet> GetLegacyOrders()
        {
            MBList<VisualOrderSet> legacyOrders = new MBList<VisualOrderSet>();
            GenericVisualOrderSet genericVisualOrderSet1 = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement"), false, false);
            genericVisualOrderSet1.AddOrder(new RTSCommandMoveVisualOrder("order_movement_move"));
            genericVisualOrderSet1.AddOrder(new RTSCommandFollowMeVisualOrder("order_movement_follow"));
            genericVisualOrderSet1.AddOrder(new RTSCommandChargeVisualOrder("order_movement_charge"));
            if (!IsHideOut)
            {
                genericVisualOrderSet1.AddOrder(new RTSCommandAdvanceVisualOrder("order_movement_advance"));
            }
            genericVisualOrderSet1.AddOrder(new RTSCommandFallbackVisualOrder("order_movement_fallback"));
            genericVisualOrderSet1.AddOrder(new RTSCommandStopVisualOrder("order_movement_stop"));
            genericVisualOrderSet1.AddOrder(new RTSCommandRetreatVisualOrder("order_movement_retreat"));
            genericVisualOrderSet1.AddOrder(new ReturnVisualOrder());
            GenericVisualOrderSet genericVisualOrderSet2 = new GenericVisualOrderSet("order_type_facing", new TextObject("{=psynaDsM}Facing"), false, false);
            RTSCommandSingleVisualOrder order1 = new RTSCommandSingleVisualOrder("order_toggle_facing", new TextObject("{=MH9Pi3ao}Face Direction"), OrderType.LookAtDirection, false, true);
            RTSCommandSingleVisualOrder order2 = new RTSCommandSingleVisualOrder("order_toggle_facing_active", new TextObject("{=u8j8nN5U}Face Enemy"), OrderType.LookAtEnemy, true, false);
            genericVisualOrderSet2.AddOrder(order1);
            genericVisualOrderSet2.AddOrder(order2);
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
            RTSCommandToggleFireVisualOrder order5 = new RTSCommandToggleFireVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire, new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto), new RTSCommandToggleVolleyVisualOrder("order_manual_volley", GameTexts.FindText("str_rts_camera_command_system_manual_volley"), GameTexts.FindText("str_rts_camera_command_system_manual_volley_off"), Logic.VolleyMode.Manual));
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
            RTSCommandGenericVisualOrderSet volleyVisualOrderSet = new RTSCommandGenericVisualOrderSet("order_type_volley", GameTexts.FindText("str_rts_camera_command_system_volley_order"), true, true, new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto));
            volleyVisualOrderSet.AddOrder(new RTSCommandToggleVolleyVisualOrder("order_auto_volley", GameTexts.FindText("str_rts_camera_command_system_auto_volley"), GameTexts.FindText("str_rts_camera_command_system_auto_volley_off"), Logic.VolleyMode.Auto));
            volleyVisualOrderSet.AddOrder(new RTSCommandToggleVolleyVisualOrder("order_manual_volley", GameTexts.FindText("str_rts_camera_command_system_manual_volley"), GameTexts.FindText("str_rts_camera_command_system_manual_volley_off"), Logic.VolleyMode.Manual));
            volleyVisualOrderSet.AddOrder(new RTSCommandVolleyFireVisualOrder("order_volley_fire"));
            volleyVisualOrderSet.AddOrder(new ReturnVisualOrder());
            legacyOrders.Add(volleyVisualOrderSet);
            var defensiveHoldOrder = new RTSCommandToggleDefensiveHoldVisualOrder("order_defensive_hold", "order_movement_stop");
            if (!Input.IsGamepadActive)
            {
                if (CommandSystemConfig.Get().AddDefensiveHoldOrder)
                {
                    legacyOrders.Add(new SingleVisualOrderSet(defensiveHoldOrder));
                }
                else
                {
                    legacyOrders.Add(new SingleVisualOrderSet(new ReturnVisualOrder()));
                }
            }
            else
            {
                volleyVisualOrderSet.AddOrder(defensiveHoldOrder);
            }
            return legacyOrders;
        }
    }
}
