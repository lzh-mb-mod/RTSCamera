using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    public class Patch_DeploymentMissionController
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
                   typeof(DeploymentMissionController).GetMethod("FinishDeployment",
                       BindingFlags.Instance | BindingFlags.Public),
                   postfix: new HarmonyMethod(typeof(Patch_DeploymentMissionController).GetMethod(
                       nameof(Postfix_FinishDeployment), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static void Postfix_FinishDeployment()
        {
            if (Mission.Current?.PlayerTeam != null && Mission.Current.PlayerTeam.IsValid)
            {
                var generalFormation = Mission.Current.PlayerTeam.GetFormation(FormationClass.General);
                if (generalFormation.AI.GetBehavior<BehaviorGeneral>() != null)
                {
                    TacticComponent.SetDefaultBehaviorWeights(generalFormation);
                    generalFormation.AI.SetBehaviorWeight<BehaviorGeneral>(1f);
                    generalFormation.SetControlledByAI(true);
                }
            }
        }
    }
}