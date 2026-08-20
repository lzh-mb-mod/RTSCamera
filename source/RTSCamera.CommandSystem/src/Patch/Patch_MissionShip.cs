using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionShip
    {
        private static PropertyInfo _beingAbandoned;
        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                var missionShipType = AccessTools.TypeByName("NavalDLC.Missions.Objects.MissionShip");
                if (missionShipType == null)
                    return true;

                harmony.Patch(
                    AccessTools.Method(missionShipType, "OnUnitAttached"),
                    prefix: new HarmonyMethod(typeof(Patch_MissionShip).GetMethod(
                        nameof(Prefix_OnUnitAttached), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }

            return true;
        }

        public static bool Prefix_OnUnitAttached(object __instance)
        {
            if (!CommandSystemConfig.Get().PreventNavalRaidMoveOrderReset ||
                Mission.Current?.IsNavalRaidBattle != true)
                return true;

            _beingAbandoned ??= AccessTools.Property(__instance.GetType(), "BeingAbandoned");
            return !(bool)_beingAbandoned.GetValue(__instance);
        }
    }
}
