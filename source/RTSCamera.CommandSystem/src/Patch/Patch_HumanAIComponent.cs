using RTSCamera.CommandSystem.Logic.Component;
using RTSCamera.CommandSystem.QuerySystem;
using RTSCamera.CommandSystem.Utilities;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(Patch_HumanAIComponent), "GetFormationFrame")]
    public class Patch_HumanAIComponent
    {
        private static readonly PropertyInfo IsDetachedFromFormation =
            typeof(Agent).GetProperty("IsDetachedFromFormation", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo GetMovementSpeedRestriction =
            typeof(ArrangementOrder).GetMethod("GetMovementSpeedRestriction",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo arrangement =
            typeof(Formation).GetProperty("arragement", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool GetFormationFrame_Prefix(ref bool __result, Agent ___Agent,
            ref HumanAIComponent __instance,
            ref WorldPosition formationPosition,
            ref Vec2 formationDirection,
            ref float speedLimit,
            ref bool isSettingDestinationSpeed,
            ref bool limitIsMultiplier)
        {
            var formation = ___Agent.Formation;
            if (!___Agent.IsMount && formation != null &&
                !(bool)IsDetachedFromFormation.GetValue(___Agent))
            {
                if (Utility.ShouldChargeToFormation(___Agent))
                {
                    isSettingDestinationSpeed = false;
                    var component = ___Agent.GetComponent<CommandSystemAgentComponent>();
                    if (component == null)
                        return true;
                    formationPosition = component.CurrentTargetPosition.Value;
                    formationDirection = formation.GetDirectionOfUnit(___Agent);

                    limitIsMultiplier = true;
                    speedLimit =
                        !___Agent.HasMount && __instance != null &&
                        HumanAIComponent.FormationSpeedAdjustmentEnabled
                            ? __instance.GetDesiredSpeedInFormation(true)
                            : -1f;
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
