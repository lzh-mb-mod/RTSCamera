using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private static Dictionary<Formation, float> _actualWidths = new Dictionary<Formation, float>();
        private static Dictionary<Formation, int> _actualUnitSpacings = new Dictionary<Formation, int>();

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                // To resolve 3 issues:
                // 1. The order troop placer is inconsistent with actual order issued during dragging if formation width is changed.
                // 2. The order troop placer is inconsistent with actual order issued during dragging if there're small number of mounted units in infantry formation.
                // 3. Sort of formations during dragging in horizontal layout is not consistent actual sort when order is issued
                // The idea is to use GetActualOrCurrentUnitSpacing(formation) instead of formation.UnitSpacing,
                // use GetActualOrCurrentWidth(formation) instead of formation.Width,
                // sort formations by their FormationClass in horizontal layout,
                // and use formation.CalculateHasSignificantNumberOfMounted instead of formation.HasAnyMountedUnit.
                harmony.Patch(
                    typeof(OrderController).GetMethod("SimulateNewOrderWithPositionAndDirectionAux",
                        BindingFlags.Static | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderController).GetMethod(
                        nameof(Prefix_SimulateNewOrderWithPositionAndDirectionAux), BindingFlags.Static | BindingFlags.Public)));
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
            _actualWidths = new Dictionary<Formation, float>();
            _actualUnitSpacings = new Dictionary<Formation, int>();
        }

        public static void OnRemoveBehavior()
        {
            _actualUnitSpacings = null;
            _actualWidths = null;
        }

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
                                _actualUnitSpacings[formation] = actualUnitSpacings[formation];
                                actualUnitSpacings.Remove(formation);
                            }
                        }
                    }
                    float num1 = !isFormationLayoutVertical ? formations.Max<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f))) : formations.Sum<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f))) + (float)(formations.Count<Formation>() - 1) * 1.5f;
                    Vec2 direction = formations.MaxBy<Formation, int>((Func<Formation, int>)(f => f.CountOfUnitsWithoutDetachedOnes)).Direction;
                    direction.RotateCCW(-1.57079637f);
                    double num2 = (double)direction.Normalize();
                    formationLineEnd = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 + num1 / 2f * direction, formationLineBegin);
                    formationLineBegin = Mission.Current.GetStraightPathToTarget(formationLineBegin.AsVec2 - num1 / 2f * direction, formationLineBegin);
                }
                else
                {
                    foreach (var formation in formations)
                    {
                        if (_actualUnitSpacings.ContainsKey(formation))
                        {
                            // move actualUnitSpacings back to OrderController
                            // to allow unit spacings recovery
                            SetActualUnitSpacing(formation, _actualUnitSpacings[formation]);
                            _actualUnitSpacings.Remove(formation);
                        }
                    }
                    formationLineEnd = Mission.Current.GetStraightPathToTarget(formationLineEnd.AsVec2, formationLineBegin);
                }
                if (isFormationLayoutVertical)
                    SimulateNewOrderWithVerticalLayout(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, out simulationAgentFrames, isSimulatingFormationChanges, out simulationFormationChanges);
                else
                   SimulateNewOrderWithHorizontalLayout(formations, simulationFormations, formationLineBegin, formationLineEnd, isSimulatingAgentFrames, out simulationAgentFrames, isSimulatingFormationChanges, out simulationFormationChanges);
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
            }
            return true;
        }

        private static void SimulateNewOrderWithHorizontalLayout(IEnumerable<Formation> formations,
            Dictionary<Formation, Formation> simulationFormations,
            WorldPosition formationLineBegin,
            WorldPosition formationLineEnd,
            bool isSimulatingAgentFrames,
            out List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            simulationAgentFrames = ((!isSimulatingAgentFrames) ? null : new List<WorldPosition>());
            simulationFormationChanges = ((!isSimulatingFormationChanges) ? null : new List<(Formation, int, float, WorldPosition, Vec2)>());
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
                a = MathF.Min(a, formation.MaximumWidth);
                WorldPosition formationPosition = formationLineBegin;
                formationPosition.SetVec2((formationLineEnd.AsVec2 + formationLineBegin.AsVec2) * 0.5f - formationDirection * num3);
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
            out List<WorldPosition> simulationAgentFrames,
            bool isSimulatingFormationChanges,
            out List<(Formation, int, float, WorldPosition, Vec2)> simulationFormationChanges)
        {
            simulationAgentFrames = !isSimulatingAgentFrames ? (List<WorldPosition>)null : new List<WorldPosition>();
            simulationFormationChanges = !isSimulatingFormationChanges ? (List<(Formation, int, float, WorldPosition, Vec2)>)null : new List<(Formation, int, float, WorldPosition, Vec2)>();
            Vec2 vec2 = formationLineEnd.AsVec2 - formationLineBegin.AsVec2;
            float length = vec2.Length;
            double num1 = (double)vec2.Normalize();
            float f1 = MathF.Max(0.0f, length - (float)(formations.Count<Formation>() - 1) * 1.5f);
            float comparedValue = formations.Sum<Formation>((Func<Formation, float>)(f => GetActualOrCurrentWidth(f)));
            bool flag = f1.ApproximatelyEqualsTo(comparedValue, 0.1f);
            float num2 = formations.Sum<Formation>((Func<Formation, float>)(f => f.MinimumWidth));
            formations.Count<Formation>();
            Vec2 formationDirection = new Vec2(-vec2.y, vec2.x).Normalized();
            float num3 = 0.0f;
            foreach (Formation formation in formations)
            {
                float minimumWidth = formation.MinimumWidth;
                var actualWidth = GetActualOrCurrentWidth(formation);
                float formationWidth = MathF.Min(flag ? actualWidth : MathF.Min((double)f1 < (double)comparedValue ? actualWidth : float.MaxValue, f1 * (minimumWidth / num2)), formation.MaximumWidth);
                WorldPosition formationPosition = formationLineBegin;
                formationPosition.SetVec2(formationPosition.AsVec2 + vec2 * (formationWidth * 0.5f + num3));
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
            FormationClass.NumberOfDefaultFormations,
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
    }
}
