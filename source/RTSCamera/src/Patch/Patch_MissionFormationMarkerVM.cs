using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;

namespace RTSCamera.Patch
{
    public class Patch_MissionFormationMarkerVM
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
                    typeof(MissionFormationMarkerVM).GetMethod(nameof(MissionFormationMarkerVM.RefreshFormationMarkers),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionFormationMarkerVM).GetMethod(
                        nameof(Prefix_RefreshFormationMarkers), BindingFlags.Static | BindingFlags.Public)));
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

        public static bool Prefix_RefreshFormationMarkers(MissionFormationMarkerVM __instance,
            Mission ____mission,
            MissionFormationMarkerVM.FormationMarkerDistanceComparer ____comparer)
        {
            // The only change: add '&& !ShouldHideFormationMarker(f)' to hide player formation marker if contains player only.
            IEnumerable<Formation> formationList = ____mission.Teams.SelectMany(t => t.FormationsIncludingEmpty.Where(f => f.CountOfUnits > 0 && !ShouldHideFormationMarker(f))).ToList();
            foreach (Formation formation1 in formationList)
            {
                Formation formation = formation1;
                if (__instance.Targets.All(t => t.Formation != formation))
                {
                    MissionFormationMarkerTargetVM formationMarkerTargetVm = new MissionFormationMarkerTargetVM(formation);
                    __instance.Targets.Add(formationMarkerTargetVm);
                    formationMarkerTargetVm.IsEnabled = __instance.IsEnabled;
                    formationMarkerTargetVm.IsFormationTargetRelevant = __instance.IsFormationTargetRelevant;
                    formationMarkerTargetVm.ShowDistanceTexts = __instance.ShowDistanceTexts;
                }
            }
            if (formationList.Count() < __instance.Targets.Count)
            {
                foreach (MissionFormationMarkerTargetVM formationMarkerTargetVm in __instance.Targets.Where(t => !formationList.Contains(t.Formation)).ToList())
                {
                    __instance.Targets.Remove(formationMarkerTargetVm);
                }
            }
            __instance.Targets.Sort(____comparer);
            foreach (MissionFormationMarkerTargetVM target in (Collection<MissionFormationMarkerTargetVM>)__instance.Targets)
                target.Refresh();
            return false;
        }

        private static bool ShouldHideFormationMarker(Formation f)
        {
            if (RTSCameraLogic.Instance.SwitchFreeCameraLogic.IsSpectatorCamera)
                return false;
            var mainAgent = Agent.Main;
            if (mainAgent == null)
                return false;
            if (mainAgent.Formation != f)
            {
                return false;
            }

            // hide formation if contains player only.
            return f.CountOfUnits == 1;
        }
    }
}
