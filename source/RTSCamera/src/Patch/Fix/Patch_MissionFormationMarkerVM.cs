using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Patch.Fix;
using SandBox.Missions.MissionLogics.Arena;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using static TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker.MissionFormationMarkerTargetVM;

namespace RTSCamera.src.Patch.Fix
{
    public class Patch_MissionFormationMarkerVM
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
                    typeof(MissionFormationMarkerVM).GetMethod("RefreshFormationPositions",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionFormationMarkerVM).GetMethod(
                        nameof(RefreshFormationPositions_Prefix),
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

        // original method:
        //private void RefreshFormationPositions()
        //{
        //    for (int i = 0; i < Targets.Count; i++)
        //    {
        //        MissionFormationMarkerTargetVM missionFormationMarkerTargetVM = Targets[i];
        //        float screenX = 0f;
        //        float screenY = 0f;
        //        float w = 0f;
        //        WorldPosition medianPosition = missionFormationMarkerTargetVM.Formation.QuerySystem.MedianPosition;
        //        medianPosition.SetVec2(missionFormationMarkerTargetVM.Formation.QuerySystem.AveragePosition);
        //        if (medianPosition.IsValid)
        //        {
        //            MBWindowManager.WorldToScreen(_missionCamera, medianPosition.GetGroundVec3() + _heightOffset, ref screenX, ref screenY, ref w);
        //            missionFormationMarkerTargetVM.IsInsideScreenBoundaries = !(screenX > Screen.RealScreenResolutionWidth) && !(screenY > Screen.RealScreenResolutionHeight) && !(screenX + 200f < 0f) && !(screenY + 100f < 0f);
        //            missionFormationMarkerTargetVM.WSign = ((!(w < 0f)) ? 1 : (-1));
        //        }

        //        if (!missionFormationMarkerTargetVM.IsTargetingAFormation && (!medianPosition.IsValid || w < 0f || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY)))
        //        {
        //            screenX = -10000f;
        //            screenY = -10000f;
        //            w = 0f;
        //        }

        //        if (_prevIsEnabled && IsEnabled)
        //        {
        //            missionFormationMarkerTargetVM.ScreenPosition = Vec2.Lerp(missionFormationMarkerTargetVM.ScreenPosition, new Vec2(screenX, screenY), 0.9f);
        //        }
        //        else
        //        {
        //            missionFormationMarkerTargetVM.ScreenPosition = new Vec2(screenX, screenY);
        //        }

        //        Agent main = Agent.Main;
        //        missionFormationMarkerTargetVM.Distance = ((main != null && main.IsActive()) ? Agent.Main.Position.Distance(medianPosition.GetGroundVec3()) : w);
        //    }
        //}

        public static bool RefreshFormationPositions_Prefix(MBBindingList<MissionFormationMarkerTargetVM> ____targets, Camera ____missionCamera, Vec3 ____heightOffset, bool ____prevIsEnabled, bool ____isEnabled)
        {
            Agent main = Agent.Main;
            // run original method if main agent is controlled by player
            if (main != null && main.IsPlayerControlled)
                return true;

            for (int i = 0; i < ____targets.Count; i++)
            {
                MissionFormationMarkerTargetVM missionFormationMarkerTargetVM = ____targets[i];
                // update team type, because team may be switched.
                missionFormationMarkerTargetVM.TeamType =
                    missionFormationMarkerTargetVM.Formation.Team.IsPlayerTeam ?
                        (int)TeamTypes.PlayerTeam : 
                        (missionFormationMarkerTargetVM.Formation.Team.IsPlayerAlly ? 
                            (int)TeamTypes.PlayerAllyTeam :
                            (int)TeamTypes.EnemyTeam);
                float screenX = 0f;
                float screenY = 0f;
                float w = 0f;
                WorldPosition medianPosition = missionFormationMarkerTargetVM.Formation.QuerySystem.MedianPosition;
                medianPosition.SetVec2(missionFormationMarkerTargetVM.Formation.QuerySystem.AveragePosition);
                if (medianPosition.IsValid)
                {
                    MBWindowManager.WorldToScreen(____missionCamera, medianPosition.GetGroundVec3() + ____heightOffset, ref screenX, ref screenY, ref w);
                    missionFormationMarkerTargetVM.IsInsideScreenBoundaries = !(screenX > Screen.RealScreenResolutionWidth) && !(screenY > Screen.RealScreenResolutionHeight) && !(screenX + 200f < 0f) && !(screenY + 100f < 0f);
                    missionFormationMarkerTargetVM.WSign = ((!(w < 0f)) ? 1 : (-1));
                }
                if (!missionFormationMarkerTargetVM.IsTargetingAFormation && (!medianPosition.IsValid || w < 0f || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY)))
                {
                    screenX = -10000f;
                    screenY = -10000f;
                    w = 0f;
                }
                if (____prevIsEnabled && ____isEnabled)
                {
                    missionFormationMarkerTargetVM.ScreenPosition = Vec2.Lerp(missionFormationMarkerTargetVM.ScreenPosition, new Vec2(screenX, screenY), 0.9f);
                }
                else
                {
                    missionFormationMarkerTargetVM.ScreenPosition = new Vec2(screenX, screenY);
                }
                // Here is the only change: set distance to w when main agent is not controlled by player
                missionFormationMarkerTargetVM.Distance = w;
            }

            return false;
        }
    }
}
