using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.View
{
    public enum OrderTargetType
    {
        Move,
        Focus,
        Attack,
        Facing,
        Use,
        StopUsing,
        Count
    }
    public class OrderPreviewData
    {
        public WorldPosition OrderPosition;
        public float? Width;
        public float? Depth;
        public float? RightSideOffset;
        public Vec2 Direction;
        public List<WorldPosition> AgentPositions = new List<WorldPosition>();
        public OrderTargetType OrderTargetType;
    }

    public struct ArrowEntity
    {
        public static uint ArrowColor = new Color(0.4f, 0.8f, 0.4f).ToUnsignedInteger();
        public static uint FocusingArrowColor = new Color(0.7f, 0.3f, 0.2f).ToUnsignedInteger();
        public static uint AttackingArrowColor = new Color(0.95f, 0.1f, 0.1f).ToUnsignedInteger();
        public static uint FacingArrowColor = new Color(0.9f, 0.6f, 0.2f).ToUnsignedInteger();

        public GameEntity ArrowHead;
        public GameEntity ArrowBody;
        public OrderTargetType? TargetType;

        public void UpdateColor(OrderTargetType orderTargetType)
        {
            if (orderTargetType != TargetType)
            {
                var color = GetColorForTargetType(orderTargetType);
                ArrowHead.SetFactorColor(color);
                ArrowHead.SetContourColor(color, true);
                ArrowBody.SetFactorColor(color);
                ArrowBody.SetContourColor(color);
                TargetType = orderTargetType;
            }
        }

        public static uint GetColorForTargetType(OrderTargetType orderTargetType)
        {
            switch (orderTargetType)
            {
                case OrderTargetType.Move:
                    return ArrowColor;
                case OrderTargetType.Focus:
                    return FocusingArrowColor;
                case OrderTargetType.Attack:
                    return AttackingArrowColor;
                case OrderTargetType.Facing:
                    return FacingArrowColor;
                default:
                    return ArrowColor;
            }
        }
    }

    public struct FormationShapeEntity
    {
        public GameEntity FrontLine;
        public GameEntity LeftLine;
        public GameEntity RightLine;
        public GameEntity LeftBackLine;
        public GameEntity RightBackLine;
        public static uint SelectedColor = new Color(0.5f, 1.0f, 0.5f).ToUnsignedInteger();
        public static uint UnselectedColor = new Color(1f, 1f, 1f).ToUnsignedInteger();

        private static Material _decalMaterial;
        private static MetaMesh _lineMesh;
        private static GameEntity _cachedEntity;

        public static uint FormationShapeColor = new Color(0.7f, 1, 0.7f).ToUnsignedInteger();


        private static GameEntity CreatePrototype()
        {
            GameEntity result = GameEntity.CreateEmpty(Mission.Current.Scene);
            //decal.SetFactor1(FormationShapeColor);
            //result.AddComponent(decal);
            //var lineMesh = MetaMesh.GetCopy("fangkuang");

            if (_lineMesh == null)
            {
                _lineMesh = MetaMesh.GetCopy("decal_mesh");
                _lineMesh.SetFactor1(FormationShapeColor);
                //_lineMesh.SetContourColor(FormationShapeColor);
                //_lineMesh.SetContourState(true);
                if (_decalMaterial == null)
                {
                    _decalMaterial = Material.GetFromResource("decal_white").CreateCopy();
                    _decalMaterial.Flags |= MaterialFlags.CullFrontFaces | MaterialFlags.NoModifyDepthBuffer;
                }
                _lineMesh.SetMaterial(_decalMaterial);
            }
            result.AddComponent(_lineMesh.CreateCopy());
            result.SetVisibilityExcludeParents(false);
            result.EntityFlags |= EntityFlags.NotAffectedBySeason;
            result.EntityVisibilityFlags = EntityVisibilityFlags.NoShadow;
            return result;
        }

        public static void Initialize()
        {
            if (_cachedEntity == null)
            {
                _cachedEntity = CreatePrototype();
            }
        }

        public static void Clear()
        {
            //_lineMesh = null;
            //_decalMaterial = null;
            _cachedEntity = null;
        }

        public void CreateEntities()
        {
            FrontLine = CreateLineEntity();
            LeftLine = CreateLineEntity();
            RightLine = CreateLineEntity();
            LeftBackLine = CreateLineEntity();
            RightBackLine = CreateLineEntity();
        }

        private GameEntity CreateLineEntity()
        {
            //GameEntity result = GameEntity.CreateEmpty(Mission.Current.Scene);
            var result = GameEntity.CopyFrom(Mission.Current.Scene, _cachedEntity);

            //if (_lineMesh == null)
            //{
            //    _lineMesh = MetaMesh.GetCopy("decal_mesh");
            //    _lineMesh.SetFactor1(FormationShapeColor);
            //    //_lineMesh.SetContourColor(FormationShapeColor);
            //    //_lineMesh.SetContourState(true);
            //    if (_decalMaterial == null)
            //    {
            //        _decalMaterial = Material.GetFromResource("decal_white").CreateCopy();
            //        _decalMaterial.Flags |= MaterialFlags.CullFrontFaces | MaterialFlags.NoModifyDepthBuffer;
            //    }
            //    _lineMesh.SetMaterial(_decalMaterial);
            //}
            //result.AddComponent(_lineMesh.CreateCopy());
            //result.SetVisibilityExcludeParents(false);
            //result.SetMobility(GameEntity.Mobility.dynamic);
            //var decal = result.GetComponentAtIndex(0, GameEntity.ComponentType.Decal) as Decal;
            //if (decal != null)
            //{
            //    decal.SetIsVisible(true);
            //    decal.CheckAndRegisterToDecalSet();
            //    Mission.Current.Scene.AddDecalInstance(decal, "editor_set", true);
            //    //decal.SetVectorArgument(1f, 1f, 0.0f, 0.0f);
            //}

            //result.EntityFlags |= EntityFlags.NotAffectedBySeason;
            //result.EntityVisibilityFlags = EntityVisibilityFlags.NoShadow;
            return result;
        }

        public void Update(Vec3 orderPosition, Vec2 direciton, float width, float depth, float rightSideOffset, bool isSelected)
        {
            if (!isSelected)
                return;
            var frontBorder = 0.5f;
            var leftBorder = 0.1f;
            var rightBorder = 0.1f + rightSideOffset;
            var backBorder = 0f;
            var rightVec2 = direciton.RightVec();
            var heightOffset = 1f;
            var frontMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (direciton * frontBorder + rightVec2 * (rightBorder - leftBorder) / 2).ToVec3(), rightVec2, width + leftBorder + rightBorder);
            FrontLine.SetGlobalFrame(frontMatrix);
            FrontLine.SetVisibilityExcludeParents(true);
            // TODO: alpha not working
            FrontLine.SetAlpha(isSelected ? -1f : 0.2f);
            var leftMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (rightVec2 * (-width / 2 - leftBorder) + direciton * (-depth + frontBorder - backBorder) / 2).ToVec3(), direciton, depth + frontBorder + backBorder);
            LeftLine.SetGlobalFrame(leftMatrix);
            LeftLine.SetVisibilityExcludeParents(true);
            LeftLine.SetAlpha(isSelected ? -1f : 0.2f);
            var rightMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (rightVec2 * (width / 2 + rightBorder) + direciton * (-depth + frontBorder - backBorder) / 2).ToVec3(), direciton, depth + frontBorder + backBorder);
            RightLine.SetGlobalFrame(rightMatrix);
            RightLine.SetVisibilityExcludeParents(true);
            RightLine.SetAlpha(isSelected ? -1f : 0.2f);
            float shortLength = MathF.Min(MathF.Clamp(width * 0.1f, 1f, 10f), depth * 0.3f);
            var leftBackmatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (direciton * (-depth - backBorder) + rightVec2 * ((shortLength - width) / 2 - leftBorder)).ToVec3(), rightVec2, shortLength);
            LeftBackLine.SetGlobalFrame(leftBackmatrix);
            LeftBackLine.SetVisibilityExcludeParents(true);
            LeftBackLine.SetAlpha(isSelected ? -1f : 0.2f);
            var rightBackMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (direciton * (-depth - backBorder) + rightVec2 * ((width - shortLength) / 2 + rightBorder)).ToVec3(), rightVec2, shortLength);
            RightBackLine.SetGlobalFrame(rightBackMatrix);
            RightBackLine.SetVisibilityExcludeParents(true);
            RightBackLine.SetAlpha(isSelected ? -1f : 0.2f);
        }

        private MatrixFrame GetMatrixFrame(Vec3 middlePosition, Vec2 lineDirection, float length)
        {
            var matrixFrame = MatrixFrame.Identity;
            matrixFrame.origin = middlePosition;
            matrixFrame.rotation = Mat3.CreateMat3WithForward(lineDirection.ToVec3());
            //matrixFrame.Scale(new Vec3(10, length * 1.095424f, 1f));
            matrixFrame.Scale(new Vec3(0.2f, length, 100f));
            return matrixFrame;
        }

        public void Hide()
        {
            FrontLine.SetVisibilityExcludeParents(false);
            LeftLine.SetVisibilityExcludeParents(false);
            RightLine.SetVisibilityExcludeParents(false);
            LeftBackLine.SetVisibilityExcludeParents(false);
            RightBackLine.SetVisibilityExcludeParents(false);
        }
    }

    public class CommandQueueFormationPreviewData
    {
        public Formation Formation;
        public OrderPreviewData PendingOrder;
        public List<OrderPreviewData> OrderList = new List<OrderPreviewData>();
        public bool IsSelected;
    }


    public class CommandQueuePreview: MissionView
    {
        //public static float a = 1f;
        //public static float minA = 0.25f;

        public static void ClearArrows()
        {
            var preview = Mission.Current.GetMissionBehavior<CommandQueuePreview>();
            preview.HideArrowEntities();
            foreach (var entity in preview._arrowEntities)
            {
                entity.ArrowHead.Remove(0);
                entity.ArrowBody.Remove(0);
            }
            preview._arrowEntities.Clear();
        }
        private CommandSystemConfig _config = CommandSystemConfig.Get();
        private OrderTroopPlacer _orderTroopPlacer;
        private List<GameEntity> _agentPositionEntities;
        private static Material _agentPositionMeshMaterial;
        private List<GameEntity> _orderPositionFlagEntities;
        //private static Material _orderFlagMeshMaterial;
        private List<ArrowEntity> _arrowEntities;
        private List<FormationShapeEntity> _formationShapeEntities;
        private bool _isPreviewShown = false;
        private bool _isFreeCamera;
        public static bool IsPreviewOutdated = false;
        private bool _showAgentFrames = false;
        private Dictionary<Formation, CommandQueueFormationPreviewData> _commandQueuePreviewData;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _orderTroopPlacer = Mission.GetMissionBehavior<OrderTroopPlacer>();
            _agentPositionEntities = new List<GameEntity>();
            _orderPositionFlagEntities = new List<GameEntity>();
            _arrowEntities = new List<ArrowEntity>();
            _formationShapeEntities = new List<FormationShapeEntity>();
            IsPreviewOutdated = true;
            _commandQueuePreviewData = new Dictionary<Formation, CommandQueueFormationPreviewData>();
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
            FormationShapeEntity.Initialize();
        }

        public override void AfterStart()
        {
            base.AfterStart();

            if (Mission.PlayerTeam?.PlayerOrderController == null)
                return;

            Mission.PlayerTeam.PlayerOrderController.OnSelectedFormationsChanged += OnSelectedFormationsChanged;
        }

        private void OnSelectedFormationsChanged()
        {
            IsPreviewOutdated = true;
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            _orderTroopPlacer = null;
            _agentPositionEntities = null;
            _arrowEntities = null;
            _commandQueuePreviewData = null;
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            FormationShapeEntity.Clear();
            if (Mission.PlayerTeam?.PlayerOrderController == null)
                return;

            Mission.PlayerTeam.PlayerOrderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
        }

        private void OnToggleFreeCamera(bool freeCamera)
        {
            _isFreeCamera = freeCamera;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            var commandQueueKey = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.CommandQueue);
            if ((commandQueueKey.IsKeyPressedInOrder() ||
                commandQueueKey.IsKeyReleasedInOrder()))
            {
                Utilities.Utility.UpdateActiveOrders();
            }

            if (_orderTroopPlacer == null)
            {
                return;
            }

            if (_orderTroopPlacer.SuspendTroopPlacer ||
                _config.CommandQueueFlagShowMode == ShowMode.Never && _config.CommandQueueArrowShowMode == ShowMode.Never && _config.CommandQueueFormationShapeShowMode == ShowMode.Never)
            {
                if (_isPreviewShown)
                {
                    HidePreview();
                    _isPreviewShown = false;
                }
            }
            else
            {
                if (!_isPreviewShown)
                {
                    _isPreviewShown = true;
                    IsPreviewOutdated = true;
                }
                UpdatePreview(dt);
            }
        }

        private void UpdatePreview(float dt)
        {
            if (Mission.PlayerTeam?.PlayerOrderController == null)
                return;

            HidePreview();

            if (IsPreviewOutdated)
            {
                _commandQueuePreviewData.Clear();
                //IsPreviewOutdated = false;
                var selectedFormations = Mission.PlayerTeam.FormationsIncludingEmpty;
                foreach (var formation in selectedFormations)
                {
                    if (formation.CountOfUnits == 0)
                        continue;
                    bool isSelected = Mission.PlayerTeam.PlayerOrderController.SelectedFormations.Contains(formation);
                    var commandQueuePreviewData = CollectCommandQueuePreviewData(formation, isSelected);
                    _commandQueuePreviewData[formation] = commandQueuePreviewData;
                }
            }

            TickPreview(dt);
        }

        private Vec3 GetInitialArrowStart(Formation formation)
        {
            var movementState = Utilities.Utility.MovementStateFromMovementOrderType(formation.GetReadonlyMovementOrderReference().OrderType);
            switch (movementState)
            {
                case MovementOrder.MovementStateEnum.Charge:
                case MovementOrder.MovementStateEnum.Hold:
                case MovementOrder.MovementStateEnum.StandGround:
                    if (formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Column)
                        return Utilities.Utility.GetColumnFormationCurrentPosition(formation);
                    return formation.QuerySystem.MedianPosition.GetGroundVec3();
                case MovementOrder.MovementStateEnum.Retreat:
                default:
                    return Vec3.Invalid;
            }
        }

        private void TickPreview(float dt)
        {
            int agentIndex = 0;
            int orderFlagIndex = 0;
            int arrowIndex = 0;
            int formationShapeIndex = 0;
            foreach (var pair in _commandQueuePreviewData)
            {
                var formation = pair.Key;
                var previewData = pair.Value;
                Vec3 arrowStart = GetInitialArrowStart(formation);
                //Vec3 arrowStart = previewData.PendingOrder == null ? formation.OrderGroundPosition : previewData.PendingOrder.OrderPosition.GetGroundVec3();
                int orderRank = 0;
                foreach (var order in previewData.OrderList)
                {
                    foreach (var agentPosition in order.AgentPositions)
                    {
                        AddAgentFrameEntity(agentIndex, agentPosition.GetGroundVec3(), 0.7f);
                        ++agentIndex;
                    }
                    var arrowEnd = order.OrderPosition.GetGroundVec3();
                    if (order.Width != null && order.Depth != null &&
                        (_config.CommandQueueFormationShapeShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueFormationShapeShowMode == ShowMode.FreeCameraOnly))
                    {
                        AddFormationShape(formationShapeIndex, arrowEnd, order.Direction, order.Width.Value, order.Depth.Value, order.RightSideOffset ?? 0, previewData.IsSelected);
                        ++formationShapeIndex;
                    }
                    if (_config.CommandQueueFlagShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueFlagShowMode == ShowMode.FreeCameraOnly)
                    {
                        AddOrderPositionFlag(orderFlagIndex, arrowEnd, order.Direction, previewData.IsSelected ? -1 : 0.2f);
                        ++orderFlagIndex;
                    }
                    if (arrowStart.IsValid && arrowEnd.IsValid && (_config.CommandQueueArrowShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueArrowShowMode == ShowMode.FreeCameraOnly))
                    {
                        var vec = arrowEnd - arrowStart;
                        var length = vec.Normalize();
                        if (length > 5)
                        {
                            var gap = MathF.Clamp(length * 0.1f, 1f, 10f);
                            AddArrow(arrowIndex, arrowStart + vec * gap, arrowEnd - vec * gap, /*MathF.Max(a - orderRank * 0.05f, minA)*/previewData.IsSelected ? -1f : 0.3f, order.OrderTargetType);
                            ++arrowIndex;
                        }
                    }
                    if (order.OrderTargetType == OrderTargetType.Move || order.OrderTargetType == OrderTargetType.Attack)
                        arrowStart = arrowEnd;
                    ++orderRank;
                }
            }
        }

        private CommandQueueFormationPreviewData CollectCommandQueuePreviewData(Formation formation, bool isSelected)
        {
            var result = new CommandQueueFormationPreviewData();
            result.Formation = formation;
            result.IsSelected = isSelected;

            // clear saved moving target
            Patch_OrderController.ClearFormationLivePositionForPreview(formation);

            var targetPreview = CollectFocusPreviewData(formation);
            if (targetPreview != null)
            {
                result.OrderList.Add(targetPreview);
            }
            var facingPreview = CollectFacingPreviewData(formation);
            if (facingPreview != null)
            {
                result.OrderList.Add(facingPreview);
            }

            if (CommandQueueLogic.PendingOrders.TryGetValue(formation, out var pendingOrder))
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(new List<Formation> { formation }));
                var pendingOrderPreviewData = CollectOrderPreviewData(pendingOrder, formation, false, true);
                if (pendingOrderPreviewData != null)
                {
                    result.OrderList.Add(pendingOrderPreviewData);
                }
            }
            foreach (var order in CommandQueueLogic.OrderQueue)
            {
                if (order.RemainingFormations.Contains(formation))
                {
                    Patch_OrderController.LivePreviewFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
                    var orderPreviewData = CollectOrderPreviewData(order, formation);
                    if (orderPreviewData != null)
                    {
                        result.OrderList.Add(orderPreviewData);
                    }
                }
            }
            return result;
        }

        private bool ShouldIncludeFormationShape(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.MoveToLineSegment:
                case OrderType.MoveToLineSegmentWithHorizontalLayout:
                case OrderType.Move:
                case OrderType.FollowMe:
                case OrderType.FollowEntity:
                case OrderType.AttackEntity:
                case OrderType.PointDefence:
                case OrderType.LookAtDirection:
                case OrderType.LookAtEnemy:
                case OrderType.Advance:
                case OrderType.FallBack:
                    {
                        return true;
                    }
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                case OrderType.StandYourGround:
                case OrderType.Retreat:
                case OrderType.ArrangementLine:
                case OrderType.ArrangementCloseOrder:
                case OrderType.ArrangementLoose:
                case OrderType.ArrangementCircular:
                case OrderType.ArrangementSchiltron:
                case OrderType.ArrangementVee:
                case OrderType.ArrangementColumn:
                case OrderType.ArrangementScatter:
                case OrderType.FireAtWill:
                case OrderType.HoldFire:
                case OrderType.Mount:
                case OrderType.Dismount:
                case OrderType.AIControlOn:
                case OrderType.AIControlOff:
                case OrderType.Use:
                    {
                        return false;
                    }
                default:
                    Utility.DisplayMessage("Error: unexpected order type");
                    break;
            }
            return false;
        }

        private static bool UpdateMovingOrderTarget(Formation formation, OrderType? movementOrder, WorldPosition? orderPosition,  Formation targetFormation, Agent targetAgent, IOrderable targetEntity, bool isPendingOrder = false)
        {
            switch (movementOrder)
            {
                case OrderType.MoveToLineSegment:
                case OrderType.MoveToLineSegmentWithHorizontalLayout:
                    {
                        break;
                    }
                case OrderType.Move:
                    {
                        //Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, orderPosition, null, null, null);
                        break;
                    }
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                    {
                        if (isPendingOrder)
                        {
                            targetFormation = formation.TargetFormation;
                        }
                        if (targetFormation == null)
                            return false;
                        var targetPosition = targetFormation.QuerySystem.MedianPosition;
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                        break;
                    }
                case OrderType.FollowMe:
                    {
                        if (isPendingOrder)
                        {
                            targetAgent = formation.GetReadonlyMovementOrderReference()._targetAgent;
                        }
                        if (targetAgent == null)
                            return false;
                        var targetPosition = Patch_OrderController.GetFollowOrderPosition(formation, targetAgent);
                        var direction = Patch_OrderController.GetFormationVirtualDirectionWhenFollowingAgent(formation, targetAgent);
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, direction, null, null);
                        break;
                    }
                case OrderType.Use:
                    {
                        if (targetEntity == null)
                            return false;
                        var usable = targetEntity as UsableMachine;
                        if (usable == null)
                            return false;
                        WorldPosition targetPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, usable.GameEntity.GlobalPosition, hasValidZ: false);
                        targetPosition.SetVec2(targetPosition.AsVec2);
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                        break;
                    }
                case OrderType.FollowEntity:
                    {
                        if (targetEntity == null)
                            return false;
                        var waitEntity = (targetEntity as UsableMachine)?.WaitEntity;
                        if (isPendingOrder)
                        {
                            waitEntity = formation.GetReadonlyMovementOrderReference().TargetEntity;
                        }
                        if (waitEntity == null)
                            return false;
                        Vec2 direction = Patch_OrderController.GetFollowEntityDirection(formation, waitEntity);
                        var targetPosition = Patch_OrderController.GetFollowEntityOrderPosition(formation, waitEntity);
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, direction, null, null);
                        break;
                    }
                case OrderType.AttackEntity:
                    {
                        if (targetEntity == null)
                            return false;
                        var missionObject = targetEntity as MissionObject;
                        if (missionObject == null)
                            return false;
                        var gameEntity = missionObject.GameEntity;
                        if (isPendingOrder)
                        {
                            gameEntity = formation.GetReadonlyMovementOrderReference().TargetEntity;
                        }
                        WorldPosition position = Patch_OrderController.GetAttackEntityWaitPosition(formation, gameEntity);
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                        break;
                    }
                case OrderType.PointDefence:
                    {
                        if (targetEntity == null)
                            return false;
                        var pointDefendable = targetEntity as IPointDefendable;
                        if (pointDefendable == null)
                            return false;
                        var position = pointDefendable.MiddleFrame.Origin;
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                        break;
                    }
                case OrderType.Advance:
                    {
                        if (isPendingOrder)
                        {
                            targetFormation = formation.TargetFormation;
                        }
                        var targetPosition = Patch_OrderController.GetAdvanceOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.None, targetFormation);
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                        break;
                    }
                case OrderType.FallBack:
                    {
                        if (isPendingOrder)
                        {
                            targetFormation = formation.TargetFormation;
                        }
                        var targetPosition = Patch_OrderController.GetFallbackOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.None, targetFormation);
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                        break;
                    }
                case OrderType.StandYourGround:
                case OrderType.Retreat:
                case OrderType.ArrangementLine:
                case OrderType.ArrangementCloseOrder:
                case OrderType.ArrangementLoose:
                case OrderType.ArrangementCircular:
                case OrderType.ArrangementSchiltron:
                case OrderType.ArrangementVee:
                case OrderType.ArrangementColumn:
                case OrderType.ArrangementScatter:
                case OrderType.FireAtWill:
                case OrderType.HoldFire:
                case OrderType.Mount:
                case OrderType.Dismount:
                case OrderType.AIControlOn:
                case OrderType.AIControlOff:
                    return false;
                case null:
                    return false;
                default:
                    Utility.DisplayMessage("Error: unexpected order type");
                    return false;
            }
            return true;
        }

        private OrderPreviewData CollectOrderPreviewData(OrderInQueue order, Formation formation, bool virtualFacingDirection = true, bool isPendingOrder = false)
        {
            var facingOrder = Patch_OrderController.GetFormationVirtualFacingOrder(formation);
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.LookAtEnemy:
                                {
                                    if (order.VirtualFormationChanges.TryGetValue(formation, out var formationChange))
                                    {
                                        var direction = virtualFacingDirection ?
                                            Patch_OrderController.GetVirtualDirectionOfFacingEnemyAccordingToPostitionAndDirection(
                                                formation,
                                                Patch_OrderController.GetFormationVirtualAveragePositionVec2(formation),
                                                Patch_OrderController.GetFormationVirtualDirection(formation)) :
                                            formation.FacingOrder.GetDirection(formation, formation.GetReadonlyMovementOrderReference()._targetAgent);
                                        // formation position can only be set after getting the direction because position will affect result of facing enemy direction.
                                        if (order.TargetFormation != null)
                                        {
                                            var targetPosition = order.TargetFormation.QuerySystem.MedianPosition;
                                            Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                                        }
                                        else
                                        {
                                            UpdateMovingOrderTarget(formation, formationChange.MovementOrderType, formationChange.WorldPosition, formationChange.TargetFormation, formationChange.TargetAgent, formationChange.TargetEntity);
                                        }
                                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                    }
                                    return CollectOrderPreviewData(formation, order.TargetFormation != null ?  false: ShouldIncludeFormationShape(order.OrderType), order.TargetFormation != null ? OrderTargetType.Facing : OrderTargetType.Move);
                                }
                            case OrderType.LookAtDirection:
                                {
                                    if (order.VirtualFormationChanges.TryGetValue(formation, out var formationChange))
                                    {
                                        UpdateMovingOrderTarget(formation, formationChange.MovementOrderType, formationChange.WorldPosition, formationChange.TargetFormation, formationChange.TargetAgent, formationChange.TargetEntity);
                                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, formationChange.Direciton, null, null);
                                    }
                                    return CollectOrderPreviewData(formation, ShouldIncludeFormationShape(order.OrderType));
                                }
                            default:
                                UpdateFacingOrderForOtherOrder(facingOrder, formation, virtualFacingDirection);
                                break;
                        }
                        break;
                    }
                case CustomOrderType.SetTargetFormation:
                    {
                        if (order.TargetFormation == null)
                            return null;
                        var targetPosition = order.TargetFormation.QuerySystem.MedianPosition;
                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                        UpdateFacingOrderForOtherOrder(facingOrder, formation, virtualFacingDirection);
                        return CollectOrderPreviewData(formation, false, OrderTargetType.Focus);
                    }
                case CustomOrderType.StopUsing:
                default:
                    UpdateFacingOrderForOtherOrder(facingOrder, formation, virtualFacingDirection);
                    break;
            }
            UpdateMovingOrderTarget(formation, order.OrderType, order.PositionBegin, order.TargetFormation, order.TargetAgent, order.TargetEntity, isPendingOrder);
            var orderTargetType = GetOrderTargetType(order);
            if (orderTargetType == OrderTargetType.Attack || orderTargetType == OrderTargetType.Move)
            {
                Patch_OrderController.SaveFormationLivePositionForPreview(formation, Patch_OrderController.GetFormationVirtualMedianPosition(formation));
            }
            return CollectOrderPreviewData(formation, ShouldIncludeFormationShape(order.OrderType), orderTargetType);
        }

        private OrderTargetType GetOrderTargetType(OrderInQueue order)
        {
            switch (order.OrderType)
            {
                case OrderType.FollowEntity:
                    return order.IsStopUsing ? OrderTargetType.StopUsing : OrderTargetType.Move;
                case OrderType.Use:
                    return order.IsStopUsing ? OrderTargetType.StopUsing : OrderTargetType.Use;
                default:
                    return GetOrderTargetType(order.OrderType);
            }
        }

        private OrderTargetType GetOrderTargetType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.AttackEntity:
                case OrderType.Charge:
                case OrderType.ChargeWithTarget:
                    return OrderTargetType.Attack;
                case OrderType.MoveToLineSegment:
                case OrderType.MoveToLineSegmentWithHorizontalLayout:
                case OrderType.Move:
                case OrderType.FollowMe:
                    return OrderTargetType.Move;
                case OrderType.FollowEntity:
                    return OrderTargetType.Move;
                case OrderType.Use:
                    return OrderTargetType.Use;
                case OrderType.PointDefence:
                case OrderType.LookAtDirection:
                case OrderType.LookAtEnemy:
                case OrderType.Advance:
                case OrderType.FallBack:
                case OrderType.StandYourGround:
                case OrderType.Retreat:
                case OrderType.ArrangementLine:
                case OrderType.ArrangementCloseOrder:
                case OrderType.ArrangementLoose:
                case OrderType.ArrangementCircular:
                case OrderType.ArrangementSchiltron:
                case OrderType.ArrangementVee:
                case OrderType.ArrangementColumn:
                case OrderType.ArrangementScatter:
                case OrderType.FireAtWill:
                case OrderType.HoldFire:
                case OrderType.Mount:
                case OrderType.Dismount:
                case OrderType.AIControlOn:
                case OrderType.AIControlOff:
                    return OrderTargetType.Move;
                default:
                    Utility.DisplayMessage("Error: unexpected order type");
                    break;
            }
            return OrderTargetType.Move;
        }

        private void UpdateFacingOrderForOtherOrder(OrderType facingOrder, Formation formation, bool virtualFacingDirection)
        {
            switch (facingOrder)
            {
                case OrderType.LookAtEnemy:
                    var previousDirection = Patch_OrderController.GetFormationVirtualDirection(formation);
                    if (Utilities.Utility.ShouldQueueCommand())
                    {
                        previousDirection = Patch_OrderController.GetFormationVirtualDirectionIncludingFacingEnemyAccordingToPositionAndDirection(
                            formation,
                            Patch_OrderController.GetFormationVirtualPositionVec2(formation),
                            Patch_OrderController.GetFormationVirtualDirection(formation)
                        );
                    }
                    var direction = virtualFacingDirection ?
                        Patch_OrderController.GetVirtualDirectionOfFacingEnemyAccordingToPostitionAndDirection(
                            formation,
                            Patch_OrderController.GetFormationVirtualAveragePositionVec2(formation),
                            previousDirection) :
                        formation.FacingOrder.GetDirection(formation, formation.GetReadonlyMovementOrderReference()._targetAgent);
                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                    break;
                case OrderType.LookAtDirection:
                    // do nothing as the positon and direciton is already in order.VirtualFormationChanges
                    break;
            }
        }

        private void AddAgentFrameEntity(int index, Vec3 groundPosition, float alpha)
        {
            while (_agentPositionEntities.Count <= index)
            {
                GameEntity empty = GameEntity.CreateEmpty(Mission.Scene);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("barrier_sphere");
                //MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                if (_agentPositionMeshMaterial == (NativeObject)null)
                {
                    //_agentPositionMeshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                    //_agentPositionMeshMaterial = Material.GetFromResource("vertex_color_blend_mat
                    _agentPositionMeshMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy();
                    //_agentPositionMeshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                }
                copy.SetMaterial(_agentPositionMeshMaterial);
                copy.SetFactor1(Patch_OrderTroopPlacer.OrderPositionEntityColor);
                empty.AddComponent((GameEntityComponent)copy);
                empty.SetVisibilityExcludeParents(false);
                _agentPositionEntities.Add(empty);
            }
            GameEntity agentPositionEntity = _agentPositionEntities[index];
            MatrixFrame frame = new MatrixFrame(Mat3.Identity, groundPosition + (Vec3.Up * 1.0f));
            agentPositionEntity.SetFrame(ref frame);
            if ((double)alpha != -1.0)
            {
                agentPositionEntity.SetVisibilityExcludeParents(true);
                agentPositionEntity.SetAlpha(alpha);
            }
            else
                agentPositionEntity.FadeIn();
        }

        private void AddFormationShape(int index, Vec3 orderPosition, Vec2 direciton, float width, float depth, float rightSideOffset, bool isSelected)
        {
            while (_formationShapeEntities.Count <= index)
            {
                var entity = new FormationShapeEntity();
                entity.CreateEntities();
                _formationShapeEntities.Add(entity);
            }

            var formationShapeEntity = _formationShapeEntities[index];

            formationShapeEntity.Update(orderPosition, direciton, width, depth, rightSideOffset, isSelected);
        }

        private void AddOrderPositionFlag(int index, Vec3 groundPosition, Vec2 direction, float alpha)
        {
            while (_orderPositionFlagEntities.Count <= index)
            {
                GameEntity empty = GameEntity.CreateEmpty(Mission.Scene);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("order_flag_a");
                //MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                //if (_orderFlagMeshMaterial== (NativeObject)null)
                //{
                //    _orderFlagMeshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                //    //_orderFlagMeshMaterial = Material.GetFromResource("vertex_color_blend_mat
                //    //_orderFlagMeshMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                //    _orderFlagMeshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                //}
                //copy.SetMaterial(_orderFlagMeshMaterial);
                //copy.SetFactor1(new Color(80, 255, 80, alpha).ToUnsignedInteger());
                empty.AddComponent((GameEntityComponent)copy);
                empty.SetVisibilityExcludeParents(false);
                _orderPositionFlagEntities.Add(empty);
            }
            GameEntity orderPositionEntity = _orderPositionFlagEntities[index];
            MatrixFrame frame = new MatrixFrame(Mat3.CreateMat3WithForward(direction.ToVec3(0)), groundPosition);
            frame.Scale(new Vec3(30, 30, 30));
            orderPositionEntity.SetFrame(ref frame);
            if ((double)alpha != -1.0)
            {
                orderPositionEntity.SetVisibilityExcludeParents(true);
                orderPositionEntity.SetAlpha(alpha);
            }
            else
                orderPositionEntity.FadeIn();
        }

        private void AddArrow(int index, Vec3 arrowStart, Vec3 arrowEnd, float alpha, OrderTargetType orderTargetType)
        {
            while (_arrowEntities.Count <= index)
            {
                //GameEntity empty = GameEntity.CreateEmpty(Mission.Scene);
                //empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                //MetaMesh copy = MetaMesh.GetCopy("order_arrow_a");
                ////MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                //if (_arrowMaterial == (NativeObject)null)
                //{
                //    //_arrowMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                //    _arrowMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy();
                //    //_arrowMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                //    //_arrowMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                //}
                //copy.SetMaterial(_arrowMaterial);
                //empty.AddComponent(copy);
                //copy.SetFactor1(new Color(r, g, b).ToUnsignedInteger());
                //empty.SetVisibilityExcludeParents(false);
                var newArrowEntity = new ArrowEntity
                {
                    ArrowHead = GameEntity.CreateEmpty(Mission.Scene),
                    ArrowBody = GameEntity.CreateEmpty(Mission.Scene)
                };
                var headMesh = MetaMesh.GetCopy("rts_arrow_head");
                var bodyMesh = MetaMesh.GetCopy("rts_arrow_body");
                //var bodyMaterial = bodyMesh.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                //bodyMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                //bodyMaterial.SetShader(Shader.GetFromResource("pbr_shading"));
                //bodyMesh.SetMaterial(bodyMaterial);
                newArrowEntity.ArrowHead.AddComponent(headMesh);
                newArrowEntity.ArrowBody.AddComponent(bodyMesh);
                newArrowEntity.ArrowHead.EntityFlags |= EntityFlags.NotAffectedBySeason;
                newArrowEntity.ArrowHead.EntityVisibilityFlags = EntityVisibilityFlags.NoShadow;
                newArrowEntity.ArrowBody.EntityFlags |= EntityFlags.NotAffectedBySeason;
                newArrowEntity.ArrowBody.EntityVisibilityFlags = EntityVisibilityFlags.NoShadow;
                //newArrowEntity.ArrowHead.SetFactorColor(new Color(r, g, b).ToUnsignedInteger());
                //newArrowEntity.ArrowBody.SetFactorColor(new Color(r, g, b).ToUnsignedInteger());
                newArrowEntity.ArrowHead.SetVisibilityExcludeParents(false);
                newArrowEntity.ArrowBody.SetVisibilityExcludeParents(false);
                _arrowEntities.Add(newArrowEntity);
            }
            var arrowEntity = _arrowEntities[index];
            arrowEntity.UpdateColor(orderTargetType);
            var direction = arrowEnd - arrowStart;
            var length = direction.Normalize();
            var basicScale = 10f;
            var height = 2f;
            if (orderTargetType == OrderTargetType.Facing)
            {
                basicScale = 7.5f;
                height = 2.01f;
                var startOffset = MathF.Min(length * 0.2f, 0.2f);
                arrowStart += startOffset * direction;
                length -= startOffset;
            }
            var connectPointToArrowEndDistance = 2.7f;
            MatrixFrame headFrame = new MatrixFrame(Mat3.CreateMat3WithForward(-direction), arrowStart + (length - connectPointToArrowEndDistance) * direction + Vec3.Up * height);
            MatrixFrame bodyFrame = new MatrixFrame(Mat3.CreateMat3WithForward(-direction), arrowStart + Vec3.Up * height);
            headFrame.Scale(new Vec3(basicScale, basicScale, 1));
            // original length = x
            // x * k = 1
            // scale * (x * k) = length - connectPointToArrowEndDistance
            bodyFrame.Scale(new Vec3(basicScale, (length - connectPointToArrowEndDistance) * 1.335942f, 1));
            arrowEntity.ArrowHead.SetFrame(ref headFrame);
            arrowEntity.ArrowBody.SetFrame(ref bodyFrame);
            if ((double)alpha != -1.0)
            {
                arrowEntity.ArrowHead.SetVisibilityExcludeParents(true);
                arrowEntity.ArrowBody.SetVisibilityExcludeParents(true);
                arrowEntity.ArrowHead.SetAlpha(alpha);
                arrowEntity.ArrowBody.SetAlpha(alpha);
            }
            else
            {
                arrowEntity.ArrowHead.FadeIn();
                arrowEntity.ArrowBody.FadeIn();
            }
        }

        private void HidePreview()
        {
            HideAgentFrameEntities();
            HideOrderPositionFlagEntities();
            HideArrowEntities();
            HideFormationShapes();
        }

        private void HideAgentFrameEntities()
        {
            foreach (GameEntity agentPositionEntity in _agentPositionEntities)
                agentPositionEntity.HideIfNotFadingOut();
        }

        private void HideOrderPositionFlagEntities()
        {
            foreach (GameEntity orderPositionFlagEntity in _orderPositionFlagEntities)
                orderPositionFlagEntity.HideIfNotFadingOut();
        }

        private void HideArrowEntities()
        {
            foreach(var arrowEntity in _arrowEntities)
            {
                arrowEntity.ArrowHead.HideIfNotFadingOut();
                arrowEntity.ArrowBody.HideIfNotFadingOut(); 
            }
        }

        private void HideFormationShapes()
        {
            foreach (var formationShape in _formationShapeEntities)
            {
                formationShape.Hide();
            }
        }

        private OrderPreviewData CollectFocusPreviewData(Formation formation)
        {
            if (formation.TargetFormation == null)
            {
                return null;
            }
            var orderTargetType = GetOrderTargetType(formation.GetReadonlyMovementOrderReference().OrderType);
            if (orderTargetType == OrderTargetType.Attack)
            {
                // for attack order type
                // the preview will be included in pended order so we don't need to include it here.
                return null;
            }
            var targetPosition = formation.TargetFormation.QuerySystem.MedianPosition;
            Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
            return CollectOrderPreviewData(formation, false, OrderTargetType.Focus);
        }

        private OrderPreviewData CollectFacingPreviewData(Formation formation)
        {
            if (formation.FacingOrder.OrderType == OrderType.LookAtEnemy)
            {
                var targetFacingEnemy = Patch_OrderController.GetFacingEnemyTargetFormation(formation);
                if (targetFacingEnemy != null)
                {
                    var targetPosition1 = targetFacingEnemy.QuerySystem.MedianPosition;
                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition1, null, null, null);
                    return CollectOrderPreviewData(formation, false, OrderTargetType.Facing);
                }
            }
            return null;
        }

        private OrderPreviewData CollectOrderPreviewData(Formation formation, bool includeFormationShape, OrderTargetType orderTargetType = OrderTargetType.Move)
        {
            float? previeweWidth = null, previewDepth = null, previewRightSideOffset = null;
            if (includeFormationShape)
            {
                Patch_OrderController.GetFormationVirtualShape(formation, out var width, out var depth, out var rightSideOffset);
                previeweWidth = width;
                previewDepth = depth;
                previewRightSideOffset = rightSideOffset;
            }
            if (_showAgentFrames)
            {
                Patch_OrderController.SimulateAgentFrames(new List<Formation> { formation },
                    Mission.PlayerTeam.PlayerOrderController.simulationFormations,
                    out var simulationFormationChanges);
                return new OrderPreviewData
                {
                    AgentPositions = simulationFormationChanges,
                    OrderPosition = Patch_OrderController.GetFormationVirtualPosition(formation),
                    Direction = Patch_OrderController.GetFormationVirtualDirection(formation),
                    Width = previeweWidth,
                    Depth = previewDepth,
                    RightSideOffset = previewRightSideOffset,
                    OrderTargetType = orderTargetType
                };
            }
            else
            {
                return new OrderPreviewData
                {
                    OrderPosition = Patch_OrderController.GetFormationVirtualPosition(formation),
                    Direction = Patch_OrderController.GetFormationVirtualDirection(formation),
                    Width = previeweWidth,
                    Depth = previewDepth,
                    RightSideOffset = previewRightSideOffset,
                    OrderTargetType = orderTargetType
                };
            }
        }
    }
}
