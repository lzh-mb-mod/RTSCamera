using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

namespace RTSCamera.Patch.Fix
{
     public class Patch_MissionGauntletFormationMarker
    {
        private static bool _patched;
        // use reflection to keep compatible with v1.3.11 and before.
        private static PropertyInfo _distanceText = typeof(MissionFormationMarkerTargetVM).GetProperty("DistanceText",
                    BindingFlags.Instance | BindingFlags.Public);
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(MissionGauntletFormationMarker).GetMethod("UpdateMarkerPositions",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(
                        typeof(Patch_MissionGauntletFormationMarker).GetMethod(nameof(Postfix_UpdateMarkerPositions),
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
        public static void Postfix_UpdateMarkerPositions(MissionGauntletFormationMarker __instance, MissionFormationMarkerVM ____dataSource)
        {
            // DistanceText is set to distance to player character.
            // We need to set it to distance to camera when in spectator mode.
            if (!RTSCameraLogic.Instance?.SwitchFreeCameraLogic.IsSpectatorCamera ?? true)
                return;
            for (int index = 0; index < ____dataSource.Targets.Count; ++index)
            {
                MissionFormationMarkerTargetVM target = ____dataSource.Targets[index];
                if (_distanceText == null)
                {
                    return;
                }
                var distanceText = _distanceText.GetValue(target) as string;
                if (!string.IsNullOrEmpty(distanceText))
                {
                    _distanceText.SetValue(target, ((int)target.Distance).ToString());
                }
            }
        }
    }
}
