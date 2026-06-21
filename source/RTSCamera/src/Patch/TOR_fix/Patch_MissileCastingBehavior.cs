using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.TOR_fix
{
    public class Patch_MissileCastingBehavior
    {
        private static FieldInfo _currentTargetField;
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;


                var method = AccessTools.Method(AccessTools.TypeByName("MissileCastingBehavior"), "UpdateTarget");
                if (method != null)
                {
                    harmony.Patch(
                        method,
                        prefix: new HarmonyMethod(
                            typeof(Patch_MissileCastingBehavior).GetMethod(nameof(Prefix_UpdateTarget),
                                BindingFlags.Static | BindingFlags.Public)));
                }

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

        public static bool Prefix_UpdateTarget(Object __instance, Object target, ref Object __result)
        {
            _currentTargetField ??= AccessTools.Field("TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior.AbstractAgentCastingBehavior:CurrentTarget");
            var currentTarget = (Threat)_currentTargetField?.GetValue(__instance) ?? null;
            if (currentTarget == null)
                return true;

            if (currentTarget.Formation == null)
            {
                __result = target;
                return false;
            }

            return true;
        }
    }
}
