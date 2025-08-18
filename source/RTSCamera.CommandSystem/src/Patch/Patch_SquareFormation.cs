using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using System;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_SquareFormation
    {
        private static bool _patched;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // for resizable square formation
                harmony.Patch(
                    typeof(SquareFormation).GetMethod("GetLocalDirectionOfUnit",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_SquareFormation).GetMethod(
                        nameof(Prefix_GetLocalDirectionOfUnit), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }
            return true;
        }

        public static bool Prefix_GetLocalDirectionOfUnit(SquareFormation __instance, MBList2D<IFormationUnit> ____units2D, int fileIndex, int rankIndex, ref Vec2 __result)
        {
            if (!CommandSystemConfig.Get().SquareFormationCornerFix)
                return true;
            var unitCountOfOuterSide = UnitCountOfOuterSide(____units2D);
            var shiftedFileIndex = ShiftFileIndex(____units2D, fileIndex);
            var mod1 = (shiftedFileIndex - rankIndex) % (unitCountOfOuterSide - 1);
            var mod2 = (shiftedFileIndex + rankIndex) % (unitCountOfOuterSide - 1);
            var side = shiftedFileIndex / (unitCountOfOuterSide - 1);
            switch (side)
            {
                case 0:
                    {
                        if (mod1 == 0 || mod2 == 0)
                        {
                            __result = (Vec2.Forward + -Vec2.Side).Normalized();
                            return false;
                        }
                        __result = Vec2.Forward;
                        return false;
                    }
                case 1:
                    {
                        if (mod1 == 0 || mod2 == 0)
                        {
                            __result = (Vec2.Forward + Vec2.Side).Normalized();
                            return false;
                        }
                        __result = Vec2.Side;
                        return false;
                    }
                case 2:
                    {
                        if (mod1 == 0 || mod2 == 0)
                        {
                            __result = (-Vec2.Forward + Vec2.Side).Normalized();
                            return false;
                        }
                        __result = -Vec2.Forward;
                        return false;
                    }
                case 3:
                    {
                        if (mod1 == 0 || mod2 == 0)
                        {
                            __result = (-Vec2.Forward + -Vec2.Side).Normalized();
                            return false;
                        }
                        __result = -Vec2.Side;
                        return false;
                    }
                default:
                    {
                        Debug.FailedAssert("false", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\Formation\\SquareFormation.cs", "GetLocalDirectionOfUnit", 448);
                        __result = Vec2.Forward;
                        return false;
                    }
            }
        }

        private static int ShiftFileIndex(MBList2D<IFormationUnit> ____units2D, int fileIndex)
        {
            var unitCountOfOuterSide = UnitCountOfOuterSide(____units2D);
            int num1 = unitCountOfOuterSide + unitCountOfOuterSide / 2 - 2;
            int num2 = fileIndex - num1;
            if (num2 < 0)
                num2 += (unitCountOfOuterSide - 1) * 4;
            return num2;
        }
        private static int UnitCountOfOuterSide(MBList2D<IFormationUnit> ____units2D) => MathF.Ceiling(____units2D.Count1 / 4f) + 1;
    }
}
