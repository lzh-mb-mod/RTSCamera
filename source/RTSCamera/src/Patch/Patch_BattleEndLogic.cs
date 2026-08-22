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
        public static bool Prefix_TryExit(BattleEndLogic __instance, ref BattleEndLogic.ExitResult __result)
        {
            if (!CommandBattleBehavior.CommandMode)
            {
                return true;
            }

            if (GameNetwork.IsClientOrReplay)
            {
                __result = BattleEndLogic.ExitResult.False;
                return false;
            }

            var mission = __instance.Mission;
            if (mission.MissionEnded || !__instance.PlayerVictory && !__instance.EnemyVictory)
            {
                // Unlike the original code, do not check whether MainAgent is active and close to an enemy,
                // because Command Mode has no player-controlled agent.
                if (!mission.MissionEnded && !__instance.IsEnemySideRetreating)
                {
                    __result = mission.IsSiegeBattle && mission.PlayerTeam.IsDefender
                        ? BattleEndLogic.ExitResult.SurrenderSiege
                        : BattleEndLogic.ExitResult.NeedsPlayerConfirmation;
                    return false;
                }

                mission.EndMission();
                __result = BattleEndLogic.ExitResult.True;
                return false;
            }

            __result = BattleEndLogic.ExitResult.False;
            return false;
        }
    }

}
