using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.Logic;
using RTSCamera.Patch.Fix;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;

namespace RTSCamera.Patch
{
    public class Patch_OrderFlag
    {
        private static bool _patched;
        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // Use mouse position instead of screen center to detect orderable entity, if mouse is visible.
                harmony.Patch(
                    typeof(OrderFlag).GetMethod("GetCollidedEntity",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderFlag).GetMethod(
                        nameof(Prefix_GetCollidedEntity), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderFlag).GetMethod("GetFlagPosition",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderFlag).GetMethod(
                        nameof(Prefix_GetFlagPosition), BindingFlags.Static | BindingFlags.Public)));
                //if (RTSCameraSubModule.IsNavalInstalled)
                //{
                //    harmony.Patch(
                //        AccessTools.Method("NavalDLC.View.MissionViews.Order.NavalOrderFlag:GetFlagPosition"),
                //    prefix: new HarmonyMethod(typeof(Patch_OrderFlag).GetMethod(
                //        nameof(Prefix_GetFlagPosition), BindingFlags.Static | BindingFlags.Public)));

                //}
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


        //public static IEnumerable<CodeInstruction> Transpile_Tick(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        //{
        //    var codes = new List<CodeInstruction>(instructions);
        //    //FixFormationUnitspacing(codes);
        //    bool foundGetCollidedEntity = false;
        //    int index_GetCollidedEntity = -1;
        //    for(int i = 0; i < codes.Count; ++i)
        //    {
        //        if (!foundGetCollidedEntity)
        //        {
        //            if (codes[i].opcode == OpCodes.Call)
        //            {
        //                if ((codes[i].operand as MethodInfo).Name == "GetCollidedEntity")
        //                {
        //                    // IL_0005
        //                    foundGetCollidedEntity = true;
        //                    index_GetCollidedEntity = i;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    if (!foundGetCollidedEntity)
        //    {
        //        throw new Exception("GetCollidedEntity not found");
        //    }

        //    codes[index_GetCollidedEntity] = new CodeInstruction(OpCodes.Call, typeof(Patch_OrderFlag).GetMethod(nameof(GetCollidedEntity), BindingFlags.Static | BindingFlags.Public));


        //    return codes.AsEnumerable();
        //}

        public static bool Prefix_GetCollidedEntity(OrderFlag __instance, ref WeakGameEntity __result, ref Vec3 closestPoint, MissionScreen ____missionScreen, Mission ____mission)
        {
            // use Input.MousePositionRanged if mouse is visible. In official code the condition is Mission.Current.GetMissionBehavior<BattleDeploymentHandler>() != null
            Vec2 screenPoint = ____missionScreen.MouseVisible ? Input.MousePositionRanged : Patch_MissionGauntletSingleplayerOrderUIHandler.MousePositionRangedBeforeDragging ?? new Vec2(0.5f, 0.5f);
            ____missionScreen.ScreenPointToWorldRay(screenPoint, out var rayBegin, out var rayEnd);
            //Vec3 vec3 = (rayEnd - rayBegin).NormalizedCopy();
            //rayEnd = rayBegin + vec3 * 10000f;
            //rayBegin = Agent.Main.GetEyeGlobalPosition();
            WeakGameEntity collidedEntity;
            ____mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float _, out closestPoint, out collidedEntity, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora);
            while (collidedEntity.IsValid && !collidedEntity.GetScriptComponents().Any<ScriptComponentBehavior>((Func<ScriptComponentBehavior, bool>)(sc => sc is IOrderable orderable && orderable.GetOrder(Mission.Current.PlayerTeam.Side) != 0)))
                collidedEntity = collidedEntity.Parent;
            __result = collidedEntity;
            return false;
        }

        public static bool Prefix_GetFlagPosition(
            OrderFlag __instance,
            ref Vec3 __result,
            ref bool isOnValidGround,
            bool checkForTargetEntity,
            Vec3 targetCollisionPoint,
            MissionScreen ____missionScreen,
            Mission ____mission)
        {
            if (!__instance.IsVisible || checkForTargetEntity)
                return true;
            // use mouse position before dragging if dragging camera in free camera mode.
            if (!____missionScreen.MouseVisible && Patch_MissionGauntletSingleplayerOrderUIHandler.MousePositionRangedBeforeDragging.HasValue)
            {
                var screenPoint = Patch_MissionGauntletSingleplayerOrderUIHandler.MousePositionRangedBeforeDragging.Value;
                ____missionScreen.ScreenPointToWorldRay(screenPoint, out var rayBegin, out var rayEnd);

                ____mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float _, out Vec3 closestPoint, out var _, 0.3f, BodyFlags.CommonCollisionExcludeFlags | BodyFlags.BodyOwnerEntity | BodyFlags.BodyOwnerFlora);

                WorldPosition orderPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, closestPoint, false);
                isOnValidGround = Mission.Current.IsOrderPositionAvailable(in orderPosition, Mission.Current.PlayerTeam);

                __result = closestPoint;
                return false;
            }

            return true;
        }
    }
}
