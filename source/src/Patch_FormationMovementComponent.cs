using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
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
                    limitIsMultiplier = false;
                    formationPosition = formation.GetOrderPositionOfUnit(___Agent);
                    formationDirection = formation.GetDirectionOfUnit(___Agent);

                    float num1 = formation.QuerySystem.MovementSpeed;
                    WorldPosition orderPosition = formation.OrderPosition;
                    isSettingDestinationSpeed = true;
                    Vec3 position1 = ___Agent.Position;
                    float length1 = (position1.AsVec2 - formationPosition.AsVec2).Length;
                    float length2 = (formation.CurrentPosition - orderPosition.AsVec2).Length;
                    float num2 = length1 - length2;
                    float? nullable1 = new float?();
                    float? runRestriction;
                    float? walkRestriction;
                    var paramObjects = new object[] { null, null };
                    GetMovementSpeedRestriction?.Invoke(formation.ArrangementOrder, paramObjects);
                    (runRestriction, walkRestriction) = ((float?)paramObjects[0], (float?)paramObjects[1]);
                    float num3 = !walkRestriction.HasValue ? runRestriction ?? 1f : 1f;
                    if (walkRestriction.HasValue)
                    {
                        IFormationUnit neighbourUnitOfLeftSide =
                            ((IFormationArrangement)arrangement.GetValue(formation)).GetNeighbourUnitOfLeftSide(
                                ___Agent);
                        IFormationUnit neighbourUnitOfRightSide =
                            ((IFormationArrangement)arrangement.GetValue(formation)).GetNeighbourUnitOfRightSide(
                                ___Agent);
                        if (neighbourUnitOfLeftSide != null && neighbourUnitOfRightSide != null)
                        {
                            Vec3 position2 = ((Agent)neighbourUnitOfLeftSide).Position;
                            Vec2 asVec2 = (((Agent)neighbourUnitOfRightSide).Position - position2).AsVec2;
                            Vec2 vb = new Vec2(-asVec2.y, asVec2.x);
                            nullable1 = -Vec2.DotProduct((position1 - position2).AsVec2, vb);
                        }
                    }
                    int num4 = 0;
                    float num5;
                    if (num2 > num1 * 3.0 * num3)
                        num5 = -1f;
                    else if (num2 > num1 * (double)num3)
                    {
                        num5 = num1 * 1.5f;
                    }
                    else
                    {
                        if (nullable1.HasValue)
                        {
                            float num6 = num1 * 0.3f;
                            if (nullable1.GetValueOrDefault() > (double)num6)
                            {
                                num5 = num1 * 1.2f;
                                goto label_29;
                            }
                        }
                        num5 = num1;
                    }
                    label_29:
                    if (____cohesionComponent == null)
                        ____cohesionComponent = ___Agent.GetComponent<FormationCohesionComponent>();
                    if (FormationCohesionComponent.FormationSpeedAdjustmentEnabled && ____cohesionComponent.ShouldCatchUpWithFormation)
                    {
                        limitIsMultiplier = true;
                        speedLimit = ____cohesionComponent.GetDesiredSpeedInFormation();
                    }
                    else
                        speedLimit = num5;
                    if (num4 != 0)
                        limitIsMultiplier = false;
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}
