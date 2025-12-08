using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using static RTSCamera.Utilities.Utility;

namespace RTSCamera.Patch.Naval
{
    public class Patch_NavalMovementOrder
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(AccessTools.TypeByName("NavalMovementOrder").Method("OnGetFormationHasOrder"),
                    prefix: new HarmonyMethod(typeof(Patch_NavalMovementOrder).GetMethod(nameof(Prefix_OnGetFormationHasOrder), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_OnGetFormationHasOrder(VisualOrder __instance, ref bool? __result, Formation formation, OrderType ____orderType)
        {
            try
            {
                var navalShipsLogic = Utilities.Utility.GetNavalShipsLogic(Mission.Current);
                if (navalShipsLogic == null)
                    return false;
                var movementOrderEnum = GetMovementOrderEnum(____orderType);
                var ship = Utilities.Utility.GetShip(navalShipsLogic, formation.Team.TeamSide, formation.FormationIndex);
                if (ship == null)
                    return false;
                // The only change, do not return null for player ship
                //return ship.IsPlayerShip || ship.IsPlayerControlled ? new bool?() : new bool?(ship.ShipOrder.MovementOrderEnum == movementOrderEnum);
                __result = Utilities.Utility.IsShipPlayerControlled(ship) ? new bool?() : new bool?(Utilities.Utility.GetShipMovementOrderEnum(Utilities.Utility.GetShipOrder(ship)) == movementOrderEnum);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }
        private static ShipMovementOrderEnum GetMovementOrderEnum(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Move:
                    return ShipMovementOrderEnum.Move;
                case OrderType.StandYourGround:
                    return ShipMovementOrderEnum.Stop;
                case OrderType.FollowMe:
                    return ShipMovementOrderEnum.StaticOrderCount;
                case OrderType.Retreat:
                    return ShipMovementOrderEnum.Retreat;
                case OrderType.Advance:
                    return ShipMovementOrderEnum.Engage;
                default:
                    Debug.FailedAssert("Failed to find corresponding ship order of: " + (object)orderType, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC.View\\VisualOrders\\Orders\\NavalMovementOrder.cs", nameof(GetMovementOrderEnum), 96 /*0x60*/);
                    return ShipMovementOrderEnum.Move;
            }
        }
    }
}
