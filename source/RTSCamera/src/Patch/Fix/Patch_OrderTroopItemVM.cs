using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace RTSCamera.Patch.Fix
{
    public class Patch_OrderTroopItemVM
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
                    typeof(OrderTroopItemVM).GetMethod("FormationOnOnUnitCountChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance),
                    prefix: new HarmonyMethod(
                        typeof(Patch_OrderTroopItemVM).GetMethod(nameof(Prefix_FormationOnOnUnitCountChanged), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)));
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

        public static bool Prefix_FormationOnOnUnitCountChanged(OrderTroopItemVM __instance, Formation formation)
        {
            __instance.CurrentMemberCount = formation.IsPlayerTroopInFormation && Agent.Main != null && !Agent.Main.IsAIControlled ? formation.CountOfUnits - 1 : formation.CountOfUnits;
            __instance.UpdateVisuals();
            return false;
        }
    }
}
