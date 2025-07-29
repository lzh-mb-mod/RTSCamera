using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_OrderController
    {
        private static bool _patched;

        private static FieldInfo actualUnitSpacingsField = typeof(OrderController).GetField("actualUnitSpacings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo actualWidthsField = typeof(OrderController).GetField("actualWidths",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _overridenHasAnyMountedUnit = typeof(Formation).GetField("_overridenHasAnyMountedUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        //private static Dictionary<Formation, float> _actualWidths = new Dictionary<Formation, float>();
        private static Dictionary<Formation, int> _naturalUnitSpacings = new Dictionary<Formation, int>();
        private static Dictionary<Formation, int> _customUnitSpacings = new Dictionary<Formation, int>();
        private static Dictionary<Formation, float> _widthsBackup = new Dictionary<Formation, float>();
        private static Dictionary<Formation, Vec2> _virtualOrderPositions = new Dictionary<Formation, Vec2>();

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // To resolve the issue that, when isLineShort = true, formation direction will still use the direction of formation with most members instead of the direction return from SimulateNewOrderWithPositionAndDirection.
                harmony.Patch(
                    typeof(OrderController).GetMethod("MoveToLineSegment",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    transpiler: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(nameof(Transpiler_MoveToLineSegment),
                        BindingFlags.Static | BindingFlags.Public)));

                // To resolve 3 issues:
                // 1. The order troop placer is inconsistent with actual order issued during dragging if formation width is changed.
                // 2. The order troop placer is inconsistent with actual order issued during dragging if there're small number of mounted units in infantry formation.
                // 3. Sort of formations during dragging in horizontal layout is not consistent actual sort when order is issued
                // The idea is to use GetActualOrCurrentUnitSpacing(formation) instead of formation.UnitSpacing,
                // use GetActualOrCurrentWidth(formation) instead of formation.Width,
                // sort formations by their FormationClass in horizontal layout,
                // and use formation.CalculateHasSignificantNumberOfMounted instead of formation.HasAnyMountedUnit.
                // additional: resolve issue that formtion width/unit spacing cannot be recovered after setting position besides wall and settting back.
                harmony.Patch(
                    typeof(OrderController).GetMethod("SimulateNewOrderWithPositionAndDirectionAux",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_SimulateNewOrderWithPositionAndDirectionAux), BindingFlags.Static | BindingFlags.Public)));

                // For facing order, turn formations as a whole.
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.GetOrderLookAtDirection),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_GetOrderLookAtDirection), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod("SimulateNewFacingOrder",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_SimulateNewFacingOrder), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.SetOrderWithPosition),
                        BindingFlags.Instance | BindingFlags.Public),
                    transpiler: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Transpile_SetOrderWithPosition), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.SetOrderWithPosition),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_SetOrderWithPosition), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static void OnBehaviorInitialize()
        {
            _naturalUnitSpacings = new Dictionary<Formation, int>();
            _customUnitSpacings = new Dictionary<Formation, int>();
            _widthsBackup = new Dictionary<Formation, float>();
            _virtualOrderPositions = new Dictionary<Formation, Vec2>();
        }

        public static void OnRemoveBehavior()
        {
            _naturalUnitSpacings = null;
            _customUnitSpacings = null;
            _widthsBackup = null;
            _virtualOrderPositions = null;
        }

        public static void OnAddTeam(Team team)
        {
            foreach (Formation formation in team.FormationsIncludingEmpty)
            {
                formation.OnAfterArrangementOrderApplied += Formation_OnAfterArrangementOrderApplied;
            }
        }

        private static void Formation_OnAfterArrangementOrderApplied(Formation formation, ArrangementOrder.ArrangementOrderEnum arrangementOrder)
        {
            _naturalUnitSpacings.Remove(formation);
            _customUnitSpacings.Remove(formation);
            _widthsBackup.Remove(formation);
        }

        public static IEnumerable<CodeInstruction> Transpiler_MoveToLineSegment(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            //FixFormationUnitspacing(codes);
            FixFormationDirection(codes);
            return codes.AsEnumerable();
        }

        private static void FixFormationActualWidths(List<CodeInstruction> codes)
        {
            bool foundSimulateNewOrderWithPositionAndDirection = false;
            bool foundSetPositioning = false;
            int startIndex = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (!foundSimulateNewOrderWithPositionAndDirection)
                {
                    if (codes[i].opcode == OpCodes.Call)
                    {
                        if ((codes[i].operand as MethodInfo).Name == nameof(OrderController.SimulateNewOrderWithPositionAndDirection))
                        {
                            // IL_008b
                            foundSimulateNewOrderWithPositionAndDirection = true;
                        }
                    }
                }
                else if (!foundSetPositioning)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        if ((codes[i].operand as MethodInfo).Name == nameof(Formation.SetPositioning))
                        {
                            // IL_011a
                            foundSetPositioning = true;
                            startIndex = i;
                            break;
                        }
                    }    
                }
            }
            if (foundSetPositioning)
            {
                // isLineShort
                codes[startIndex + 1].opcode = OpCodes.Ldloc_1;
                codes[startIndex + 1].operand = null;
                // remove callvirt get_UnitSpacing and ldloc.s unitSpacing
                codes.RemoveRange(startIndex + 2, 2);
                // run this.actualUnitSpacings[key] = unitSpacing; only when isLineShort is true
                codes[startIndex + 2].opcode = OpCodes.Brfalse_S;
            }
        }

        private static void FixFormationDirection(List<CodeInstruction> codes)
        {
            bool foundGetActiveFacingOrderOf = false;
            bool foundLookAtDirection = false;
            bool foundSetMovementOrder = false;
            bool foundset_FacingOrder = false;
            int startIndex = -1;
            int endIndex = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (!foundGetActiveFacingOrderOf)
                {
                    if (codes[i].opcode == OpCodes.Call)
                    {
                        var methodOperand = codes[i].operand as MethodInfo;
                        if (methodOperand != null && methodOperand.Name == nameof(OrderController.GetActiveFacingOrderOf))
                        {
                            // IL_024a
                            foundGetActiveFacingOrderOf = true;
                        }
                    }
                }
                else if (!foundLookAtDirection)
                {
                    if (codes[i].opcode == OpCodes.Ldc_I4_S)
                    {
                        var operand = (sbyte)codes[i].operand;
                        if (operand == (sbyte)OrderType.LookAtDirection)
                        {
                            // IL_02ba
                            foundLookAtDirection = true;
                        }

                    }
                }
                else if (!foundSetMovementOrder)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == nameof(Formation.SetMovementOrder))
                        {
                            // IL_02c7
                            foundSetMovementOrder = true;
                            startIndex = i;
                        }

                    }
                }
                else if (!foundset_FacingOrder)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == "set_FacingOrder")
                        {
                            // IL_02da
                            foundset_FacingOrder = true;
                            endIndex = i;
                            break;
                        }
                    }
                }
            }
            if (foundSetMovementOrder && foundset_FacingOrder)
            {
                // use direction returned from SimulateNewOrderWithPositionAndDirection
                codes[startIndex + 2].opcode = OpCodes.Ldloc_S;
                codes[startIndex + 2].operand = (sbyte)13;
                codes[startIndex + 3].opcode = OpCodes.Nop;
            }
        }

        //public static bool Prefix_MoveToLineSegment(OrderController __instance,
        //    IEnumerable<Formation> formations,
        //    WorldPosition TargetLineSegmentBegin,
        //    WorldPosition TargetLineSegmentEnd,
        //    bool isFormationLayoutVertical,
        //    Dictionary<Formation, int> ___actualUnitSpacings,
        //    Dictionary<Formation, float> ___actualWidths
        //    )
        //{
        //    foreach (Formation formation in formations)
        //    {
        //        int num;
        //        if (___actualUnitSpacings.TryGetValue(formation, out num))
        //            formation.SetPositioning(unitSpacing: new int?(num));
        //        float customWidth;
        //        if (___actualWidths.TryGetValue(formation, out customWidth))
        //            formation.FormOrder = FormOrder.FormOrderCustom(customWidth);
        //    }
        //    formations = GetSortedFormations(formations, isFormationLayoutVertical);
        //    List<(Formation, int, float, WorldPosition, Vec2)> formationChanges;
        //    bool isLineShort;
        //    OrderController.SimulateNewOrderWithPositionAndDirection(formations, __instance.simulationFormations, TargetLineSegmentBegin, TargetLineSegmentEnd, out formationChanges, out isLineShort, isFormationLayoutVertical);
        //    if (!formations.Any<Formation>())
        //        return false;
        //    foreach ((Formation key, int num1, float customWidth, WorldPosition position, Vec2 direction) in formationChanges)
        //    {
        //        int unitSpacing = key.UnitSpacing;
        //        float width = key.Width;
        //        if (num1 > 0)
        //        {
        //            int num2 = MathF.Max(key.UnitSpacing - num1, 0);
        //            key.SetPositioning(unitSpacing: new int?(num2));
        //            if (key.UnitSpacing != unitSpacing)
        //                ___actualUnitSpacings[key] = unitSpacing;
        //        }
        //        if ((double)key.Width != (double)customWidth && key.ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Column)
        //        {
        //            key.FormOrder = FormOrder.FormOrderCustom(customWidth);
        //            if (isLineShort)
        //                ___actualWidths[key] = width;
        //        }
        //        if (!isLineShort)
        //        {
        //            key.SetMovementOrder(MovementOrder.MovementOrderMove(position));
        //            key.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
        //            key.FormOrder = FormOrder.FormOrderCustom(customWidth);
        //            if (this.OnOrderIssued != null)
        //            {
        //                MBList<Formation> mbList = new MBList<Formation>();
        //                mbList.Add(key);
        //                MBList<Formation> appliedFormations = mbList;
        //                this.OnOrderIssued(OrderType.Move, (MBReadOnlyList<Formation>)appliedFormations, this, (object)position);
        //                this.OnOrderIssued(OrderType.LookAtDirection, (MBReadOnlyList<Formation>)appliedFormations, this, (object)direction);
        //                this.OnOrderIssued(OrderType.FormCustom, (MBReadOnlyList<Formation>)appliedFormations, this, (object)customWidth);
        //            }
        //        }
        //        else
        //        {
        //            Formation formation = formations.MaxBy<Formation, int>((Func<Formation, int>)(f => f.CountOfUnitsWithoutDetachedOnes));
        //            switch (OrderController.GetActiveFacingOrderOf(formation))
        //            {
        //                case OrderType.LookAtEnemy:
        //                    key.SetMovementOrder(MovementOrder.MovementOrderMove(position));
        //                    if (this.OnOrderIssued != null)
        //                    {
        //                        MBList<Formation> mbList = new MBList<Formation>();
        //                        mbList.Add(key);
        //                        MBList<Formation> appliedFormations = mbList;
        //                        this.OnOrderIssued(OrderType.Move, (MBReadOnlyList<Formation>)appliedFormations, this, (object)position);
        //                        this.OnOrderIssued(OrderType.LookAtEnemy, (MBReadOnlyList<Formation>)appliedFormations, this);
        //                        continue;
        //                    }
        //                    continue;
        //                case OrderType.LookAtDirection:
        //                    key.SetMovementOrder(MovementOrder.MovementOrderMove(position));
        //                    key.FacingOrder = FacingOrder.FacingOrderLookAtDirection(formation.Direction);
        //                    if (this.OnOrderIssued != null)
        //                    {
        //                        MBList<Formation> mbList = new MBList<Formation>();
        //                        mbList.Add(key);
        //                        MBList<Formation> appliedFormations = mbList;
        //                        this.OnOrderIssued(OrderType.Move, (MBReadOnlyList<Formation>)appliedFormations, this, (object)position);
        //                        this.OnOrderIssued(OrderType.LookAtDirection, (MBReadOnlyList<Formation>)appliedFormations, this, (object)formation.Direction);
        //                        continue;
        //                    }
        //                    continue;
        //                default:
        //                    TaleWorlds.Library.Debug.FailedAssert("false", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\OrderController.cs", nameof(MoveToLineSegment), 2361);
        //                    continue;
        //            }
        //        }
        //    }

        //    return false;
        //}

        public static bool Prefix_SimulateNewOrderWithPositionAndDirectionAux(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition formationLineBegin,
            WorldPosition formationLineEnd,
            bool isSimulatingAgentFrames,
            ref List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            ref List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            ref bool isLineShort,
            bool isFormationLayoutVertical = true)
        {
            try
            {
                if (formations.FirstOrDefault()?.Team != Mission.Current?.PlayerTeam || (formations.FirstOrDefault()?.IsAIControlled ?? true))
                    return true;
                simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
                simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());
                float length = (formationLineEnd.AsVec2 - formationLineBegin.AsVec2).Length;
                isLineShort = false;
                if ((double)length < (double)ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius))
                {
                    isLineShort = true;
                }
                else
                {
                    float num = !isFormationLayoutVertical ? formations.Max<Formation>((Func<Formation, float>)(f => f.MinimumWidth /*changed from f.Width to MinimumWidth*/)) : formations.Sum<Formation>((Func<Formation, float>)(f => f.MinimumWidth)) + (float)(formations.Count<Formation>() - 1) * 1.5f;
                    if ((double)length < (double)num)
                        isLineShort = true;
                }
                var remainingFormations = Enumerable.Empty<Formation>();
                if (isLineShort)
                {
                    var actualUnitSpacings = GetActualUnitSpacings();
                    if (actualUnitSpacings != null)
                    {
                        foreach (var formation in formations)
                        {
                            if (actualUnitSpacings.ContainsKey(formation))
                            {
                                // moves actualUnitSpacings to our own storage.
                                // to prevent formations' unit spacing set to actualUnitSpacings[formation] in OrderController.MoveToLineSegment.
                                // so that unit spacing is kept if click on ground
                                if (!_naturalUnitSpacings.ContainsKey(formation))
                                {
                                    _naturalUnitSpacings[formation] = actualUnitSpacings[formation];
                                }
                                if (_customUnitSpacings.ContainsKey(formation))
                                {
                                    actualUnitSpacings[formation] = _customUnitSpacings[formation];
                                }
                            }
                            else
                            {
                                _customUnitSpacings.Remove(formation);
                                //_widthsBackup.Remove(formation);
                            }
                        }
                    }
                    var actualWidths = GetActualWidths();
                    if (actualWidths != null)
                    {
                        foreach (var formation in formations)
                        {
                            if (_widthsBackup.ContainsKey(formation))
                            {
                                actualWidths[formation] = _widthsBackup[formation];
                            }
                            else
                            {
                                _widthsBackup[formation] = formation.Width;
                            }
                        }
                    }
                    if (Utilities.Utility.ShouldLockFormation())
                    {
                        if (formations.Any())
                        {
                            SimulateNewOrderWithKeepingRelativePositions(formations, simulationFormations, true, formationLineBegin, null, null, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges, out remainingFormations);
                            formations = remainingFormations;
                        }
                    }
                    if (formations.Any())
                    {
                        float num1 = !isFormationLayoutVertical ? formations.Max<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f))) : formations.Sum<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f))) + (float)(formations.Count<Formation>() - 1) * 1.5f;
                        Vec2 direction = formations.MaxBy<Formation, int>((Func<Formation, int>)(f => f.CountOfUnitsWithoutDetachedOnes)).Direction;
                        direction.RotateCCW(-1.57079637f);
                        double num2 = (double)direction.Normalize();
                        formationLineEnd = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 + num1 / 2f * direction, formationLineBegin);
                        formationLineBegin = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 - num1 / 2f * direction, formationLineBegin);
                    }
                }
                else
                {
                    foreach (var formation in formations)
                    {
                        if (_naturalUnitSpacings.ContainsKey(formation))
                        {
                            // move actualUnitSpacings back to OrderController
                            // to allow unit spacings recovery
                            SetActualUnitSpacing(formation, _naturalUnitSpacings[formation]);
                        }
                    }
                    formationLineEnd = Mission.Current.GetStraightPathToTarget(formationLineEnd.AsVec2, formationLineBegin);
                    if (Utilities.Utility.ShouldLockFormation())
                    {
                        var clickedCenter = formationLineBegin;
                        clickedCenter.SetVec2((formationLineBegin.AsVec2 + formationLineEnd.AsVec2) / 2f);
                        SimulateNewOrderWithKeepingRelativePositions(formations, simulationFormations, false, clickedCenter, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges, out remainingFormations);
                        formations = remainingFormations;
                    }

                }
                if (formations.Any())
                {
                    if (isFormationLayoutVertical)
                        SimulateNewOrderWithVerticalLayout(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges);
                    else
                        SimulateNewOrderWithHorizontalLayout(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges);
                }

                // Added.
                if (isSimulatingFormationChanges && !isLineShort)
                {
                    FixUnitSpacingAndWidth(simulationFormationChanges);
                }
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }

        private static void FixUnitSpacingAndWidth(
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            foreach ((Formation formation, int unitSpacingReduction, float customWidth, WorldPosition position, Vec2 direction) in simulationFormationChanges)
            {
                if (unitSpacingReduction > 0)
                {
                    _customUnitSpacings[formation] = MathF.Max(formation.UnitSpacing - unitSpacingReduction, 0);
                }
                else if (_customUnitSpacings.ContainsKey(formation))
                {
                    _customUnitSpacings.Remove(formation);
                }
                if (formation.Width != customWidth && formation.ArrangementOrder.OrderEnum != ArrangementOrder.ArrangementOrderEnum.Column)
                {
                    _widthsBackup[formation] = customWidth;
                    //SetActualWidth(formation, customWidth);
                }
            }
        }

        private static void SimulateNewOrderWithKeepingRelativePositions(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            bool isLineShort,
            WorldPosition clickedCenter,
            WorldPosition? formationLineBegin,
            WorldPosition? formationLineEnd,
            bool isSimulatingAgentFrames,
            List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            out IEnumerable<Formation> remainingFormations
            )
        {
            simulationAgentFrames = !isSimulatingAgentFrames ? (List<WorldPosition>)null : simulationAgentFrames;
            simulationFormationChanges = !isSimulatingFormationChanges ? (List<(Formation, int, float, WorldPosition, Vec2)>)null : simulationFormationChanges;

            var formationOrderPositionDictionary = CollectFormationOrderPositions(formations, out var averageOrderPosition, !isLineShort, out var weightedAverageDirection);
            var remainingFormationsList = new List<Formation>();
            remainingFormations = remainingFormationsList;

            float availableWidthFromDragging = 0;
            float oldOverallWidth = 0;
            bool isWidthApproximatelySame = false;
            float minOverallWidth = 0;
            Vec2 newOverallDirection = Vec2.Zero;
            if (!isLineShort)
            {
                Vec2 dragVec = formationLineEnd.Value.AsVec2 - formationLineBegin.Value.AsVec2;
                float dragLength = dragVec.Length;
                dragVec.Normalize();
                availableWidthFromDragging = MathF.Max(0.0f, dragLength - (float)(formations.Count<Formation>() - 1) * 1.5f);
                oldOverallWidth = formations.Sum<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f)));
                isWidthApproximatelySame = availableWidthFromDragging.ApproximatelyEqualsTo(oldOverallWidth, 0.1f);
                minOverallWidth = formations.Sum<Formation>((Func<Formation, float>)(f => f.MinimumWidth));
                formations.Count<Formation>();
                newOverallDirection = new Vec2(-dragVec.y, dragVec.x).Normalized();
            }
            foreach (var pair in formationOrderPositionDictionary)
            {
                var formation = pair.Key;
                var oldOrderPosition = pair.Value;
                if (!oldOrderPosition.IsValid)
                {
                    remainingFormationsList.Add(formation);
                    continue;
                }
                WorldPosition formationPosition;
                Vec2 formationPositionVec2;
                int unitSpacingReduction = 0;
                var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                var actualOrCurrentWidth = GetActualOrCurrentWidth(formation);


                Vec2 formationDirection = formation.Direction;
                float formationWidth;
                if (isLineShort)
                {
                    formationPositionVec2 = oldOrderPosition - averageOrderPosition + clickedCenter.AsVec2;
                    formationPosition = clickedCenter;
                    formationPosition.SetVec2(formationPositionVec2);
                    GetFormationLineBeginEnd(formation, formationPosition, out var begin, out var end);
                    Vec2 vec = end.AsVec2 - begin.AsVec2;
                    float length = vec.Length;
                    vec.Normalize();
                    bool flag = length.ApproximatelyEqualsTo(actualOrCurrentWidth, 0.1f);
                    formationWidth = MathF.Min(flag ? actualOrCurrentWidth : length, formation.MaximumWidth);
                }
                else
                {
                    formationPositionVec2 = rotateVector(oldOrderPosition - averageOrderPosition, weightedAverageDirection, newOverallDirection) + clickedCenter.AsVec2;
                    formationPosition = clickedCenter;
                    formationPosition.SetVec2(formationPositionVec2);
                    formationWidth = MathF.Min(isWidthApproximatelySame || Utilities.Utility.ShouldKeepFormationWidth() ? actualOrCurrentWidth : MathF.Min((double)availableWidthFromDragging < (double)oldOverallWidth ? actualOrCurrentWidth : float.MaxValue, availableWidthFromDragging * (actualOrCurrentWidth / oldOverallWidth)), formation.MaximumWidth);
                    formationDirection = rotateVector(formationDirection, weightedAverageDirection, newOverallDirection);
                }
                if (isSimulatingFormationChanges)
                {
                    _virtualOrderPositions[formation] = formationPositionVec2;
                }

                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
                // TODO: what's the meaning of simulateFormationDepth?
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, simulateFormationDepth: false, out var simulatedFormationDepth, actualUnitSpacing);
            }
        }

        private static void GetFormationLineBeginEnd(Formation formation, WorldPosition formationLineBegin, out WorldPosition begin, out WorldPosition end)
        {
            float actualorCurrentWidth = GetActualOrCurrentWidth(formation);
            Vec2 direction = formation.Direction;
            direction.RotateCCW(-1.57079637f);
            double num2 = (double)direction.Normalize();
            end = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 - actualorCurrentWidth / 2f * direction, formationLineBegin);
            begin = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 + actualorCurrentWidth / 2f * direction, formationLineBegin);
        }

        private static void SimulateNewOrderWithHorizontalLayout(IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition formationLineBegin,
            WorldPosition formationLineEnd,
            bool isSimulatingAgentFrames,
            List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            // use input list, which may have elements added in SimulateNewOrderWithKeepingRelativePositions
            simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : simulationAgentFrames);
            simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : simulationFormationChanges);
            Vec2 vec = formationLineEnd.AsVec2 - formationLineBegin.AsVec2;
            float num = vec.Normalize();
            float num2 = formations.Max((Formation f) => f.MinimumWidth);
            if (num < num2)
            {
                num = num2;
            }
            Vec2 formationDirection = new Vec2(0f - vec.y, vec.x).Normalized();
            float num3 = 0f;
            // sort to keep consistent with actual order issued.
            formations = SortFormationsForHorizontalLayout(formations);
            foreach (Formation formation in formations)
            {
                float a = num;
                a = MathF.Min(Utilities.Utility.ShouldKeepFormationWidth() ? GetActualOrCurrentWidth(formation) : a, formation.MaximumWidth);
                WorldPosition formationPosition = formationLineBegin;
                var formationPositionVec2 = (formationLineEnd.AsVec2 + formationLineBegin.AsVec2) * 0.5f - formationDirection * num3;
                formationPosition.SetVec2(formationPositionVec2);


                if (isSimulatingFormationChanges)
                {
                    _virtualOrderPositions[formation] = formationPositionVec2;
                }
                int unitSpacingReduction = 0;
                // override official code from using formation.UnitSpacing to using GetActualUnitSpacing(formation)
                var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref a, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, a, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth, actualUnitSpacing);
                num3 += simulatedFormationDepth + GetGapBetweenLinesOfFormation(formation, actualUnitSpacing - unitSpacingReduction);
            }
        }

        private static void SimulateNewOrderWithVerticalLayout(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition formationLineBegin,
            WorldPosition formationLineEnd,
            bool isSimulatingAgentFrames,
            List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            // use input list, which may have elements added in SimulateNewOrderWithKeepingRelativePositions
            simulationAgentFrames = !isSimulatingAgentFrames ? (List<WorldPosition>)null : simulationAgentFrames;
            simulationFormationChanges = !isSimulatingFormationChanges ? (List<(Formation, int, float, WorldPosition, Vec2)>)null : simulationFormationChanges;
            Vec2 dragVec = formationLineEnd.AsVec2 - formationLineBegin.AsVec2;
            float dragLength = dragVec.Length;
            dragVec.Normalize();
            float availableWidthFromDragging = MathF.Max(0.0f, dragLength - (float)(formations.Count<Formation>() - 1) * 1.5f);
            float oldOverallWidth = formations.Sum<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f)));
            bool isWidthApproximatelySame = availableWidthFromDragging.ApproximatelyEqualsTo(oldOverallWidth, 0.1f);
            float minOverallWidth = formations.Sum<Formation>((Func<Formation, float>)(f => f.MinimumWidth));
            formations.Count<Formation>();
            Vec2 formationDirection = new Vec2(-dragVec.y, dragVec.x).Normalized();
            float num3 = 0.0f;
            foreach (Formation formation in formations)
            {
                float minimumWidth = formation.MinimumWidth;
                var actualOrCurrentWidth = GetActualOrCurrentWidth(formation);
                float formationWidth = MathF.Min(isWidthApproximatelySame || Utilities.Utility.ShouldKeepFormationWidth() ? actualOrCurrentWidth : MathF.Min((double)availableWidthFromDragging < (double)oldOverallWidth ? actualOrCurrentWidth : float.MaxValue, availableWidthFromDragging * (minimumWidth / minOverallWidth)), formation.MaximumWidth);
                WorldPosition formationPosition = formationLineBegin;
                var formationPositionVec2 = formationPosition.AsVec2 + dragVec * (formationWidth * 0.5f + num3);
                formationPosition.SetVec2(formationPositionVec2);

                if (isSimulatingFormationChanges)
                {
                    _virtualOrderPositions[formation] = formationPositionVec2;
                }
                int unitSpacingReduction = 0;
                // override official code from using formation.UnitSpacing to using GetActualUnitSpacing(formation)
                var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, false, out float _, actualUnitSpacing);
                num3 += formationWidth + 1.5f;
            }
        }

        private static IEnumerable<Formation> GetSortedFormations(
          IEnumerable<Formation> formations,
          bool isFormationLayoutVertical)
        {
            return isFormationLayoutVertical ? formations : OrderController.SortFormationsForHorizontalLayout(formations);
        }

        private static IEnumerable<Formation> SortFormationsForHorizontalLayout(IEnumerable<Formation> formations)
        {
            return formations.OrderBy((Formation f) => GetLineOrderByClass(f.FormationIndex));
        }

        private static int GetLineOrderByClass(FormationClass formationClass)
        {
            return Array.IndexOf(new FormationClass[8]
            {
                FormationClass.HeavyInfantry,
                FormationClass.Infantry,
                FormationClass.HeavyCavalry,
                FormationClass.Cavalry,
                FormationClass.LightCavalry,
                FormationClass.Skirmisher,
                FormationClass.Ranged,
                FormationClass.HorseArcher
            }, formationClass);
        }

        private static Dictionary<Formation, int> GetActualUnitSpacings()
        {
            var orderController = Mission.Current?.PlayerTeam?.PlayerOrderController;
            if (orderController != null)
            {
                return actualUnitSpacingsField.GetValue(orderController) as Dictionary<Formation, int>;
            }
            return null;
        }

        private static int GetActualOrCurrentUnitSpacing(Formation formation)
        {
            var actualUnitSpacings = GetActualUnitSpacings();
            if (actualUnitSpacings == null)
                return formation.UnitSpacing;
            if (actualUnitSpacings.ContainsKey(formation))
            {
                return actualUnitSpacings[formation];
            }
            return formation.UnitSpacing;
        }

        private static void SetActualUnitSpacing(Formation formation, int unitSpacing)
        {
            var actualUnitSpacings = GetActualUnitSpacings();
            if (actualUnitSpacings == null)
                return;
            if (actualUnitSpacings.ContainsKey(formation))
            {
                actualUnitSpacings[formation] = unitSpacing;
            }
            else
            {
                actualUnitSpacings.Add(formation, unitSpacing);
            }
        }

        private static void RemoveActualUnitSpacing(Formation formation)
        {
            var actualUnitSpacings = GetActualUnitSpacings();
            if (actualUnitSpacings == null)
                return;
            if (actualUnitSpacings.ContainsKey(formation))
            {
                actualUnitSpacings.Remove(formation);
            }
        }

        private static Dictionary<Formation, float> GetActualWidths()
        {
            var orderController = Mission.Current?.PlayerTeam?.PlayerOrderController;
            if (orderController != null)
            {
                return actualWidthsField.GetValue(orderController) as Dictionary<Formation, float>;
            }
            return null;
        }

        private static float GetActualOrCurrentWidth(Formation formation)
        {
            var actualWidths = GetActualWidths();
            if (actualWidths.ContainsKey(formation))
            {
                return actualWidths[formation];
            }
            return formation.Width;
        }

        private static void SetActualWidth(Formation formation, float width)
        {
            var actualWidths = GetActualWidths();
            if (actualWidths == null)
                return;
            if (actualWidths.ContainsKey(formation))
            {
                actualWidths[formation] = width;
            }
            else
            {
                actualWidths.Add(formation, width);
            }
    }

        private static void RemoveActualWidth(Formation formation)
        {
            var actualWidths = GetActualWidths();
            if (actualWidths == null)
                return;
            if (actualWidths.ContainsKey(formation))
            {
                actualWidths.Remove(formation);
            }
        }

        private static void DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(Formation formation, Formation simulationFormation, in WorldPosition formationPosition, in Vec2 formationDirection, ref float formationWidth, ref int unitSpacingReduction, int actualUnitSpacing)
        {
            if (simulationFormation.UnitSpacing != actualUnitSpacing)
            {
                simulationFormation = new Formation(null, -1);
            }
            bool hasAnyMountUnit = formation.CalculateHasSignificantNumberOfMounted && !(formation.RidingOrder == RidingOrder.RidingOrderDismount);
            _overridenHasAnyMountedUnit.SetValue(formation, hasAnyMountUnit);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, hasAnyMountUnit);
            int unitIndex = formation.CountOfUnitsWithoutDetachedOnes - 1;
            float actualWidth = formationWidth;
            do
            {
                formation.GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, unitIndex, in formationPosition, in formationDirection, formationWidth, actualUnitSpacing - unitSpacingReduction, out var unitSpawnPosition, out var _, out actualWidth);
                if (unitSpawnPosition.HasValue)
                {
                    break;
                }
                unitSpacingReduction++;
            }
            while (actualUnitSpacing - unitSpacingReduction >= 0);
            unitSpacingReduction = MathF.Min(unitSpacingReduction, actualUnitSpacing);
            if (unitSpacingReduction > 0)
            {
                formationWidth = actualWidth;
            }
            _overridenHasAnyMountedUnit.SetValue(formation, null);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, null);
        }
        private static void SimulateNewOrderWithFrameAndWidth(Formation formation, Formation simulationFormation, List<WorldPosition> simulationAgentFrames, List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges, in WorldPosition formationPosition, in Vec2 formationDirection, float formationWidth, int unitSpacingReduction, bool simulateFormationDepth, out float simulatedFormationDepth, int actualUnitSpacing)
        {
            int unitIndex = 0;
            float num2 = (simulateFormationDepth ? 0f : float.NaN);
            bool flag = Mission.Current.Mode != MissionMode.Deployment || Mission.Current.IsOrderPositionAvailable(in formationPosition, formation.Team);
            // override HasAnyMountUnit to be consistent with actual command execution.
            bool hasAnyMountUnit = formation.CalculateHasSignificantNumberOfMounted && !(formation.RidingOrder == RidingOrder.RidingOrderDismount);
            _overridenHasAnyMountedUnit.SetValue(formation, hasAnyMountUnit);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, hasAnyMountUnit);
            foreach (Agent item2 in from u in formation.GetUnitsWithoutDetachedOnes()
                                    orderby MBCommon.Hash(u.Index, u)
                                    select u)
            {
                WorldPosition? unitSpawnPosition = null;
                Vec2? unitSpawnDirection = null;
                if (flag)
                {
                    formation.GetUnitPositionWithIndexAccordingToNewOrder(simulationFormation, unitIndex, in formationPosition, in formationDirection, formationWidth, actualUnitSpacing - unitSpacingReduction, out unitSpawnPosition, out unitSpawnDirection);
                }
                else
                {
                    unitSpawnPosition = item2.GetWorldPosition();
                    unitSpawnDirection = item2.GetMovementDirection();
                }
                if (unitSpawnPosition.HasValue)
                {
                    simulationAgentFrames?.Add(unitSpawnPosition.Value);
                    if (simulateFormationDepth)
                    {
                        float num3 = Vec2.DistanceToLine(formationPosition.AsVec2, formationPosition.AsVec2 + formationDirection.RightVec(), unitSpawnPosition.Value.AsVec2);
                        if (num3 > num2)
                        {
                            num2 = num3;
                        }
                    }
                }
                unitIndex++;
            }
            _overridenHasAnyMountedUnit.SetValue(formation, null);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, null);
            if (flag)
            {
                simulationFormationChanges?.Add(ValueTuple.Create(formation, unitSpacingReduction, formationWidth, formationPosition, formationDirection));
            }
            else
            {
                WorldPosition item = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
                simulationFormationChanges?.Add(ValueTuple.Create(formation, unitSpacingReduction, formationWidth, item, formation.Direction));
            }
            simulatedFormationDepth = num2 + formation.UnitDiameter;
        }


        private static Formation GetSimulationFormation(Formation formation, Dictionary<Formation, Formation> simulationFormations)
        {
            return simulationFormations?[formation];
        }
        private static float GetGapBetweenLinesOfFormation(Formation f, float unitSpacing)
        {
            float num = 0f;
            float num2 = 0.2f;
            if (f.CalculateHasSignificantNumberOfMounted && !(f.RidingOrder == RidingOrder.RidingOrderDismount))
            {
                num = 2f;
                num2 = 0.6f;
            }
            return num + unitSpacing * num2;
        }


        public static bool Prefix_GetOrderLookAtDirection(IEnumerable<Formation> formations, Vec2 target, ref Vec2 __result)
        {
            if (formations.FirstOrDefault()?.Team != Mission.Current?.PlayerTeam || (formations.FirstOrDefault()?.IsAIControlled ?? true))
                return true;
            if (Utilities.Utility.ShouldLockFormation())
            {
                var formationCount = 0;
                Vec2 averageOrderPosition = Vec2.Zero;
                foreach (var formation in formations)
                {
                    bool hasVirtualOrderPosition = _virtualOrderPositions.ContainsKey(formation);
                    if (hasVirtualOrderPosition || formation.OrderPositionIsValid)
                    {
                        formationCount++;
                        var orderPosition = hasVirtualOrderPosition ? _virtualOrderPositions[formation] : formation.OrderPosition;
                        averageOrderPosition += orderPosition;
                    }
                }
                if (formationCount > 0)
                {
                    averageOrderPosition = averageOrderPosition * 1f / (float)formationCount;
                    __result = (target - averageOrderPosition).Normalized();
                    return false;
                }
            }
            return true;
        }

        public static bool Prefix_SimulateNewFacingOrder(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            Vec2 direction,
            ref List<WorldPosition> simulationAgentFrames)
        {
            if (formations.FirstOrDefault()?.Team != Mission.Current?.PlayerTeam || (formations.FirstOrDefault()?.IsAIControlled ?? true))
                return true;
            if (Utilities.Utility.ShouldLockFormation())
            {
                SimulateNewFacingOrder(formations,
                    simulationFormations,
                    direction,
                    true,
                    out simulationAgentFrames,
                    false,
                    out _);
                return false;
            }
            return true;
        }



        public static bool Prefix_SetOrderWithPosition(OrderController __instance, OrderType orderType, WorldPosition orderPosition)
        {
            if (__instance != Mission.Current?.PlayerTeam?.PlayerOrderController)
            {
                return true;
            }

            if (orderType == OrderType.LookAtDirection)
            {
                if (Utilities.Utility.ShouldLockFormation())
                {
                    SimulateNewFacingOrder(__instance.SelectedFormations,
                        __instance.simulationFormations,
                        OrderController.GetOrderLookAtDirection(__instance.SelectedFormations, orderPosition.AsVec2),
                        false,
                        out _,
                        true,
                        out var simulationFormationChanges);
                    foreach ((Formation formation, int unitSpacingReduction, float customWidth, WorldPosition position, Vec2 direction) in simulationFormationChanges)
                    {
                            formation.SetMovementOrder(MovementOrder.MovementOrderMove(position));
                            formation.FacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                    }
                }
                else
                {
                    FacingOrder facingOrder = FacingOrder.FacingOrderLookAtDirection(OrderController.GetOrderLookAtDirection(__instance.SelectedFormations, orderPosition.AsVec2));
                    foreach (var formation in __instance.SelectedFormations)
                    {
                        formation.FacingOrder = facingOrder;
                    }
                }
            }
                return true;
        }

        public static IEnumerable<CodeInstruction> Transpile_SetOrderWithPosition(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            FixingFormationFacingOrder(codes);
            return codes.AsEnumerable();
        }

        private static void FixingFormationFacingOrder(List<CodeInstruction> codes)
        {
            bool foundFacingOrderLookAtDirection = false;
            bool foundget_SelectedFormations = false;
            bool foundset_FacingOrder = false;
            bool foundEndFinally = false;

            int facingOrderLookAtDirectionIndex = -1;
            int get_SelectedFormationsIndex = -1;
            int set_FacingOrderIndex = -1;
            int endFinallyIndex = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (!foundFacingOrderLookAtDirection)
                {
                    if (codes[i].opcode == OpCodes.Call)
                    {
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == nameof(FacingOrder.FacingOrderLookAtDirection))
                        {
                            // IL_00d4
                            foundFacingOrderLookAtDirection = true;
                            facingOrderLookAtDirectionIndex = i;
                        }
                    }
                }
                else if (!foundset_FacingOrder)
                {
                    if (codes[i].opcode == OpCodes.Callvirt)
                    {
                        // IL_00f0
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == "set_FacingOrder")
                        {
                            foundset_FacingOrder = true;
                            set_FacingOrderIndex = i;
                        }
                    }
                }
                else if (!foundEndFinally)
                {
                    if (codes[i].opcode == OpCodes.Endfinally)
                    {
                        // IL_010d
                        foundEndFinally = true;
                        endFinallyIndex = i;
                        break;
                    }
                }
            }
            if (!foundFacingOrderLookAtDirection)
            {
                throw new Exception("FacingOrderLookAtDirection not found");
            }
            for (int i = facingOrderLookAtDirectionIndex; i >= 0; --i)
            {
                if (!foundget_SelectedFormations)
                {
                    if (codes[i].opcode == OpCodes.Call)
                    {
                        var operand = codes[i].operand as MethodInfo;
                        if (operand.Name == "get_SelectedFormations")
                        {
                            // IL_00c3
                            foundget_SelectedFormations = true;
                            get_SelectedFormationsIndex = i;
                        }
                    }
                }
            }
            if (!foundset_FacingOrder)
            {
                throw new Exception("set_FacingOrderIndex not found");
            }
            if (!foundEndFinally)
            {
                throw new Exception("EndFinally not found");
            }
            // jump to IL_0173
            codes[get_SelectedFormationsIndex - 1].opcode = OpCodes.Br_S;
            codes[get_SelectedFormationsIndex - 1].operand = codes[set_FacingOrderIndex + 4].operand;
            // from IL_00c3
            codes.RemoveRange(get_SelectedFormationsIndex, endFinallyIndex - get_SelectedFormationsIndex + 1);
        }

        private static Dictionary<Formation, Vec2> CollectFormationOrderPositions(
            IEnumerable<Formation> formations,
            out Vec2 averageOrderPosition,
            bool collectDirection,
            out Vec2 weightedAverageDirection)
        {
            var formationOrderPositionDictionary = new Dictionary<Formation, Vec2>();
            var remainingFormationList = new List<Formation>();
            var formationCount = 0;
            averageOrderPosition = Vec2.Zero;
            weightedAverageDirection = Vec2.Zero;
            foreach (var formation in formations)
            {
                bool hasVirtualOrderPosition = _virtualOrderPositions.ContainsKey(formation);
                formationCount++;
                var orderPosition = hasVirtualOrderPosition ? _virtualOrderPositions[formation] : formation.OrderPosition.IsValid ? formation.OrderPosition : formation.CurrentPosition;
                if (orderPosition.IsValid)
                {
                    averageOrderPosition += orderPosition;
                }
                formationOrderPositionDictionary.Add(formation, orderPosition);
            }
            if (formationCount > 0)
            {
                averageOrderPosition = averageOrderPosition * 1f / (float)formationCount;
            }
            if (collectDirection)
            {
                foreach (var pair in formationOrderPositionDictionary)
                {
                    var formation = pair.Key;
                    var orderPositionVec2 = pair.Value;
                    if (orderPositionVec2.IsValid)
                    {
                        weightedAverageDirection += formation.Direction * (1 / MathF.Max(5f, orderPositionVec2.DistanceSquared(averageOrderPosition)));
                    }
                }
                weightedAverageDirection.Normalize();
            }
            return formationOrderPositionDictionary;
        }

        private static void SimulateNewFacingOrder(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            Vec2 direction,
            bool isSimulatingAgentFrames,
            out List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
            simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());
            var formationOrderPositionDictionary = CollectFormationOrderPositions(formations, out var averageOrderPosition, true, out var weightedAverageDirection);

            foreach (var pair in formationOrderPositionDictionary)
            {
                var formation = pair.Key;
                var orderPositionVec2 = pair.Value;
                Vec2 newPositionVec2;
                Vec2 newDirection;
                if (formation.FacingOrder.OrderEnum == FacingOrder.FacingOrderEnum.LookAtEnemy)
                {
                    newPositionVec2 = orderPositionVec2;
                    newDirection = direction;
                }
                else
                {
                    newPositionVec2 = rotateVector(orderPositionVec2 - averageOrderPosition, weightedAverageDirection, direction) + averageOrderPosition;
                    newDirection = rotateVector(formation.Direction, weightedAverageDirection, direction);
                }
                float width = formation.Width;
                WorldPosition formationPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
                formationPosition.SetVec2(newPositionVec2);

                if (isSimulatingFormationChanges)
                {
                    _virtualOrderPositions[formation] = newPositionVec2;
                }
                int unitSpacingReduction = 0;
                var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in newDirection, ref width, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in newDirection, width, unitSpacingReduction, false, out var _, actualUnitSpacing);
            }
        }

        private static Vec2 rotateVector(Vec2 input, Vec2 from, Vec2 to)
        {
            var cos = from.x * to.x + from.y * to.y;
            var sin = from.x * to.y - from.y * to.x;
            return new Vec2(
                input.x * cos - input.y * sin,
                input.x * sin + input.y * cos);
        }
    }
}
