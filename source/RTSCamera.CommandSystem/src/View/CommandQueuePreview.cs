using MissionSharedLibrary.Utilities;
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

namespace RTSCamera.CommandSystem.View
{
    public class OrderPreviewData
    {
        public WorldPosition OrderPosition;
        public Vec2 Direction;
        public List<WorldPosition> AgentPositions = new List<WorldPosition>();
    }

    public class CommandQueuePreviewData
    {
        public Formation Formation;
        public List<OrderPreviewData> OrderList = new List<OrderPreviewData>();
    }


    public class CommandQueuePreview: MissionView
    {
        private OrderTroopPlacer _orderTroopPlacer;
        private List<GameEntity> _agentPositionEntities;
        private static Material _agentPositionMeshMaterial;
        private List<GameEntity> _orderPositionFlagEntities;
        private static Material _orderFlagMeshMaterial;
        private bool _isPreviewShown = false;
        public static bool IsPreviewOutdated = false;
        private bool _showAgentFrames = false;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _orderTroopPlacer = Mission.GetMissionBehavior<OrderTroopPlacer>();
            _agentPositionEntities = new List<GameEntity>();
            _orderPositionFlagEntities = new List<GameEntity>();
            IsPreviewOutdated = true;
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
            if (Mission.PlayerTeam?.PlayerOrderController == null)
                return;

            Mission.PlayerTeam.PlayerOrderController.OnSelectedFormationsChanged -= OnSelectedFormationsChanged;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (_orderTroopPlacer == null)
            {
                return;
            }

            if (_orderTroopPlacer.SuspendTroopPlacer)
            {
                HidePreview();
                _isPreviewShown = false;
            }
            else
            {
                if (!_isPreviewShown)
                {
                    _isPreviewShown = true;
                    IsPreviewOutdated = true;
                }
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (Mission.PlayerTeam?.PlayerOrderController == null)
                return;
            if (!IsPreviewOutdated)
                return;
            IsPreviewOutdated = false;
            HidePreview();
            var selectedFormations = Mission.PlayerTeam.PlayerOrderController.SelectedFormations;
            int agentIndex = 0;
            int orderFlagIndex = 0;
            foreach (var formation in selectedFormations)
            {
                var commandQueuePreviewData = CollectCommandQueuePreviewData(formation);
                foreach (var order in commandQueuePreviewData.OrderList)
                {
                    foreach (var agentPosition in order.AgentPositions)
                    {
                        AddAgentFrameEntity(agentIndex, agentPosition.GetGroundVec3(), 0.7f);
                        ++agentIndex;
                    }
                    AddOrderPositionFlag(orderFlagIndex, order.OrderPosition.GetGroundVec3(), order.Direction, 0.7f);
                    ++orderFlagIndex;
                }
            }
        }


        private CommandQueuePreviewData CollectCommandQueuePreviewData(Formation formation)
        {
            var result = new CommandQueuePreviewData();
            result.Formation = formation;
            foreach (var order in CommandQueueLogic.OrderQueue)
            {
                if (order.RemainingFormations.Contains(formation))
                {
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
            Patch_OrderController.LivePreviewFormationChanges.SetChanges(order.VirtualFormationChanges.Where(pair => pair.Key == formation));
            switch (order.CustomOrderType)
            {
                case CustomOrderType.Original:
                    {
                        switch (order.OrderType)
                        {
                            case OrderType.Charge:
                            case OrderType.ChargeWithTarget:
                                return null;
                            case OrderType.LookAtEnemy:
                                {
                                    var direction = Patch_OrderController.GetVirtualDirectionOfFacingEnemy(formation);
                                    Patch_OrderController.LivePreviewFormationChanges.UpdateFormationChange(formation, null, direction, null, null);
                                    return CollectOrderPreviewData(formation);
                                }
                            case OrderType.FollowMe:
                                return null;
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
                                return null;
                            case OrderType.Retreat:
                                return null;
                            case OrderType.ArrangementLine:
                                return null;
                            case OrderType.ArrangementCloseOrder:
                                return null;
                            case OrderType.ArrangementLoose:
                                return null;
                            case OrderType.ArrangementCircular:
                                return null;
                            case OrderType.ArrangementSchiltron:
                                return null;
                            case OrderType.ArrangementVee:
                                return null;
                            case OrderType.ArrangementColumn:
                                return null;
                            case OrderType.ArrangementScatter:
                                return null;
                            default:
                                Utility.DisplayMessage("Error: unexpected order type");
                                break;
                        }
                        break;
                    }
                case CustomOrderType.MoveToLineSegment:
                case CustomOrderType.MoveToLineSegmentWithHorizontalLayout:
                    {
                        if (order.IsLineShort)
                        {
                            // TODO: What if facing order is changed in command queue?
                            switch (OrderController.GetActiveFacingOrderOf(formation))
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
                case CustomOrderType.ToggleFacing:
                    // TODO: Need to know the previous facing order in command queue.
                case CustomOrderType.ToggleFire:
                case CustomOrderType.ToggleMount:
                case CustomOrderType.ToggleAI:
                    return null;
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
                if (_orderFlagMeshMaterial== (NativeObject)null)
                {
                    _orderFlagMeshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                    //_orderFlagMeshMaterial = Material.GetFromResource("vertex_color_blend_mat
                    //_orderFlagMeshMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                    _orderFlagMeshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                }
                copy.SetMaterial(_orderFlagMeshMaterial);
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

        private void HidePreview()
        {
            HideAgentFrameEntities();
            HideOrderPositionFlagEntities();
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

        private OrderPreviewData CollectOrderPreviewData(Formation formation)
        {
            if (_showAgentFrames)
            {
                Patch_OrderController.SimulateAgentFrames(new List<Formation> { formation },
                    Mission.PlayerTeam.PlayerOrderController.simulationFormations,
                    out var simulationFormationChanges);
                return new OrderPreviewData { AgentPositions = simulationFormationChanges, OrderPosition = Patch_OrderController.GetFormationVirtualPosition(formation) };
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
