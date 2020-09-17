namespace RTSCamera.Patch
{

    //[HarmonyLib.HarmonyPatch(typeof(Patch_BehaviorCharge), "CalculateCurrentOrder")]
    public class Patch_BehaviorCharge
    {
        //public static bool CalculateCurrentOrder_Prefix(ref Formation ___formation, ref MovementOrder ____currentOrder,
        //    ref bool ___IsCurrentOrderChanged)
        //{
        //    if (___formation.TargetFormation != null)
        //    {
        //        ____currentOrder = (MovementOrder) typeof(MovementOrder)
        //            .GetMethod("MovementOrderChargeToTarget", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null,
        //                new object[]
        //                {
        //                    ___formation.TargetFormation
        //                });
        //        return false;
        //    }

        //    return true;
        //}
    }
}
