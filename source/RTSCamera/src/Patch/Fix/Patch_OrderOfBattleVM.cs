using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;

namespace RTSCamera.Patch.Fix
{
    public class Patch_OrderOfBattleVM
    {

        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                //Harmony.Patch(
                //    typeof(OrderOfBattleVM).GetMethod("OnCommanderAssignmentRequested",
                //        BindingFlags.Instance | BindingFlags.NonPublic),
                //    prefix: new HarmonyMethod(typeof(Patch_OrderOfBattleVM).GetMethod(
                //        nameof(Prefix_OnCommanderAssignmentRequested), BindingFlags.Static | BindingFlags.Public)));

                // Fix crash when there's not available hero.
                harmony.Patch(
                    typeof(OrderOfBattleVM).GetMethod("SelectHeroItem",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderOfBattleVM).GetMethod(
                        nameof(Prefix_SelectHeroItem), BindingFlags.Static | BindingFlags.Public)));

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

        public static bool Prefix_SelectHeroItem(OrderOfBattleHeroItemVM heroItem)
        {
            if (heroItem == null)
                return false;
            return true;
        }

        // TODO: Resolve this legacy patch
        //public static bool Prefix_OnCommanderAssignmentRequested(OrderOfBattleVM __instance, OrderOfBattleHeroItemVM emptyCaptainSlotItem,
        //    ref int ____selectedFormationIndex, List<OrderOfBattleFormationItemVM> ____allFormations, Mission ____mission)
        //{
        //    if (__instance.IsPlayerGeneral)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        ____selectedFormationIndex = emptyCaptainSlotItem.FormationIndex;
        //        OrderOfBattleHeroItemVM heroItem = __instance.Commanders.FirstOrDefault(c => c.Agent == Agent.Main);
        //        if (heroItem == null)
        //            return false;
        //        int num = emptyCaptainSlotItem.FormationIndex == heroItem.FormationIndex ? 1 : 0;
        //        if (heroItem.IsLeadingAFormation)
        //            heroItem.CurrentAssignedFormationItem.UnassignCommander();
        //        if (num != 0)
        //            return false;
        //        typeof(OrderOfBattleVM).GetMethod("OnCommanderAssigned", BindingFlags.Instance | BindingFlags.NonPublic)
        //            ?.Invoke(__instance, new object[] { heroItem });
        //        for (int index = 0; index < ____allFormations.Count; ++index)
        //        {
        //            if (index != ____selectedFormationIndex && !____allFormations[index].Commander.IsPreassigned)
        //                ____allFormations[index].Commander.CurrentAssignedFormationItem?.UnassignCommander();
        //        }
        //        ____mission.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>().OnPlayerChoiceMade(____selectedFormationIndex, false);
        //        foreach (OrderOfBattleHeroItemVM commander in __instance.Commanders)
        //            commander.IsPlayerAssignedToAFormation = true;
        //        foreach (OrderOfBattleFormationItemVM allFormation in ____allFormations)
        //            allFormation.Commander.IsPlayerAssignedToAFormation = true;
        //        return false;
        //    }
        //}
    }
}
