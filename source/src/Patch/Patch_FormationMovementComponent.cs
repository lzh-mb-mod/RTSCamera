using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(FormationMovementComponent), "GetFormationFrame")]
    public class Patch_FormationMovementComponent
    {
        private static readonly MethodInfo IsUnitDetached =
            typeof(Formation).GetMethod("IsUnitDetached", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo GetMovementSpeedRestriction =
            typeof(ArrangementOrder).GetMethod("GetMovementSpeedRestriction",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo arrangement =
            typeof(Formation).GetProperty("arragement", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool GetFormationFrame_Prefix(ref bool __result, Agent ___Agent, ref FormationCohesionComponent ____cohesionComponent,
            ref WorldPosition formationPosition,
            ref Vec2 formationDirection,
            ref float speedLimit,
            ref bool isSettingDestinationSpeed,
            ref bool limitIsMultiplier)
        {
            var formation = ___Agent.Formation;
            if (!___Agent.IsMount && formation != null && !(bool)IsUnitDetached.Invoke(formation, new object[] { ___Agent }))
            {
                if (formation.MovementOrder.OrderType == OrderType.ChargeWithTarget)
                {
                    isSettingDestinationSpeed = false;
                    formationPosition = formation.GetOrderPositionOfUnit(___Agent);
                    formationDirection = formation.GetDirectionOfUnit(___Agent);

                    limitIsMultiplier = true;
                    speedLimit = ____cohesionComponent != null && FormationCohesionComponent.FormationSpeedAdjustmentEnabled ? ____cohesionComponent.GetDesiredSpeedInFormation() : -1f;
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
