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
                if (!string.IsNullOrEmpty(target.DistanceText))
                {
                    target.DistanceText = ((int)target.Distance).ToString();
                }
            }
        }
    }
}
