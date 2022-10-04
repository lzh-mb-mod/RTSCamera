using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;

namespace RTSCamera.Patch
{
    public class Patch_DeploymentMissionController
    {
        private static readonly Harmony Harmony = new Harmony(RTSCameraSubModule.ModuleId + "_" + nameof(Patch_DeploymentMissionController));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(DeploymentMissionController).GetMethod("OnPlayerDeploymentFinishedAux",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_DeploymentMissionController).GetMethod(
                        nameof(Postfix_OnPlayerDeploymentFinishedAux), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static void Postfix_OnPlayerDeploymentFinishedAux()
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