using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera
{
    public class RTSCameraOrderTroopPlacer : MissionView
    {
        private bool _suspendTroopPlacer;
        private bool _isMouseDown;
        private List<GameEntity> _orderPositionEntities;
        private List<GameEntity> _orderRotationEntities;
        private bool _formationDrawingMode;
        private Formation _mouseOverFormation;
        private Formation _clickedFormation;
        private Vec2 _lastMousePosition;
        private Vec2 _deltaMousePosition;
        private int _mouseOverDirection;
        private WorldPosition? _formationDrawingStartingPosition;
        private Vec2? _formationDrawingStartingPointOfMouse;
        private float? _formationDrawingStartingTime;
        private OrderController PlayerOrderController;
        private Team PlayerTeam;
        public bool Initialized;
        private Timer formationDrawTimer;
        public bool IsDrawingForced;
        public bool IsDrawingFacing;
        public bool IsDrawingForming;
        public bool IsDrawingAttaching;
        private bool _wasDrawingForced;
        private bool _wasDrawingFacing;
        private bool _wasDrawingForming;
        private GameEntity attachArrow;
        private float attachArrowLength;
        private GameEntity widthEntityLeft;
        private GameEntity widthEntityRight;
        private bool isDrawnThisFrame;
        private bool wasDrawnPreviousFrame;
        private static Material _meshMaterial;

        public bool SuspendTroopPlacer
        {
            get => this._suspendTroopPlacer;
            set
            {
                this._suspendTroopPlacer = value;
                if (value)
                    this.HideOrderPositionEntities();
                else
                    this._formationDrawingStartingPosition = new WorldPosition?();
                this.Reset();
            }
        }

        public Formation AttachTarget { get; private set; }

        public MovementOrder.Side AttachSide { get; private set; }

        public WorldPosition AttachPosition { get; private set; }

        public override void AfterStart()
        {
            base.AfterStart();
            this._formationDrawingStartingPosition = new WorldPosition?();
            this._formationDrawingStartingPointOfMouse = new Vec2?();
            this._formationDrawingStartingTime = new float?();
            this._orderRotationEntities = new List<GameEntity>();
            this._orderPositionEntities = new List<GameEntity>();
            this.formationDrawTimer = new Timer(MBCommon.GetTime(MBCommon.TimeType.Application), 0.03333334f, true);
            this.attachArrow = GameEntity.CreateEmpty(this.Mission.Scene, true);
            this.attachArrow.AddComponent((GameEntityComponent)MetaMesh.GetCopy("order_arrow_a", true, false));
            this.attachArrow.SetVisibilityExcludeParents(false);
            BoundingBox boundingBox = this.attachArrow.GetMetaMesh(0).GetBoundingBox();
            this.attachArrowLength = boundingBox.max.y - boundingBox.min.y;
            this.widthEntityLeft = GameEntity.CreateEmpty(this.Mission.Scene, true);
            this.widthEntityLeft.AddComponent((GameEntityComponent)MetaMesh.GetCopy("order_arrow_a", true, false));
            this.widthEntityLeft.SetVisibilityExcludeParents(false);
            this.widthEntityRight = GameEntity.CreateEmpty(this.Mission.Scene, true);
            this.widthEntityRight.AddComponent((GameEntityComponent)MetaMesh.GetCopy("order_arrow_a", true, false));
            this.widthEntityRight.SetVisibilityExcludeParents(false);
        }

        private void InitializeInADisgustingManner()
        {
            this.PlayerTeam = this.Mission.PlayerTeam;
            this.PlayerOrderController = this.PlayerTeam.PlayerOrderController;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (this.Initialized)
                return;
            MissionPeer missionPeer = GameNetwork.IsMyPeerReady
                ? GameNetwork.MyPeer.GetComponent<MissionPeer>()
                : (MissionPeer)null;
            if (this.Mission.PlayerTeam == null && (missionPeer == null ||
                                                    missionPeer.Team != this.Mission.AttackerTeam &&
                                                    missionPeer.Team != this.Mission.DefenderTeam))
                return;
            this.InitializeInADisgustingManner();
            this.Initialized = true;
        }

        public void UpdateAttachVisuals(bool isVisible)
        {
            if (this.AttachTarget == null)
                isVisible = false;
            this.attachArrow.SetVisibilityExcludeParents(isVisible);
            if (isVisible)
            {
                Vec2 vec2 = this.AttachTarget.Direction;
                switch (this.AttachSide)
                {
                    case MovementOrder.Side.Front:
                        vec2 *= -1f;
                        break;
                    case MovementOrder.Side.Left:
                        vec2 = vec2.RightVec();
                        break;
                    case MovementOrder.Side.Right:
                        vec2 = vec2.LeftVec();
                        break;
                }

                float rotationInRadians = vec2.RotationInRadians;
                Mat3 identity1 = Mat3.Identity;
                identity1.RotateAboutUp(rotationInRadians);
                MatrixFrame identity2 = MatrixFrame.Identity;
                identity2.rotation = identity1;
                identity2.origin = this.AttachPosition.GetGroundVec3();
                identity2.Advance(-this.attachArrowLength);
                this.attachArrow.SetFrame(ref identity2);
            }

            if (!isVisible)
                return;
            this.MissionScreen.GetOrderFlagPosition();
            this.AddAttachPoints();
        }

        private void UpdateFormationDrawingForFacingOrder(bool giveOrder)
        {
            this.isDrawnThisFrame = true;
            List<(Agent, WorldFrame)> simulationAgentFrames;
            this.PlayerOrderController.SimulateNewFacingOrder(
                OrderController.GetOrderLookAtDirection(this.PlayerOrderController.SelectedFormations,
                    this.MissionScreen.GetOrderFlagPosition().AsVec2), out simulationAgentFrames);
            int entityIndex = 0;
            this.HideOrderPositionEntities();
            foreach ((Agent _, WorldFrame frame) in simulationAgentFrames)
            {
                var worldFrame = frame;
                this.AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder, -1f);
                ++entityIndex;
            }
        }

        private void UpdateFormationDrawingForDestination(bool giveOrder)
        {
            this.isDrawnThisFrame = true;
            List<(Agent, WorldFrame)> simulationAgentFrames;
            this.PlayerOrderController.SimulateDestinationFrames(out simulationAgentFrames, 3f);
            int entityIndex = 0;
            this.HideOrderPositionEntities();
            foreach ((Agent _, WorldFrame frame) in simulationAgentFrames)
            {
                var worldFrame = frame;
                this.AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder, 0.7f);
                ++entityIndex;
            }
        }

        private void UpdateFormationDrawingForFormingOrder(bool giveOrder)
        {
            this.isDrawnThisFrame = true;
            MatrixFrame orderFlagFrame = this.MissionScreen.GetOrderFlagFrame();
            Vec3 origin1 = orderFlagFrame.origin;
            Vec2 asVec2 = orderFlagFrame.rotation.f.AsVec2;
            float orderFormCustomWidth =
                OrderController.GetOrderFormCustomWidth(this.PlayerOrderController.SelectedFormations, origin1);
            List<(Agent, WorldFrame)> simulationAgentFrames;
            this.PlayerOrderController.SimulateNewCustomWidthOrder(orderFormCustomWidth, out simulationAgentFrames);
            Formation formation =
                this.PlayerOrderController.SelectedFormations.MaxBy<Formation, int>(
                    (Func<Formation, int>)(f => f.CountOfUnits));
            int entityIndex = 0;
            this.HideOrderPositionEntities();
            foreach ((Agent _, WorldFrame frame1) in simulationAgentFrames)
            {
                var worldFrame = frame1;
                this.AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder, -1f);
                ++entityIndex;
            }

            float unitDiameter = formation.UnitDiameter;
            float interval = formation.Interval;
            int num1 = Math.Max(0,
                (int)(((double)orderFormCustomWidth - (double)unitDiameter) /
                    ((double)interval + (double)unitDiameter) + 9.99999974737875E-06)) + 1;
            float num2 = (float)(num1 - 1) * (interval + unitDiameter);
            for (int index = 0; index < num1; ++index)
            {
                Vec2 a = new Vec2(
                    (float)((double)index * ((double)interval + (double)unitDiameter) - (double)num2 / 2.0), 0.0f);
                Vec2 parentUnitF = asVec2.TransformToParentUnitF(a);
                WorldPosition origin2 = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, origin1, false);
                origin2.SetVec2(origin2.AsVec2 + parentUnitF);
                WorldFrame frame2 = new WorldFrame(orderFlagFrame.rotation, origin2);
                this.AddOrderPositionEntity(entityIndex++, ref frame2, false, -1f);
            }
        }

        private void UpdateFormationDrawing(bool giveOrder)
        {
            this.isDrawnThisFrame = true;
            this.HideOrderPositionEntities();
            if (!this._formationDrawingStartingPosition.HasValue)
                return;
            WorldPosition formationRealEndingPosition = WorldPosition.Invalid;
            bool flag = false;
            if (this.MissionScreen.MouseVisible && this._formationDrawingStartingPointOfMouse.HasValue)
            {
                Vec2 vec2 = this._formationDrawingStartingPointOfMouse.Value - this.Input.GetMousePositionPixel();
                if ((double)Math.Abs(vec2.x) < 10.0 && (double)Math.Abs(vec2.y) < 10.0)
                {
                    flag = true;
                    formationRealEndingPosition = this._formationDrawingStartingPosition.Value;
                }
            }

            if (this.MissionScreen.MouseVisible && this._formationDrawingStartingTime.HasValue &&
                MBCommon.GetTime(MBCommon.TimeType.Application) -
                this._formationDrawingStartingTime.Value < 0.300000011920929)
            {
                flag = true;
                formationRealEndingPosition = this._formationDrawingStartingPosition.Value;
            }

            if (!flag)
            {
                Vec3 rayBegin;
                Vec3 rayEnd;
                this.MissionScreen.ScreenPointToWorldRay(this.GetScreenPoint(), out rayBegin, out rayEnd);
                float collisionDistance;
                if (!this.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance, 0.3f,
                    BodyFlags.CommonFocusRayCastExcludeFlags))
                    return;
                Vec3 vec3 = rayEnd - rayBegin;
                double num = vec3.Normalize();
                formationRealEndingPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero,
                    rayBegin + vec3 * collisionDistance, false);
            }

            WorldPosition worldPosition;
            if (this._mouseOverDirection == 1)
            {
                worldPosition = formationRealEndingPosition;
                formationRealEndingPosition = this._formationDrawingStartingPosition.Value;
            }
            else
                worldPosition = this._formationDrawingStartingPosition.Value;
            if (!OrderFlag.IsPositionOnValidGround(worldPosition))
                return;
            bool isFormationLayoutVertical = !this.DebugInput.IsControlDown();
            if ((!InputKey.LeftMouseButton.IsDown() || this._formationDrawingStartingPointOfMouse.HasValue) &&
                this.IsDrawingAttaching)
                this.UpdateFormationDrawingForAttachOrder(giveOrder, isFormationLayoutVertical);
            else if (true)
                this.UpdateFormationDrawingForMovementOrder(giveOrder, worldPosition, formationRealEndingPosition,
                    isFormationLayoutVertical);
            this._deltaMousePosition *= Math.Max((float)(1.0 - (double)(this.Input.GetMousePositionRanged() - this._lastMousePosition).Length * 10.0), 0.0f);
            this._lastMousePosition = this.Input.GetMousePositionRanged();
        }

        private void UpdateFormationDrawingForMovementOrder(
            bool giveOrder,
            WorldPosition formationRealStartingPosition,
            WorldPosition formationRealEndingPosition,
            bool isFormationLayoutVertical)
        {
            this.isDrawnThisFrame = true;
            List<(Agent, WorldFrame)> simulationAgentFrames;
            this.PlayerOrderController.SimulateNewOrderWithPositionAndDirection(formationRealStartingPosition,
                formationRealEndingPosition, out simulationAgentFrames, isFormationLayoutVertical);
            if (giveOrder)
            {
                if (!isFormationLayoutVertical)
                    this.PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegmentWithHorizontalLayout,
                        formationRealStartingPosition, formationRealEndingPosition);
                else
                    this.PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment,
                        formationRealStartingPosition, formationRealEndingPosition);
            }

            int entityIndex = 0;
            foreach ((Agent _, WorldFrame frame) in simulationAgentFrames)
            {
                var worldFrame = frame;
                this.AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder, -1f);
                ++entityIndex;
            }
        }

        private void UpdateFormationDrawingForAttachOrder(
            bool giveOrder,
            bool isFormationLayoutVertical)
        {
            this.isDrawnThisFrame = true;
            int entityIndex = 0;
            foreach (Formation selectedFormation in this.PlayerOrderController.SelectedFormations)
            {
                WorldPosition attachPosition =
                    MovementOrder.GetAttachPosition(selectedFormation, this.AttachTarget, this.AttachSide);
                Vec2 vec2 = this.AttachTarget.Direction.LeftVec() * (selectedFormation.Width / 2f);
                WorldPosition formationLineBegin = attachPosition;
                formationLineBegin.SetVec2(formationLineBegin.AsVec2 + vec2);
                WorldPosition formationLineEnd = attachPosition;
                formationLineEnd.SetVec2(formationLineEnd.AsVec2 - vec2);
                List<(Agent, WorldFrame)> simulationAgentFrames;
                OrderController.SimulateNewOrderWithPositionAndDirection(
                    Enumerable.Repeat<Formation>(selectedFormation, 1), this.PlayerOrderController.simulationFormations,
                    formationLineBegin, formationLineEnd, out simulationAgentFrames, isFormationLayoutVertical);
                foreach ((Agent _, WorldFrame frame2) in simulationAgentFrames)
                {
                    var worldFrame = frame2;
                    this.AddOrderPositionEntity(entityIndex, ref worldFrame, giveOrder, -1f);
                    ++entityIndex;
                }
            }

            if (!giveOrder)
                return;
            this.PlayerOrderController.SetOrderWithFormationAndNumber(OrderType.Attach, this.AttachTarget,
                (int)this.AttachSide);
        }

        private void HandleMouseDown()
        {
            if (this.PlayerOrderController.SelectedFormations.IsEmpty<Formation>() || this._clickedFormation != null)
                return;
            switch (this.GetCursorState())
            {
                case RTSCameraOrderTroopPlacer.CursorState.Normal:
                    this._formationDrawingMode = true;
                    Vec3 rayBegin;
                    Vec3 rayEnd;
                    this.MissionScreen.ScreenPointToWorldRay(this.GetScreenPoint(), out rayBegin, out rayEnd);
                    float collisionDistance;
                    if (this.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance,
                        0.3f, BodyFlags.CommonFocusRayCastExcludeFlags))
                    {
                        Vec3 vec3 = rayEnd - rayBegin;
                        double num = (double)vec3.Normalize();
                        this._formationDrawingStartingPosition = new WorldPosition?(
                            new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, rayBegin + vec3 * collisionDistance,
                                false));
                        this._formationDrawingStartingPointOfMouse = new Vec2?(this.Input.GetMousePositionPixel());
                        this._formationDrawingStartingTime = new float?(MBCommon.GetTime(MBCommon.TimeType.Application));
                        break;
                    }

                    this._formationDrawingStartingPosition = new WorldPosition?();
                    this._formationDrawingStartingPointOfMouse = new Vec2?();
                    this._formationDrawingStartingTime = new float?();
                    break;
                case RTSCameraOrderTroopPlacer.CursorState.Enemy:
                case RTSCameraOrderTroopPlacer.CursorState.Friend:
                    this._clickedFormation = this._mouseOverFormation;
                    break;
                case RTSCameraOrderTroopPlacer.CursorState.Rotation:
                    if (this._mouseOverFormation.CountOfUnits <= 0)
                        break;
                    this.HideNonSelectedOrderRotationEntities(this._mouseOverFormation);
                    this.PlayerOrderController.ClearSelectedFormations();
                    this.PlayerOrderController.SelectFormation(this._mouseOverFormation);
                    this._formationDrawingMode = true;
                    WorldPosition orderPosition = this._mouseOverFormation.OrderPosition;
                    Vec2 direction = this._mouseOverFormation.Direction;
                    direction.RotateCCW(-1.570796f);
                    this._formationDrawingStartingPosition = new WorldPosition?(orderPosition);
                    this._formationDrawingStartingPosition.Value.SetVec2(
                        this._formationDrawingStartingPosition.Value.AsVec2 + direction *
                        (this._mouseOverDirection == 1 ? 0.5f : -0.5f) * this._mouseOverFormation.Width);
                    WorldPosition worldPosition = orderPosition;
                    worldPosition.SetVec2(worldPosition.AsVec2 + direction *
                        (this._mouseOverDirection == 1 ? -0.5f : 0.5f) *
                        this._mouseOverFormation.Width);
                    this._deltaMousePosition =
                        this.MissionScreen.SceneView.WorldPointToScreenPoint(worldPosition.GetGroundVec3()) -
                        this.GetScreenPoint();
                    this._lastMousePosition = this.Input.GetMousePositionRanged();
                    break;
            }
        }

        private void HandleMouseUp()
        {
            if (this._clickedFormation != null)
            {
                if (this._clickedFormation.CountOfUnits > 0 && this._clickedFormation.Team == this.PlayerTeam)
                {
                    Formation clickedFormation = this._clickedFormation;
                    this._clickedFormation = (Formation)null;
                    int cursorState = (int)this.GetCursorState();
                    this._clickedFormation = clickedFormation;
                    this.HideNonSelectedOrderRotationEntities(this._clickedFormation);
                    this.PlayerOrderController.ClearSelectedFormations();
                    this.PlayerOrderController.SelectFormation(this._clickedFormation);
                }

                this._clickedFormation = (Formation)null;
            }
            else if (this.GetCursorState() == RTSCameraOrderTroopPlacer.CursorState.Ground)
            {
                if (this.IsDrawingFacing || this._wasDrawingFacing)
                    this.UpdateFormationDrawingForFacingOrder(true);
                else if (this.IsDrawingForming || this._wasDrawingForming)
                    this.UpdateFormationDrawingForFormingOrder(true);
                else
                    this.UpdateFormationDrawing(true);
                if (this.IsDeployment)
                    SoundEvent.PlaySound2D("event:/ui/mission/deploy");
            }

            this._formationDrawingMode = false;
            this._deltaMousePosition = Vec2.Zero;
        }

        private Vec2 GetScreenPoint()
        {
            return !this.MissionScreen.MouseVisible
                ? new Vec2(0.5f, 0.5f) + this._deltaMousePosition
                : this.Input.GetMousePositionRanged() + this._deltaMousePosition;
        }

        private RTSCameraOrderTroopPlacer.CursorState GetCursorState()
        {
            RTSCameraOrderTroopPlacer.CursorState cursorState = RTSCameraOrderTroopPlacer.CursorState.Invisible;
            this.AttachTarget = (Formation)null;
            if (!this.PlayerOrderController.SelectedFormations.IsEmpty<Formation>() && this._clickedFormation == null)
            {
                Vec3 rayBegin;
                Vec3 rayEnd;
                this.MissionScreen.ScreenPointToWorldRay(this.GetScreenPoint(), out rayBegin, out rayEnd);
                float collisionDistance;
                GameEntity collidedEntity;
                if (!this.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance,
                    out collidedEntity, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags))
                    collisionDistance = 1000f;
                if (cursorState == RTSCameraOrderTroopPlacer.CursorState.Invisible && (double)collisionDistance < 1000.0)
                {
                    if (!this._formationDrawingMode && (NativeObject)collidedEntity == (NativeObject)null)
                    {
                        for (int index = 0; index < this._orderRotationEntities.Count; ++index)
                        {
                            GameEntity orderRotationEntity = this._orderRotationEntities[index];
                            if (orderRotationEntity.IsVisibleIncludeParents() &&
                                (NativeObject)collidedEntity == (NativeObject)orderRotationEntity)
                            {
                                this._mouseOverFormation =
                                    this.PlayerOrderController.SelectedFormations.ElementAt<Formation>(index / 2);
                                this._mouseOverDirection = 1 - (index & 1);
                                cursorState = RTSCameraOrderTroopPlacer.CursorState.Rotation;
                                break;
                            }
                        }
                    }

                    if (cursorState == RTSCameraOrderTroopPlacer.CursorState.Invisible &&
                        this.MissionScreen.OrderFlag.FocusedOrderableObject != null)
                        cursorState = RTSCameraOrderTroopPlacer.CursorState.OrderableEntity;
                    if (cursorState == RTSCameraOrderTroopPlacer.CursorState.Invisible)
                    {
                        cursorState = this.IsCursorStateGroundOrNormal();
                        this.UpdateAttachData();
                    }
                }
            }

            if (cursorState != RTSCameraOrderTroopPlacer.CursorState.Ground &&
                cursorState != RTSCameraOrderTroopPlacer.CursorState.Rotation)
                this._mouseOverDirection = 0;
            return cursorState;
        }

        private RTSCameraOrderTroopPlacer.CursorState IsCursorStateGroundOrNormal()
        {
            return !this._formationDrawingMode
                ? RTSCameraOrderTroopPlacer.CursorState.Normal
                : RTSCameraOrderTroopPlacer.CursorState.Ground;
        }

        private void UpdateAttachData()
        {
            if (!this.IsDrawingForced)
                return;
            Vec3 orderFlagPosition = this.MissionScreen.GetOrderFlagPosition();
            foreach (Formation formation in this.PlayerTeam.Formations.Where<Formation>(
                (Func<Formation, bool>)(f => !this.PlayerOrderController.IsFormationListening(f))))
            {
                WorldPosition worldPosition;
                Vec2 asVec2;
                if (this.AttachTarget != null)
                {
                    worldPosition = formation.RearAttachmentPoint;
                    asVec2 = worldPosition.AsVec2;
                    double num1 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                    worldPosition = this.AttachPosition;
                    asVec2 = worldPosition.AsVec2;
                    double num2 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                    if (num1 >= num2)
                        goto label_7;
                }

                this.AttachTarget = formation;
                this.AttachSide = MovementOrder.Side.Rear;
                this.AttachPosition = formation.RearAttachmentPoint;
            label_7:
                worldPosition = formation.LeftAttachmentPoint;
                asVec2 = worldPosition.AsVec2;
                double num3 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                worldPosition = this.AttachPosition;
                asVec2 = worldPosition.AsVec2;
                double num4 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                if (num3 < num4)
                {
                    this.AttachTarget = formation;
                    this.AttachSide = MovementOrder.Side.Left;
                    this.AttachPosition = formation.LeftAttachmentPoint;
                }

                worldPosition = formation.RightAttachmentPoint;
                asVec2 = worldPosition.AsVec2;
                double num5 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                worldPosition = this.AttachPosition;
                asVec2 = worldPosition.AsVec2;
                double num6 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                if (num5 < num6)
                {
                    this.AttachTarget = formation;
                    this.AttachSide = MovementOrder.Side.Right;
                    this.AttachPosition = formation.RightAttachmentPoint;
                }

                worldPosition = formation.FrontAttachmentPoint;
                asVec2 = worldPosition.AsVec2;
                double num7 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                worldPosition = this.AttachPosition;
                asVec2 = worldPosition.AsVec2;
                double num8 = (double)asVec2.DistanceSquared(orderFlagPosition.AsVec2);
                if (num7 < num8)
                {
                    this.AttachTarget = formation;
                    this.AttachSide = MovementOrder.Side.Front;
                    this.AttachPosition = formation.FrontAttachmentPoint;
                }
            }
        }

        private void AddOrderPositionEntity(
            int entityIndex,
            ref WorldFrame frame,
            bool fadeOut,
            float alpha = -1f)
        {
            while (this._orderPositionEntities.Count <= entityIndex)
            {
                GameEntity empty = GameEntity.CreateEmpty(this.Mission.Scene, true);
                empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                MetaMesh copy = MetaMesh.GetCopy("order_flag_small", true, false);
                if ((NativeObject)RTSCameraOrderTroopPlacer._meshMaterial == (NativeObject)null)
                {
                    RTSCameraOrderTroopPlacer._meshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                    RTSCameraOrderTroopPlacer._meshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                }

                copy.SetMaterial(RTSCameraOrderTroopPlacer._meshMaterial);
                empty.AddComponent((GameEntityComponent)copy);
                empty.SetVisibilityExcludeParents(false);
                this._orderPositionEntities.Add(empty);
            }

            GameEntity orderPositionEntity = this._orderPositionEntities[entityIndex];
            Vec3 rayBegin;
            this.MissionScreen.ScreenPointToWorldRay(Vec2.One * 0.5f, out rayBegin, out Vec3 _);
            float rotationZ = MatrixFrame.CreateLookAt(rayBegin, frame.Origin.GetGroundVec3(), Vec3.Up).rotation.f
                .RotationZ;
            frame.Rotation = Mat3.Identity;
            frame.Rotation.RotateAboutUp(rotationZ);
            MatrixFrame groundMatrixFrame = frame.ToGroundMatrixFrame();
            orderPositionEntity.SetFrame(ref groundMatrixFrame);
            if ((double)alpha != -1.0)
            {
                orderPositionEntity.SetVisibilityExcludeParents(true);
                orderPositionEntity.SetAlpha(alpha);
            }
            else if (fadeOut)
                orderPositionEntity.FadeOut(0.3f, false);
            else
                orderPositionEntity.FadeIn(true);
        }

        private void HideNonSelectedOrderRotationEntities(Formation formation)
        {
            for (int index = 0; index < this._orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = this._orderRotationEntities[index];
                if ((NativeObject)orderRotationEntity == (NativeObject)null &&
                    orderRotationEntity.IsVisibleIncludeParents() &&
                    this.PlayerOrderController.SelectedFormations.ElementAt<Formation>(index / 2) != formation)
                {
                    orderRotationEntity.SetVisibilityExcludeParents(false);
                    orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
                }
            }
        }

        private void HideOrderPositionEntities()
        {
            foreach (GameEntity orderPositionEntity in this._orderPositionEntities)
                orderPositionEntity.HideIfNotFadingOut();
            for (int index = 0; index < this._orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = this._orderRotationEntities[index];
                orderRotationEntity.SetVisibilityExcludeParents(false);
                orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
            }
        }

        [Conditional("DEBUG")]
        private void DebugTick(float dt)
        {
            int num = this.Initialized ? 1 : 0;
        }

        private void Reset()
        {
            this._isMouseDown = false;
            this._formationDrawingMode = false;
            this._formationDrawingStartingPosition = new WorldPosition?();
            this._formationDrawingStartingPointOfMouse = new Vec2?();
            this._formationDrawingStartingTime = new float?();
            this._mouseOverFormation = (Formation)null;
            this._clickedFormation = (Formation)null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (!this.Initialized)
                return;
            base.OnMissionScreenTick(dt);
            if (!this.PlayerOrderController.SelectedFormations.Any<Formation>())
                return;
            this.isDrawnThisFrame = false;
            if (this.SuspendTroopPlacer)
                return;
            if (this.Input.IsKeyPressed(InputKey.LeftMouseButton))
            {
                this._isMouseDown = true;
                this.HandleMouseDown();
            }

            if (this.Input.IsKeyReleased(InputKey.LeftMouseButton) && this._isMouseDown)
            {
                this._isMouseDown = false;
                this.HandleMouseUp();
            }
            else if (this.Input.IsKeyDown(InputKey.LeftMouseButton) && this._isMouseDown)
            {
                if (this.formationDrawTimer.Check(MBCommon.GetTime(MBCommon.TimeType.Application)) &&
                    !this.IsDrawingFacing &&
                    (!this.IsDrawingForming &&
                     this.IsCursorStateGroundOrNormal() == RTSCameraOrderTroopPlacer.CursorState.Ground) &&
                    this.GetCursorState() == RTSCameraOrderTroopPlacer.CursorState.Ground)
                    this.UpdateFormationDrawing(false);
            }
            else if (this.IsDrawingForced)
            {
                this.Reset();
                this.HandleMouseDown();
                this.UpdateFormationDrawing(false);
            }
            else if (this.IsDrawingFacing || this._wasDrawingFacing)
            {
                if (this.IsDrawingFacing)
                {
                    this.Reset();
                    this.UpdateFormationDrawingForFacingOrder(false);
                }
            }
            else if (this.IsDrawingForming || this._wasDrawingForming)
            {
                if (this.IsDrawingForming)
                {
                    this.Reset();
                    this.UpdateFormationDrawingForFormingOrder(false);
                }
            }
            else if (this._wasDrawingForced)
                this.Reset();
            else
                this.UpdateFormationDrawingForDestination(false);

            foreach (GameEntity orderPositionEntity in this._orderPositionEntities)
                orderPositionEntity.SetPreviousFrameInvalid();
            foreach (GameEntity orderRotationEntity in this._orderRotationEntities)
                orderRotationEntity.SetPreviousFrameInvalid();
            this._wasDrawingForced = this.IsDrawingForced;
            this._wasDrawingFacing = this.IsDrawingFacing;
            this._wasDrawingForming = this.IsDrawingForming;
            this.wasDrawnPreviousFrame = this.isDrawnThisFrame;
        }

        private bool IsDeployment
        {
            get { return this.Mission.GetMissionBehaviour<SiegeDeploymentHandler>() != null; }
        }

        private void AddAttachPoints()
        {
            foreach (Formation formation in this.PlayerTeam.FormationsIncludingSpecial.Except<Formation>(
                this.PlayerOrderController.SelectedFormations))
            {
                WorldPosition rearAttachmentPoint = formation.RearAttachmentPoint;
                WorldPosition frontAttachmentPoint = formation.FrontAttachmentPoint;
                WorldPosition leftAttachmentPoint = formation.LeftAttachmentPoint;
                WorldPosition rightAttachmentPoint = formation.RightAttachmentPoint;
            }

            if (this.AttachTarget == null)
                return;
            WorldPosition attachPosition = this.AttachPosition;
        }
        protected enum CursorState
        {
            Invisible,
            Normal,
            Ground,
            Enemy,
            Friend,
            Rotation,
            Count,
            OrderableEntity,
        }
    }
}
