using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Naval
{
    public class Patch_ShipControllerMachine
    {
        private static bool _patched;

        private static PropertyInfo _attackedShip;
        private static PropertyInfo _battleSide;
        private static MethodInfo _isAttachedShipVacant;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                if (!RTSCameraSubModule.IsNavalInstalled)
                    return true;
                harmony.Patch(AccessTools.TypeByName("ShipControllerMachine").Method("GetDescriptionText"),
                    prefix: new HarmonyMethod(typeof(Patch_ShipControllerMachine).GetMethod(nameof(Prefix_GetDescriptionText), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_GetDescriptionText(UsableMachine __instance, WeakGameEntity gameEntity, ref TextObject __result, TextObject ____overridenDescriptionForActiveEnemyShipControllerMachine)
        {
            //if (this.AttachedShip.BattleSide == Mission.Current.PlayerTeam.Side)
            //    return new TextObject("{=6PmvlYcT}Control the Ship");
            //if (!this.IsAttachedShipVacant())
            //    return new TextObject("{=UrBktTYi}Clear the crew to capture the ship");
            //if (MissionShip.AreShipsConnected(this._navalShipsLogic.GetShipAssignment(Agent.Main.Formation.Team.TeamSide, Agent.Main.Formation.FormationIndex).MissionShip, this.AttachedShip))
            //    return new TextObject("{=fOX1aVDv}Capture the ship");
            //return this._overridenDescriptionForActiveEnemyShipControllerMachine != (TextObject)null ? this._overridenDescriptionForActiveEnemyShipControllerMachine : new TextObject("{=lS53LgyN}You need to be boarded to capture the ship");

            _attackedShip ??= AccessTools.TypeByName("ShipControllerMachine").Property("AttachedShip");
            var attachedShip = _attackedShip.GetValue(__instance);
            _battleSide ??= AccessTools.TypeByName("MissionShip").Property("BattleSide");
            var battleSide = (BattleSideEnum)_battleSide.GetValue(attachedShip);
            if (battleSide == Mission.Current.PlayerTeam.Side)
            {
                return true;
            }
            _isAttachedShipVacant ??= AccessTools.TypeByName("ShipControllerMachine").Method("IsAttachedShipVacant");
            var isAttachedShipVacant = (bool)_isAttachedShipVacant.Invoke(__instance, null);
            if (!isAttachedShipVacant)
            {
                return true;
            }
            // The only case to handle is when Agent.Main.Formation is null
            if (Agent.Main.Formation == null)
            {
                __result = ____overridenDescriptionForActiveEnemyShipControllerMachine != null ? ____overridenDescriptionForActiveEnemyShipControllerMachine : GameTexts.FindText("RTSCamera_ship_need_to_be_connected");
                return false;
            }
            return true;
        }
    }
}
