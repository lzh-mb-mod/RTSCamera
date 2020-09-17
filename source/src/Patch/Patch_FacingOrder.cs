namespace RTSCamera.Patch
{
    //[HarmonyLib.HarmonyPatch(typeof(FacingOrder), "GetDirection")]
    public class Patch_FacingOrder
    {
        //public static bool GetDirection_Prefix(FacingOrder __instance, ref Vec2 __result, Formation f, Agent targetAgent)
        //{
        //    if (targetAgent == null && f.MovementOrder.OrderType == OrderType.ChargeWithTarget && f.MovementOrder.TargetFormation != null && __instance.OrderType == OrderType.LookAtEnemy)
        //    {
        //        var targetPosition = f.MovementOrder.TargetFormation.QuerySystem.MedianPosition.AsVec2;
        //        var newDirection = targetPosition - f.CurrentPosition;
        //        var distance = newDirection.Normalize();
        //        var oldDirection = f.Direction;
        //        int enemyUnitCount = f.QuerySystem.Team.EnemyUnitCount;
        //        int myUnitCount = f.CountOfUnits;
        //        float width = (f.LeftAttachmentPoint.AsVec2 - f.RightAttachmentPoint.AsVec2).Length;
        //        float depth = (f.FrontAttachmentPoint.AsVec2 - f.RearAttachmentPoint.AsVec2).Length;
        //        bool updateDirection = distance > width * 0.3 && distance > depth * 0.3;
        //        if (enemyUnitCount == 0 || myUnitCount == 0)
        //            updateDirection = false;
        //        float threshold = !updateDirection ? 1f : MBMath.ClampFloat((float)myUnitCount / enemyUnitCount, 0.3333f, 3f) * MBMath.ClampFloat(myUnitCount / distance, 0.3333f, 3f);
        //        if (updateDirection && (double)Math.Abs(oldDirection.AngleBetween(newDirection)) > Math.PI / 18 * threshold)
        //            oldDirection = newDirection;
        //        __result = oldDirection;
        //        return false;
        //    }

        //    return true;
        //}
    }
}
