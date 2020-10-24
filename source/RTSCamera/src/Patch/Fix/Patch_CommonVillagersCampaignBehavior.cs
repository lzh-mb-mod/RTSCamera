using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.Fix
{
    //[HarmonyLib.HarmonyPatch(typeof(CommonVillagersCampaignBehavior), "conversation_guard_start_on_condition")]
    public class Patch_CommonVillagersCampaignBehavior
    {
        public static bool CheckIfConversationAgentIsEscortingThePlayer_Prefix(ref bool __result)
        {
            if (Agent.Main == null || !Agent.Main.IsActive() || ((CharacterObject)Agent.Main.Character)?.HeroObject?.CurrentSettlement == null)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}
