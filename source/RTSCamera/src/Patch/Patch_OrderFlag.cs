using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

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

        public static bool Prefix_GetCollidedEntity(OrderFlag __instance, ref GameEntity __result, MissionScreen ____missionScreen, Mission ____mission)
        {
            // use Input.MousePositionRanged if mouse is visible. In official code the condition is Mission.Current.GetMissionBehavior<BattleDeploymentHandler>() != null
            Vec2 screenPoint = ____missionScreen.MouseVisible ? Input.MousePositionRanged : new Vec2(0.5f, 0.5f);
            ____missionScreen.ScreenPointToWorldRay(screenPoint, out var rayBegin, out var rayEnd);
            ____mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float _, out GameEntity collidedEntity, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora);
            while (collidedEntity != null && !collidedEntity.GetScriptComponents().Any((ScriptComponentBehavior sc) => sc is IOrderable orderable && orderable.GetOrder(Mission.Current.PlayerTeam.Side) != OrderType.None))
            {
                collidedEntity = collidedEntity.Parent;
            }
            __result = collidedEntity;
            return false;
        }

        //public static GameEntity GetCollidedEntity(OrderFlag __instance)
        //{
        //    var missionScreen = Utility.GetMissionScreen();
        //    var mission = Mission.Current;
        //    // use Input.MousePositionRanged if mouse is visible. In official code the condition is Mission.Current.GetMissionBehavior<BattleDeploymentHandler>() != null
        //    Vec2 screenPoint = missionScreen.MouseVisible ? Input.MousePositionRanged : new Vec2(0.5f, 0.5f);
        //    missionScreen.ScreenPointToWorldRay(screenPoint, out var rayBegin, out var rayEnd);
        //    mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out float _, out GameEntity collidedEntity, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora);
        //    while (collidedEntity != null && !collidedEntity.GetScriptComponents().Any((ScriptComponentBehavior sc) => sc is IOrderable orderable && orderable.GetOrder(Mission.Current.PlayerTeam.Side) != OrderType.None))
        //    {
        //        collidedEntity = collidedEntity.Parent;
        //    }
        //    return collidedEntity;
        //}
    }
}
