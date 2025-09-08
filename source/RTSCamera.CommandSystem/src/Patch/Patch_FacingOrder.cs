using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.QuerySystem;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_FacingOrder
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
                    typeof(FacingOrder).GetMethod("GetDirectionAux",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(
                        typeof(Patch_FacingOrder).GetMethod(nameof(Prefix_GetDirectionAux),
                            BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        // show formation marker below troop card
        public static bool Prefix_GetDirectionAux(Formation f, Agent targetAgent, ref Vec2 __result, FacingOrder.FacingOrderEnum ___OrderEnum)
        {
            if (f.IsAIControlled)
                return true;
            var targetFormation = Patch_OrderController.GetFacingEnemyTargetFormation(f);
            if (targetFormation == null)
                return true;
            if (f.PhysicalClass.IsMounted() && targetAgent != null)
            {
                return true;
            }
            if (___OrderEnum == FacingOrder.FacingOrderEnum.LookAtDirection)
                return true;
            if (f.Arrangement is CircularFormation || f.Arrangement is SquareFormation)
                return true;
            __result = GetDirectionFacingToEnemyFormation(f, targetFormation);
            return false;
        }

        public static Vec2 GetDirectionFacingToEnemyFormation(Formation f, Formation target)
        {
            Vec2 currentPosition = f.CurrentPosition;
            Vec2 averageEnemyPosition = CommandQuerySystem.GetQueryForFormation(f).WeightedAverageEnemyPosition;
            if (!averageEnemyPosition.IsValid)
            {
                return f.Direction;
            }
            Vec2 vec2 = (averageEnemyPosition - currentPosition).Normalized();
            float length = (averageEnemyPosition - currentPosition).Length;
            int enemyUnitCount = target.CountOfUnits;
            int countOfUnits = f.CountOfUnits;
            Vec2 vector2 = f.Direction;
            bool flag = (double)length >= (double)countOfUnits * 0.20000000298023224;
            if (enemyUnitCount == 0 || countOfUnits == 0)
                flag = false;
            float num = !flag ? 1f : MBMath.ClampFloat((float)countOfUnits * 1f / (float)enemyUnitCount, 0.333333343f, 3f) * MBMath.ClampFloat(length / (float)countOfUnits, 0.333333343f, 3f);
            if (flag && (double)TaleWorlds.Library.MathF.Abs(vec2.AngleBetween(vector2)) > 0.17453292012214661 * (double)num)
                vector2 = vec2;
            return vector2;
        }
    }
}
