using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionGauntletNavalShipMarker
    {
        private static bool _patched;
        private static PropertyInfo _shipMarkers;
        private static PropertyInfo _distanceText;
        private static PropertyInfo _distance;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    AccessTools.Method("NavalDLC.GauntletUI.MissionViews.MissionGauntletNavalShipMarker:UpdateMarkerPositions"),
                    postfix: new HarmonyMethod(
                        typeof(Patch_MissionGauntletNavalShipMarker).GetMethod(nameof(Postfix_UpdateMarkerPositions),
                            BindingFlags.Static | BindingFlags.Public)));

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

        public static void Postfix_UpdateMarkerPositions(MissionBattleUIBaseView __instance, ViewModel ____dataSource)
        {
            // DistanceText is set to distance to player character.
            // We need to set it to distance to camera when in spectator mode.
            if (!RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? true)
                return;
            _shipMarkers ??= AccessTools.Property(____dataSource.GetType(), "ShipMarkers");
            _distanceText ??= AccessTools.Property("NavalDLC.ViewModelCollection.HUD.ShipMarker.NavalShipMarkerItemVM:DistanceText");
            _distance ??= AccessTools.Property("NavalDLC.ViewModelCollection.HUD.ShipMarker.NavalShipMarkerItemVM:Distance");
            var shipMarkers = (IMBBindingList)_shipMarkers.GetValue(____dataSource);
            for (int index = 0; index < shipMarkers.Count; ++index)
            {
                var shipMarker = shipMarkers[index];
                var distance = (float)_distance.GetValue(shipMarker);
                var distanceText = (string)_distanceText.GetValue(shipMarker);
                if (!string.IsNullOrEmpty(distanceText))
                {
                    _distanceText.SetValue(shipMarker, ((int)distance).ToString());
                }
            }
        }
    }
}
