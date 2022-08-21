using RTSCamera.Logic.SubLogic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

namespace RTSCamera.src.Patch.Fix
{
    public class Patch_MissionFormationMarkerVM
    {

        // original method:
        //private void RefreshFormationPositions()
        //{
        //    foreach (MissionFormationMarkerTargetVM target in (Collection<MissionFormationMarkerTargetVM>)this.Targets)
        //    {
        //        float screenX = 0.0f;
        //        float screenY = 0.0f;
        //        float w = 0.0f;
        //        WorldPosition medianPosition = target.Formation.QuerySystem.MedianPosition;
        //        medianPosition.SetVec2(target.Formation.QuerySystem.AveragePosition);
        //        double insideUsableArea = (double)MBWindowManager.WorldToScreenInsideUsableArea(this._missionCamera, medianPosition.Position + this._heightOffset, ref screenX, ref screenY, ref w);
        //        if ((double)w < 0.0 || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY))
        //        {
        //            screenX = -10000f;
        //            screenY = -10000f;
        //        }
        //        target.ScreenPosition = !this._prevIsEnabled || !this.IsEnabled ? new Vec2(screenX, screenY) : Vec2.Lerp(target.ScreenPosition, new Vec2(screenX, screenY), 0.9f);
        //        MissionFormationMarkerTargetVM formationMarkerTargetVm = target;
        //        Agent main = Agent.Main;
        //        double num = (main != null ? (main.IsActive() ? 1 : 0) : 0) != 0 ? (double)Agent.Main.Position.Distance(medianPosition.Position) : (double)w;
        //        formationMarkerTargetVm.Distance = (float)num;
        //    }
        //}

        public static bool RefreshFormationPositions_Prefix(MBBindingList<MissionFormationMarkerTargetVM> ____targets, Camera ____missionCamera, Vec3 ____heightOffset, bool ____prevIsEnabled, bool ____isEnabled)
        {
            Agent main = Agent.Main;
            // run original method if main agent is controlled by player
            if (main != null && main.IsPlayerControlled)
                return true;

            foreach (MissionFormationMarkerTargetVM target in ____targets)
            {
                float screenX = 0.0f;
                float screenY = 0.0f;
                float w = 0.0f;
                WorldPosition medianPosition = target.Formation.QuerySystem.MedianPosition;
                medianPosition.SetVec2(target.Formation.QuerySystem.AveragePosition);
                double insideUsableArea = MBWindowManager.WorldToScreenInsideUsableArea(____missionCamera, medianPosition.GetGroundVec3() + ____heightOffset, ref screenX, ref screenY, ref w);
                if (w < 0.0 || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY))
                {
                    screenX = -10000f;
                    screenY = -10000f;
                }
                target.ScreenPosition = !____prevIsEnabled || !____isEnabled ? new Vec2(screenX, screenY) : Vec2.Lerp(target.ScreenPosition, new Vec2(screenX, screenY), 0.9f);
                target.Distance = w;
            }

            return false;
        }
    }
}
