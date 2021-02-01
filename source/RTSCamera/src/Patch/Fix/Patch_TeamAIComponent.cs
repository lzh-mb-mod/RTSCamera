using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    public class Patch_TeamAIComponent
    {
        public static bool TickOccasionally_Prefix(TacticComponent ____currentTactic)
        {
            if (____currentTactic == null)
                return false;

            return true;
        }
    }
}
