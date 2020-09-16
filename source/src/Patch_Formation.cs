using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RTSCamera.QuerySystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    //[HarmonyLib.HarmonyPatch(typeof(Formation), "LeaveDetachment")]
    public class Patch_Formation
    {
        private static readonly MethodInfo GetOrderPositionOfUnitAux =
            typeof(Formation).GetMethod("GetOrderPositionOfUnitAux", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool LeaveDetachment_Prefix(
            Formation __instance,
            List<IDetachment> ____detachments,
            IDetachment detachment)
        {
            BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.NonPublic;

            foreach (Agent agent in detachment.Agents.Where<Agent>((Func<Agent, bool>)(a => a.Formation == __instance && a.IsAIControlled)).ToList<Agent>())
            {
                detachment.RemoveAgent(agent);
                typeof(Formation).GetMethod("AttachUnit", bindingAttr).Invoke(__instance, new object[] { agent });
            }

            ____detachments.Remove(detachment);
            var detachmentManager = (DetachmentManager)typeof(Team).GetProperty("DetachmentManager", bindingAttr)
                ?.GetValue(__instance.Team);
            typeof(DetachmentManager).GetMethod("OnFormationLeaveDetachment", bindingAttr).Invoke(detachmentManager, new object[2]
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
                            .NearestOfAverageOfNearestPosition(unitPosition, 5)?.GetWorldPosition();
                        if (targetPosition != null)
                        {
                            __result = targetPosition.Value;
                            var distance = MathF.Clamp((__result.AsVec2 - unit.Position.AsVec2).Length, 0, 10);
                            __result.SetVec2(unit.GetMovementDirection().AsVec2 * distance + __result.AsVec2);
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
