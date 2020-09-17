using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RTSCamera.QuerySystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Patch_Formation
    {
        private static readonly MethodInfo AttachUnit =
            typeof(Formation).GetMethod("AttachUnit", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly PropertyInfo DetachmentManager =
            typeof(Team).GetProperty("DetachmentManager", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo OnFormationLeaveDetachment =
            typeof(DetachmentManager).GetMethod("OnFormationLeaveDetachment",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo GetOrderPositionOfUnitAux =
            typeof(Formation).GetMethod("GetOrderPositionOfUnitAux", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool LeaveDetachment_Prefix(
            Formation __instance,
            List<IDetachment> ____detachments,
            IDetachment detachment)
        {

            foreach (Agent agent in detachment.Agents.Where<Agent>((Func<Agent, bool>)(a => a.Formation == __instance && a.IsAIControlled)).ToList<Agent>())
            {
                detachment.RemoveAgent(agent);
                AttachUnit?.Invoke(__instance, new object[] { agent });
            }

            ____detachments.Remove(detachment);
            var detachmentManager = (DetachmentManager) DetachmentManager?.GetValue(__instance.Team);
            OnFormationLeaveDetachment?.Invoke(detachmentManager, new object[2]
            {
                __instance,
                detachment
            });
            return false;
        }

        public static bool GetOrderPositionOfUnit_Prefix(Formation __instance, Agent unit, List<Agent> ___detachedUnits, ref WorldPosition __result)
        {
            if (!___detachedUnits.Contains(unit) && __instance.MovementOrder.OrderType == OrderType.ChargeWithTarget)
            {
                //__result = (WorldPosition) (GetOrderPositionOfUnitAux?.Invoke(__instance, new object[] {unit}) ??
                //                            new WorldPosition());
                var targetFormation = QueryDataStore.Get(__instance.TargetFormation);

                Vec2 unitPosition;
                if (QueryLibrary.IsCavalry(unit))
                {
                    if (QueryLibrary.IsRangedCavalry(unit))
                    {
                        unitPosition = unit.Position.AsVec2;
                        __result = targetFormation
                            .NearestAgent(unitPosition)?.GetWorldPosition() ?? new WorldPosition();
                    }
                    else
                    {
                        unitPosition = __instance.GetCurrentGlobalPositionOfUnit(unit, true) * 0.2f +
                                       unit.Position.AsVec2 * 0.8f;
                        var targetPosition = targetFormation
                            .NearestOfAverageOfNearestPosition(unitPosition, 10)?.GetWorldPosition();
                        if (targetPosition != null)
                        {
                            __result = targetPosition.Value;
                            var targetDirection = __result.AsVec2 - unit.Position.AsVec2;
                            var distance = targetDirection.Normalize();
                            var component = unit.GetComponent<RTSCameraAgentComponent>();
                            var moveDirection = component?.CurrentDirection ?? unit.GetMovementDirection().AsVec2;
                            if (distance < 3)
                            {
                                __result = unit.GetWorldPosition();
                                __result.SetVec2(moveDirection * 20 + __result.AsVec2);
                            }
                            else
                            {
                                if (distance < 20 && targetDirection.DotProduct(moveDirection) < 0)
                                {
                                    __result.SetVec2(-targetDirection * 50 + __result.AsVec2);
                                }
                                else
                                {
                                    unit.GetComponent<RTSCameraAgentComponent>()?.SetCurrentDirection(targetDirection);
                                    __result.SetVec2(targetDirection * 10 + __result.AsVec2);
                                }
                            }
                        }
                        else
                        {
                            __result = new WorldPosition();
                        }
                    }
                }
                else
                {
                    unitPosition = __instance.GetCurrentGlobalPositionOfUnit(unit, true) * 0.2f +
                                   unit.Position.AsVec2 * 0.8f;
                    __result = targetFormation
                        .NearestAgent(unitPosition)?.GetWorldPosition() ?? new WorldPosition();
                }

                return false;
            }

            return true;
        }
    }
}
