using HarmonyLib;
using Microsoft.VisualBasic;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.QuerySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using MathF = TaleWorlds.Library.MathF;


namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_OrderController
    {
        public class StackRecord
        {
            public List<Formation> Formations = new List<Formation>();
            public float LeftMost = 0;
            public float RightMost = 0;
            public float Width => RightMost - LeftMost;
            public float Center => (LeftMost + RightMost) * 0.5f;
            public float MinimumWidth = 0;
            public float MaximumWidth = 0;
        }

        public class MovingTarget
        {
            public WorldPosition? MedianPosition;
        }


        private static bool _patched;

        private static FieldInfo actualUnitSpacingsField = typeof(OrderController).GetField("actualUnitSpacings",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo actualWidthsField = typeof(OrderController).GetField("actualWidths",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _overridenHasAnyMountedUnit = typeof(Formation).GetField("_overridenHasAnyMountedUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo ResetForSimulation = typeof(Formation).GetMethod("ResetForSimulation", BindingFlags.Instance | BindingFlags.NonPublic);
        private static PropertyInfo OverridenUnitCount = typeof(Formation).GetProperty("OverridenUnitCount", BindingFlags.Instance | BindingFlags.Public);
        private static FieldInfo _arrangementOrder = typeof(Formation).GetField("_arrangementOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _simulationFormationTemp = typeof(Formation).GetField("_simulationFormationTemp", BindingFlags.Static | BindingFlags.NonPublic);
        private static FieldInfo _simulationFormationUniqueIdentifier = typeof(Formation).GetField("_simulationFormationUniqueIdentifier", BindingFlags.Static | BindingFlags.NonPublic);

        // If line short, OrderController.actualUnitSpacings can be considered as the live preview unit spacing.
        // Otherwise, it's should be set to the natural (max) unit spacing of the arrangement to allow wider unit spacing than current to be dragged out.
        // Custom unit spacing is the unit spacing that users dragged out and should be set as live preview in line short.


        // Or say:
        // If line short, formation unit spacing to use in preview is the same as the current custom formation unit spacing.
        // else, during dragging, formation unit spacing to use in preview should be the max unit spacing of the arrangement.
        private static Dictionary<Formation, int> _naturalUnitSpacings = new Dictionary<Formation, int>();
        private static Dictionary<Formation, int> _customUnitSpacings = new Dictionary<Formation, int>();
        private static Dictionary<Formation, float> _widthsBackup = new Dictionary<Formation, float>();
        public static FormationChanges LivePreviewFormationChanges = new FormationChanges();
        private static Dictionary<Formation, MovingTarget> _currentMovingTarget;

        public static Dictionary<Formation, Formation> FacingEnemeyTarget = new Dictionary<Formation, Formation>();

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
                // For facing order
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.SetOrderWithPosition),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_SetOrderWithPosition), BindingFlags.Static | BindingFlags.Public)));

                // for order queue
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.GetActiveMovementOrderOf),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_GetActiveMovementOrderOf), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.GetActiveFacingOrderOf),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_GetActiveFacingOrderOf), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.GetActiveFiringOrderOf),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_GetActiveFiringOrderOf), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.GetActiveRidingOrderOf),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_GetActiveRidingOrderOf), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderController).GetMethod(nameof(OrderController.GetActiveArrangementOrderOf),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_GetActiveArrangementOrderOf), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                return false;
            }

            return true;
        }

        public static void OnAfterMissionCreated()
        {
            _naturalUnitSpacings = new Dictionary<Formation, int>();
            _customUnitSpacings = new Dictionary<Formation, int>();
            _widthsBackup = new Dictionary<Formation, float>();
            LivePreviewFormationChanges = new FormationChanges();
            _currentMovingTarget = new Dictionary<Formation, MovingTarget>();
            FacingEnemeyTarget = new Dictionary<Formation, Formation>();
        }

        public static void OnRemoveBehavior()
        {
            _naturalUnitSpacings = null;
            _customUnitSpacings = null;
            _widthsBackup = null;
            LivePreviewFormationChanges = null;
            _currentMovingTarget = null;
            FacingEnemeyTarget = null;
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
            //_naturalUnitSpacings.Remove(formation);
            _naturalUnitSpacings[formation] = ArrangementOrder.GetUnitSpacingOf(arrangementOrder);
            _customUnitSpacings.Remove(formation);
            _widthsBackup.Remove(formation);
            // if (CommandQueueLogic.CurrentFormationChanges.VirtualChanges.TryGetValue(formation, out var change))
            // {
            //     change.Width = null;
            //     CommandQueueLogic.CurrentFormationChanges.VirtualChanges[formation] = change;
            // }
        }

        public static IEnumerable<CodeInstruction> Transpiler_MoveToLineSegment(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            //FixFormationUnitspacing(codes);
            FixNoLineShortFormationDirection(codes);
            FixLineShortFacingOrder(codes);
            return codes.AsEnumerable();
        }

        private static void FixNoLineShortFormationDirection(List<CodeInstruction> codes)
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

        private static void FixLineShortFacingOrder(List<CodeInstruction> codes)
        {
            bool foundGetActiveFacingOrderOf = false;
            bool foundSetMovementOrder = false;
            int indexOfGetActiveFacingOrderOf = -1;
            int indexOfSetMovementOf = -1;
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
                            indexOfGetActiveFacingOrderOf = i;
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
                            // IL_0260
                            foundSetMovementOrder = true;
                            indexOfSetMovementOf = i;
                        }

                    }
                }
            }
            if (foundGetActiveFacingOrderOf && foundSetMovementOrder)
            {
                // use argument of SetMovementOrder
                codes[indexOfGetActiveFacingOrderOf - 1].opcode = OpCodes.Ldloc_S;
                codes[indexOfGetActiveFacingOrderOf - 1].operand = codes[indexOfSetMovementOf - 3].operand;
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
            return SimulateNewOrderWithPositionAndDirection(formations,
                simulationFormations,
                formationLineBegin,
                formationLineEnd,
                isSimulatingAgentFrames,
                out simulationAgentFrames,
                isSimulatingFormationChanges,
                out simulationFormationChanges,
                out isLineShort,
                isFormationLayoutVertical);
        }

        public static bool SimulateNewOrderWithPositionAndDirection(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition formationLineBegin,
            WorldPosition formationLineEnd,
            bool isSimulatingAgentFrames,
            out List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            out bool isLineShort,
            bool isFormationLayoutVertical = true,
            bool isFromPlayerInput = false)
        {
            simulationAgentFrames = null;
            simulationFormationChanges = null;
            isLineShort = false;
            try
            {
                if (!isFromPlayerInput && !Utilities.Utility.ShouldEnablePlayerOrderControllerPatchForFormation(formations))
                    return true;
                var allFormations = formations.ToList();
                simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
                simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());
                float length = (formationLineEnd.AsVec2 - formationLineBegin.AsVec2).Length;
                isLineShort = false;

                foreach (var formation in formations)
                {
                    var virtualWidth = GetFormationVirtualWidth(formation);
                    if (virtualWidth != null)
                    {
                        SetActualWidth(formation, virtualWidth.Value);
                    }
                    else
                    {
                        SetFormationVirtualWidth(formation, GetActualOrCurrentWidth(formation));
                    }
                    TryIntializeFormationChanges(formation);
                }

                if (Utilities.Utility.ShouldLockFormation())
                {
                    if (Utilities.Utility.ShouldKeepFormationWidth())
                    {
                        if ((double)length < (double)ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRadius))
                        {
                            isLineShort = true;
                        }
                    }
                    else
                    {
                        CollectStacksRecord(formations, out _, out var minOverallWidth, out var shouldFormationBeStackedWithPreviousFormation, out var stacksRecord, out _, out _, out _);
                        if (length < minOverallWidth + (formations.Count() - shouldFormationBeStackedWithPreviousFormation.Count(pair => pair.Value == true) - 1) * 1.5f)
                        {
                            isLineShort = true;
                        }
                    }
                }
                else
                {
                    float num = !isFormationLayoutVertical ? formations.Max(f => GetFormationVirtualMinimumWidth(f) /*changed from f.Width to MinimumWidth*/) : formations.Sum(f => GetFormationVirtualMinimumWidth(f)) + (formations.Count() - 1) * 1.5f;
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
                                //if (_customUnitSpacings.ContainsKey(formation))
                                //{
                                //    actualUnitSpacings[formation] = _customUnitSpacings[formation];
                                //}
                            }
                            else
                            {
                                _naturalUnitSpacings[formation] = ArrangementOrder.GetUnitSpacingOf(GetFormationVirtualArrangementOrder(formation));
                            }
                                //else
                                //{
                                //    _customUnitSpacings.Remove(formation);
                                //    //_widthsBackup.Remove(formation);
                                //}
                            var virtualUnitSpacing = GetFormationVirtualUnitSpacing(formation);
                            if (virtualUnitSpacing != null)
                                actualUnitSpacings[formation] = virtualUnitSpacing.Value;
                        }
                    }
                    if (Utilities.Utility.ShouldLockFormation())
                    {
                        if (formations.Any())
                        {
                            var clickedPosition = formationLineBegin;
                            SimulateNewOrderWithKeepingRelativePositions(formations, simulationFormations, true, clickedPosition, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges, out remainingFormations);
                            formations = remainingFormations;
                        }
                    }
                    if (formations.Any())
                    {
                        float num1 = !isFormationLayoutVertical ? formations.Max(f => GetActualOrCurrentWidth(f)) : formations.Sum((f => GetActualOrCurrentWidth(f))) + (formations.Count() - 1) * 1.5f;
                        Vec2 direction = GetFormationVirtualDirection(TaleWorlds.Core.Extensions.MaxBy(formations, f => f.CountOfUnitsWithoutDetachedOnes));
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
                        SetActualUnitSpacing(formation, GetFormationVirtualNaturalUnitSpacing(formation));
                        //if (_naturalUnitSpacings.ContainsKey(formation))
                        //{
                        //    // move actualUnitSpacings back to OrderController
                        //    // to allow unit spacings recovery
                        //    SetActualUnitSpacing(formation, _naturalUnitSpacings[formation]);
                        //}
                        //if (_widthsBackup.ContainsKey(formation))
                        //{
                        //    SetActualWidth(formation, _widthsBackup[formation]);
                        //}
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
                        SimulateNewOrderWithVerticalLayout(formations, simulationFormations, isLineShort, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges);
                    else
                        SimulateNewOrderWithHorizontalLayout(formations, simulationFormations, isLineShort, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges);
                }

                foreach (var formation in allFormations)
                {
                    //RemoveActualUnitSpacing(formation);
                    RemoveActualWidth(formation);
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
            out IEnumerable<Formation> remainingFormations)
        {
            if (isLineShort)
            {
                SimulateNewOrderWithKeepingRelativePositionsLineShort(formations, simulationFormations, clickedCenter, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges, out remainingFormations);
            }
            else if (Utilities.Utility.ShouldKeepFormationWidth())
            {
                SimulateNewOrderWithKeepingRelativePositionsNotLineShortKeepingFormationWidth(formations, simulationFormations, clickedCenter, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges, out remainingFormations);
            }
            else
            {
                SimulateNewOrderWithKeepingRelativePositionsNotLineShortNotKeepingFormationWidth(formations, simulationFormations, clickedCenter, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, simulationAgentFrames, isSimulatingFormationChanges, simulationFormationChanges, out remainingFormations);
            }
        }
        private static void SimulateNewOrderWithKeepingRelativePositionsLineShort(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition clickedCenter,
            WorldPosition? formationLineBegin,
            WorldPosition? formationLineEnd,
            bool isSimulatingAgentFrames,
            List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            out IEnumerable<Formation> remainingFormations)
        {
            simulationAgentFrames = !isSimulatingAgentFrames ? null : simulationAgentFrames;
            simulationFormationChanges = !isSimulatingFormationChanges ? null : simulationFormationChanges;

            var formationOrderPositionList = CollectFormationOrderPositions(formations, out var averageOrderPosition, false, out var _).ToList();
            var remainingFormationsList = new List<Formation>();
            remainingFormations = remainingFormationsList;

            var dragVec = formationLineEnd.Value.AsVec2 - formationLineBegin.Value.AsVec2;
            float dragLength = dragVec.Length;
            Vec2 newOverallDirection = Vec2.Zero;
            Dictionary<Formation, bool> shouldFormationBeStackedWithPreviousFormation = new Dictionary<Formation, bool>();

            Vec2 previousOldPosition = Vec2.Invalid;
            float previousWidth = 0f;
            foreach (var pair in formationOrderPositionList)
            {
                var formation = pair.Key;
                var oldOrderPosition = pair.Value;
                var stackWidth = 0f;
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


                float formationWidth;
                formationPositionVec2 = oldOrderPosition - averageOrderPosition + clickedCenter.AsVec2;
                formationPosition = clickedCenter;
                formationPosition.SetVec2(formationPositionVec2);
                Vec2 formationDirection;
                if (GetFormationVirtualFacingOrder(formation) == OrderType.LookAtEnemy)
                {
                    // set formation virtual position to preview position to allows previewing the facing direction
                    var formationPositionToRecover = GetFormationVirtualPosition(formation);
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, null, null, null);
                    formationDirection = GetFormationVirtualDirectionIncludingFacingEnemy(formation);
                    // recover previous position after getting the direction.
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPositionToRecover, null, null, null);
                }
                else
                {
                    formationDirection = GetFormationVirtualDirectionIncludingFacingEnemy(formation);
                }
                GetFormationLineBeginEnd(formation, formationPosition, out var begin, out var end);
                Vec2 vec = end.AsVec2 - begin.AsVec2;
                float length = vec.Length;
                vec.Normalize();
                bool flag = length.ApproximatelyEqualsTo(actualOrCurrentWidth, 0.1f);
                formationWidth = MathF.Clamp(flag ? actualOrCurrentWidth : length, GetFormationVirtualMinimumWidth(formation), GetFormationVirtualMaximumWidth(formation));
                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, null, null, null);
                }
                if (!Mission.Current.IsPositionInsideBoundaries(formationPosition.AsVec2))
                {
                    Vec2 boundaryPosition = Mission.Current.GetClosestBoundaryPosition(formationPosition.AsVec2);
                    formationPosition.SetVec2(boundaryPosition);
                }

                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth, actualUnitSpacing);

                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.SetPreviewShape(formation, formationWidth, simulatedFormationDepth);
                }

                previousOldPosition = oldOrderPosition;
                previousWidth = stackWidth;
            }
        }

        private static void SimulateNewOrderWithKeepingRelativePositionsNotLineShortKeepingFormationWidth(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition clickedCenter,
            WorldPosition? formationLineBegin,
            WorldPosition? formationLineEnd,
            bool isSimulatingAgentFrames,
            List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            out IEnumerable<Formation> remainingFormations)
        {
            simulationAgentFrames = !isSimulatingAgentFrames ? null : simulationAgentFrames;
            simulationFormationChanges = !isSimulatingFormationChanges ? null : simulationFormationChanges;

            var formationOrderPositionList = CollectFormationOrderPositions(formations, out var averageOrderPosition, true, out var weightedAverageDirection).ToList();
            var remainingFormationsList = new List<Formation>();
            remainingFormations = remainingFormationsList;

            var dragVec = formationLineEnd.Value.AsVec2 - formationLineBegin.Value.AsVec2;
            dragVec.Normalize();

            Vec2 newOverallDirection = new Vec2(-dragVec.y, dragVec.x).Normalized();

            foreach (var pair in formationOrderPositionList)
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


                Vec2 formationDirection = GetFormationVirtualDirection(formation);
                float formationWidth;
                formationPositionVec2 = rotateVector(oldOrderPosition - averageOrderPosition, weightedAverageDirection, newOverallDirection) + clickedCenter.AsVec2;
                formationPosition = clickedCenter;
                formationPosition.SetVec2(formationPositionVec2);
                formationWidth = MathF.Min(actualOrCurrentWidth, GetFormationVirtualMaximumWidth(formation));
                formationDirection = rotateVector(formationDirection, weightedAverageDirection, newOverallDirection);
                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, formationDirection, null, formationWidth);
                    LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formations);
                }

                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth, actualUnitSpacing);

                if (isSimulatingFormationChanges)
                {
                    // Do not update unit spacing when keeping formation width.
                    //var unitSpacing = MathF.Max(GetActualOrCurrentUnitSpacing(formation) - unitSpacingReduction, 0);
                    //LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, unitSpacing, null);
                    LivePreviewFormationChanges.SetPreviewShape(formation, formationWidth, simulatedFormationDepth);
                }
            }
        }

        private static Vec2 GetLeftFlankPosition(Formation formation, Vec2 orderPosition, Vec2 dragVec)
        {
            var width = GetActualOrCurrentWidth(formation);
            //var formationDirection = GetFormationVirtualDirection(formation);
            //var directionToLeftFlank = new Vec2(-formationDirection.y, formationDirection.x);
            return orderPosition + -dragVec * width * 0.5f;
        }

        private static Vec2 GetRightFlankPosition(Formation formation, Vec2 orderPosition, Vec2 dragVec)
        {
            var width = GetActualOrCurrentWidth(formation);
            //var formationDirection = GetFormationVirtualDirection(formation);
            //var directionToRightFlank = new Vec2(formationDirection.y, -formationDirection.x);
            return orderPosition + dragVec * width * 0.5f;
        }

        private static void CollectStacksRecord(
            IEnumerable<Formation> formations,
            out float oldOverallWidth,
            out float minOverallWidth,
            out Dictionary<Formation, bool> shouldFormationBeStackedWithPreviousFormation,
            out List<StackRecord> stacksRecord,
            out List<KeyValuePair<Formation, Vec2>> formationOrderPositionList,
            out Vec2 averageOrderPosition,
            out Vec2 weightedAverageDirection

            )
        {
            formationOrderPositionList = CollectFormationOrderPositions(formations, out averageOrderPosition, true, out weightedAverageDirection).ToList();
            oldOverallWidth = 0;
            minOverallWidth = 0;
            shouldFormationBeStackedWithPreviousFormation = new Dictionary<Formation, bool>();
            stacksRecord = new List<StackRecord>();
            var oldDragVec = new Vec2(weightedAverageDirection.y, -weightedAverageDirection.x);
            oldDragVec.Normalize();
            formationOrderPositionList.Sort((pair1, pair2) =>
            {
                // compare by left flank
                return GetLeftFlankPosition(pair1.Key, pair1.Value, oldDragVec).DotProduct(oldDragVec).CompareTo(GetLeftFlankPosition(pair2.Key, pair2.Value, oldDragVec).DotProduct(oldDragVec));
            });
            var currentStack = new StackRecord()
            {
                Formations = new List<Formation> { formationOrderPositionList[0].Key },
                LeftMost = GetLeftFlankPosition(formationOrderPositionList[0].Key, formationOrderPositionList[0].Value, oldDragVec).DotProduct(oldDragVec),
                RightMost = GetRightFlankPosition(formationOrderPositionList[0].Key, formationOrderPositionList[0].Value, oldDragVec).DotProduct(oldDragVec),
                MinimumWidth = GetFormationVirtualMinimumWidth(formationOrderPositionList[0].Key),
                MaximumWidth = GetFormationVirtualMaximumWidth(formationOrderPositionList[0].Key),
            };
            for (int i = 1; i < formationOrderPositionList.Count; ++i)
            {
                var currentFormation = formationOrderPositionList[i].Key;
                var currentFormationOrderPosition = formationOrderPositionList[i].Value;
                var actualOrCurrentWidth = GetActualOrCurrentWidth(currentFormation);
                if (ShouldFormationBeStackedTogether(currentStack, currentFormation, currentFormationOrderPosition, oldDragVec))
                {
                    shouldFormationBeStackedWithPreviousFormation[currentFormation] = true;
                    currentStack.MinimumWidth = MathF.Max(currentStack.MinimumWidth, GetFormationVirtualMinimumWidth(currentFormation));
                    currentStack.MaximumWidth = MathF.Max(currentStack.MaximumWidth, GetFormationVirtualMaximumWidth(currentFormation));
                    currentStack.LeftMost = MathF.Min(currentStack.LeftMost, GetLeftFlankPosition(currentFormation, currentFormationOrderPosition, oldDragVec).DotProduct(oldDragVec));
                    currentStack.RightMost = MathF.Max(currentStack.RightMost, GetRightFlankPosition(currentFormation, currentFormationOrderPosition, oldDragVec).DotProduct(oldDragVec));
                    currentStack.Formations.Add(currentFormation);
                }
                else
                {
                    oldOverallWidth += currentStack.Width;
                    minOverallWidth += currentStack.MinimumWidth;
                    stacksRecord.Add(currentStack);

                    currentStack = new StackRecord()
                    {
                        Formations = new List<Formation> { currentFormation },
                        LeftMost = GetLeftFlankPosition(currentFormation, currentFormationOrderPosition, oldDragVec).DotProduct(oldDragVec),
                        RightMost = GetRightFlankPosition(currentFormation, currentFormationOrderPosition, oldDragVec).DotProduct(oldDragVec),
                        MinimumWidth = GetFormationVirtualMinimumWidth(currentFormation),
                        MaximumWidth = GetFormationVirtualMaximumWidth(currentFormation),
                    };
                }
            }
            oldOverallWidth += currentStack.Width;
            minOverallWidth += currentStack.MinimumWidth;
            stacksRecord.Add(currentStack);
        }

        private static void SimulateNewOrderWithKeepingRelativePositionsNotLineShortNotKeepingFormationWidth(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition clickedCenter,
            WorldPosition? formationLineBegin,
            WorldPosition? formationLineEnd,
            bool isSimulatingAgentFrames,
            List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            out IEnumerable<Formation> remainingFormations)
        {
            simulationAgentFrames = !isSimulatingAgentFrames ? null : simulationAgentFrames;
            simulationFormationChanges = !isSimulatingFormationChanges ? null : simulationFormationChanges;

            var remainingFormationsList = new List<Formation>();
            remainingFormations = remainingFormationsList;

            CollectStacksRecord(formations, out var oldOverallWidth, out var minOverallWidth, out var shouldFormationBeStackedWithPreviousFormation, out var stacksRecord, out var formationOrderPositionList, out var averageOrderPosition, out var weightedAverageDirection);
            var formationOrderPostionDictionary = formationOrderPositionList.ToDictionary(pair => pair.Key, pair => pair.Value);
            var dragVec = formationLineEnd.Value.AsVec2 - formationLineBegin.Value.AsVec2;
            float dragLength = dragVec.Length;
            dragVec.Normalize();
            float availableWidthFromDragging = MathF.Max(0.0f, dragLength - (float)(formations.Count<Formation>() - shouldFormationBeStackedWithPreviousFormation.Count - 1) * 1.5f);
            bool isWidthApproximatelySame = availableWidthFromDragging.ApproximatelyEqualsTo(oldOverallWidth, 0.1f);

            Vec2 newOverallDirection = new Vec2(-dragVec.y, dragVec.x).Normalized();
            // sort formation by position

            float offset = 0f;
            Vec2 previousOldPosition = Vec2.Invalid;
            foreach (var stack in stacksRecord)
            {
                var formationsInStack = stack.Formations;
                var newStackWidth = MathF.Min(isWidthApproximatelySame ? stack.Width : (stack.Width * availableWidthFromDragging / oldOverallWidth), stack.MaximumWidth);
                // sort from front to rear
                formationsInStack.Sort((f1, f2) =>
                {
                    var f1OrderPosition = formationOrderPostionDictionary[f1];
                    var f2OrderPosition = formationOrderPostionDictionary[f2];
                    return f1OrderPosition.DotProduct(-weightedAverageDirection).CompareTo(f2OrderPosition.DotProduct(-weightedAverageDirection));
                });
                float? startPoint = null;
                foreach (var formation in formationsInStack)
                {
                    var oldOrderPosition = formationOrderPostionDictionary[formation];
                    if (!oldOrderPosition.IsValid)
                    {
                        remainingFormationsList.Add(formation);
                        continue;
                    }
                    int unitSpacingReduction = 0;
                    var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                    var actualOrCurrentWidth = GetActualOrCurrentWidth(formation);
                    var newFormationWidth = MathF.Max(MathF.Min(isWidthApproximatelySame ? actualOrCurrentWidth : availableWidthFromDragging * (actualOrCurrentWidth / oldOverallWidth), GetFormationVirtualMaximumWidth(formation)), GetFormationVirtualMinimumWidth(formation));
                    var newFormationDireciton = newOverallDirection;
                    Vec2 formationPositionVec2 = rotateVector(oldOrderPosition - averageOrderPosition, weightedAverageDirection, newOverallDirection) + clickedCenter.AsVec2;
                    if (startPoint == null)
                    {
                        startPoint = MathF.Clamp(formationPositionVec2.DotProduct(-newOverallDirection) - formationLineBegin.Value.AsVec2.DotProduct(-newOverallDirection), -20f, 10f);
                    }
                    formationPositionVec2 = formationLineBegin.Value.AsVec2 + startPoint.Value *  -newOverallDirection;
                    formationPositionVec2 += dragVec * (newStackWidth * 0.5f + offset);
                    WorldPosition formationPosition = clickedCenter;
                    formationPosition.SetVec2(formationPositionVec2);
                    if (isSimulatingFormationChanges)
                    {
                        LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, newFormationDireciton, null, newFormationWidth);
                        LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formations);
                    }

                    DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in newFormationDireciton, ref newFormationWidth, ref unitSpacingReduction, actualUnitSpacing);
                    SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in newFormationDireciton, newFormationWidth, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth, actualUnitSpacing);

                    var newUnitSpacing = MathF.Max(actualUnitSpacing - unitSpacingReduction, 0);
                    if (isSimulatingFormationChanges)
                    {
                        LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, newUnitSpacing, null);
                        LivePreviewFormationChanges.SetPreviewShape(formation, newFormationWidth, simulatedFormationDepth);
                    }
                    startPoint += simulatedFormationDepth + GetGapBetweenLinesOfFormation(formation, newUnitSpacing);
                }
                offset += newStackWidth + 1.5f;

            }
            //foreach (var pair in formationOrderPositionList)
            //{
            //    var formation = pair.Key;
            //    var oldOrderPosition = pair.Value;
            //    if (!oldOrderPosition.IsValid)
            //    {
            //        remainingFormationsList.Add(formation);
            //        continue;
            //    }
            //    int unitSpacingReduction = 0;
            //    var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
            //    var actualOrCurrentWidth = GetActualOrCurrentWidth(formation);


            //    var stackWidth = stacksRecord.First(s => s.Formations.Contains(formation)).Width * availableWidthFromDragging / oldOverallWidth;
            //    float formationWidth = MathF.Min(isWidthApproximatelySame ? actualOrCurrentWidth : MathF.Min((double)availableWidthFromDragging < (double)oldOverallWidth ? actualOrCurrentWidth : float.MaxValue, availableWidthFromDragging * (actualOrCurrentWidth / oldOverallWidth)), formation.MaximumWidth);
            //    Vec2 formationDirection = newOverallDirection;
            //    Vec2 formationPositionVec2 = rotateVector(oldOrderPosition - averageOrderPosition, weightedAverageDirection, newOverallDirection) + clickedCenter.AsVec2;

            //    formationPositionVec2 = formationLineBegin.Value.AsVec2 + (formationPositionVec2.DotProduct(formationDirection) - formationLineBegin.Value.AsVec2.DotProduct(formationDirection)) * formationDirection;
            //    if (!shouldFormationBeStackedWithPreviousFormation.TryGetValue(formation, out var shouldStack) || !shouldStack)
            //    {
            //        offset += previousWidth + 1.5f;
            //    }
            //    formationPositionVec2 += dragVec * (stackWidth * 0.5f + offset);
            //    WorldPosition formationPosition = clickedCenter;
            //    formationPosition.SetVec2(formationPositionVec2);
            //    if (isSimulatingFormationChanges)
            //    {
            //        LivePreviewFormationChanges.UpdateFormationChange(formation, formationPositionVec2, formationDirection, null, null);
            //        LatestOrderInQueueChanges.UpdateFormationChange(formation, formationPositionVec2, formationDirection, null, null);
            //        LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, null, formationWidth);
            //        LatestOrderInQueueChanges.UpdateFormationChange(formation, null, null, null, formationWidth);
            //    }

            //    DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
            //    SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth, actualUnitSpacing);

            //    if (isSimulatingFormationChanges)
            //    {
            //        var unitSpacing = MathF.Max(GetActualOrCurrentUnitSpacing(formation) - unitSpacingReduction, 0);
            //        LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, unitSpacing, null);
            //        LatestOrderInQueueChanges.UpdateFormationChange(formation, null, null, unitSpacing, null);
            //    }
            //    previousOldPosition = oldOrderPosition;
            //    previousWidth = stackWidth;
            //}
        }

        private static bool ShouldFormationBeStackedTogether(StackRecord stackRecord, Formation formation, Vec2 orderPosition, Vec2 dragVec)
        {
            return MathF.Abs(stackRecord.Center - orderPosition.DotProduct(dragVec)) < MathF.Max(stackRecord.Width, GetActualOrCurrentWidth(formation)) * 0.5f;
        }

        private static void GetFormationLineBeginEnd(Formation formation, WorldPosition formationLineBegin, out WorldPosition begin, out WorldPosition end)
        {
            float actualorCurrentWidth = GetActualOrCurrentWidth(formation);
            Vec2 direction = GetFormationVirtualDirection(formation);
            direction.RotateCCW(-1.57079637f);
            double num2 = (double)direction.Normalize();
            end = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 - actualorCurrentWidth / 2f * direction, formationLineBegin);
            begin = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 + actualorCurrentWidth / 2f * direction, formationLineBegin);
        }

        private static void SimulateNewOrderWithHorizontalLayout(IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            bool isLineShort,
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
            float minimumWidth = formations.Max((Formation f) => GetFormationVirtualMinimumWidth(f));
            if (num < minimumWidth)
            {
                num = minimumWidth;
            }
            Vec2 formationDirection = new Vec2(0f - vec.y, vec.x).Normalized();
            float num3 = 0f;
            // sort to keep consistent with actual order issued.
            formations = SortFormationsForHorizontalLayout(formations);
            foreach (Formation formation in formations)
            {
                float formationWidth = num;
                formationWidth = MathF.Min(Utilities.Utility.ShouldKeepFormationWidth() ? GetActualOrCurrentWidth(formation) : formationWidth, GetFormationVirtualMaximumWidth(formation));
                WorldPosition formationPosition = formationLineBegin;
                var formationPositionVec2 = (formationLineEnd.AsVec2 + formationLineBegin.AsVec2) * 0.5f - formationDirection * num3;
                formationPosition.SetVec2(formationPositionVec2);


                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, formationDirection, null, null);
                }
                if (isSimulatingFormationChanges && !isLineShort)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, null, formationWidth);
                    LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formations);
                }
                int unitSpacingReduction = 0;
                // override official code from using formation.UnitSpacing to using GetActualUnitSpacing(formation)
                var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, simulateFormationDepth: true, out var simulatedFormationDepth, actualUnitSpacing);
                if (isSimulatingFormationChanges && !isLineShort)
                {
                    var unitSpacing = MathF.Max(GetActualOrCurrentUnitSpacing(formation) - unitSpacingReduction, 0);
                    LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, unitSpacing, null);
                    LivePreviewFormationChanges.SetPreviewShape(formation, formationWidth, simulatedFormationDepth);
                }
                num3 += simulatedFormationDepth + GetGapBetweenLinesOfFormation(formation, actualUnitSpacing - unitSpacingReduction);
            }
        }

        private static void SimulateNewOrderWithVerticalLayout(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            bool isLineShort,
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
            float minOverallWidth = formations.Sum<Formation>((Func<Formation, float>)(f => GetFormationVirtualMinimumWidth(f)));
            Vec2 formationDirection = new Vec2(-dragVec.y, dragVec.x).Normalized();
            float num3 = 0.0f;
            foreach (Formation formation in formations)
            {
                float minimumWidth = GetFormationVirtualMinimumWidth(formation);
                var actualOrCurrentWidth = GetActualOrCurrentWidth(formation);
                float formationWidth = MathF.Min(isWidthApproximatelySame || Utilities.Utility.ShouldKeepFormationWidth() ? actualOrCurrentWidth : MathF.Min((double)availableWidthFromDragging < (double)oldOverallWidth ? actualOrCurrentWidth : float.MaxValue, availableWidthFromDragging * (minimumWidth / minOverallWidth)), GetFormationVirtualMaximumWidth(formation));
                WorldPosition formationPosition = formationLineBegin;
                var formationPositionVec2 = formationPosition.AsVec2 + dragVec * (formationWidth * 0.5f + num3);
                formationPosition.SetVec2(formationPositionVec2);

                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, formationDirection, null, null);
                }
                if (isSimulatingFormationChanges && !isLineShort)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, null, formationWidth);
                    LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formations);
                }
                int unitSpacingReduction = 0;
                // override official code from using formation.UnitSpacing to using GetActualUnitSpacing(formation)
                var actualUnitSpacing = GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in formationDirection, ref formationWidth, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in formationDirection, formationWidth, unitSpacingReduction, true, out float simulatedFormationDepth, actualUnitSpacing);
                if (isSimulatingFormationChanges && !isLineShort)
                {
                    var unitSpacing = MathF.Max(GetActualOrCurrentUnitSpacing(formation) - unitSpacingReduction, 0);
                    LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, unitSpacing, null);
                    LivePreviewFormationChanges.SetPreviewShape(formation, formationWidth, simulatedFormationDepth);
                }
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
            bool hasAnyMountUnit = formation.CalculateHasSignificantNumberOfMounted && GetFormationVirtualRidingOrder(formation) != OrderType.Dismount;
            _overridenHasAnyMountedUnit.SetValue(formation, hasAnyMountUnit);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, hasAnyMountUnit);
            int unitIndex = formation.CountOfUnitsWithoutDetachedOnes - 1;
            float actualWidth = formationWidth;
            if (unitIndex >= 0)
            {
                do
                {
                    GetUnitPositionWithIndexAccordingToNewOrder(formation, simulationFormation, GetFormationVirtualArrangementOrder(formation), null, unitIndex, in formationPosition, in formationDirection, formation.Arrangement, formationWidth, actualUnitSpacing - unitSpacingReduction, formation.Arrangement.UnitCount, formation.HasAnyMountedUnit, formation.Index, out var unitSpawnPosition, out var _, out actualWidth);
                    if (unitSpawnPosition.HasValue)
                    {
                        break;
                    }
                    unitSpacingReduction++;
                }
                while (actualUnitSpacing - unitSpacingReduction >= 0);
            }
            unitSpacingReduction = MathF.Min(unitSpacingReduction, actualUnitSpacing);
            //if (unitSpacingReduction > 0)
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
            bool hasAnyMountUnit = formation.CalculateHasSignificantNumberOfMounted && GetFormationVirtualRidingOrder(formation) != OrderType.Dismount;
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
                    GetUnitPositionWithIndexAccordingToNewOrder(formation, simulationFormation, GetFormationVirtualArrangementOrder(formation), null, unitIndex, in formationPosition, in formationDirection, formation.Arrangement, formationWidth, actualUnitSpacing - unitSpacingReduction, formation.Arrangement.UnitCount, formation.HasAnyMountedUnit, formation.Index, out unitSpawnPosition, out unitSpawnDirection, out _);
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
                WorldPosition item = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
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
            float num = 1f;
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
            if (!Utilities.Utility.ShouldEnablePlayerOrderControllerPatchForFormation(formations))
                return true;
            if (Utilities.Utility.IsAnyFormationHavingMovingOrderPostion(formations))
            {
                var formationCount = 0;
                Vec2 averageOrderPosition = Vec2.Zero;
                foreach (var formation in formations)
                {
                    if (Utilities.Utility.IsFormationOrderPositionMoving(formation))
                    {
                        var movingTarget = Utilities.Utility.GetFormationMovingOrderPosition(formation);
                        if (movingTarget != null)
                        {
                            averageOrderPosition += movingTarget.Value.AsVec2;
                            formationCount++;
                            continue;
                        }
                    }
                    var orderPosition = GetFormationVirtualPositionVec2(formation);
                    if (orderPosition.IsValid)
                    {
                        averageOrderPosition += orderPosition;
                        formationCount++;
                    }
                }
                if (formationCount > 0)
                {
                    averageOrderPosition = averageOrderPosition * 1f / (float)formationCount;
                    __result = (target - averageOrderPosition).Normalized();
                    return false;
                }
            }
            if (Utilities.Utility.ShouldLockFormation())
            {
                var formationCount = 0;
                Vec2 averageOrderPosition = Vec2.Zero;
                foreach (var formation in formations)
                {
                    var orderPosition = GetFormationVirtualPositionVec2(formation);
                    if (orderPosition.IsValid)
                    {
                        averageOrderPosition += orderPosition;
                        formationCount++;
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

            var selectedFormations = formations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            if (!Utilities.Utility.ShouldEnablePlayerOrderControllerPatchForFormation(selectedFormations))
                return true;

            var formationsWithLocking = new List<Formation> { };
            var formationsWithoutLocking = new List<Formation> { };
            foreach (var formation in selectedFormations)
            {
                if (Utilities.Utility.ShouldLockFormationDuringLookAtDirection(formation))
                {
                    formationsWithLocking.Add(formation);
                }
                else
                {
                    formationsWithoutLocking.Add(formation);
                }
            }
            if (formationsWithoutLocking.Count > 0)
            {
                foreach (var formation in formationsWithoutLocking)
                {
                    if (Utilities.Utility.IsFormationOrderPositionMoving(formation))
                    {
                        var movingPosition = Utilities.Utility.GetFormationMovingOrderPosition(formation);
                        var movingDirection = Utilities.Utility.GetFormationMovingDirection(formation);
                        LivePreviewFormationChanges.UpdateFormationChange(formation, movingPosition, movingDirection.IsValid ? movingDirection : (Vec2?)null, null, null);
                    }
                    if (LivePreviewFormationChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                    {
                        if (formationChange.MovementOrderType == OrderType.Advance)
                        {
                            var targetFormation = formationChange.TargetFormation;
                            var advancePosition = GetAdvanceOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.NavMeshVec3, targetFormation);

                            LivePreviewFormationChanges.UpdateFormationChange(formation, advancePosition, null, null, null);
                        }
                    }
                }
                SimulateNewFacingOrderWithoutLockingFormations(formationsWithoutLocking,
                    simulationFormations,
                    direction,
                    true,
                    out var agentFrames,
                    false,
                    out _);
                if (simulationAgentFrames == null)
                {
                    simulationAgentFrames = agentFrames;
                }
                else
                {
                    simulationAgentFrames.AddRange(agentFrames);
                }
            }
            if (formationsWithLocking.Count > 0)
            {
                SimulateNewFacingOrderWithLockingFormations(formationsWithLocking,
                    simulationFormations,
                    direction,
                    true,
                    out var agentFrames,
                    false,
                    out _);
                if (simulationAgentFrames == null)
                {
                    simulationAgentFrames = agentFrames;
                }
                else
                {
                    simulationAgentFrames.AddRange(agentFrames);
                }
            }
            return false;
        }



        public static bool Prefix_SetOrderWithPosition(OrderController __instance, OrderType orderType, WorldPosition orderPosition)
        {
            if (__instance != Mission.Current?.PlayerTeam?.PlayerOrderController)
            {
                return true;
            }

            if (orderType == OrderType.LookAtDirection)
            {
                var formationsWithLocking = new List<Formation> { };
                var formationsWithoutLocking = new List<Formation> { };
                foreach (var formation in __instance.SelectedFormations)
                {
                    if (Utilities.Utility.ShouldLockFormationDuringLookAtDirection(formation))
                    {
                        formationsWithLocking.Add(formation);
                    }
                    else
                    {
                        formationsWithoutLocking.Add(formation);
                    }
                }
                if (formationsWithLocking.Count > 0)
                {
                    SimulateNewFacingOrderWithLockingFormations(formationsWithLocking,
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
                if (formationsWithoutLocking.Count > 0)
                {
                    var direction = OrderController.GetOrderLookAtDirection(__instance.SelectedFormations, orderPosition.AsVec2);
                    FacingOrder facingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                    foreach (var formation in formationsWithoutLocking)
                    {
                        formation.FacingOrder = facingOrder;
                        LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                        LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formation);
                    }
                }
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpile_SetOrderWithPosition(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            // remove the code setting FacingOrder in LookAtDirection order to avoid overritting to Prefix_SetOrderWithPosition.
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
            out Vec2 weightedAverageOrderPosition,
            bool collectDirection,
            out Vec2 weightedAverageDirection)
        {
            var formationOrderPositionDictionary = new Dictionary<Formation, Vec2>();
            //var remainingFormationList = new List<Formation>();
            //var formationCount = 0;
            int sumOfUnits = 0;
            weightedAverageOrderPosition = Vec2.Zero;
            weightedAverageDirection = Vec2.Zero;
            foreach (var formation in formations)
            {
                var orderPosition = GetFormationVirtualPositionVec2(formation);
                if (orderPosition.IsValid)
                {
                    weightedAverageOrderPosition += orderPosition * formation.CountOfUnitsWithoutDetachedOnes;
                    //formationCount++;
                    sumOfUnits += formation.CountOfUnitsWithoutDetachedOnes;
                }
                formationOrderPositionDictionary.Add(formation, orderPosition);
            }
            if (sumOfUnits > 0)
            {
                weightedAverageOrderPosition = weightedAverageOrderPosition * 1f / sumOfUnits;
            }
            if (collectDirection)
            {
                foreach (var pair in formationOrderPositionDictionary)
                {
                    var formation = pair.Key;
                    var orderPositionVec2 = pair.Value;
                    if (orderPositionVec2.IsValid)
                    {
                        weightedAverageDirection += GetFormationVirtualDirection(formation) * (1 / MathF.Max(5f, orderPositionVec2.DistanceSquared(weightedAverageOrderPosition)));
                    }
                }
                weightedAverageDirection.Normalize();
            }
            return formationOrderPositionDictionary;
        }

        public static void SimulateNewFacingOrderWithoutLockingFormations(
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

            foreach (var formation in formations)
            {
                Vec2 newPositionVec2 = GetFormationVirtualPositionVec2(formation);
                Vec2 newDirection = direction;
                float width = GetFormationVirtualWidth(formation) ?? GetActualOrCurrentWidth(formation);
                WorldPosition formationPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
                formationPosition.SetVec2(newPositionVec2);

                if (isSimulatingFormationChanges)
                {
                    // should we update formationPosition?
                    LivePreviewFormationChanges.UpdateFormationChange(formation, /*formationPosition*/null, newDirection, null, null);
                    LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formation);
                }
                int unitSpacingReduction = 0;
                var actualUnitSpacing = GetFormationVirtualUnitSpacing(formation) ?? GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in newDirection, ref width, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in newDirection, width, unitSpacingReduction, true, out var simulatedFormationDepth, actualUnitSpacing);
                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.SetPreviewShape(formation, width, simulatedFormationDepth);
                }
            }
        }

        public static void SimulateNewFacingOrderWithLockingFormations(
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
                Vec2 newPositionVec2 = rotateVector(orderPositionVec2 - averageOrderPosition, weightedAverageDirection, direction) + averageOrderPosition;
                Vec2 newDirection = rotateVector(GetFormationVirtualDirection(formation), weightedAverageDirection, direction);
                float width = GetFormationVirtualWidth(formation) ?? GetActualOrCurrentWidth(formation);
                WorldPosition formationPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
                formationPosition.SetVec2(newPositionVec2);

                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.UpdateFormationChange(formation, formationPosition, newDirection, null, null);
                    LivePreviewFormationChanges.SetFacingOrder(OrderType.LookAtDirection, formation);
                }

                if (!Mission.Current.IsPositionInsideBoundaries(formationPosition.AsVec2))
                {
                    Vec2 boundaryPosition = Mission.Current.GetClosestBoundaryPosition(formationPosition.AsVec2);
                    formationPosition.SetVec2(boundaryPosition);
                }
                int unitSpacingReduction = 0;
                var actualUnitSpacing = GetFormationVirtualUnitSpacing(formation) ?? GetActualOrCurrentUnitSpacing(formation);
                DecreaseUnitSpacingAndWidthIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), in formationPosition, in newDirection, ref width, ref unitSpacingReduction, actualUnitSpacing);
                SimulateNewOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), simulationAgentFrames, simulationFormationChanges, in formationPosition, in newDirection, width, unitSpacingReduction, true, out var simulatedFormationDepth, actualUnitSpacing);
                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.SetPreviewShape(formation, width, simulatedFormationDepth);
                }
            }
        }


        public static void SimulateAgentFrames(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            out List<WorldPosition> simulationAgentFrames)
        {
            simulationAgentFrames = new List<WorldPosition>();
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges = null;
            var formationOrderPositionDictionary = CollectFormationOrderPositions(formations, out var averageOrderPosition, true, out var weightedAverageDirection);


            foreach (var formation in formations)
            {
                var formationPosition = GetFormationVirtualPosition(formation);
                var newDirection = GetFormationVirtualDirection(formation);
                float width = GetFormationVirtualWidth(formation) ?? GetActualOrCurrentWidth(formation);
                var actualUnitSpacing = GetFormationVirtualUnitSpacing(formation) ?? GetActualOrCurrentUnitSpacing(formation);
                int unitSpacingReduction = 0;
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
        public static Vec2 GetFormationVirtualPositionVec2(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].Position != null ? LivePreviewFormationChanges.VirtualChanges[formation].Position.Value : formation.OrderPosition.IsValid ? formation.OrderPosition : formation.CurrentPosition;
        }

        public static WorldPosition GetFormationVirtualPosition(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].WorldPosition != null ? LivePreviewFormationChanges.VirtualChanges[formation].WorldPosition.Value : formation.OrderPosition.IsValid ? formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3) : GetFormationCurrentPositionAsWorldPosition(formation);
        }

        private static WorldPosition GetFormationCurrentPositionAsWorldPosition(Formation formation)
        {
            var result = formation.QuerySystem.MedianPosition;
            var vec2 = formation.CurrentPosition;
            result.SetVec2(vec2);
            return result;
        }

        public static Vec2 GetFormationVirtualDirection(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].Direciton != null ? LivePreviewFormationChanges.VirtualChanges[formation].Direciton.Value : formation.Direction;
        }

        public static Vec2 GetFormationVirtualDirectionIncludingFacingEnemy(Formation formation)
        {
            if (GetFormationVirtualFacingOrder(formation) == OrderType.LookAtEnemy)
            {
                return GetVirtualDirectionOfFacingEnemy(formation);
            }
            return GetFormationVirtualDirection(formation);
        }

        public static Vec2 GetFormationVirtualDirectionWhenFollowingAgent(Formation formation, Agent targetAgent)
        {
            if (formation.PhysicalClass.IsMounted() && targetAgent != null)
            {
                Vec3 velocity = targetAgent.Velocity;
                if ((double)velocity.LengthSquared > (double)targetAgent.RunSpeedCached * (double)targetAgent.RunSpeedCached * 0.090000003576278687)
                {
                    velocity = targetAgent.Velocity;
                    return velocity.AsVec2.Normalized();
                }
            }
            return GetFormationVirtualDirection(formation);
        }

        private static void TryIntializeFormationChanges(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.TryGetValue(formation, out var change);
            if (!hasFormation || change.Position == null)
            {
                LivePreviewFormationChanges.UpdateFormationChange(formation, formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3), null, null, null);
            }
            if (!hasFormation || change.Direciton == null)
            {
                LivePreviewFormationChanges.UpdateFormationChange(formation, null, formation.Direction, null, null);
            }
        }

        public static int? GetFormationVirtualUnitSpacing(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].UnitSpacing != null ? LivePreviewFormationChanges.VirtualChanges[formation].UnitSpacing : null;
        }

        public static int GetFormationVirtualNaturalUnitSpacing(Formation formation)
        {
            var arrrangementOrder = GetFormationVirtualArrangementOrder(formation);
            return ArrangementOrder.GetUnitSpacingOf(arrrangementOrder);
        }

        public static float? GetFormationVirtualWidth(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].Width != null ? LivePreviewFormationChanges.VirtualChanges[formation].Width : null;
        }

        public static void SetFormationVirtualWidth(Formation formation, float width)
        {
            LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, null, width);
        }

        public static WorldPosition GetFormationVirtualMedianPosition(Formation formation)
        {
            var orderPosition = GetFormationVirtualPosition(formation);
            var direction = GetFormationVirtualDirection(formation);
            orderPosition.SetVec2(orderPosition.AsVec2 + direction.TransformToParentUnitF(formation.OrderLocalAveragePosition));
            return orderPosition;
        }

        public static Vec2 GetFormationVirtualAveragePositionVec2(Formation formation)
        {
            var orderPosition = GetFormationVirtualPositionVec2(formation);
            var direction = GetFormationVirtualDirection(formation);
            return orderPosition + direction.TransformToParentUnitF(formation.OrderLocalAveragePosition);
        }
        public static void GetFormationVirtualShape(Formation formation, out float width, out float depth, out float rightSideOffset)
        {
            if (LivePreviewFormationChanges.VirtualChanges.TryGetValue(formation, out var change))
            {
                if (change.PreviewWidth != null && change.PreviewDepth != null)
                {
                    width = change.PreviewWidth.Value;
                    depth = change.PreviewDepth.Value;
                    rightSideOffset = GetRightSideOffset(formation);
                    return;
                }
            }
            width = formation.Width;
            depth = formation.Depth;
            rightSideOffset = GetRightSideOffset(formation);
        }

        private static float GetRightSideOffset(Formation formation)
        {
            if (formation.Arrangement.RankCount <= 1)
                return 0;
            var arrangementOrder = GetFormationVirtualArrangementOrder(formation);
            if (arrangementOrder == ArrangementOrder.ArrangementOrderEnum.Line ||
                arrangementOrder == ArrangementOrder.ArrangementOrderEnum.Loose ||
                arrangementOrder == ArrangementOrder.ArrangementOrderEnum.ShieldWall)
            {
                return (Utilities.Utility.GetFormationInterval(formation, GetFormationVirtualUnitSpacing(formation) ?? formation.UnitSpacing) + formation.UnitDiameter) / 2;
            }
            return 0;
        }

        public static OrderType GetFormationVirtualMovementorder(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].MovementOrderType != null ? LivePreviewFormationChanges.VirtualChanges[formation].MovementOrderType.Value : formation.GetReadonlyMovementOrderReference().OrderType;
        }

        public static OrderType GetFormationVirtualFacingOrder(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].FacingOrderType != null ? LivePreviewFormationChanges.VirtualChanges[formation].FacingOrderType.Value : formation.FacingOrder.OrderType;
        }

        public static ArrangementOrder.ArrangementOrderEnum GetFormationVirtualArrangementOrder(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].ArrangementOrder != null ? LivePreviewFormationChanges.VirtualChanges[formation].ArrangementOrder.Value : formation.ArrangementOrder.OrderEnum;
        }

        private static FormationQuerySystem GetTargetOrClosestEnemyFormationQuerySystem(Formation f, Formation targetFormation)
        {
            return targetFormation?.QuerySystem ?? (CommandQuerySystem.GetQueryForFormation(f).ClosestEnemyFormation?.QuerySystem);
        }

        public static Formation GetFormationVirtualTargetFormation(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].TargetFormation != null ? LivePreviewFormationChanges.VirtualChanges[formation].TargetFormation : null;
        }

        public static Agent GetFormationVirtualTargetAgent(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].TargetAgent != null ? LivePreviewFormationChanges.VirtualChanges[formation].TargetAgent : null;
        }

        public static OrderType GetFormationVirtualRidingOrder(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation && LivePreviewFormationChanges.VirtualChanges[formation].RidingOrderType != null ? LivePreviewFormationChanges.VirtualChanges[formation].RidingOrderType.Value: formation.RidingOrder.OrderType;
        }


        public static void SaveFormationLivePositionForPreview(Formation formation, WorldPosition? medianPosition)
        {
            _currentMovingTarget[formation] = new MovingTarget { MedianPosition = medianPosition};
        }

        public static void ClearFormationLivePositionForPreview(Formation formation)
        {
            if (_currentMovingTarget.ContainsKey(formation))
            {
                _currentMovingTarget.Remove(formation);
            }
        }

        public static void GetFormationMovingTargetForPreview(Formation formation, out WorldPosition? medianPosition, WorldPosition? defaultPosition = null)
        {
            if (_currentMovingTarget.TryGetValue(formation, out var movingTarget))
            {
                medianPosition = movingTarget.MedianPosition ?? defaultPosition;
            }
            else
            {
                medianPosition = defaultPosition;
            }
        }

        public static Vec2 GetAdvanceOrFallbackEnemyDirection(Formation f, Formation targetFormation)
        {
            var enemyQuerySystem = GetTargetOrClosestEnemyFormationQuerySystem(f, targetFormation);
            GetFormationMovingTargetForPreview(f, out var medianPosition);
            return enemyQuerySystem == null ? Vec2.Forward : (enemyQuerySystem.MedianPosition.AsVec2 - (medianPosition?.AsVec2 ?? f.QuerySystem.AveragePosition)).Normalized();
        }

        public static WorldPosition GetAdvanceOrderPosition(Formation f, WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache, Formation targetFormation)
        {
            var querySystem = f.QuerySystem;
            var enemyQuerySystem = GetTargetOrClosestEnemyFormationQuerySystem(f, targetFormation);
            WorldPosition targetPosition;
            if (enemyQuerySystem == null)
            {
                var commandQuerySystem = CommandQuerySystem.GetQueryForFormation(f);
                Agent closestEnemyAgent;
                    closestEnemyAgent = commandQuerySystem.ClosestEnemyAgent;
                if (closestEnemyAgent == null)
                    return f.CreateNewOrderWorldPosition(worldPositionEnforcedCache);
                targetPosition = closestEnemyAgent.GetWorldPosition();
            }
            else
            {
                targetPosition = enemyQuerySystem.MedianPosition;
            }
            if (querySystem.IsRangedFormation || querySystem.IsRangedCavalryFormation || querySystem.HasThrowing)
            {
                Vec2 directionAux = GetAdvanceOrFallbackEnemyDirection(f, targetFormation);
                targetPosition.SetVec2(targetPosition.AsVec2 - directionAux * querySystem.MissileRangeAdjusted);
            }
            else if (enemyQuerySystem != null)
            {
                GetFormationMovingTargetForPreview(f, out var medianPosition);
                Vec2 vec2 = (enemyQuerySystem.AveragePosition - (medianPosition?.AsVec2 ?? f.QuerySystem.AveragePosition)).Normalized();
                float num = 2f;
                if ((double)enemyQuerySystem.FormationPower < (double)f.QuerySystem.FormationPower * 0.20000000298023224)
                    num = 0.1f;
                targetPosition.SetVec2(targetPosition.AsVec2 - vec2 * num);
            }
            return targetPosition;
        }

        public static WorldPosition GetFallbackOrderPosition(Formation f, WorldPosition.WorldPositionEnforcedCache worldPositionEnforcedCache, Formation targetFormation)
        {
            Vec2 direction = GetAdvanceOrFallbackEnemyDirection(f, targetFormation);
            GetFormationMovingTargetForPreview(f, out var medianPosition);
            var averagePosition = medianPosition ?? f.QuerySystem.MedianPosition;
            averagePosition.SetVec2((medianPosition?.AsVec2 ?? f.QuerySystem.AveragePosition) - direction * 7f);
            return averagePosition;
        }

        public static Vec2 GetVirtualDirectionOfFacingEnemy(Formation f)
        {
            var targetAgent = GetFormationVirtualTargetAgent(f);
            if (f.PhysicalClass.IsMounted() && targetAgent != null)
            {
                Vec3 velocity = targetAgent.Velocity;
                if (velocity.LengthSquared > targetAgent.RunSpeedCached * targetAgent.RunSpeedCached * 0.090000003576278687)
                {
                    velocity = targetAgent.Velocity;
                    return velocity.AsVec2.Normalized();
                }
            }
            var arrangementOrder = GetFormationVirtualArrangementOrder(f);
            if (arrangementOrder == ArrangementOrder.ArrangementOrderEnum.Circle || arrangementOrder == ArrangementOrder.ArrangementOrderEnum.Square)
                return f.Direction;
            var targetFormation = GetVirtualFacingEnemyTargetFormation(f);
            if (targetFormation != null)
            {
                return Patch_FacingOrder.GetVirtualDirectionFacingToEnemyFormation(f, targetFormation);
            }
            var averageEnemyPosition = CommandQuerySystem.GetQueryForFormation(f).VirtualWeightedAverageEnemyPosition;
            return Patch_FacingOrder.GetDirectionFacingToEnemy(f, GetFormationVirtualPositionVec2(f), GetFormationVirtualDirection(f), averageEnemyPosition);
        }

        public static WorldPosition GetFollowOrderPosition(Formation f, Agent targetAgent)
        {
            float speed = targetAgent.GetCurrentVelocity().Length;
            var offset = Vec2.Zero;
            var lastPosition = targetAgent.GetWorldPosition();
            var worldPosition = lastPosition;
            if (speed < 0.01f || speed < targetAgent.Monster.WalkingSpeedLimit * 0.7f)
            {
                // stop or depart
                if (targetAgent.MountAgent != null)
                {
                    offset += f.Direction * -2f;
                }
                worldPosition.SetVec2(worldPosition.AsVec2 - f.GetMiddleFrontUnitPositionOffset() + offset);
                var distanceThreshold = f.PhysicalClass.IsMounted() ? 4f : 2.5f;
                if (Mission.Current.IsTeleportingAgents || worldPosition.AsVec2.DistanceSquared(lastPosition.AsVec2) > distanceThreshold * distanceThreshold)
                {
                    lastPosition = worldPosition;
                }
            }
            else
            {
                // move
                if (f.PhysicalClass.IsMounted())
                {
                    offset += 2f * targetAgent.Velocity.AsVec2;
                }
                worldPosition.SetVec2(worldPosition.AsVec2 - f.GetMiddleFrontUnitPositionOffset() + offset);
                lastPosition = worldPosition;
            }

            return lastPosition;
        }

        public static Vec2 GetFollowEntityDirection(Formation f, GameEntity gameEntity)
        {
            return gameEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
        }
        public static WorldPosition GetFollowEntityOrderPosition(Formation f, GameEntity targetEntity)
        {
            WorldPosition result = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, targetEntity.GlobalPosition, hasValidZ: false);
            result.SetVec2(result.AsVec2);
            return result;
        }

        // Copied from MovementOrder
        public static  WorldPosition GetAttackEntityWaitPosition(
          Formation formation,
          GameEntity targetEntity)
        {
            Scene scene = formation.Team.Mission.Scene;
            WorldPosition worldPosition = new WorldPosition(scene, UIntPtr.Zero, targetEntity.GlobalPosition, false);
            Vec2 vec2_1 = formation.QuerySystem.AveragePosition - worldPosition.AsVec2;
            Vec2 v = targetEntity.GetGlobalFrame().rotation.f.AsVec2.Normalized();
            Vec2 vec2_2 = (double)vec2_1.DotProduct(v) >= 0.0 ? v : -v;
            WorldPosition position1 = worldPosition;
            position1.SetVec2(worldPosition.AsVec2 + vec2_2 * 3f);
            if (scene.DoesPathExistBetweenPositions(position1, formation.QuerySystem.MedianPosition))
                return position1;
            WorldPosition position2 = worldPosition;
            position2.SetVec2(worldPosition.AsVec2 - vec2_2 * 3f);
            if (scene.DoesPathExistBetweenPositions(position2, formation.QuerySystem.MedianPosition))
                return position2;
            WorldPosition position3 = worldPosition;
            position3.SetVec2(worldPosition.AsVec2 + targetEntity.GetGlobalFrame().rotation.s.AsVec2.Normalized() * 3f);
            if (scene.DoesPathExistBetweenPositions(position3, formation.QuerySystem.MedianPosition))
                return position3;
            WorldPosition position4 = worldPosition;
            position4.SetVec2(worldPosition.AsVec2 - targetEntity.GetGlobalFrame().rotation.s.AsVec2.Normalized() * 3f);
            return scene.DoesPathExistBetweenPositions(position4, formation.QuerySystem.MedianPosition) ? position4 : position1;
        }

        public static void FillOrderLookingAtPosition(OrderInQueue order, OrderController orderController, MissionScreen missionScreen)
        {
            var selectedFormations = orderController.SelectedFormations.Where(f => f.CountOfUnitsWithoutDetachedOnes > 0).ToList();
            var formationsWithLocking = new List<Formation> { };
            var formationsWithoutLocking = new List<Formation> { };
            foreach (var formation in selectedFormations)
            {
                if (Utilities.Utility.ShouldLockFormationDuringLookAtDirection(formation))
                {
                    formationsWithLocking.Add(formation);
                }
                else
                {
                    formationsWithoutLocking.Add(formation);
                }
            }
            if (formationsWithLocking.Count > 0)
            {
                SimulateNewFacingOrderWithLockingFormations(formationsWithLocking,
                    orderController.simulationFormations,
                    OrderController.GetOrderLookAtDirection(selectedFormations, missionScreen.GetOrderFlagPosition().AsVec2),
                    false,
                    out _,
                    true,
                    out var simulationFormationChanges);
                order.ActualFormationChanges.AddRange(simulationFormationChanges);
                var changes = LivePreviewFormationChanges.CollectChanges(formationsWithLocking);
                foreach (var change in changes)
                {
                    order.ShouldLockFormationInFacingOrder[change.Key] = true;
                    order.VirtualFormationChanges[change.Key] = change.Value;
                }
            }
            if (formationsWithoutLocking.Count > 0)
            {
                order.PositionBegin = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, missionScreen.GetOrderFlagPosition(), false);
                SimulateNewFacingOrderWithoutLockingFormations(formationsWithoutLocking,
                    orderController.simulationFormations,
                    OrderController.GetOrderLookAtDirection(selectedFormations, missionScreen.GetOrderFlagPosition().AsVec2),
                    false,
                    out _,
                    true,
                    out var simulationFormationChanges);
                order.ActualFormationChanges.AddRange(simulationFormationChanges);
                var changes = LivePreviewFormationChanges.CollectChanges(formationsWithoutLocking);
                foreach (var change in changes)
                {
                    order.ShouldLockFormationInFacingOrder[change.Key] = false;
                    order.VirtualFormationChanges[change.Key] = change.Value;
                }
            }
        }

        public static void SimulateNewArrangementOrder(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            ArrangementOrder.ArrangementOrderEnum newArrangementOrder,
            bool isSimulatingAgentFrames,
            out List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
            simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());

            foreach (var formation in formations)
            {
                var oldArrangementOrder = GetFormationVirtualArrangementOrder(formation);
                Vec2 positionVec2 = GetFormationVirtualPositionVec2(formation);
                Vec2 direction = GetFormationVirtualDirectionIncludingFacingEnemy(formation);
                //var actualUnitSpacing = GetFormationVirtualUnitSpacing(formation) ?? GetActualOrCurrentUnitSpacing(formation);
                var actualUnitSpacing = ArrangementOrder.GetUnitSpacingOf(newArrangementOrder);
                float width = GetNewWidthOfArrangementOrder(formation, oldArrangementOrder, newArrangementOrder, actualUnitSpacing);
                WorldPosition formationPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
                formationPosition.SetVec2(positionVec2);

                DecreaseUnitSpacingAndWidthWithNewArrangementOrderIfNotAllUnitsFit(formation, GetSimulationFormation(formation, simulationFormations), oldArrangementOrder, newArrangementOrder, in formationPosition, in direction, ref width, out var unitSpacingReduction, actualUnitSpacing);
                SimulateNewArrangementOrderWithFrameAndWidth(formation, GetSimulationFormation(formation, simulationFormations), oldArrangementOrder, newArrangementOrder, simulationAgentFrames, simulationFormationChanges, in formationPosition, in direction, width, unitSpacingReduction, true, out var simulatedFormationDepth, actualUnitSpacing);
                if (isSimulatingFormationChanges)
                {
                    LivePreviewFormationChanges.SetArrangementOrder(newArrangementOrder, new List<Formation> { formation });
                    LivePreviewFormationChanges.UpdateFormationChange(formation, null, null, actualUnitSpacing - unitSpacingReduction, width);
                    LivePreviewFormationChanges.SetPreviewShape(formation, width, simulatedFormationDepth);
                }
            }
        }

        //public static float GetNewWidthOfArrangementChange(Formation formation,
        //    IFormationArrangement oldArrangement,
        //    ArrangementOrder.ArrangementOrderEnum newArrangementOrder)
        //{
        //    var oldWidth = GetFormationVirtualWidth(formation) ?? formation.Width;
        //    var oldArrangementType = oldArrangement.GetType();
        //    var newArrangementType = Utilities.Utility.GetTypeOfArrangement(newArrangementOrder, true);
        //    var oldArrangementOrder = Utilities.Utility.GetOrderEnumOfArrangement(oldArrangement);
        //    if (oldArrangementOrder != ArrangementOrder.ArrangementOrderEnum.Column && newArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Column)
        //    {
        //        return oldWidth * 0.1f;
        //    }
        //    if (oldArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Column && newArrangementOrder != ArrangementOrder.ArrangementOrderEnum.Column)
        //    {
        //        return oldWidth / 0.1f;
        //    }
        //    var oldUnitSpacing = GetFormationVirtualUnitSpacing(formation) ?? formation.UnitSpacing;
        //    var oldFlankWidth = oldWidth;
        //    if (oldArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Circle)
        //    {
        //        oldFlankWidth = oldWidth * MathF.PI;
        //    }
        //    else if (oldArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Square)
        //    {
        //        // given that:
        //        // flankwidth = (filecount - 1) * (interval + unitdiameter) + unitdiameter
        //        // width = (ceiling(filecount / 4)) * (interval + unitdiameter) + unitdiameter
        //        // we have:
        //        // ceiling(filecount / 4) = (width - unitdiameter) / (interval + unitdiameter)
        //        // filecount = 4 * (width - unitdiameter) / (interval + unitdiameter)
        //        // flankwidth = (4 * (width - unitdiameter) / (interval + unitdiameter) - 1) * (interval + unitdiameter) + unitdiameter
        //        // flankwidth = 4 * (width - unitdiameter) - interval
        //        oldFlankWidth = 4 * (oldWidth - formation.UnitDiameter) - Utilities.Utility.GetFormationInterval(formation, oldUnitSpacing);
        //    }

        //    var oldFileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, oldUnitSpacing);
        //    var naturalUnitSpacing = ArrangementOrder.GetUnitSpacingOf(newArrangementOrder);
        //    var newUnitSpacing = naturalUnitSpacing;
        //    var minimumFileCount = (int)Utilities.Utility.MinimumFileCount.GetValue(formation.Arrangement);
        //    while (newUnitSpacing < naturalUnitSpacing && Utilities.Utility.GetUnlimitedFileCountFromWidth(formation, oldFlankWidth, newUnitSpacing + 1) >= minimumFileCount)
        //    {
        //        newUnitSpacing++;
        //    }
        //    var newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, oldFileCount, newUnitSpacing);
        //    //if (oldArrangementType == newArrangementType)
        //    //{
        //    //    var fileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, oldUnitSpacing);
        //    //    newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, fileCount, newUnitSpacing);
        //    //}
        //    //else if (oldArrangementType == typeof(LineFormation))
        //    //{
        //    //    //From line to circle: new width = old width / pi, ignoring unit spacing.
        //    //    //From circle to line: new width = pi * old width, ignoring unit spacing
        //    //    //From shieldwall to circle:
        //    //    //                1.convert to line with file count unchanged
        //    //    //    2.convert to circle with new width = old width / pi, ignoring unit spacing.
        //    //    //From circle to shieldwall:
        //    //    //    1.convert to line with new width = pi * old width, ignoring unit spacing.
        //    //    //    2.convert to shieldwall with file count unchanged.

        //    //    //From loose to circle:
        //    //    //    1.convert to line with file count unchanged
        //    //    //    2.convert to circle with new width = old width / pi, ignoring unit spacing.
        //    //    //From circle to loose:
        //    //    //    1.convert to line with file count unchanged
        //    //    //    2.convert to circle with new width = old width / pi, ignoring unit spacing.
        //    //    var fileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, oldUnitSpacing);
        //    //    newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, fileCount, ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.ArrangementOrderEnum.Line));
        //    //}
        //    //else if (newArrangementType == typeof(LineFormation))
        //    //{
        //    //    var fileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.ArrangementOrderEnum.Line));
        //    //    newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, fileCount, newUnitSpacing);
        //    //}
        //    var newWidth = newFlankWidth;
        //    if (newArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Circle)
        //    {
        //        newWidth = newFlankWidth / MathF.PI/*+ 0.363f*/;
        //    }
        //    else if (newArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Square)
        //    {
        //        // given that:
        //        // flankwidth = (filecount - 1) * (interval + unitdiameter) + unitdiameter
        //        // width = (ceiling(filecount / 4)) * (interval + unitdiameter) + unitdiameter
        //        // we have:
        //        // ceiling(filecount / 4) = (width - unitdiameter) / (interval + unitdiameter)
        //        // filecount = 4 * (width - unitdiameter) / (interval + unitdiameter)
        //        // flankwidth = (4 * (width - unitdiameter) / (interval + unitdiameter) - 1) * (interval + unitdiameter) + unitdiameter
        //        // flankwidth = 4 * (width - unitdiameter) - interval
        //        if (CommandSystemConfig.Get().HollowSquare)
        //        {
        //            newWidth = Utilities.Utility.ConvertFromFlankWidthToWidthOfSquareFormation(formation, newUnitSpacing, newFlankWidth);
        //        }
        //        else
        //        {
        //            newWidth = MathF.Min(Utilities.Utility.GetMinimumWidthOfSquareFormation(formation), newFlankWidth);
        //        }
        //    }
        //    newWidth = MathF.Clamp(newWidth, GetFormationMinimumWidthOfArrangementOrder(formation, newArrangementOrder), GetFormationMaximumWidthOfArrangementOrder(formation, newArrangementOrder));
        //    return newWidth;
        //}

        private static float GetNewWidthOfArrangementOrder(Formation formation,
            ArrangementOrder.ArrangementOrderEnum oldArrangementOrder,
            ArrangementOrder.ArrangementOrderEnum newArrangementOrder,
            int newUnitSpacing)
        {
            var oldWidth = GetFormationVirtualWidth(formation) ?? formation.Width;
            if (oldArrangementOrder == newArrangementOrder)
            {
                return oldWidth;
            }
            if (oldArrangementOrder != ArrangementOrder.ArrangementOrderEnum.Column && newArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Column)
            {
                return oldWidth * 0.1f;
            }
            if (oldArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Column && newArrangementOrder != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                return oldWidth / 0.1f;
            }
            var oldUnitSpacing = GetFormationVirtualUnitSpacing(formation) ?? formation.UnitSpacing;
            var oldFlankWidth = oldWidth;
            if (oldArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Circle)
            {
                oldFlankWidth = oldWidth * MathF.PI;
            }
            else if (oldArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Square)
            {
                // given that:
                // flankwidth = (filecount - 1) * (interval + unitdiameter) + unitdiameter
                // width = (ceiling(filecount / 4)) * (interval + unitdiameter) + unitdiameter
                // we have:
                // ceiling(filecount / 4) = (width - unitdiameter) / (interval + unitdiameter)
                // filecount = 4 * (width - unitdiameter) / (interval + unitdiameter)
                // flankwidth = (4 * (width - unitdiameter) / (interval + unitdiameter) - 1) * (interval + unitdiameter) + unitdiameter
                // flankwidth = 4 * (width - unitdiameter) - interval
                oldFlankWidth = 4 * (oldWidth - formation.UnitDiameter) - Utilities.Utility.GetFormationInterval(formation, oldUnitSpacing);
            }

            var oldFileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, oldUnitSpacing);
            //var naturalUnitSpacing = ArrangementOrder.GetUnitSpacingOf(newArrangementOrder);
            //if (newUnitSpacing == null)
            //{
            //    newUnitSpacing = naturalUnitSpacing;
            //}
            //var minimumFileCount = (int)Utilities.Utility.MinimumFileCount.GetValue(formation.Arrangement);
            //while (newUnitSpacing < naturalUnitSpacing && Utilities.Utility.GetUnlimitedFileCountFromWidth(formation, oldFlankWidth, newUnitSpacing + 1) >= minimumFileCount)
            //{
            //    newUnitSpacing++;
            //}
            var newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, oldFileCount, newUnitSpacing);
            //if (Utilities.Utility.GetTypeOfArrangement(oldArrangementOrder) == Utilities.Utility.GetTypeOfArrangement(newArrangementOrder))
            //{
            //    var fileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, oldUnitSpacing);
            //    newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, fileCount, newUnitSpacing);
            //}
            //else if (Utilities.Utility.GetTypeOfArrangement(oldArrangementOrder) == typeof(LineFormation))
            //{
            //    //From line to circle: new width = old width / pi, ignoring unit spacing.
            //    //From circle to line: new width = pi * old width, ignoring unit spacing
            //    //From shieldwall to circle:
            //    //                1.convert to line with file count unchanged
            //    //    2.convert to circle with new width = old width / pi, ignoring unit spacing.
            //    //From circle to shieldwall:
            //    //    1.convert to line with new width = pi * old width, ignoring unit spacing.
            //    //    2.convert to shieldwall with file count unchanged.

            //    //From loose to circle:
            //    //    1.convert to line with file count unchanged
            //    //    2.convert to circle with new width = old width / pi, ignoring unit spacing.
            //    //From circle to loose:
            //    //    1.convert to line with file count unchanged
            //    //    2.convert to circle with new width = old width / pi, ignoring unit spacing.
            //    var fileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, oldUnitSpacing);
            //    newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, fileCount, ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.ArrangementOrderEnum.Line));
            //}
            //else if (Utilities.Utility.GetTypeOfArrangement(newArrangementOrder) == typeof(LineFormation))
            //{
            //    var fileCount = Utilities.Utility.GetFileCountFromWidth(formation, oldFlankWidth, ArrangementOrder.GetUnitSpacingOf(ArrangementOrder.ArrangementOrderEnum.Line));
            //    newFlankWidth = Utilities.Utility.GetFlankWidthFromFileCount(formation, fileCount, newUnitSpacing);
            //}
            var newWidth = newFlankWidth;
            if (newArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Circle)
            {
                newWidth = newFlankWidth / MathF.PI/* + 0.363f*/;
            }
            else if (newArrangementOrder == ArrangementOrder.ArrangementOrderEnum.Square)
            {
                // given that:
                // flankwidth = (filecount - 1) * (interval + unitdiameter) + unitdiameter
                // width = (ceiling(filecount / 4)) * (interval + unitdiameter) + unitdiameter
                // we have:
                // ceiling(filecount / 4) = (width - unitdiameter) / (interval + unitdiameter)
                // filecount = 4 * (width - unitdiameter) / (interval + unitdiameter)
                // flankwidth = (4 * (width - unitdiameter) / (interval + unitdiameter) - 1) * (interval + unitdiameter) + unitdiameter
                // flankwidth = 4 * (width - unitdiameter) - interval
                if (CommandSystemConfig.Get().HollowSquare)
                {
                    newWidth = Utilities.Utility.ConvertFromFlankWidthToWidthOfSquareFormation(formation, newUnitSpacing, newFlankWidth);
                }
                else
                {
                    newWidth = MathF.Min(Utilities.Utility.GetMinimumWidthOfSquareFormation(formation), newFlankWidth);
                }
            }
            newWidth = MathF.Clamp(newWidth, GetFormationMinimumWidthOfArrangementOrder(formation, newArrangementOrder), GetFormationMaximumWidthOfArrangementOrder(formation, newArrangementOrder));
            return newWidth;
        }

        private static void DecreaseUnitSpacingAndWidthWithNewArrangementOrderIfNotAllUnitsFit(Formation formation,
            Formation simulationFormation,
            ArrangementOrder.ArrangementOrderEnum oldArrangementOrder,
            ArrangementOrder.ArrangementOrderEnum arrangementOrder,
            in WorldPosition formationPosition,
            in Vec2 formationDirection,
            ref float formationWidth,
            out int unitSpacingReduction,
            int actualUnitSpacing)
        {
            if (simulationFormation.UnitSpacing != actualUnitSpacing)
            {
                simulationFormation = new Formation(null, -1);
            }
            bool hasAnyMountUnit = formation.CalculateHasSignificantNumberOfMounted && GetFormationVirtualRidingOrder(formation) != OrderType.Dismount;
            _overridenHasAnyMountedUnit.SetValue(formation, hasAnyMountUnit);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, hasAnyMountUnit);
            int unitIndex = formation.CountOfUnitsWithoutDetachedOnes - 1;
            float actualWidth = formationWidth;
            if (unitIndex >= 0)
            {
                if (CommandSystemConfig.Get().CircleFormationUnitSpacingPreference == CircleFormationUnitSpacingPreference.Tight && arrangementOrder == ArrangementOrder.ArrangementOrderEnum.Circle)
                {
                    unitSpacingReduction = actualUnitSpacing;
                    do
                    {
                        formationWidth = GetNewWidthOfArrangementOrder(formation, oldArrangementOrder, arrangementOrder, actualUnitSpacing - unitSpacingReduction);
                        GetUnitPositionWithIndexAccordingToNewOrder(formation, simulationFormation, oldArrangementOrder, arrangementOrder, unitIndex, in formationPosition, in formationDirection, formation.Arrangement, formationWidth, actualUnitSpacing - unitSpacingReduction, formation.Arrangement.UnitCount, formation.HasAnyMountedUnit, formation.Index, out var unitSpawnPosition, out var _, out actualWidth);
                        if (unitSpawnPosition.HasValue)
                        {
                            break;
                        }
                        unitSpacingReduction--;
                    }
                    while (unitSpacingReduction >= 0);
                    unitSpacingReduction = MathF.Max(unitSpacingReduction, 0);
                    if (unitSpacingReduction > 0)
                    {
                        formationWidth = actualWidth;
                    }
                }
                else
                {
                    unitSpacingReduction = 0;
                    do
                    {
                        formationWidth = GetNewWidthOfArrangementOrder(formation, oldArrangementOrder, arrangementOrder, actualUnitSpacing - unitSpacingReduction);
                        GetUnitPositionWithIndexAccordingToNewOrder(formation, simulationFormation, oldArrangementOrder, arrangementOrder, unitIndex, in formationPosition, in formationDirection, formation.Arrangement, formationWidth, actualUnitSpacing - unitSpacingReduction, formation.Arrangement.UnitCount, formation.HasAnyMountedUnit, formation.Index, out var unitSpawnPosition, out var _, out actualWidth);
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
                }
            }
            else
            {
                unitSpacingReduction = 0;
            }
            _overridenHasAnyMountedUnit.SetValue(formation, null);
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, null);
        }

        private static void SimulateNewArrangementOrderWithFrameAndWidth(Formation formation,
            Formation simulationFormation,
            ArrangementOrder.ArrangementOrderEnum oldArrangementOrder,
            ArrangementOrder.ArrangementOrderEnum arrangementOrder,
            List<WorldPosition> simulationAgentFrames,
            List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges,
            in WorldPosition formationPosition,
            in Vec2 formationDirection,
            float formationWidth,
            int unitSpacingReduction,
            bool simulateFormationDepth,
            out float simulatedFormationDepth,
            int actualUnitSpacing)
        {
            int unitIndex = 0;
            float num2 = (simulateFormationDepth ? 0f : float.NaN);
            bool flag = Mission.Current.Mode != MissionMode.Deployment || Mission.Current.IsOrderPositionAvailable(in formationPosition, formation.Team);
            // override HasAnyMountUnit to be consistent with actual command execution.
            bool hasAnyMountUnit = formation.CalculateHasSignificantNumberOfMounted && GetFormationVirtualRidingOrder(formation) != OrderType.Dismount;
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
                    GetUnitPositionWithIndexAccordingToNewOrder(formation, simulationFormation, oldArrangementOrder, arrangementOrder, unitIndex, in formationPosition, in formationDirection, formation.Arrangement, formationWidth, actualUnitSpacing - unitSpacingReduction, formation.Arrangement.UnitCount, formation.HasAnyMountedUnit, formation.Index, out unitSpawnPosition, out unitSpawnDirection, out _);
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
                WorldPosition item = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
                simulationFormationChanges?.Add(ValueTuple.Create(formation, unitSpacingReduction, formationWidth, item, formation.Direction));
            }
            simulatedFormationDepth = num2 + formation.UnitDiameter;
        }

        // copied from Formation.GetUnitPositionWithIndexAccordingToNewOrder
        private static void GetUnitPositionWithIndexAccordingToNewOrder(
            Formation formation,
            Formation simulationFormation,
            ArrangementOrder.ArrangementOrderEnum oldArrangementOrder,
            ArrangementOrder.ArrangementOrderEnum? newArrangementOrder,
            int unitIndex,
            in WorldPosition formationPosition,
            in Vec2 formationDirection,
            IFormationArrangement arrangement,
            float width,
            int unitSpacing,
            int unitCount,
            bool isMounted,
            int index,
            out WorldPosition? unitPosition,
            out Vec2? unitDirection,
            out float newWidth)
        {
            unitPosition = new WorldPosition?();
            unitDirection = new Vec2?();
            if (simulationFormation == null)
            {
                if (_simulationFormationTemp.GetValue(null) == null|| (int)_simulationFormationUniqueIdentifier.GetValue(null) != index)
                    _simulationFormationTemp.SetValue(null, new Formation((Team)null, -1));
                simulationFormation = (Formation)_simulationFormationTemp.GetValue(null);
            }
            Vec2 direction;
            var oldArrangementType = Utilities.Utility.GetTypeOfArrangement(oldArrangementOrder, true);
            if (simulationFormation.UnitSpacing == unitSpacing && /*(double)MathF.Abs((float)((double)simulationFormation.Width - (double)width + 9.9999997473787516E-06)) < (double)simulationFormation.Interval + (double)simulationFormation.UnitDiameter - 9.9999997473787516E-06 &&*/ simulationFormation.OrderPositionIsValid && simulationFormation.OrderGroundPosition.NearlyEquals(formationPosition.GetGroundVec3(), 0.1f))
            {
                direction = simulationFormation.Direction;
                var newArrangementType = newArrangementOrder == null ? null : Utilities.Utility.GetTypeOfArrangement(newArrangementOrder.Value, true);
                var simulationFormationArrangementType = simulationFormation.Arrangement.GetType();
                var simulationFormationArrangementOrderType = Utilities.Utility.GetTypeOfArrangement(simulationFormation.ArrangementOrder.OrderEnum, true);
                if (direction.NearlyEquals(formationDirection, 0.1f))
                {
                    if (simulationFormationArrangementType == arrangement.GetType())
                        goto label_1;
                    else if (direction.NearlyEquals(formationDirection, 0.1f) && simulationFormationArrangementType == oldArrangementType && simulationFormationArrangementOrderType == oldArrangementType)
                        goto label_2;
                    else if (direction.NearlyEquals(formationDirection, 0.1f) && newArrangementOrder != null && simulationFormationArrangementType == newArrangementType && simulationFormationArrangementOrderType == newArrangementType)
                        goto label_3;
                }
            }
            _overridenHasAnyMountedUnit.SetValue(simulationFormation, new bool?(isMounted));

            ResetForSimulation.Invoke(simulationFormation, new object[] { });
            //simulationFormation.Arrangement.Reset();
            ////simulationFormation.SetMovementOrder(MovementOrder.MovementOrderStop);
            //simulationFormation.FormOrder = FormOrder.FormOrderWide;
            //simulationFormation.Arrangement.Width = formation.UnitDiameter;
            ////simulationFormation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;

            simulationFormation.SetPositioning(unitSpacing: new int?(unitSpacing));
            OverridenUnitCount.SetValue(simulationFormation, new int?(unitCount));
            simulationFormation.SetPositioning(new WorldPosition?(formationPosition), new Vec2?(formationDirection));
            simulationFormation.Rearrange(arrangement.Clone(simulationFormation));
            simulationFormation.Arrangement.DeepCopyFrom(arrangement);
            simulationFormation.Arrangement.Width = width;
            _simulationFormationUniqueIdentifier.SetValue(null, index);
        label_1:
            //if (arrangement.GetType() != oldArrangementType)
            {
                _arrangementOrder.SetValue(simulationFormation, formation.ArrangementOrder);
                simulationFormation.ArrangementOrder = Utilities.Utility.GetArrangementOrder(oldArrangementOrder);
                simulationFormation.SetPositioning(unitSpacing: new int?(unitSpacing));
                simulationFormation.FormOrder = FormOrder.FormOrderCustom(width);
            }
        label_2:
            if (newArrangementOrder != null)
            {
                simulationFormation.ArrangementOrder = Utilities.Utility.GetArrangementOrder(newArrangementOrder.Value);
                simulationFormation.SetPositioning(unitSpacing: new int?(unitSpacing));
                simulationFormation.FormOrder = FormOrder.FormOrderCustom(width);
            }
        label_3:
            newWidth = simulationFormation.Width;
            // add a small number to resolve the issue that the movement target marker may disappear during dragging.
            if ((double)width + 0.363f < (double)newWidth && unitSpacing > 0)
                return;
            Vec2? nullable = simulationFormation.Arrangement.GetLocalPositionOfUnitOrDefault(unitIndex);
            if (!nullable.HasValue)
                nullable = simulationFormation.Arrangement.CreateNewPosition(unitIndex);
            if (!nullable.HasValue)
                return;
            direction = simulationFormation.Direction;
            Vec2 parentUnitF = direction.TransformToParentUnitF(nullable.Value);
            WorldPosition orderWorldPosition = simulationFormation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.NavMeshVec3);
            orderWorldPosition.SetVec2(orderWorldPosition.AsVec2 + parentUnitF);
            unitPosition = new WorldPosition?(orderWorldPosition);
            unitDirection = new Vec2?(formationDirection);
        }

        private static float GetFormationVirtualMinimumWidth(Formation formation)
        {
            var arrangementOrder = GetFormationVirtualArrangementOrder(formation);
            return GetFormationMinimumWidthOfArrangementOrder(formation, arrangementOrder);
        }

        private static float GetFormationMinimumWidthOfArrangementOrder(Formation formation, ArrangementOrder.ArrangementOrderEnum arrangementOrder)
        {
            switch (arrangementOrder)
            {
                case ArrangementOrder.ArrangementOrderEnum.Square:
                    return Utilities.Utility.GetMinimumWidthOfSquareFormation(formation);
                case ArrangementOrder.ArrangementOrderEnum.Circle:
                    return Utilities.Utility.GetMinimumWidthOfCircularFormation(formation, GetFormationVirtualUnitSpacing(formation) ?? formation.UnitSpacing);
                default:
                    return Utilities.Utility.GetMinimumWidthOfLineFormation(formation);
            }
        }

        private static float GetFormationVirtualMaximumWidth(Formation formation)
        {
            var arrangementOrder = GetFormationVirtualArrangementOrder(formation);
            return GetFormationMaximumWidthOfArrangementOrder(formation, arrangementOrder);
        }

        private static float GetFormationMaximumWidthOfArrangementOrder(Formation formation, ArrangementOrder.ArrangementOrderEnum arrangementOrder)
        {
            switch (arrangementOrder)
            {
                case ArrangementOrder.ArrangementOrderEnum.Square:
                    return Utilities.Utility.GetMaximumWidthOfSquareFormation(formation);
                case ArrangementOrder.ArrangementOrderEnum.Circle:
                    return Utilities.Utility.GetMaximumWidthOfCircularFormation(formation, GetFormationVirtualUnitSpacing(formation) ?? formation.UnitSpacing);
                default:
                    return Utilities.Utility.GetMaximumWidthOfLineFormation(formation);
            }
        }


        public static bool Prefix_GetActiveMovementOrderOf(Formation formation, ref OrderType __result)
        {
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            if (queueCommand)
            {
                if (CommandQueueLogic.LatestOrderInQueueChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                {
                    if (formationChange.MovementOrderType != null)
                    {
                        var orderType = formationChange.MovementOrderType.Value;
                        switch (Utilities.Utility.MovementStateFromMovementOrderType(orderType))
                        {
                            case MovementOrder.MovementStateEnum.Charge:
                                 __result = orderType == OrderType.GuardMe ? OrderType.GuardMe : OrderType.Charge;
                                return false;
                            case MovementOrder.MovementStateEnum.Hold:
                                switch (orderType)
                                {
                                    case OrderType.ChargeWithTarget:
                                        __result = OrderType.Charge;
                                        break;
                                    case OrderType.FollowMe:
                                        __result = OrderType.FollowMe;
                                        break;
                                    case OrderType.Advance:
                                        __result = OrderType.Advance;
                                        break;
                                    case OrderType.FallBack:
                                        __result = OrderType.FallBack;
                                        break;
                                    default:
                                        __result = OrderType.Move;
                                        break;
                                }
                                return false;
                            case MovementOrder.MovementStateEnum.Retreat:
                                __result = OrderType.Retreat;
                                return false;
                            case MovementOrder.MovementStateEnum.StandGround:
                                __result = OrderType.StandYourGround;
                                return false;
                            default:
                                __result = OrderType.Move;
                                return false;
                        }
                    }
                }
            }
            return true;
        }

        public static bool Prefix_GetActiveFacingOrderOf(Formation formation, ref OrderType __result)
        {
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            if (queueCommand)
            {
                if (CommandQueueLogic.LatestOrderInQueueChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                {
                    if (formationChange.FacingOrderType != null)
                    {
                        __result = formationChange.FacingOrderType.Value;
                        return false;
                    }
                }
            }
            return true;
        }


        public static bool Prefix_GetActiveFiringOrderOf(Formation formation, ref OrderType __result)
        {
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            if (queueCommand)
            {
                if (CommandQueueLogic.LatestOrderInQueueChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                {
                    if (formationChange.FiringOrderType != null)
                    {
                        __result = formationChange.FiringOrderType.Value;
                        return false;
                    }
                }
            }
            return true;
        }


        public static bool Prefix_GetActiveRidingOrderOf(Formation formation, ref OrderType __result)
        {
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            if (queueCommand)
            {
                if (CommandQueueLogic.LatestOrderInQueueChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                {
                    if (formationChange.RidingOrderType != null)
                    {
                        __result = formationChange.RidingOrderType.Value;
                        return false;
                    }
                }
            }
            return true;
        }


        public static bool Prefix_GetActiveArrangementOrderOf(Formation formation, ref OrderType __result)
        {
            bool queueCommand = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue).IsKeyDownInOrder();
            if (queueCommand)
            {
                if (CommandQueueLogic.LatestOrderInQueueChanges.VirtualChanges.TryGetValue(formation, out var formationChange))
                {
                    if (formationChange.ArrangementOrder != null)
                    {
                        __result = Utilities.Utility.ArrangementOrderEnumToOrderType(formationChange.ArrangementOrder.Value);
                        return false;
                    }
                }
            }
            return true;
        }

        public static Formation GetFacingEnemyTargetFormation(Formation formation)
        {
            if (FacingEnemeyTarget.TryGetValue(formation, out var target))
            {
                return target;
            }
            return null;
        }

        public static Formation GetVirtualFacingEnemyTargetFormation(Formation formation)
        {
            bool hasFormation = LivePreviewFormationChanges.VirtualChanges.ContainsKey(formation);
            return hasFormation ? LivePreviewFormationChanges.VirtualChanges[formation].FacingEnemyTargetFormation : GetFacingEnemyTargetFormation(formation);
        }

        public static void SetFacingEnemyTargetFormation(IEnumerable<Formation> formations, Formation targetFormation)
        {
            foreach (var formation in formations)
            {
                SetFacingEnemyTargetFormation(formation, targetFormation);
            }
        }

        public static void SetFacingEnemyTargetFormation(Formation formation, Formation targetFormation)
        {
            if (targetFormation == null)
            {
                FacingEnemeyTarget.Remove(formation);
            }
            else
            {
                FacingEnemeyTarget[formation] = targetFormation;
            }
        }
    }
}
