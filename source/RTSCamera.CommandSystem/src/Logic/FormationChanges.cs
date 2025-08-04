using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

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
        public IOrderable TargetEntity;
        public OrderType? FacingOrderType;
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

                if (pair.Value.Position != null)
                {
                    change.WorldPosition = pair.Value.WorldPosition.Value;
                }
                if (pair.Value.Direciton != null)
                {
                    change.Direciton = pair.Value.Direciton.Value;
                }
                if (pair.Value.UnitSpacing != null)
                {
                    change.UnitSpacing = pair.Value.UnitSpacing.Value;
                }
                if (pair.Value.Width != null)
                {
                    change.Width = pair.Value.Width.Value;
                }
                change.MovementOrderType = pair.Value.MovementOrderType;
                change.TargetFormation = pair.Value.TargetFormation;
                change.TargetAgent = pair.Value.TargetAgent;
                change.TargetEntity = pair.Value.TargetEntity;
                change.FacingOrderType = pair.Value.FacingOrderType;
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
        
        public void SetFacingOrder(OrderType orderType, IEnumerable<Formation> formations)
        {
            foreach (var formation in formations)
            {
                if (!VirtualChanges.TryGetValue(formation, out var change))
                {
                    change = new FormationChange();
                }

                change.FacingOrderType = orderType;
                VirtualChanges[formation] = change;
            }
        }
    }
}
