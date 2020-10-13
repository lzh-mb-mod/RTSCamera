using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.CircularFormation
{
    public class Patch_OrderController
    {
        private static readonly MethodInfo FacingOrderLookAtDirection =
            typeof(FacingOrder).GetMethod("FacingOrderLookAtDirection", BindingFlags.Static | BindingFlags.NonPublic);

        private static int GetLineOrderByClass(FormationClass formationClass)
        {
            return Array.IndexOf(new FormationClass[8]
            {
                FormationClass.HeavyInfantry,
                FormationClass.Infantry,
                FormationClass.HeavyCavalry,
                FormationClass.Cavalry,
                FormationClass.LightCavalry,
                FormationClass.NumberOfDefaultFormations,
                FormationClass.Ranged,
                FormationClass.HorseArcher
            }, formationClass);
        }

        private static IEnumerable<Formation> SortFormationsForHorizontalLayout(
            IEnumerable<Formation> formations)
        {
            return formations.OrderBy(f => GetLineOrderByClass(f.FormationIndex));
        }

        private static IEnumerable<Formation> GetSortedFormations(
            IEnumerable<Formation> formations,
            bool isFormationLayoutVertical)
        {
            return isFormationLayoutVertical ? formations : SortFormationsForHorizontalLayout(formations);
        }

        public static bool Patch(Harmony harmony)
        {
            try
            {
                harmony.Patch(
                    typeof(OrderController).GetMethod("MoveToLineSegment",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(nameof(Prefix_MoveToLineSegment),
                        BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool Prefix_MoveToLineSegment(
            IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition TargetLineSegmentBegin,
            WorldPosition TargetLineSegmentEnd,
            OnOrderIssuedDelegate OnOrderIssued,
            Dictionary<Formation, float> actualWidths,
            Dictionary<Formation, int> actualUnitSpacings,
            bool isFormationLayoutVertical)
        {
            try
            {
                foreach (Formation formation in formations)
                {
                    if (actualUnitSpacings.TryGetValue(formation, out var num))
                        formation.SetPositioning(new WorldPosition?(), new Vec2?(), num);
                    if (actualWidths.TryGetValue(formation, out var customWidth))
                        formation.FormOrder = FormOrder.FormOrderCustom(customWidth);
                }
                formations = GetSortedFormations(formations, isFormationLayoutVertical);
                OrderController.SimulateNewOrderWithPositionAndDirection(formations, simulationFormations, TargetLineSegmentBegin, TargetLineSegmentEnd, out var formationChanges, out var isLineShort, isFormationLayoutVertical);
                foreach ((Formation element, int unitSpacingReduction, float customWidth1, Vec2 direction, WorldPosition position) in formationChanges)
                {
                    int oldUnitSpacing = element.UnitSpacing;
                    float oldWidth = element.Width;
                    if (unitSpacingReduction > 0)
                    {
                        // change minimum unit spacing from 0 to 1 in circle arrangement.
                        int newUnitSpacing = Math.Max(oldUnitSpacing - unitSpacingReduction,
                            element.ArrangementOrder.OrderType == OrderType.ArrangementCircular ? 1 : 0);
                        element.SetPositioning(new WorldPosition?(), new Vec2?(), newUnitSpacing);
                        if (element.UnitSpacing != oldUnitSpacing)
                            actualUnitSpacings[element] = oldUnitSpacing;
                    }
                    if (Math.Abs(element.Width - (double)customWidth1) > 0.1f)
                    {
                        element.FormOrder = FormOrder.FormOrderCustom(customWidth1);
                        if (isLineShort)
                            actualWidths[element] = oldWidth;
                    }
                    if (!isLineShort)
                    {
                        element.MovementOrder = MovementOrder.MovementOrderMove(position);
                        element.FacingOrder = (FacingOrder)FacingOrderLookAtDirection.Invoke(null, new object[] { direction });
                        element.FormOrder = FormOrder.FormOrderCustom(customWidth1);
                        if (OnOrderIssued != null)
                        {
                            IEnumerable<Formation> singleFormation = Enumerable.Repeat(element, 1);
                            OnOrderIssued(OrderType.Move, singleFormation, (object)position);
                            OnOrderIssued(OrderType.LookAtDirection, singleFormation, (object)direction);
                            OnOrderIssued(OrderType.FormCustom, singleFormation, (object)customWidth1);
                        }
                    }
                    else
                    {
                        Formation largestFormation = formations.MaxBy(f => f.CountOfUnitsWithoutDetachedOnes);
                        switch (OrderController.GetActiveFacingOrderOf(largestFormation))
                        {
                            case OrderType.LookAtEnemy:
                                element.MovementOrder = MovementOrder.MovementOrderMove(position);
                                if (OnOrderIssued != null)
                                {
                                    IEnumerable<Formation> local_20 = Enumerable.Repeat(element, 1);
                                    OnOrderIssued(OrderType.Move, local_20, (object)position);
                                    OnOrderIssued(OrderType.LookAtEnemy, local_20, Array.Empty<object>());
                                }
                                continue;
                            case OrderType.LookAtDirection:
                                element.MovementOrder = MovementOrder.MovementOrderMove(position);
                                element.FacingOrder = (FacingOrder)FacingOrderLookAtDirection.Invoke(null, new object[] { direction });
                                if (OnOrderIssued != null)
                                {
                                    IEnumerable<Formation> local_21 = Enumerable.Repeat(element, 1);
                                    OnOrderIssued(OrderType.Move, local_21, (object)position);
                                    OnOrderIssued(OrderType.LookAtDirection, local_21, (object)largestFormation.Direction);
                                }
                                continue;
                            default:
                                continue;
                        }
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
