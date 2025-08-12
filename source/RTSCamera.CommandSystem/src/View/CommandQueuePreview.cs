using MissionLibrary.Event;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Patch;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera.CommandSystem.View
{
    public class OrderPreviewData
    {
        public WorldPosition OrderPosition;
        public Vec2 Direction;
        public List<WorldPosition> AgentPositions = new List<WorldPosition>();

    }

    public class CommandQueueFormationPreviewData
    {
        public Formation Formation;
        public OrderPreviewData PendingOrder;
        public List<OrderPreviewData> OrderList = new List<OrderPreviewData>();
    }


    public class CommandQueuePreview: MissionView
    {
        public static float r = 0f;
        public static float g = 0.4f;
        public static float b = 0f;
        public static float a = 0.5f;
        public static float minA = 0.25f;
        public static void ClearArrows()
        {
            var preview = Mission.Current.GetMissionBehavior<CommandQueuePreview>();
            preview.HideArrowEntities();
            foreach (var entity in preview._arrowEntities)
            {
                entity.Remove(0);
            }
            preview._arrowEntities.Clear();
        }
        private CommandSystemConfig _config = CommandSystemConfig.Get();
        private OrderTroopPlacer _orderTroopPlacer;
        private List<GameEntity> _agentPositionEntities;
        private static Material _agentPositionMeshMaterial;
        private List<GameEntity> _orderPositionFlagEntities;
        //private static Material _orderFlagMeshMaterial;
        private List<GameEntity> _arrowEntities;
        private static Material _arrowMaterial;
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
            _arrowEntities = new List<GameEntity>();
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
                _config.CommandQueueFlagShowMode == ShowMode.Never && _config.CommandQueueArrowShowMode == ShowMode.Never)
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

        private void TickPreview(float dt)
        {
            int agentIndex = 0;
            int orderFlagIndex = 0;
            int arrowIndex = 0;
            foreach (var pair in _commandQueuePreviewData)
            {
                var formation = pair.Key;
                var previewData = pair.Value;
                Vec3 arrowStart = previewData.PendingOrder == null ? formation.OrderGroundPosition : previewData.PendingOrder.OrderPosition.GetGroundVec3();
                int orderRank = 0;
                foreach (var order in previewData.OrderList)
                {
                    foreach (var agentPosition in order.AgentPositions)
                    {
                        AddAgentFrameEntity(agentIndex, agentPosition.GetGroundVec3(), 0.7f);
                        ++agentIndex;
                    }
                    var arrowEnd = order.OrderPosition.GetGroundVec3();
                    if (_config.CommandQueueFlagShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueFlagShowMode == ShowMode.FreeCameraOnly)
                    {
                        AddOrderPositionFlag(orderFlagIndex, arrowEnd, order.Direction, 0.7f);
                        ++orderFlagIndex;
                    }
                    if (arrowStart.IsValid && arrowEnd.IsValid && (_config.CommandQueueArrowShowMode == ShowMode.Always || _isFreeCamera && _config.CommandQueueArrowShowMode == ShowMode.FreeCameraOnly))
                    {
                        var vec = arrowEnd - arrowStart;
                        var length = vec.Normalize();
                        if (length > 3)
                        {
                            var gap = MathF.Max(length * 0.1f, 1f);
                            AddArrow(arrowIndex, arrowStart + vec * gap, arrowEnd - vec * gap, MathF.Max(a - orderRank * 0.05f, minA));
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
                result.PendingOrder = CollectOrderPreviewData(pendingOrder, formation);
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
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.Move:
                                {
                                    var position = order.PositionBegin;
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.Charge:
                            case OrderType.ChargeWithTarget:
                                {
                                    if (order.TargetFormation != null)
                                    {
                                        Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, order.TargetFormation.QuerySystem.MedianPosition, null, null, null);
                                        return CollectOrderPreviewData(formation);
                                    }
                                }
                                return null;
                            case OrderType.LookAtEnemy:
                                {
                                    var direction = Patch_OrderController.GetVirtualDirectionOfFacingEnemy(formation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.FollowMe:
                                {
                                    var targetPosition = Patch_OrderController.GetFollowOrderPosition(formation, order.TargetAgent);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, null, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.FollowEntity:
                                {
                                    var waitEntity = (order.TargetEntity as UsableMachine).WaitEntity;
                                    Vec2 direction = Patch_OrderController.GetFollowEntityDirection(formation, waitEntity);
                                    var targetPosition = Patch_OrderController.GetFollowEntityOrderPosition(formation, waitEntity);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, direction, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.AttackEntity:
                                {
                                    var missionObject = order.TargetEntity as MissionObject;
                                    var gameEntity = missionObject.GameEntity;
                                    WorldPosition position = Patch_OrderController.GetAttackEntityWaitPosition(formation, gameEntity);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.PointDefence:
                                {
                                    var pointDefendable = order.TargetEntity as IPointDefendable;
                                    var position = pointDefendable.MiddleFrame.Origin;
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, position, null, null, null);
                                    return CollectOrderPreviewData(formation);
                                }

                            case OrderType.LookAtDirection:
                                {
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.Advance:
                                {
                                    var targetPosition = Patch_OrderController.GetAdvanceOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.None, order.TargetFormation);
                                    var targetDirection = Patch_OrderController.GetAdvanceOrFallbackOrderDirection(formation, order.TargetFormation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, targetDirection, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.FallBack:
                                {
                                    var targetPosition = Patch_OrderController.GetFallbackOrderPosition(formation, WorldPosition.WorldPositionEnforcedCache.None, order.TargetFormation);
                                    var targetDirection = Patch_OrderController.GetAdvanceOrFallbackOrderDirection(formation, order.TargetFormation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, targetPosition, targetDirection, null, null);
                                    return CollectOrderPreviewData(formation);
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
                MetaMesh copy = MetaMesh.GetCopy("pyhsics_test_box");
                //MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                if (_agentPositionMeshMaterial == (NativeObject)null)
                {
                    //_agentPositionMeshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                    //_agentPositionMeshMaterial = Material.GetFromResource("vertex_color_blend_mat
                    _agentPositionMeshMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                    //_agentPositionMeshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                }
                copy.SetMaterial(_agentPositionMeshMaterial);
                copy.SetFactor1(new Color(80, 255, 80, alpha).ToUnsignedInteger());
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
                GameEntity empty = GameEntity.CreateEmpty(Mission.Scene);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("order_arrow_a");
                //MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                if (_arrowMaterial == (NativeObject)null)
                {
                    //_arrowMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                    _arrowMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy();
                    //_arrowMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                    //_arrowMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                }
                copy.SetMaterial(_arrowMaterial);
                empty.AddComponent(copy);
                copy.SetFactor1(new Color(r, g, b).ToUnsignedInteger());
                empty.SetVisibilityExcludeParents(false);
                _arrowEntities.Add(empty);
            }
            GameEntity arrowEntity = _arrowEntities[index];
            var direction = arrowEnd - arrowStart;
            var length = direction.Normalize();
            var scale = length / 2;
            MatrixFrame frame = new MatrixFrame(Mat3.CreateMat3WithForward(direction), arrowStart + Vec3.Up * 5f);
            frame.Scale(new Vec3(scale, scale, scale));
            //arrowEntity.GetMetaMesh(0).SetFactor1(new Color(r, g, b).ToUnsignedInteger());
            arrowEntity.SetFrame(ref frame);
            if ((double)alpha != -1.0)
            {
                arrowEntity.SetVisibilityExcludeParents(true);
                arrowEntity.SetAlpha(alpha);
            }
            else
                arrowEntity.FadeIn();
        }

        private void HidePreview()
        {
            HideAgentFrameEntities();
            HideOrderPositionFlagEntities();
            HideArrowEntities();

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
                arrowEntity.HideIfNotFadingOut();
            }
        }

        private OrderPreviewData CollectOrderPreviewData(Formation formation)
        {
            if (_showAgentFrames)
            {
                Patch_OrderController.SimulateAgentFrames(new List<Formation> { formation },
                    Mission.PlayerTeam.PlayerOrderController.simulationFormations,
                    out var simulationFormationChanges);
                return new OrderPreviewData
                {
                    AgentPositions = simulationFormationChanges,
                    OrderPosition = Patch_OrderController.GetFormationVirtualPosition(formation),
                    Direction = Patch_OrderController.GetFormationVirtualDirection(formation)
                };
            }
            else
            {
                return new OrderPreviewData
                {
                    OrderPosition = Patch_OrderController.GetFormationVirtualPosition(formation),
                    Direction = Patch_OrderController.GetFormationVirtualDirection(formation)
                };
            }
        }
    }
}
