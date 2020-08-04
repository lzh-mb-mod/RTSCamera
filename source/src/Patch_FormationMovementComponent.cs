using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    //[HarmonyLib.HarmonyPatch(typeof(FormationMovementComponent), "GetFormationFrame")]
    public class Patch_FormationMovementComponent
    {
        private static readonly MethodInfo IsUnitDetached =
            typeof(Formation).GetMethod("IsUnitDetached", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void GetFormationFrame_Postfix(ref bool __result, Agent ___Agent)
        {
            var formation = ___Agent.Formation;
            if (!___Agent.IsMount && formation != null && !(bool)IsUnitDetached.Invoke(formation, new object[]{___Agent}))
            {
                if (formation.MovementOrder.OrderType == OrderType.ChargeWithTarget)
                    __result = true;
            }
        }
    }
}
