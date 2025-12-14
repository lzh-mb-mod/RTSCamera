using HarmonyLib;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_SquareFormation
    {
        private enum Side
        {
            Front,
            Right,
            Rear,
            Left,
        }
        private static bool _patched;
        private static Type _sideEnum;
        private static ConstructorInfo _nullableCtor;

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
                var getSideOfUnitPosition = AccessTools.Method(typeof(SquareFormation), "GetSideOfUnitPosition",
                    new Type[]
                    {
                        typeof(int), typeof(int)
                    });
                var nullableSide = getSideOfUnitPosition.ReturnType;
                _sideEnum = Nullable.GetUnderlyingType(nullableSide);
                _nullableCtor = nullableSide.GetConstructor(new[] { _sideEnum });
                harmony.Patch(
                    getSideOfUnitPosition,
                    prefix: new HarmonyMethod(typeof(Patch_SquareFormation).GetMethod(
                        nameof(Prefix_GetSideOfUnitPosition), BindingFlags.Static | BindingFlags.Public)));


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

        public static bool Prefix_GetLocalDirectionOfUnit(SquareFormation __instance, MBList2D<IFormationUnit> ____units2D, int fileIndex, int rankIndex, ref Vec2 __result)
        {
            if (!CommandSystemConfig.Get().SquareFormationCornerFix)
                return true;
            var unitCountOfOuterSide = UnitCountOfOuterSide(____units2D);
            var shiftedFileIndex = ShiftFileIndex(unitCountOfOuterSide, fileIndex);
            var mod1 = (shiftedFileIndex - rankIndex) % (unitCountOfOuterSide - 1);
            var mod2 = (shiftedFileIndex + rankIndex) % (unitCountOfOuterSide - 1);
            var side = GetSideOfUnitPosition(unitCountOfOuterSide, shiftedFileIndex);
            switch (side)
            {
                case Side.Front:
                    {
                        // If there's only one unit in current rank:
                        // this means that the unitCountOfOuterSide is odd number,
                        // and we are setting direction of the center unit.
                        int unitCountOfOneSideInCurrentRank = unitCountOfOuterSide - 2 * rankIndex;
                        if (unitCountOfOneSideInCurrentRank > 1 &&( mod1 == 0 || mod2 == 0))
                        {
                            __result = (Vec2.Forward + -Vec2.Side).Normalized();
                            return false;
                        }
                        __result = Vec2.Forward;
                        return false;
                    }
                case Side.Right:
                    {
                        if (mod1 == 0 || mod2 == 0)
                        {
                            __result = (Vec2.Forward + Vec2.Side).Normalized();
                            return false;
                        }
                        __result = Vec2.Side;
                        return false;
                    }
                case Side.Rear:
                    {
                        if (mod1 == 0 || mod2 == 0)
                        {
                            __result = (-Vec2.Forward + Vec2.Side).Normalized();
                            return false;
                        }
                        __result = -Vec2.Forward;
                        return false;
                    }
                case Side.Left:
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

        private static int ShiftFileIndex(int unitCountOfOuterSide, int fileIndex)
        {
            int num1 = unitCountOfOuterSide + unitCountOfOuterSide / 2 - 2;
            int num2 = fileIndex - num1;
            if (num2 < 0)
                num2 += (unitCountOfOuterSide - 1) * 4;
            return num2;
        }

        // SquareFormation.UnitCountOfOuterSide => MathF.Ceiling((float) this.FileCount / 4f) + 1;
        private static int UnitCountOfOuterSide(MBList2D<IFormationUnit> ____units2D) => MathF.Ceiling(____units2D.Count1 / 4f) + 1;

        private static Side GetSideOfUnitPosition(int unitCountOfOuterSide, int fileIndex)
        {
            return (Side)(fileIndex / (unitCountOfOuterSide - 1));
        }



        public static bool Prefix_GetSideOfUnitPosition(SquareFormation __instance, int fileIndex, int rankIndex, MBList2D<IFormationUnit> ____units2D, ref object __result)
        {
            // The original code:
            //
            //private SquareFormation.Side? GetSideOfUnitPosition(int fileIndex, int rankIndex)
            //{
            //    SquareFormation.Side sideOfUnitPosition = this.GetSideOfUnitPosition(fileIndex);
            //    if (rankIndex == 0)
            //        return new SquareFormation.Side?(sideOfUnitPosition);
            //    int num1 = this.UnitCountOfOuterSide - 2 * rankIndex;
            //    if (num1 == 1 && sideOfUnitPosition != SquareFormation.Side.Front)
            //        return new SquareFormation.Side?();
            //    int num2 = fileIndex % (this.UnitCountOfOuterSide - 1);
            //    int num3 = (this.UnitCountOfOuterSide - num1) / 2;
            //    return num2 >= num3 && this.UnitCountOfOuterSide - num2 - 1 > num3 ? new SquareFormation.Side?(sideOfUnitPosition) : new SquareFormation.Side?();
            //}
            // The issue is that, if UnitCountOfOuterSide is an odd number, the center unit is not positioned
            // in this case num1 == 1, and UnitCountOfOuterSide - num2 - 1 == num3

            var unitCountOfOuterSide = UnitCountOfOuterSide(____units2D);
            var sideOfUnitPosition = GetSideOfUnitPosition(unitCountOfOuterSide, fileIndex);
            if (rankIndex == 0)
            {
                __result = _nullableCtor.Invoke(new[] { Enum.ToObject(_sideEnum, (int)sideOfUnitPosition) });
                return false;
            }
            int unitCountOfOneSideInCurrentRank = unitCountOfOuterSide - 2 * rankIndex;
            if (unitCountOfOneSideInCurrentRank == 1 && sideOfUnitPosition != Side.Front)
            {
                __result = null;
                return false;
            }
            int indexInCurrentSide = fileIndex % (unitCountOfOuterSide - 1);
            // num3 equals to rankIndex
            //int num3 = (unitCountOfOuterSide - unitCountOfOneSideInCurrentRank) / 2;
            if (indexInCurrentSide >= rankIndex)
            {
                if (indexInCurrentSide < unitCountOfOuterSide - rankIndex - 1 || indexInCurrentSide == unitCountOfOuterSide - rankIndex - 1 && unitCountOfOneSideInCurrentRank == 1 /*&& sideOfUnitPosition == Side.Front*/)
                {
                    __result = _nullableCtor.Invoke(new[] { Enum.ToObject(_sideEnum, (int)sideOfUnitPosition) });
                    return false;
                }
            }

            __result = null;
            return false;
        }
    }
}
