using HarmonyLib;
using MissionLibrary.Event;
using MissionSharedLibrary.QuerySystem;
using MissionSharedLibrary.Utilities;
using RTSCamera.CommandSystem.CampaignGame;
using RTSCamera.CommandSystem.Config;
using RTSCamera.CommandSystem.Config.HotKey;
using RTSCamera.CommandSystem.Logic;
using RTSCamera.CommandSystem.Logic.SubLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using CursorState = TaleWorlds.MountAndBlade.View.MissionViews.Order.OrderTroopPlacer.CursorState;

namespace RTSCamera.CommandSystem.Patch
{
    public class Patch_OrderTroopPlacer
    {
        public static uint OrderPositionEntityColor = new Color(0.15f, 0.65f, 0.15f).ToUnsignedInteger();
        public static float OrderPositionEntityPreviewAlpha = 1f;
        public static float OrderPositionEntityDestinationAlpha = 0.5f;
        private static float _cachedTimeOfDay = 0;
        private static bool _patched;

        private static readonly FieldInfo _dataSource =
            typeof(MissionGauntletSingleplayerOrderUIHandler).GetField(nameof(_dataSource),
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _orderPositionEntities = typeof(OrderTroopPlacer).GetField("_orderPositionEntities", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo _activeCursorState = typeof(OrderTroopPlacer).GetProperty("ActiveCursorState", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _cursorState = typeof(OrderTroopPlacer).GetMethod("GetCursorState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _handleMouseDown = typeof(OrderTroopPlacer).GetMethod("HandleMouseDown", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _handleMouseUp = typeof(OrderTroopPlacer).GetMethod("HandleMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawing = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawing", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawingForFacingOrder = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForFacingOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawingForFormingOrder = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForFormingOrder", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _updateFormationDrawingForDestination = typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForDestination", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _getGroundVec3 = typeof(OrderTroopPlacer).GetMethod("GetGroundedVec3", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _addOrderPositionEntity = typeof(OrderTroopPlacer).GetMethod("AddOrderPositionEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo _reset = typeof(OrderTroopPlacer).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);

        private static CursorState _currentCursorState = CursorState.Invisible;
        private static UiQueryData<CursorState> _cachedCursorState;
        private static FormationColorSubLogicV2 _outlineView;
        private static FormationColorSubLogicV2 _groundMarkerView;
        private static OrderTroopPlacer _orderTroopPlacer;
        private static MissionFormationTargetSelectionHandler _targetSelectionHandler;
        private static MBReadOnlyList<Formation> _focusedFormationsCache;
        public static bool IsFreeCamera;
        private static MovementTargetHighlightStyle _previousMovementTargetHightlightStyle = MovementTargetHighlightStyle.Count;
        private static List<GameEntity> _originalOrderPositionEntities;
        private static List<GameEntity> _newModelOrderPositionEntities;
        private static List<GameEntity> _alwaysVisibleOrderPositionEntities;
        private static Material _originalMaterial;
        private static Material _newModelMaterial;
        private static Material _alwaysVisibleMaterial;
        private static bool _skipDrawingForDestinationForOneTick;

        public static bool Patch(Harmony harmony)
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("InitializeInADisgustingManner",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    postfix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(
                        nameof(Postfix_InitializeInADisgustingManner), BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("HandleMouseDown",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_HandleMouseDown),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("GetCursorState",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_GetCursorState),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("AddOrderPositionEntity",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_AddOrderPositionEntity),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod(nameof(OrderTroopPlacer.OnMissionScreenTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_OnMissionScreenTick),
                        BindingFlags.Static | BindingFlags.Public)));
                // For command queue
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForMovementOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_UpdateFormationDrawingForMovementOrder),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("UpdateFormationDrawingForFacingOrder",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_UpdateFormationDrawingForFacingOrder),
                        BindingFlags.Static | BindingFlags.Public)));
                harmony.Patch(
                    typeof(OrderTroopPlacer).GetMethod("HideOrderPositionEntities",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_OrderTroopPlacer).GetMethod(nameof(Prefix_HideOrderPositionEntities),
                        BindingFlags.Static | BindingFlags.Public)));
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Utility.DisplayMessage(e.ToString());
                MBDebug.Print(e.ToString());
                return false;
            }
        }

        public static void Postfix_InitializeInADisgustingManner(OrderTroopPlacer __instance)
        {
            _orderTroopPlacer = __instance;
            _cachedCursorState = new UiQueryData<CursorState>(GetCursorState, 0.05f);
            _outlineView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().OutlineColorSubLogic;
            _groundMarkerView = Mission.Current.GetMissionBehavior<CommandSystemLogic>().GroundMarkerColorSubLogic;

            typeof(Input).GetProperty(nameof(Input.DebugInput), BindingFlags.Static | BindingFlags.Public)
                .SetValue(null, __instance.Input);

            _cachedTimeOfDay = __instance.Mission.Scene.TimeOfDay;
        }

        public static void OnBehaviorInitialize()
        {
            _targetSelectionHandler = Mission.Current.GetMissionBehavior<MissionFormationTargetSelectionHandler>();
            if (_targetSelectionHandler != null)
            {
                _targetSelectionHandler.OnFormationFocused += OnFormationFocused;
            }
            MissionEvent.ToggleFreeCamera += OnToggleFreeCamera;
            IsFreeCamera = false;
            _previousMovementTargetHightlightStyle = MovementTargetHighlightStyle.Count;
        }

        public static void OnRemoveBehavior()
        {
            _cachedTimeOfDay = 0;
            _orderTroopPlacer = null;
            _outlineView = null;
            _groundMarkerView = null;
            _cachedCursorState = null;
            _focusedFormationsCache = null;
            if (_targetSelectionHandler != null)
            {
                _targetSelectionHandler.OnFormationFocused -= OnFormationFocused;
            }
            _targetSelectionHandler = null;
            MissionEvent.ToggleFreeCamera -= OnToggleFreeCamera;
            IsFreeCamera = false;
            _originalOrderPositionEntities = _newModelOrderPositionEntities = _alwaysVisibleOrderPositionEntities = null;
            _originalMaterial = _newModelMaterial = _alwaysVisibleMaterial = null;
            _previousMovementTargetHightlightStyle = MovementTargetHighlightStyle.Count;
        }

        private static void OnFormationFocused(MBReadOnlyList<Formation> focusedFormations)
        {
            _focusedFormationsCache = focusedFormations;
        }

        private static void OnToggleFreeCamera(bool isFreeCamera)
        {
            IsFreeCamera = isFreeCamera;
            Patch_MissionOrderVM.OrderToSelectTarget = OrderSubType.None;
        }

        public static bool IsDraggingFormation(OrderTroopPlacer __instance, Vec2? ____formationDrawingStartingPointOfMouse, float? ____formationDrawingStartingTime)
        {
            if (____formationDrawingStartingPointOfMouse.HasValue)
            {
                Vec2 vec2 = ____formationDrawingStartingPointOfMouse.Value - __instance.Input.GetMousePositionPixel();
                if (Math.Abs(vec2.x) >= 10.0 || Math.Abs(vec2.y) >= 10.0)
                {
                    return true;
                }
            }

            //if (____formationDrawingStartingTime.HasValue &&
            //    __instance.Mission.CurrentTime -
            //    ____formationDrawingStartingTime.Value >= 0.300000011920929)
            //{
            //    return true;
            //}

            return false;
        }

        public static CursorState GetCursorState()
        {
            return (CursorState)_cursorState.Invoke(_orderTroopPlacer, new object[] { });
        }
        private static Vec2 GetScreenPoint(OrderTroopPlacer __instance, ref Vec2 ____deltaMousePosition)
        {
            return !__instance.MissionScreen.MouseVisible
                ? new Vec2(0.5f, 0.5f) + ____deltaMousePosition
                : __instance.Input.GetMousePositionRanged() + ____deltaMousePosition;
        }

        private static void BeginFormationDraggingOrClicking(OrderTroopPlacer __instance, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime)
        {
            Vec3 rayBegin;
            Vec3 rayEnd;
            __instance.MissionScreen.ScreenPointToWorldRay(GetScreenPoint(__instance, ref ____deltaMousePosition), out rayBegin, out rayEnd);
            float collisionDistance;
            if (__instance.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out collisionDistance,
                    0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora))
            {
                Vec3 vec3 = rayEnd - rayBegin;
                double num = vec3.Normalize();
                ____formationDrawingStartingPosition = new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, rayBegin + vec3 * collisionDistance,
                    false);
                ____formationDrawingStartingPointOfMouse = __instance.Input.GetMousePositionPixel();
                // Fix the issue that can't drag when slow motion is enabled and mouse is visible.
                ____formationDrawingStartingTime = 0;
                return;
            }

            ____formationDrawingStartingPosition = new WorldPosition?();
            ____formationDrawingStartingPointOfMouse = new Vec2?();
            ____formationDrawingStartingTime = new float?();
        }

        public static bool Prefix_HandleMouseDown(OrderTroopPlacer __instance, ref Formation ____clickedFormation, ref Formation ____mouseOverFormation,
            ref bool ____formationDrawingMode, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime, bool ____isMouseDown)
        {
            if (__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.IsEmpty() || ____clickedFormation != null)
                return false;
            switch (_currentCursorState)
            {
                case CursorState.Normal:
                case CursorState.Enemy:
                case CursorState.Friend:
                    ____formationDrawingMode = true;
                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    break;
                case CursorState.Rotation:
                    return true;
            }

            return false;
        }


        private static void HideNonSelectedOrderRotationEntities(OrderController ___PlayerOrderController, List<GameEntity> ____orderRotationEntities, Formation formation)
        {
            for (int index = 0; index < ____orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = ____orderRotationEntities[index];
                if (orderRotationEntity == null && orderRotationEntity.IsVisibleIncludeParents() && ___PlayerOrderController.SelectedFormations.ElementAt(index / 2) != formation)
                {
                    orderRotationEntity.SetVisibilityExcludeParents(false);
                    orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
                }
            }
        }


        private static void TryTransformFromClickingToDragging(OrderTroopPlacer __instance, Vec2? ____formationDrawingStartingPointOfMouse, float? ____formationDrawingStartingTime, OrderController ___PlayerOrderController,
            ref Formation ____clickedFormation, ref bool ____formationDrawingMode, bool ____isMouseDown)
        {
            if (___PlayerOrderController.SelectedFormations.IsEmpty())
                return;
            switch (_currentCursorState)
            {
                case CursorState.Enemy:
                case CursorState.Friend:
                    if (IsDraggingFormation(__instance, ____formationDrawingStartingPointOfMouse, ____formationDrawingStartingTime))
                    {
                        if ((__instance.Input.IsKeyDown(InputKey.LeftMouseButton) ||
                             __instance.Input.IsKeyDown(InputKey.ControllerRTrigger)) && ____isMouseDown)
                        {
                            ____formationDrawingMode = true;
                            ____clickedFormation = null;
                        }
                    }

                    break;
            }
        }

        public static bool Prefix_GetCursorState(OrderTroopPlacer __instance, ref CursorState __result, OrderController ___PlayerOrderController, ref Formation ____clickedFormation, ref Formation ____mouseOverFormation,
            List<GameEntity> ____orderRotationEntities, ref Vec2 ____deltaMousePosition, ref bool ____formationDrawingMode, ref int ____mouseOverDirection, bool ____isMouseDown)
        {
            CursorState cursorState = CursorState.Invisible;
            if (!___PlayerOrderController.SelectedFormations.IsEmpty() && ____clickedFormation == null)
            {
                __instance.MissionScreen.ScreenPointToWorldRay(GetScreenPoint(__instance, ref ____deltaMousePosition), out var rayBegin, out var rayEnd);
                if (!__instance.Mission.Scene.RayCastForClosestEntityOrTerrain(rayBegin, rayEnd, out var collisionDistance,
                    out GameEntity collidedEntity, 0.3f, BodyFlags.CommonFocusRayCastExcludeFlags | BodyFlags.BodyOwnerFlora))
                    collisionDistance = 1000f;
                if (cursorState == CursorState.Invisible && collisionDistance < 1000.0)
                {
                    if (!____formationDrawingMode && collidedEntity == null)
                    {
                        for (int index = 0; index < ____orderRotationEntities.Count; ++index)
                        {
                            GameEntity orderRotationEntity = ____orderRotationEntities[index];
                            if (orderRotationEntity.IsVisibleIncludeParents() &&
                                collidedEntity == orderRotationEntity)
                            {
                                ____mouseOverFormation =
                                    ___PlayerOrderController.SelectedFormations.ElementAt(index / 2);
                                ____mouseOverDirection = 1 - (index & 1);
                                cursorState = CursorState.Rotation;
                                break;
                            }
                        }
                    }

                    if (cursorState == CursorState.Invisible)
                    {
                        if (__instance.MissionScreen.OrderFlag.FocusedOrderableObject != null)
                            cursorState = CursorState.OrderableEntity;
                        else if (CommandSystemConfig.Get().IsMouseOverEnabled())
                        {
                            var formation = GetMouseOverFormation(__instance, collisionDistance,
                                ___PlayerOrderController, ref ____deltaMousePosition,
                                ____formationDrawingMode);
                            ____mouseOverFormation = formation;
                            if (formation != null)
                            {
                                if (formation.Team.IsEnemyOf(__instance.Mission.PlayerTeam))
                                {
                                    if (CommandSystemConfig.Get().AttackSpecificFormation)
                                    {
                                        cursorState = CursorState.Enemy;
                                    }
                                }
                                else
                                {
                                    if (CommandSystemConfig.Get().ClickToSelectFormation)
                                    {
                                        cursorState = CursorState.Friend;
                                    }
                                }
                            }
                        }
                    }
                    if (cursorState == CursorState.Invisible &&
                        !(CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation).IsKeyDown(__instance.Input) && CommandSystemConfig.Get().IsMouseOverEnabled()) || // press middle mouse button to avoid accidentally click on ground.
                        ____formationDrawingMode)
                    {
                        cursorState = IsCursorStateGroundOrNormal(____formationDrawingMode);
                    }
                }
            }
            else if (____clickedFormation != null) // click on formation and hold.
            {
                cursorState = _currentCursorState;
            }

            if (cursorState != CursorState.Ground &&
                cursorState != CursorState.Rotation)
                ____mouseOverDirection = 0;
            __result = cursorState;
            return false;
        }

        private static CursorState IsCursorStateGroundOrNormal(bool ____formationDrawingMode)
        {
            return !____formationDrawingMode
                ? CursorState.Normal
                : CursorState.Ground;
        }

        private static Agent RayCastForAgent(OrderTroopPlacer __instance, float distance, ref Vec2 ____deltaMousePosition)
        {
            __instance.MissionScreen.ScreenPointToWorldRay(GetScreenPoint(__instance, ref ____deltaMousePosition), out var rayBegin, out var rayEnd);
            var agent = __instance.Mission.RayCastForClosestAgent(rayBegin, rayEnd, out var agentDistance,
                __instance.MissionScreen.LastFollowedAgent?.Index ?? -1, 0.3f);
            if (agentDistance > distance || agent == null)
            {
                agent = __instance.Mission.RayCastForClosestAgent(rayBegin, rayEnd, out agentDistance,
                    __instance.MissionScreen.LastFollowedAgent?.Index ?? -1, 0.8f);
            }
            return agentDistance > distance ? null : agent;
        }

        private static Formation GetMouseOverFormation(OrderTroopPlacer __instance, float collisionDistance, OrderController ___PlayerOrderController, ref Vec2 ____deltaMousePosition, bool ____formationDrawingMode)
        {

            if (_focusedFormationsCache != null && _focusedFormationsCache.Count > 0)
            {
                return _focusedFormationsCache[0];
            }
            var agent = RayCastForAgent(__instance, collisionDistance, ref ____deltaMousePosition);
            if (agent != null && agent.IsMount)
                agent = agent.RiderAgent;
            if (agent == null)
                return null;
            if (CommandSystemConfig.Get().IsMouseOverEnabled() && !__instance.IsDrawingForced && !____formationDrawingMode && agent?.Formation != null &&
                !(___PlayerOrderController.SelectedFormations.Count == 1 &&
                  ___PlayerOrderController.SelectedFormations.Contains(agent.Formation)))
            {
                return agent.Formation;
            }

            return null;
        }

        private static void AddOrderPositionEntity(OrderTroopPlacer __instance, int entityIndex,
            Vec3 groundPosition, bool fadeOut, float alpha,
            ref List<GameEntity> ____orderPositionEntities, ref Material ____meshMaterial)
        {
            var config = CommandSystemConfig.Get();
            var currentMovementTargetHighlightStyle = IsFreeCamera ? config.MovementTargetHighlightStyleInRTSMode : config.MovementTargetHighlightStyleInCharacterMode;

            switch (currentMovementTargetHighlightStyle)
            {
                case MovementTargetHighlightStyle.Original:
                    if (_originalOrderPositionEntities == null)
                    {
                        _originalOrderPositionEntities = new List<GameEntity>();
                        _originalMaterial = null;
                    }
                    break;
                case MovementTargetHighlightStyle.NewModelOnly:
                    if (_newModelOrderPositionEntities == null)
                    {
                        _newModelOrderPositionEntities = new List<GameEntity>();
                        _newModelMaterial = Material.GetFromResource("vertex_color_blend_mat").CreateCopy(); ;
                    }
                    break;
                case MovementTargetHighlightStyle.AlwaysVisible:
                    if (_alwaysVisibleOrderPositionEntities == null)
                    {
                        _alwaysVisibleOrderPositionEntities = new List<GameEntity>();
                        _alwaysVisibleMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy(); ;
                    }
                    break;
            }
            if (_previousMovementTargetHightlightStyle != currentMovementTargetHighlightStyle)
            {
                switch (_previousMovementTargetHightlightStyle)
                {
                    case MovementTargetHighlightStyle.Original:
                        _originalMaterial = ____meshMaterial;
                        foreach (GameEntity orderPositionEntity in _originalOrderPositionEntities ?? Enumerable.Empty<GameEntity>())
                        {
                            orderPositionEntity.SetVisibilityExcludeParents(false);
                        }
                        break;
                    case MovementTargetHighlightStyle.NewModelOnly:
                        foreach (GameEntity orderPositionEntity in _newModelOrderPositionEntities ?? Enumerable.Empty<GameEntity>())
                        {
                            orderPositionEntity.SetVisibilityExcludeParents(false);
                        }
                        break;
                    case MovementTargetHighlightStyle.AlwaysVisible:
                        foreach (GameEntity orderPositionEntity in _alwaysVisibleOrderPositionEntities ?? Enumerable.Empty<GameEntity>())
                        {
                            orderPositionEntity.SetVisibilityExcludeParents(false);
                        }
                        break;
                }
                _previousMovementTargetHightlightStyle = currentMovementTargetHighlightStyle;

                switch (currentMovementTargetHighlightStyle)
                {
                    case MovementTargetHighlightStyle.Original:
                        ____orderPositionEntities = _originalOrderPositionEntities;
                        ____meshMaterial = _originalMaterial;
                        break;
                    case MovementTargetHighlightStyle.NewModelOnly:
                        ____orderPositionEntities = _newModelOrderPositionEntities;
                        ____meshMaterial = _newModelMaterial;
                        break;
                    case MovementTargetHighlightStyle.AlwaysVisible:
                        ____orderPositionEntities = _alwaysVisibleOrderPositionEntities;
                        ____meshMaterial = _alwaysVisibleMaterial;
                        break;
                }
            }
            if (currentMovementTargetHighlightStyle != MovementTargetHighlightStyle.Original)
            {
                while (____orderPositionEntities.Count <= entityIndex)
                {
                    GameEntity gameEntity = GameEntity.CreateEmpty(__instance.Mission.Scene);
                    gameEntity.EntityFlags |= EntityFlags.NotAffectedBySeason;
                    MetaMesh copy = MetaMesh.GetCopy("barrier_sphere");
                    //MetaMesh copy = MetaMesh.GetCopy("pyhsics_test_box");
                    //MetaMesh copy = MetaMesh.GetCopy("unit_arrow");
                    if (____meshMaterial == null)
                    {
                        //____meshMaterial = copy.GetMeshAtIndex(0).GetMaterial().CreateCopy();
                        ____meshMaterial = Material.GetFromResource("vertex_color_blend_no_depth_mat").CreateCopy();
                        //____meshMaterial = Material.GetFromResource("unit_arrow").CreateCopy();
                        //____meshMaterial.SetAlphaBlendMode(Material.MBAlphaBlendMode.Factor);
                    }
                    copy.SetMaterial(____meshMaterial);
                    copy.SetFactor1(OrderPositionEntityColor);
                    //copy.SetContourColor(OrderPositionEntityColor);
                    //copy.SetContourState(true);
                    gameEntity.AddComponent(copy);
                    //gameEntity.SetContourColor(OrderPositionEntityColor, true);
                    gameEntity.SetVisibilityExcludeParents(false);
                    ____orderPositionEntities.Add(gameEntity);
                }
                GameEntity orderPositionEntity = ____orderPositionEntities[entityIndex];
                MatrixFrame frame = new MatrixFrame(Mat3.Identity, groundPosition + (Vec3.Up * 1.0f));
                orderPositionEntity.SetFrame(ref frame);
                if (fadeOut)
                    orderPositionEntity.FadeOut(CommandSystemConfig.Get().MovementTargetFadeOutDuration, false);
                else if (alpha != -1.0)
                {
                    alpha = OrderPositionEntityDestinationAlpha;
                    orderPositionEntity.SetVisibilityExcludeParents(true);
                    orderPositionEntity.SetAlpha(alpha);
                    //orderPositionEntity.FadeIn();
                }
                else
                {
                    //alpha = OrderPositionEntityPreviewAlpha;
                    //orderPositionEntity.SetVisibilityExcludeParents(true);
                    //orderPositionEntity.SetAlpha(alpha);
                    orderPositionEntity.FadeIn();
                }

            }
            else
            {
                while (____orderPositionEntities.Count <= entityIndex)
                {
                    GameEntity empty = GameEntity.CreateEmpty(__instance.Mission.Scene);
                    empty.EntityFlags |= EntityFlags.NotAffectedBySeason;
                    MetaMesh copy = MetaMesh.GetCopy("order_flag_small");
                    empty.AddComponent((GameEntityComponent)copy);
                    empty.SetVisibilityExcludeParents(false);
                    ____orderPositionEntities.Add(empty);
                }
                GameEntity orderPositionEntity = ____orderPositionEntities[entityIndex];
                MatrixFrame frame = new MatrixFrame(Mat3.Identity, groundPosition);
                orderPositionEntity.SetFrame(ref frame);
                if ((double)alpha != -1.0)
                {
                    orderPositionEntity.SetVisibilityExcludeParents(true);
                    orderPositionEntity.SetAlpha(alpha);
                }
                else if (fadeOut)
                    orderPositionEntity.FadeOut(0.3f, false);
                else
                    orderPositionEntity.FadeIn();
            }
        }

        public static bool Prefix_AddOrderPositionEntity(OrderTroopPlacer __instance, int entityIndex,
            ref Vec3 groundPosition, bool fadeOut, float alpha,
            ref List<GameEntity> ____orderPositionEntities, ref Material ____meshMaterial)
        {
            AddOrderPositionEntity(__instance, entityIndex, groundPosition, fadeOut, alpha, ref ____orderPositionEntities, ref ____meshMaterial);
            return false;
        }

        private static void HandleSelectFormationKeyDown(OrderTroopPlacer __instance, ref Formation ____clickedFormation, ref Formation ____mouseOverFormation,
            ref bool ____formationDrawingMode, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime)
        {
            if (__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations.IsEmpty() || ____clickedFormation != null)
                return;
            switch (_currentCursorState)
            {
                case CursorState.Enemy:
                    ____formationDrawingMode = false;
                    ____clickedFormation = ____mouseOverFormation;

                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    break;
                case CursorState.Friend:
                    ____formationDrawingMode = false;
                    if (____mouseOverFormation != null && __instance.Mission.PlayerTeam.PlayerOrderController.IsFormationSelectable(____mouseOverFormation))
                    {
                        ____clickedFormation = ____mouseOverFormation;
                    }
                    BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                        ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                        ref ____formationDrawingStartingTime);
                    break;
            }
        }

        private static void HandleSelectFormationKeyUp(OrderTroopPlacer __instance, ref Formation ____clickedFormation, OrderController ___PlayerOrderController, List<GameEntity> ____orderRotationEntities,
            ref bool ____formationDrawingMode, ref Vec2 ____deltaMousePosition,
            ref WorldPosition? ____formationDrawingStartingPosition, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime)
        {
            if (!____formationDrawingMode)
            {
                if (_focusedFormationsCache != null && _focusedFormationsCache.Count > 0)
                {
                    ____clickedFormation = _focusedFormationsCache.FirstOrDefault();
                }
                if (____clickedFormation != null)
                {
                    if (____clickedFormation.CountOfUnits > 0)
                    {
                        bool isEnemy = Utility.IsEnemy(____clickedFormation);
                        if (!isEnemy)
                        {
                            HideNonSelectedOrderRotationEntities(___PlayerOrderController, ____orderRotationEntities, ____clickedFormation);

                            if (___PlayerOrderController.IsFormationSelectable(____clickedFormation))
                            {
                                //SelectFormationFromUI(__instance, ____clickedFormation);
                                SelectFormationFromController(__instance, ___PlayerOrderController, ____clickedFormation);
                            }
                        }
                        else if (CommandSystemConfig.Get().AttackSpecificFormation)
                        {
                            bool keepMovementOrder = CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepMovementOrder).IsKeyDownInOrder(__instance.Input);
                            if (keepMovementOrder)
                            {
                                if (Campaign.Current == null || CommandSystemSkillBehavior.CanIssueChargeToFormationOrder)
                                {
                                    Utilities.Utility.FocusOnFormation(___PlayerOrderController, ____clickedFormation);
                                }
                                else
                                {
                                    Utility.DisplayMessage(GameTexts
                                        .FindText("str_rts_camera_command_system_tactic_level_required")
                                        .SetTextVariable("level",
                                            CommandSystemSkillBehavior.RequiredTacticsLevelToIssueChargeToFormationOrder)
                                        .ToString());
                                }
                            }
                            else
                            {
                                Utilities.Utility.ChargeToFormation(___PlayerOrderController, ____clickedFormation);
                            }
                        }
                    }

                    ____clickedFormation = null;

                    ____formationDrawingMode = false;
                    ____formationDrawingStartingPosition = null;
                    ____formationDrawingStartingPointOfMouse = null;
                    ____formationDrawingStartingTime = null;
                    ____deltaMousePosition = Vec2.Zero;
                }
            }
        }

        public static bool Prefix_OnMissionScreenTick(OrderTroopPlacer __instance, ref bool ____initialized, ref OrderController ___PlayerOrderController,
            ref bool ___isDrawnThisFrame, ref bool ____isMouseDown, ref Timer ___formationDrawTimer, ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime,
            ref Formation ____clickedFormation, ref bool ____formationDrawingMode, Formation ____mouseOverFormation,
            ref List<GameEntity> ____orderPositionEntities, ref List<GameEntity> ____orderRotationEntities,
            ref bool ____wasDrawingForced, ref bool ____wasDrawingFacing, ref bool ____wasDrawingForming, ref bool ___wasDrawnPreviousFrame, ref WorldPosition? ____formationDrawingStartingPosition,
            ref Vec2 ____deltaMousePosition)
        {
            if (!____initialized)
                return false;
            if (!___PlayerOrderController.SelectedFormations.Any())
                return false;
            ___isDrawnThisFrame = false;
            if (__instance.SuspendTroopPlacer)
                return false;
            
            bool isSelectFormationKeyPressed = CommandSystemConfig.Get().IsMouseOverEnabled() &&
                                            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)
                                                .IsKeyPressed(__instance.Input);
            bool isSelectFormationKeyReleased = CommandSystemConfig.Get().IsMouseOverEnabled() &&
                                                CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)
                                                    .IsKeyReleased(__instance.Input);
            bool isSelectFormationKeyDown = CommandSystemConfig.Get().IsMouseOverEnabled() &&
                                            CommandSystemGameKeyCategory.GetKey(GameKeyEnum.SelectFormation)
                                                .IsKeyDown(__instance.Input);
            _currentCursorState = _cachedCursorState.Value;
            bool isLeftButtonPressed = __instance.Input.IsKeyPressed(InputKey.LeftMouseButton) ||
                              __instance.Input.IsKeyPressed(InputKey.ControllerRTrigger);

            if (isLeftButtonPressed)
            {
                ____isMouseDown = true;
                _handleMouseDown?.Invoke(__instance, new object[] { });
            }
            if (isSelectFormationKeyPressed)
            {
                HandleSelectFormationKeyDown(__instance, ref ____clickedFormation, ref ____mouseOverFormation,
                    ref ____formationDrawingMode, ref ____deltaMousePosition, ref ____formationDrawingStartingPosition,
                    ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime);
            }
            if (isSelectFormationKeyReleased)
            {
                HandleSelectFormationKeyUp(__instance, ref ____clickedFormation, ___PlayerOrderController,
                    ____orderRotationEntities, ref ____formationDrawingMode, ref ____deltaMousePosition,
                    ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                    ref ____formationDrawingStartingTime);
            }
            if ((__instance.Input.IsKeyReleased(InputKey.LeftMouseButton) || __instance.Input.IsKeyReleased(InputKey.ControllerRTrigger)) && ____isMouseDown || isSelectFormationKeyReleased)
            {
                ____isMouseDown = false;
                // Formation.GetOrderPositionOfUnit is wrong in the next tick after movement order is issued.
                // we skip updating from the wrong position for 1 tick.
                _skipDrawingForDestinationForOneTick = true;
                _handleMouseUp?.Invoke(__instance, new object[] { });
            }
            else if (____isMouseDown && isSelectFormationKeyDown)
            {
                if (!__instance.IsDrawingFacing && !__instance.IsDrawingForming)
                {
                    TryTransformFromClickingToDragging(__instance, ____formationDrawingStartingPointOfMouse,
                        ____formationDrawingStartingTime, ___PlayerOrderController, ref ____clickedFormation,
                        ref ____formationDrawingMode, ____isMouseDown);
                }
            }
            if ((__instance.Input.IsKeyDown(InputKey.LeftMouseButton) || __instance.Input.IsKeyDown(InputKey.ControllerRTrigger)) && ____isMouseDown)
            {
                if (___formationDrawTimer.Check(MBCommon.GetApplicationTime()) &&
                    !__instance.IsDrawingFacing &&
                    !__instance.IsDrawingForming)
                {
                    if (_currentCursorState == CursorState.Ground)
                        _updateFormationDrawing.Invoke(__instance, new object[] { false });
                }
            }
            else if (__instance.IsDrawingForced)
            {
                //Utilities.DisplayMessage("drawing forced");
                Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                    ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                    ref ____mouseOverFormation, ref ____clickedFormation);
                ____formationDrawingMode = true;
                BeginFormationDraggingOrClicking(__instance, ref ____deltaMousePosition,
                    ref ____formationDrawingStartingPosition, ref ____formationDrawingStartingPointOfMouse,
                    ref ____formationDrawingStartingTime);
                //HandleMousePressed();
                _updateFormationDrawing.Invoke(__instance, new object[] { false });
            }
            else if (__instance.IsDrawingFacing || ____wasDrawingFacing)
            {
                if (__instance.IsDrawingFacing)
                {
                    Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                        ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                        ref ____mouseOverFormation, ref ____clickedFormation);

                    _updateFormationDrawingForFacingOrder.Invoke(__instance, new object[] { false });
                }
            }
            else if (__instance.IsDrawingForming || ____wasDrawingForming)
            {
                if (__instance.IsDrawingForming)
                {
                    Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                        ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                        ref ____mouseOverFormation, ref ____clickedFormation);

                    _updateFormationDrawingForFormingOrder.Invoke(__instance, new object[] { false });
                }
            }
            else if (____wasDrawingForced)
            {
                Reset(ref ____isMouseDown, ref ____formationDrawingMode, ref ____formationDrawingStartingPosition,
                    ref ____formationDrawingStartingPointOfMouse, ref ____formationDrawingStartingTime,
                    ref ____mouseOverFormation, ref ____clickedFormation);
            }
            else if (_skipDrawingForDestinationForOneTick)
            {
                _skipDrawingForDestinationForOneTick = false;
            }
            else
            {
                _updateFormationDrawingForDestination.Invoke(__instance, new object[] { false });
            }

            UpdateMouseOverFormation(____mouseOverFormation);
            foreach (GameEntity orderPositionEntity in ____orderPositionEntities)
                orderPositionEntity.SetPreviousFrameInvalid();
            foreach (GameEntity orderRotationEntity in ____orderRotationEntities)
                orderRotationEntity.SetPreviousFrameInvalid();
            ____wasDrawingForced = __instance.IsDrawingForced;
            ____wasDrawingFacing = __instance.IsDrawingFacing;
            ____wasDrawingForming = __instance.IsDrawingForming;
            ___wasDrawnPreviousFrame = ___isDrawnThisFrame;

            return false;
        }

        private static void UpdateMouseOverFormation(Formation ____mouseOverFormation)
        {
            _outlineView?.MouseOver(____mouseOverFormation);
            _groundMarkerView?.MouseOver(____mouseOverFormation);
        }
        private static void Reset(ref bool ____isMouseDown, ref bool ____formationDrawingMode, ref WorldPosition? ____formationDrawingStartingPosition,
            ref Vec2? ____formationDrawingStartingPointOfMouse, ref float? ____formationDrawingStartingTime,
            ref Formation ____mouseOverFormation, ref Formation ____clickedFormation)
        {
            ____isMouseDown = false;
            ____formationDrawingMode = false;
            ____formationDrawingStartingPosition = new WorldPosition?();
            ____formationDrawingStartingPointOfMouse = new Vec2?();
            ____formationDrawingStartingTime = new float?();
            ____mouseOverFormation = (Formation)null;
            ____clickedFormation = (Formation)null;
        }

        //public static void SelectFormationFromUI(OrderTroopPlacer __instance, Formation ____clickedFormation)
        //{
        //    var uiHandler = __instance.Mission.GetMissionBehavior<MissionGauntletSingleplayerOrderUIHandler>();
        //    if (uiHandler == null)
        //        return;
        //    var dataSource = (MissionOrderVM)_dataSource.GetValue(uiHandler);
        //    if (dataSource == null)
        //    {
        //        return;
        //    }

        //    dataSource.OnSelect(____clickedFormation.Index);
        //}

        public static void SelectFormationFromController(OrderTroopPlacer __instance, OrderController ___PlayerOrderController, Formation ____clickedFormation)
        {
            if (!CommandSystemGameKeyCategory.GetKey(GameKeyEnum.KeepFormationWidth).IsKeyDownInOrder(__instance.Input))
            {
                ___PlayerOrderController.ClearSelectedFormations();
                ___PlayerOrderController.SelectFormation(____clickedFormation);
            }
            else if (___PlayerOrderController.IsFormationListening(____clickedFormation))
            {
                ___PlayerOrderController.DeselectFormation(____clickedFormation);
            }
            else
            {
                ___PlayerOrderController.SelectFormation(____clickedFormation);
            }
        }

        public static bool Prefix_UpdateFormationDrawingForMovementOrder(OrderTroopPlacer __instance,
            bool giveOrder,
            WorldPosition formationRealStartingPosition,
            WorldPosition formationRealEndingPosition,
            bool isFormationLayoutVertical, ref bool ___isDrawnThisFrame, OrderController ___PlayerOrderController)
        {
            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(___PlayerOrderController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(___PlayerOrderController.SelectedFormations));
            }
            Patch_OrderController.LivePreviewFormationChanges.SetMovementOrder(isFormationLayoutVertical ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout, ___PlayerOrderController.SelectedFormations, null, null, null);
            ___isDrawnThisFrame = true;
            List<WorldPosition> simulationAgentFrames = null;
            bool isLineShort = false;
            IEnumerable<Formation> formations = ___PlayerOrderController.SelectedFormations.Where((f => f.CountOfUnitsWithoutDetachedOnes > 0));
            if (!formations.Any())
                return true;
            bool fadeOut = Utilities.Utility.ShouldFadeOut() && giveOrder && !queueCommand;
            bool shouldAddAgentFrames = !giveOrder || fadeOut;
            Patch_OrderController.SimulateNewOrderWithPositionAndDirection(formations, ___PlayerOrderController.simulationFormations, formationRealStartingPosition, formationRealEndingPosition, shouldAddAgentFrames, out simulationAgentFrames, giveOrder, out var formationChanges, out isLineShort, isFormationLayoutVertical, true);
            bool shouldLimitFormationSpeedToLowest = Utilities.Utility.ShouldLockFormation();
            if (giveOrder)
            {
                if (!queueCommand)
                {
                    if (!isFormationLayoutVertical)
                        ___PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegmentWithHorizontalLayout, formationRealStartingPosition, formationRealEndingPosition);
                    else
                        ___PlayerOrderController.SetOrderWithTwoPositions(OrderType.MoveToLineSegment, formationRealStartingPosition, formationRealEndingPosition);
                    CommandQueueLogic.TryPendingOrder(formations, new OrderInQueue
                    {
                        SelectedFormations = formations.ToList(),
                        OrderType = isFormationLayoutVertical ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout,
                        IsLineShort = isLineShort,
                        PositionBegin = formationRealStartingPosition,
                        PositionEnd = formationRealEndingPosition,
                        VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(__instance.Mission.PlayerTeam.PlayerOrderController.SelectedFormations),
                        ShouldAdjustFormationSpeed = shouldLimitFormationSpeedToLowest
                    });
                }
                else
                {
                    CommandQueueLogic.AddOrderToQueue(new OrderInQueue
                    {
                        SelectedFormations = formations.ToList(),
                        OrderType = isFormationLayoutVertical ? OrderType.MoveToLineSegment : OrderType.MoveToLineSegmentWithHorizontalLayout,
                        IsLineShort = isLineShort,
                        ActualFormationChanges = formationChanges,
                        PositionBegin = formationRealStartingPosition,
                        PositionEnd = formationRealEndingPosition,
                        VirtualFormationChanges = Patch_OrderController.LivePreviewFormationChanges.CollectChanges(___PlayerOrderController.SelectedFormations),
                        ShouldAdjustFormationSpeed = shouldLimitFormationSpeedToLowest
                    });
                }
            }
            if (shouldAddAgentFrames)
            {
                AddOrderPositionEntities(simulationAgentFrames, fadeOut);
            }
            return false;
        }
        private static Vec3 GetGroundedVec3(Mission mission, WorldPosition worldPosition)
        {
            return worldPosition.GetGroundVec3();
        }

        public static bool Prefix_UpdateFormationDrawingForFacingOrder(OrderTroopPlacer __instance,
            bool giveOrder, OrderController ___PlayerOrderController)
        {
            bool queueCommand = Utilities.Utility.ShouldQueueCommand();
            if (!queueCommand)
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.CurrentFormationChanges.CollectChanges(___PlayerOrderController.SelectedFormations));
            }
            else
            {
                Patch_OrderController.LivePreviewFormationChanges.SetChanges(CommandQueueLogic.LatestOrderInQueueChanges.CollectChanges(___PlayerOrderController.SelectedFormations));
            }
            return true;
        }

        public static void SetIsDrawingFacing(bool isDrawingFacing)
        {
            if (_orderTroopPlacer == null)
                return;
            _orderTroopPlacer.IsDrawingFacing = isDrawingFacing;
        }

        public static void Reset()
        {
            if (_orderTroopPlacer == null)
                return;
            _reset.Invoke(_orderTroopPlacer, new object[] { });
        }

        public static void AddOrderPositionEntities(List<WorldPosition> agentFrames, bool fadeOut, int startIndex = 0)
        {
            ref List<GameEntity> ____orderPositionEntities =
                ref AccessTools.FieldRefAccess<OrderTroopPlacer, List<GameEntity>>(_orderTroopPlacer, _orderPositionEntities);
            ref Material ____meshMaterial = ref AccessTools.StaticFieldRefAccess<Material>(typeof(OrderTroopPlacer), "_meshMaterial");
            foreach (WorldPosition worldPosition in agentFrames)
            {
                AddOrderPositionEntity(_orderTroopPlacer, startIndex, GetGroundedVec3(_orderTroopPlacer.Mission, worldPosition), fadeOut, -1f, ref ____orderPositionEntities, ref ____meshMaterial);
                ++startIndex;
            }
        }

        public static bool Prefix_HideOrderPositionEntities(OrderTroopPlacer __instance,
            ref List<GameEntity> ____orderPositionEntities,
            List<GameEntity> ____orderRotationEntities)
        {
            if (__instance.SuspendTroopPlacer)
            {
                foreach (GameEntity orderPositionEntity in ____orderPositionEntities)
                    orderPositionEntity.HideIfNotFadingOut();
            }
            else
            {
                foreach (GameEntity orderPositionEntity in ____orderPositionEntities)
                    orderPositionEntity.SetVisibilityExcludeParents(false);
            }
            for (int index = 0; index < ____orderRotationEntities.Count; ++index)
            {
                GameEntity orderRotationEntity = ____orderRotationEntities[index];
                orderRotationEntity.SetVisibilityExcludeParents(false);
                orderRotationEntity.BodyFlag |= BodyFlags.Disabled;
            }
            return false;
        }
    }
}
