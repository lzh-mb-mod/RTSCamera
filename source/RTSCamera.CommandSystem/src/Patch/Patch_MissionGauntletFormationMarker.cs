using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_MissionGauntletFormationMarker
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
                    typeof(MissionGauntletFormationMarker).GetMethod("RefreshTargetProperties",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_MissionGauntletFormationMarker).GetMethod(nameof(Prefix_RefreshTargetProperties),
                            BindingFlags.Static | BindingFlags.Public)));

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
        public static bool Prefix_RefreshTargetProperties(MissionGauntletFormationMarker __instance, MissionFormationMarkerVM ____dataSource,
            MBReadOnlyList<Formation> ____focusedFormationsCache)
        {
            if (!____dataSource.IsFormationTargetRelevant)
            {
                for (int index = 0; index < ____dataSource.Targets.Count; ++index)
                    ____dataSource.Targets[index].SetTargetedState(false, false);
            }
            else
            {
                List<Formation> targetedFormations = new List<Formation>();
                MBReadOnlyList<Formation> selectedFormations = Agent.Main?.Team.PlayerOrderController?.SelectedFormations;
                if (selectedFormations != null)
                {
                    for (int index = 0; index < selectedFormations.Count; ++index)
                    {
                        if (selectedFormations[index].TargetFormation != null)
                        {
                            MovementOrder movementOrder = selectedFormations[index].GetReadonlyMovementOrderReference();
                            // The only change: add movementOrder.OrderType == OrderType.ChargeWithTarget
                            if (movementOrder.OrderType == OrderType.Charge || movementOrder.OrderType == OrderType.Advance || movementOrder.OrderType == OrderType.ChargeWithTarget)
                                targetedFormations.Add(selectedFormations[index].TargetFormation);
                        }
                    }
                }
                for (int index = 0; index < ____dataSource.Targets.Count; ++index)
                {
                    MissionFormationMarkerTargetVM target = ____dataSource.Targets[index];
                    if (target.TeamType == 2)
                    {
                        bool isTargeted = targetedFormations.Contains(target.Formation);
                        bool isFocused = ____focusedFormationsCache?.Contains(target.Formation) ?? false;
                        target.SetTargetedState(isFocused, isTargeted);
                    }
                }
            }
            return false;
        }
    }
}
