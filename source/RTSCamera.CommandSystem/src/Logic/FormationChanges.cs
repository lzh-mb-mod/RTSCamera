using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.ArrangementOrder;

namespace RTSCamera.CommandSystem.Logic
{
    public struct FormationChange
    {
        public WorldPosition? WorldPosition;
        public readonly Vec2? Position => WorldPosition?.AsVec2;
        public Vec2? Direciton;
        public int? UnitSpacing;
        public float? Width;
        public OrderType? MovementOrderType;
        public Formation TargetFormation;
        public Agent TargetAgent;
        public Formation FacingEnemyTargetFormation;
        public IOrderable TargetEntity;
        public OrderType? FacingOrderType;
        public OrderType? FiringOrderType;
        public OrderType? RidingOrderType;
        public OrderType? AIControlOrderType;
        public ArrangementOrderEnum? ArrangementOrder;
        public float? PreviewWidth;
        public float? PreviewDepth;
    }
    public class FormationChanges
    {

        public Dictionary<Formation, FormationChange> VirtualChanges { get; set; } = new Dictionary<Formation, FormationChange>();
        public void SetChanges(IEnumerable<KeyValuePair<Formation, FormationChange>> virtualPositions)
        {
            foreach (var pair in virtualPositions)
            {
                if (!VirtualChanges.TryGetValue(pair.Key, out var change))
                {
                    change = new FormationChange();
                }
                change.WorldPosition = pair.Value.WorldPosition;
                change.Direciton = pair.Value.Direciton;
                change.UnitSpacing = pair.Value.UnitSpacing;
                change.Width = pair.Value.Width;
                change.MovementOrderType = pair.Value.MovementOrderType;
                change.TargetFormation = pair.Value.TargetFormation;
                change.TargetAgent = pair.Value.TargetAgent;
                change.TargetEntity = pair.Value.TargetEntity;
                change.FacingOrderType = pair.Value.FacingOrderType;
                change.FiringOrderType = pair.Value.FiringOrderType;
                change.RidingOrderType = pair.Value.RidingOrderType;
                change.ArrangementOrder = pair.Value.ArrangementOrder;
                change.PreviewWidth = pair.Value.PreviewWidth;
                change.PreviewDepth = pair.Value.PreviewDepth;
                VirtualChanges[pair.Key] = change;
            }
        }

        public Dictionary<Formation, FormationChange> CollectChanges(IEnumerable<Formation> formations)
        {
            return VirtualChanges.Where(pair => formations.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        public void UpdateFormationChange(Formation formation, WorldPosition? position, Vec2? direction, int? unitSpacing, float? width)
        {
            if (!VirtualChanges.TryGetValue(formation, out var change))
            {
                change = new FormationChange();
            }
            if (position != null)
            {
                change.WorldPosition = position.Value;
            }
            if (direction != null)
            {
                change.Direciton = direction.Value;
            }
            if (unitSpacing != null)
            {
                change.UnitSpacing = unitSpacing.Value;
            }
            if (width != null)
            {
                change.Width = width.Value;
            }
            VirtualChanges[formation] = change;
        }

        public void SetMovementOrder(OrderType orderType, IEnumerable<Formation> formations, Formation targetFormation, Agent targetAgent, IOrderable targetEntity)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }

                change.MovementOrderType = orderType;
                change.TargetFormation = targetFormation;
                change.TargetAgent = targetAgent;
                change.TargetEntity = targetEntity;
                VirtualChanges[formation] = change;
            }
        }
        
        public void SetFacingOrder(OrderType orderType, IEnumerable<Formation> formations, Formation targetFormation = null)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }

                change.FacingOrderType = orderType;
                change.FacingEnemyTargetFormation = targetFormation;
                VirtualChanges[formation] = change;
            }
        }

        public void SetFacingOrder(OrderType orderType, Formation formation, Formation targetFormation = null)
        {
            if (!VirtualChanges.TryGetValue(formation, out var change))
            {
                change = new FormationChange();
            }

            change.FacingOrderType = orderType;
            change.FacingEnemyTargetFormation = targetFormation;
            VirtualChanges[formation] = change;
        }

        public void ClearFacingOrderTarget(IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }

                change.FacingEnemyTargetFormation = null;
                VirtualChanges[formation] = change;
            }
        }

        public void ClearFacingOrderTarget(Formation formation)
        {
            if (!VirtualChanges.TryGetValue(formation, out var change))
            {
                change = new FormationChange();
            }

            change.FacingEnemyTargetFormation = null;
            VirtualChanges[formation] = change;
        }

        public void SetFiringOrder(OrderType orderType, IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }

                change.FiringOrderType = orderType;
                VirtualChanges[formation] = change;
            }
        }

        public void ClearFiringOrder(IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    return;
                }

                change.FiringOrderType = null;
                VirtualChanges[formation] = change;
            }
        }

        public void SetRidingOrder(OrderType orderType, IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }
                change.RidingOrderType = orderType;
                VirtualChanges[formation] = change;
            }
        }

        public void ClearRidingOrder(IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    return;
                }
                change.RidingOrderType = null;
                VirtualChanges[formation] = change;
            }
        }

        public void SetArrangementOrder(ArrangementOrder.ArrangementOrderEnum newArrangementOrder, IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }

                change.ArrangementOrder = newArrangementOrder;
                VirtualChanges[formation] = change;
            }
        }

        public void SetAIControlOrder(OrderType orderType, IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }
                change.AIControlOrderType = orderType;
                VirtualChanges[formation] = change;
            }
        }

        public void SetPreviewShape(Formation formation, float width, float depth)
        {
            if (!VirtualChanges.TryGetValue(formation, out var change))
            {
                change = new FormationChange();
            }
            change.PreviewWidth = width;
            change.PreviewDepth = depth;
            VirtualChanges[formation] = change;
        }

        private static float TransformCustomWidthBetweenArrangementOrientations(
            ArrangementOrder.ArrangementOrderEnum orderTypeOld,
            ArrangementOrder.ArrangementOrderEnum orderTypeNew,
            float currentCustomWidth)
        {
            if (orderTypeOld == ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Circle && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Column)
            {
                return (float)(currentCustomWidth / Math.PI);
            }
            if (orderTypeOld != ArrangementOrder.ArrangementOrderEnum.Column && orderTypeNew == ArrangementOrder.ArrangementOrderEnum.Column)
                return currentCustomWidth * 0.1f;
            return orderTypeOld == ArrangementOrder.ArrangementOrderEnum.Column && orderTypeNew != ArrangementOrder.ArrangementOrderEnum.Column ? currentCustomWidth / 0.1f : currentCustomWidth;
        }




    }
}
