using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.TOR_fix
{
    // Fix crash in The Old Realms
    public class Patch_CareerHelper
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;


                var method = AccessTools.Method(AccessTools.TypeByName("CareerHelper"), "ApplyCareerAbilityCharge");
                if (method != null)
                {
                    harmony.Patch(
                        AccessTools.Method(AccessTools.TypeByName("CareerHelper"), "ApplyCareerAbilityCharge"),
                        prefix: new HarmonyMethod(
                            typeof(Patch_CareerHelper).GetMethod(nameof(Prefix_ApplyCareerAbilityCharge),
                                BindingFlags.Static | BindingFlags.Public)));
                }

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
        public static bool Prefix_ApplyCareerAbilityCharge(Object __instance)
        {
            if (Agent.Main == null)
                return true;
            if (Game.Current.GameType is Campaign && Agent.Main.Character is CharacterObject && (Agent.Main.Character as CharacterObject)?.HeroObject == Hero.MainHero && Hero.MainHero != null)
                return true;
            return false;
        }

        public static void OnMainAgentChanged()
        {
            var type = AccessTools.TypeByName("CustomCrosshairMissionBehavior");
            var behavior = Mission.Current.MissionBehaviors.FirstOrDefault(b => b.GetType() == type);
            if (behavior == null)
                return;

            AccessTools.Method(type, "OnMissionScreenFinalize")?.Invoke(behavior, new object[] { });
            AccessTools.Field(type, "_currentCrosshair")?.SetValue(behavior, null);
        }
    }
}
