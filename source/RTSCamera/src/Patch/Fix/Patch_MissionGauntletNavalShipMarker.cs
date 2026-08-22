using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Linq.Expressions;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.Patch.Fix
{
    public class Patch_MissionGauntletNavalShipMarker
    {
        private static bool _patched;
        private static Func<object, IMBBindingList> _getShipMarkers;
        private static Func<object, float> _getDistance;
        private static Func<object, string> _getDistanceText;
        private static Action<object, string> _setDistanceText;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                var target = AccessTools.Method(
                    "NavalDLC.GauntletUI.MissionViews.MissionGauntletNavalShipMarker:UpdateMarkerPositions");
                var dataSourceField = AccessTools.Field(target.DeclaringType, "_dataSource");
                _getShipMarkers = CompilePropertyGetter<IMBBindingList>(
                    AccessTools.Property(dataSourceField.FieldType, "ShipMarkers"));
                var distanceText = AccessTools.Property(
                    "NavalDLC.ViewModelCollection.HUD.ShipMarker.NavalShipMarkerItemVM:DistanceText");
                _getDistance = CompilePropertyGetter<float>(AccessTools.Property(
                    "NavalDLC.ViewModelCollection.HUD.ShipMarker.NavalShipMarkerItemVM:Distance"));
                _getDistanceText = CompilePropertyGetter<string>(distanceText);
                _setDistanceText = CompilePropertySetter<string>(distanceText);
                harmony.Patch(
                    target,
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
            var shipMarkers = _getShipMarkers(____dataSource);
            for (int index = 0; index < shipMarkers.Count; ++index)
            {
                var shipMarker = shipMarkers[index];
                var distance = _getDistance(shipMarker);
                var distanceText = _getDistanceText(shipMarker);
                if (!string.IsNullOrEmpty(distanceText))
                {
                    _setDistanceText(shipMarker, ((int)distance).ToString());
                }
            }
        }

        private static Func<object, T> CompilePropertyGetter<T>(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            return Expression.Lambda<Func<object, T>>(
                Expression.Convert(
                    Expression.Property(Expression.Convert(instance, property.DeclaringType), property),
                    typeof(T)),
                instance).Compile();
        }

        private static Action<object, T> CompilePropertySetter<T>(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(T), "value");
            return Expression.Lambda<Action<object, T>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(instance, property.DeclaringType), property),
                    Expression.Convert(value, property.PropertyType)),
                instance,
                value).Compile();
        }
    }
}
