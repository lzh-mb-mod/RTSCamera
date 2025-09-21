using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
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
                    prefix: new HarmonyMethod(
                        typeof(Patch_MissionGauntletFormationMarker).GetMethod(nameof(Prefix_UpdateMarkerPositions),
                            BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }
        public static bool Prefix_UpdateMarkerPositions(MissionGauntletFormationMarker __instance, bool isFirstFrame, MissionFormationMarkerVM ____dataSource, Vec3 ____heightOffset)
        {
            Agent main = Agent.Main;
            // run original method if main agent is controlled by player
            if (main != null && main.IsPlayerControlled)
                return true;
            for (int index = 0; index < ____dataSource.Targets.Count; ++index)
            {
                MissionFormationMarkerTargetVM target = ____dataSource.Targets[index];
                float screenX = 0.0f;
                float screenY = 0.0f;
                float w = 0.0f;
                WorldPosition cachedMedianPosition = target.Formation.CachedMedianPosition;
                cachedMedianPosition.SetVec2(target.Formation.CachedAveragePosition);
                if (cachedMedianPosition.IsValid)
                {
                    double screen = (double)MBWindowManager.WorldToScreen(__instance.MissionScreen.CombatCamera, cachedMedianPosition.GetGroundVec3() + ____heightOffset, ref screenX, ref screenY, ref w);
                    if (!TaleWorlds.Library.MathF.IsValidValue(w) || !TaleWorlds.Library.MathF.IsValidValue(screenX) || !TaleWorlds.Library.MathF.IsValidValue(screenY))
                    {
                        screenX = -10000f;
                        screenY = -10000f;
                        w = -1f;
                    }
                    target.WSign = (double)w < 0.0 ? -1 : 1;
                }
                if (!target.IsTargetingAFormation && (!cachedMedianPosition.IsValid || !TaleWorlds.Library.MathF.IsValidValue(w) || (double)w < 0.0 || !TaleWorlds.Library.MathF.IsValidValue(screenX) || !TaleWorlds.Library.MathF.IsValidValue(screenY)))
                {
                    screenX = -10000f;
                    screenY = -10000f;
                    w = 0.0f;
                }
                target.ScreenPosition = !isFirstFrame ? Vec2.Lerp(target.ScreenPosition, new Vec2(screenX, screenY), 0.9f) : new Vec2(screenX, screenY);
                if (____dataSource.IsDistanceRelevant)
                {
                    MissionFormationMarkerTargetVM formationMarkerTargetVm = target;
                    Vec3 position;
                    double num;
                    // Here is the only change: set distance to combat camera when main agent is not controlled by player
                    if (main == null || !main.IsActive() || !main.IsPlayerControlled)
                    {
                        position = __instance.MissionScreen.CombatCamera.Position;
                        num = (double)position.Distance(cachedMedianPosition.GetGroundVec3());
                    }
                    else
                    {
                        position = Agent.Main.Position;
                        num = (double)position.Distance(cachedMedianPosition.GetGroundVec3());
                    }
                    formationMarkerTargetVm.Distance = (float)num;
                }
            }
            return false;
        }
    }
}
