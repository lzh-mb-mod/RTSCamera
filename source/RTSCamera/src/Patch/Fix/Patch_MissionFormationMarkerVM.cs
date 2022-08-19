using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace RTSCamera.src.Patch.Fix
{
    public class Patch_MissionFormationMarkerVM
    {
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
