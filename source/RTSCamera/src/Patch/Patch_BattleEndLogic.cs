using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CampaignGame.Behavior;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_BattleEndLogic
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
                    typeof(BattleEndLogic).GetMethod(nameof(BattleEndLogic.TryExit),
                        BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(Patch_BattleEndLogic).GetMethod(
                        nameof(Prefix_TryExit), BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }
        }
        public static bool Prefix_TryExit(BattleEndLogic __instance, ref BattleEndLogic.ExitResult __result, bool ____isEnemySideRetreating)
        {
            if (!CommandBattleBehavior.CommandMode)
            {
                return true;
            }

            if (!__instance.Mission.MissionEnded && !____isEnemySideRetreating)
            {
                __result = Mission.Current.IsSiegeBattle && __instance.Mission.PlayerTeam.IsDefender ? BattleEndLogic.ExitResult.SurrenderSiege : BattleEndLogic.ExitResult.NeedsPlayerConfirmation;
                return false;
            }
            __instance.Mission.EndMission();
            __result = BattleEndLogic.ExitResult.True;
            return false;
        }
    }

}
