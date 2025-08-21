using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
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
    public class OrderPreviewData
    {
        public WorldPosition OrderPosition;
        public float? Width;
        public float? Depth;
        public Vec2 Direction;
        public List<WorldPosition> AgentPositions = new List<WorldPosition>();

    }

    public struct ArrowEntity
    {
        public GameEntity ArrowHead;
        public GameEntity ArrowBody;
    }

    public struct FormationShapeEntity
    {
        public GameEntity FrontLine;
        public GameEntity LeftLine;
        public GameEntity RightLine;
        public GameEntity LeftBackLine;
        public GameEntity RightBackLine;

        private static Material _decalMaterial;
        private static MetaMesh _lineMesh;

        public static uint FormationShapeColor = new Color(0.7f, 1, 0.7f).ToUnsignedInteger();

        public void Initialize()
        {
            FrontLine = CreateLineEntity();
            LeftLine = CreateLineEntity();
            RightLine = CreateLineEntity();
            LeftBackLine = CreateLineEntity();
            RightBackLine = CreateLineEntity();
        }

        private GameEntity CreateLineEntity()
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

        public void Update(Vec3 orderPosition, Vec2 direciton, float width, float depth)
        {
            var frontBorder = 1f;
            var leftBorder = 1f;
            var rightBorder = 1f;
            var backBorder = 0f;
            var rightVec2 = direciton.RightVec();
            var heightOffset = -1f;
            var frontMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (direciton * frontBorder + rightVec2 * (rightBorder - leftBorder) / 2).ToVec3(), rightVec2, width + leftBorder + rightBorder);
            FrontLine.SetFrame(ref frontMatrix);
            FrontLine.SetVisibilityExcludeParents(true);
            var leftMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (rightVec2 * (-width / 2 - leftBorder) + direciton * (-depth + frontBorder - backBorder) / 2).ToVec3(), direciton, depth + frontBorder + backBorder);
            LeftLine.SetFrame(ref leftMatrix);
            LeftLine.SetVisibilityExcludeParents(true);
            var rightMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (rightVec2 * (width / 2 + rightBorder) + direciton * (-depth + frontBorder - backBorder) / 2).ToVec3(), direciton, depth + frontBorder + backBorder);
            RightLine.SetFrame(ref rightMatrix);
            RightLine.SetVisibilityExcludeParents(true);
            float shortLength = MathF.Min(MathF.Clamp(width * 0.1f, 1f, 10f), depth * 0.8f);
            var leftBackmatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (direciton * (-depth - backBorder) + rightVec2 * ((shortLength - width) / 2 - leftBorder)).ToVec3(), rightVec2, shortLength);
            LeftBackLine.SetFrame(ref leftBackmatrix);
            LeftBackLine.SetVisibilityExcludeParents(true);
            var rightBackMatrix = GetMatrixFrame(orderPosition + Vec3.Up * heightOffset + (direciton * (-depth - backBorder) + rightVec2 * ((width - shortLength) / 2 + rightBorder)).ToVec3(), rightVec2, shortLength);
            RightBackLine.SetFrame(ref rightBackMatrix);
            RightBackLine.SetVisibilityExcludeParents(true);
        }

        private MatrixFrame GetMatrixFrame(Vec3 middlePosition, Vec2 lineDirection, float length)
        {
            var matrixFrame = MatrixFrame.Identity;
            matrixFrame.origin = middlePosition;
            matrixFrame.rotation = Mat3.CreateMat3WithForward(lineDirection.ToVec3());
            //matrixFrame.Scale(new Vec3(10, length * 1.095424f, 1f));
            matrixFrame.Scale(new Vec3(0.2f, length, 20f));
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
    }


    public class CommandQueuePreview: MissionView
    {
        //public static float a = 1f;
        //public static float minA = 0.25f;

        public static uint ArrowColor = new Color(0.4f, 0.8f, 0.4f).ToUnsignedInteger();

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
            if ((commandQueueKey.IsKeyPressedInOrder(Utility.GetMissionScreen().SceneLayer.Input) ||
                commandQueueKey.IsKeyReleasedInOrder(Utility.GetMissionScreen().SceneLayer.Input)))
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
                var selectedFormations = Mission.PlayerTeam.PlayerOrderController.SelectedFormations;
                foreach (var formation in selectedFormations)
                {
                    var commandQueuePreviewData = CollectCommandQueuePreviewData(formation);
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
                    return formation.QuerySystem.MedianPosition.GetGroundVec3();
                case MovementOrder.MovementStateEnum.Retreat:
                case MovementOrder.MovementStateEnum.StandGround:
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
                        _config.CommandQueueFormationShapeShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueFormationShapeShowMode == ShowMode.FreeCameraOnly)
                    {
                        AddFormationShape(formationShapeIndex, arrowEnd, order.Direction, order.Width.Value, order.Depth.Value);
                        ++formationShapeIndex;
                    }
                    if (_config.CommandQueueFlagShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueFlagShowMode == ShowMode.FreeCameraOnly)
                    {
                        AddOrderPositionFlag(orderFlagIndex, arrowEnd, order.Direction, -1f);
                        ++orderFlagIndex;
                    }
                    if (arrowStart.IsValid && arrowEnd.IsValid && (_config.CommandQueueArrowShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueArrowShowMode == ShowMode.FreeCameraOnly))
                    {
                        var vec = arrowEnd - arrowStart;
                        var length = vec.Normalize();
                        if (length > 5)
                        {
                            var gap = MathF.Clamp(length * 0.1f, 1f, 10f);
                            AddArrow(arrowIndex, arrowStart + vec * gap, arrowEnd - vec * gap, /*MathF.Max(a - orderRank * 0.05f, minA)*/-1f);
                            ++arrowIndex;
                        }
                    }
                    arrowStart = arrowEnd;
                    ++orderRank;
                }
            }
        }

        private CommandQueueFormationPreviewData CollectCommandQueuePreviewData(Formation formation)
        {
            var result = new CommandQueueFormationPreviewData();
            result.Formation = formation;

            if (CommandQueueLogic.PendingOrders.TryGetValue(formation, out var pendingOrder))
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(new List<Formation> { formation }));
                var pendingOrderPreviewData = CollectOrderPreviewData(pendingOrder, formation);
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

        private OrderPreviewData CollectOrderPreviewData(OrderInQueue order, Formation formation)
        {
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.MoveToLineSegment:
                            case OrderType.MoveToLineSegmentWithHorizontalLayout:
                                {
                                    if (order.IsLineShort)
                                    {
                                        switch (Patch_OrderController.GetFormationVirtualFacingOrder(formation))
                                        {
                                            case OrderType.LookAtEnemy:
                                                var direction = Patch_OrderController.GetVirtualDirectionOfFacingEnemy(formation);
                                                Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                                break;
                                            case OrderType.LookAtDirection:
                                                // do nothing as the positon and direciton is already in order.VirtualFormationChanges
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        // do nothing as the positon and direciton is already in order.VirtualFormationChanges
                                    }
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.Move:
                                {
                                    var position = order.PositionBegin;
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.Charge:
                            case OrderType.ChargeWithTarget:
                                {
                                    if (order.TargetFormation != null)
                                    {
                                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, order.TargetFormation.QuerySystem.MedianPosition, null, null, null);
                                        return CollectOrderPreviewData(formation, false);
                                    }
                                }
                                return null;
                            case OrderType.LookAtEnemy:
                                {
                                    var direction = Patch_OrderController.GetVirtualDirectionOfFacingEnemy(formation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.FollowMe:
                                {
                                    var targetPosition = Patch_OrderController.GetFollowOrderPosition(formation, order.TargetAgent);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.FollowEntity:
                                {
                                    var waitEntity = (order.TargetEntity as UsableMachine).WaitEntity;
                                    Vec2 direction = Patch_OrderController.GetFollowEntityDirection(formation, waitEntity);
                                    var targetPosition = Patch_OrderController.GetFollowEntityOrderPosition(formation, waitEntity);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, direction, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.AttackEntity:
                                {
                                    var missionObject = order.TargetEntity as MissionObject;
                                    var gameEntity = missionObject.GameEntity;
                                    WorldPosition position = Patch_OrderController.GetAttackEntityWaitPosition(formation, gameEntity);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.PointDefence:
                                {
                                    var pointDefendable = order.TargetEntity as IPointDefendable;
                                    var position = pointDefendable.MiddleFrame.Origin;
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }

                            case OrderType.LookAtDirection:
                                {
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.Advance:
                                {
                                    var targetPosition = Patch_OrderController.GetAdvanceOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.None, order.TargetFormation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                                    return CollectOrderPreviewData(formation, true);
                                }
                            case OrderType.FallBack:
                                {
                                    var targetPosition = Patch_OrderController.GetFallbackOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.None, order.TargetFormation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                                    return CollectOrderPreviewData(formation, true);
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
                                return null;
                            default:
                                Utility.DisplayMessage("Error: unexpected order type");
                                break;
                        }
                        break;
                    }
                case CustomOrderType.FollowMainAgent:
                    return null;
                case CustomOrderType.SetTargetFormation:
                    return null;
            }
            return null;
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

        private void AddFormationShape(int index, Vec3 orderPosition, Vec2 direciton, float width, float depth)
        {
            while (_formationShapeEntities.Count <= index)
            {
                var entity = new FormationShapeEntity();
                entity.Initialize();
                _formationShapeEntities.Add(entity);
            }

            var formationShapeEntity = _formationShapeEntities[index];

            formationShapeEntity.Update(orderPosition, direciton, width, depth);
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

        private void AddArrow(int index, Vec3 arrowStart, Vec3 arrowEnd, float alpha)
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
                var color = ArrowColor;
                headMesh.SetFactor1(color);
                headMesh.SetContourColor(color);
                headMesh.SetContourState(true);
                bodyMesh.SetFactor1(color);
                bodyMesh.SetContourColor(color);
                bodyMesh.SetContourState(true);
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
            var direction = arrowEnd - arrowStart;
            var length = direction.Normalize();
            var scale = length;
            var basicScale = 10f;
            var connectPointToArrowEndDistance = 0.27f * basicScale;
            MatrixFrame headFrame = new MatrixFrame(Mat3.CreateMat3WithForward(-direction), arrowStart + (1f * length - connectPointToArrowEndDistance) * direction + Vec3.Up * 2f);
            MatrixFrame bodyFrame = new MatrixFrame(Mat3.CreateMat3WithForward(-direction), arrowStart + Vec3.Up * 2f);
            headFrame.Scale(new Vec3(basicScale, basicScale, -1));
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
                //arrowEntity.ArrowHead.SetAlpha(alpha);
                //arrowEntity.ArrowBody.SetAlpha(alpha);
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

        private OrderPreviewData CollectOrderPreviewData(Formation formation, bool includeFormatioShape)
        {
            float? previeweWidth = null, previewDepth = null;
            if (includeFormatioShape)
            {
                Patch_OrderController.GetFormationVirtualShape(formation, out var width, out var depth);
                previeweWidth = width;
                previewDepth = depth;
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
                    Depth = previewDepth
                };
            }
            else
            {
                return new OrderPreviewData
                {
                    OrderPosition = Patch_OrderController.GetFormationVirtualPosition(formation),
                    Direction = Patch_OrderController.GetFormationVirtualDirection(formation),
                    Width = previeweWidth,
                    Depth = previewDepth
                };
            }
        }
    }
}
