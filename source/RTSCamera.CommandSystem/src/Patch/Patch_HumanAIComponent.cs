using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Logic;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_HumanAIComponent
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
                    typeof(HumanAIComponent).GetMethod(nameof(HumanAIComponent.GetDesiredSpeedInFormation),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(
                        typeof(Patch_HumanAIComponent).GetMethod(nameof(Prefix_GetDesiredSpeedInFormation),
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

        public static bool Prefix_GetDesiredSpeedInFormation(HumanAIComponent __instance, Agent ___Agent, ref float __result, bool isCharging)
        {
            if (Mission.Current.IsNavalBattle || isCharging || !CommandSystemConfig.Get().ShouldSyncFormationSpeed)
                return true;
            if (___Agent.Formation == null || ___Agent.Team == null || !___Agent.Team.IsPlayerTeam)
                return true;
            if (___Agent.Formation.Arrangement is ColumnFormation || !__instance.ShouldCatchUpWithFormation || isCharging || Mission.Current.IsMissionEnding)
                return true;
            if (!CommandQueueLogic.PendingOrders.TryGetValue(___Agent.Formation, out var pendingOrder))
                return true;

            if (!pendingOrder.ShouldAdjustFormationSpeed || pendingOrder.FormationSpeedLimits.Count <= 1 || !pendingOrder.FormationSpeedLimits.ContainsKey(___Agent.Formation))
                return true;

            Agent mountAgent = ___Agent.MountAgent;
            float num1 = mountAgent != null ? mountAgent.GetMaximumForwardUnlimitedSpeed() : ___Agent.GetMaximumForwardUnlimitedSpeed();
            bool flag = !isCharging;
            Vec3 vec3;
            if (isCharging)
            {
                FormationQuerySystem closestEnemyFormation = ___Agent.Formation.CachedClosestEnemyFormation;
                float num2 = float.MaxValue;
                float num3 = 4f * num1 * num1;
                if (closestEnemyFormation != null)
                {
                    num2 = ___Agent.Formation.CachedMedianPosition.AsVec2.DistanceSquared(closestEnemyFormation.Formation.CachedMedianPosition.AsVec2);
                    if ((double)num2 <= (double)num3)
                    {
                        WorldPosition cachedMedianPosition = ___Agent.Formation.CachedMedianPosition;
                        vec3 = cachedMedianPosition.GetNavMeshVec3MT();
                        ref Vec3 local = ref vec3;
                        cachedMedianPosition = closestEnemyFormation.Formation.CachedMedianPosition;
                        Vec3 navMeshVec3Mt = cachedMedianPosition.GetNavMeshVec3MT();
                        num2 = local.DistanceSquared(navMeshVec3Mt);
                    }
                }
                flag = (double)num2 > (double)num3;
            }
            if (flag)
            {
                Vec2 globalPositionOfUnit = ___Agent.Formation.GetCurrentGlobalPositionOfUnit(___Agent, true);
                vec3 = ___Agent.Position;
                Vec2 asVec2 = vec3.AsVec2;
                Vec2 v = globalPositionOfUnit - asVec2;
                float num4 = MathF.Clamp(-___Agent.GetMovementDirection().DotProduct(v), 0.0f, 100f);
                float num5 = ___Agent.MountAgent != null ? 4f : 2f;
                // The only change: limit locked formations.
                //float num6 = (isCharging ? ___Agent.Formation.CachedFormationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents : ___Agent.Formation.CachedMovementSpeed) / num1;
                float num6 = (isCharging ? ___Agent.Formation.CachedFormationIntegrityData.AverageMaxUnlimitedSpeedExcludeFarAgents : MathF.Min(pendingOrder.FormationSpeedLimits[___Agent.Formation], ___Agent.Formation.CachedMovementSpeed)) / num1;
                __result = MathF.Clamp((float)(0.699999988079071 + 0.40000000596046448 * (((double)num1 - (double)num4 * (double)num5) / ((double)num1 + (double)num4 * (double)num5))) * num6, 0.1f, 1f);
                return false;
            }
            return true;
        }
    }
}
