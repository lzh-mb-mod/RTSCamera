using MissionSharedLibrary.HotKey.Category;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using Module = TaleWorlds.MountAndBlade.Module;

namespace MissionSharedLibrary.Utilities
{
    public static class Utility
    {
        public static string ModuleId;

        public static bool ShouldDisplayMessage { get; set; }
        public static void DisplayLocalizedText(string id, string variation = null)
        {
            try
            {
                if (!ShouldDisplayMessage)
                    return;
                DisplayMessageImpl(GameTexts.FindText(id, variation).ToString());
            }
            catch
            {
                // ignored
            }
        }
        public static void DisplayLocalizedText(string id, string variation, Color color)
        {
            try
            {
                if (!ShouldDisplayMessage)
                    return;
                DisplayMessageImpl(GameTexts.FindText(id, variation).ToString(), color);
            }
            catch
            {
                // ignored
            }
        }
        public static void DisplayMessage(string msg)
        {
            try
            {
                if (!ShouldDisplayMessage)
                    return;
                DisplayMessageImpl(new TextObject(msg).ToString());
            }
            catch
            {
                // ignored
            }
        }
        public static void DisplayMessage(string msg, Color color)
        {
            try
            {
                if (!ShouldDisplayMessage)
                    return;
                DisplayMessageImpl(new TextObject(msg).ToString(), color);
            }
            catch
            {
                // ignored
            }
        }

        private static void DisplayMessageImpl(string str)
        {
            InformationManager.DisplayMessage(new InformationMessage($"{ModuleId}: " + str));
        }

        private static void DisplayMessageImpl(string str, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage($"{ModuleId}: " + str, color));
        }

        public static void PrintUsageHint()
        {
            var keyName = GeneralGameKeyCategories.GetKey(GeneralGameKey.OpenMenu).ToSequenceString();
            var hint = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_open_menu_hint").SetTextVariable("KeyName", keyName).ToString();
            DisplayMessageForced(hint);
        }

        public static void DisplayMessageForced(string text)
        {
            DisplayMessageImpl(text);
        }

        public static TextObject TextForKey(InputKey key)
        {
            return Module.CurrentModule.GlobalTextManager.FindText("str_game_key_text",
                 new Key(key).ToString().ToLower());
        }

        public static bool IsAgentDead(Agent agent)
        {
            return agent == null || !agent.IsActive();
        }

        public static bool IsPlayerDead()
        {
            return IsAgentDead(Mission.Current.MainAgent);
        }

        public static void SetPlayerAsCommander(bool forced = false)
        {
            var mission = Mission.Current;
            if (mission?.PlayerTeam == null)
                return;
            mission.PlayerTeam.PlayerOrderController.Owner = mission.MainAgent;
            foreach (var formation in mission.PlayerTeam.FormationsIncludingEmpty)
            {
                if (formation.PlayerOwner != null || forced)
                {
                    bool isAIControlled = formation.IsAIControlled;
                    formation.PlayerOwner = mission.MainAgent;
                    formation.SetControlledByAI(isAIControlled);
                }
            }
        }

        public static void CancelPlayerAsCommander()
        {
        }

        public static void SetPlayerFormation(FormationClass formationClass)
        {
            var mission = Mission.Current;
            if (mission.MainAgent != null && mission.PlayerTeam != null)
            {
                var originalFormation = mission.MainAgent.Formation;
                if (originalFormation?.FormationIndex != formationClass)
                {
                    var formation = mission.PlayerTeam.GetFormation(formationClass);
                    if (formation.CountOfUnits == 0)
                    {
                        // Fix the bug when player is a sergeant of another formation, and the target formation is led by another sergeant, the formation will not be controlled by AI.
                        if (Mission.Current.PlayerTeam.IsPlayerGeneral && formation.IsAIControlled)
                            formation.SetControlledByAI(false);
                        if (originalFormation == null)
                        {
                            // fix crash when begin a battle and assign player to an empty formation, then give it an shield wall order.
                            formation.SetMovementOrder(
                                MovementOrder.MovementOrderMove(mission.MainAgent.GetWorldPosition()));
                        }
                        else
                        {
                            // copied from Formation.CopyOrdersFrom
                            formation.SetMovementOrder(originalFormation.GetReadonlyMovementOrderReference());
                            formation.FormOrder = originalFormation.FormOrder;
                            formation.SetPositioning(unitSpacing: originalFormation.UnitSpacing);
                            formation.RidingOrder = originalFormation.RidingOrder;
                            formation.WeaponUsageOrder = originalFormation.WeaponUsageOrder;
                            formation.FiringOrder = originalFormation.FiringOrder;                            
                            formation.SetControlledByAI(originalFormation.IsAIControlled || !originalFormation.Team.IsPlayerGeneral);
                            formation.AI.Side = originalFormation.AI.Side;
                            formation.SetMovementOrder(originalFormation.GetReadonlyMovementOrderReference());
                            formation.FacingOrder = originalFormation.FacingOrder;
                            formation.ArrangementOrder = originalFormation.ArrangementOrder;

                            WorldPosition orderWorldPosition = formation.CreateNewOrderWorldPosition(WorldPosition.WorldPositionEnforcedCache.None);
                            formation.SetPositioning(orderWorldPosition, formation.Direction, formation.UnitSpacing);
                        }
                    }

                    if (mission.MainAgent.Formation != null)
                        SetHasPlayer(mission.MainAgent.Formation, false);
                    mission.MainAgent.Formation = formation;
                    // add player's troop card. Disabled because the result is not good.
                    //mission.MainAgent.Team.MasterOrderController.TransferUnits(
                    //    originalFormation ?? (mission.MainAgent.Team.FormationsIncludingSpecialAndEmpty[0] == formation
                    //        ? mission.MainAgent.Team.FormationsIncludingSpecialAndEmpty[1]
                    //        : mission.MainAgent.Team.FormationsIncludingSpecialAndEmpty[0]), formation, 0);
                }
            }
        }

        public static bool IsInPlayerParty(Agent agent)
        {
            if (Campaign.Current != null)
            {
                if (agent.Origin is SimpleAgentOrigin simpleAgentOrigin && simpleAgentOrigin.Party == Campaign.Current.MainParty?.Party ||
                    agent.Origin is PartyAgentOrigin partyAgentOrigin && partyAgentOrigin.Party == Campaign.Current.MainParty?.Party ||
                    agent.Origin is PartyGroupAgentOrigin partyGroupAgentOrigin && partyGroupAgentOrigin.Party == Campaign.Current.MainParty?.Party)
                    return true;
            }
            else
            {
                return agent.Team == Mission.Current.PlayerTeam;
            }
            return false;
        }

        public static void PlayerControlAgent(Agent agent)
        {
            bool isUsingGameObject = agent.IsUsingGameObject;
            agent.Controller = Agent.ControllerType.Player;
            if (isUsingGameObject)
            {
                agent.DisableScriptedMovement();
                agent.AIUseGameObjectEnable(false);
            }

            var component = agent.GetComponent<VictoryComponent>();
            if (component != null)
            {
                agent.RemoveComponent(component);
                agent.SetActionChannel(1, ActionIndexCache.act_none, true);
                agent.ClearTargetFrame();
            }
        }

        public static void AIControlMainAgent(bool changeAlarmed, bool alarmed = false)
        {
            var mission = Mission.Current;
            if (mission?.MainAgent == null)
                return;
            try
            {
                mission.GetMissionBehavior<MissionMainAgentController>()?.InteractionComponent.ClearFocus();
                if (mission.MainAgent.Controller == Agent.ControllerType.Player)
                {
                    if (mission.MainAgent.Formation != null && mission.MainAgent.IsUsingGameObject && !(mission.MainAgent.CurrentlyUsedGameObject is SpawnedItemEntity))
                    {
                        mission.MainAgent.HandleStopUsingAction();
                    }
                    mission.MainAgent.Controller = Agent.ControllerType.AI;
                    if (changeAlarmed)
                    {
                        if (alarmed)
                        {
                            if ((mission.MainAgent.AIStateFlags & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.None)
                                SetMainAgentAlarmed(true);
                        }
                        else
                        {
                            SetMainAgentAlarmed(false);
                        }
                    }

                    mission.MainAgent.Formation?.GetReadonlyMovementOrderReference()
                        .OnUnitJoinOrLeave(mission.MainAgent.Formation, mission.MainAgent, true);
                }
            }
            catch (Exception e)
            {
                DisplayMessage(e.ToString());
            }

            // avoid crash after victory. After victory, team ai decision won't be made so that current tactics won't be updated.
            // Update: Removed because it may cause enemy cannot retreat.
            //if (mission.MissionEnded())
            //mission.AllowAiTicking = false;
        }

        public static void SetMainAgentAlarmed(bool alarmed)
        {
            Mission.Current.MainAgent?.SetWatchState(alarmed
                ? Agent.WatchState.Alarmed
                : Agent.WatchState.Patrolling);
        }

        public static bool IsEnemy(Agent agent)
        {
            return Mission.Current.MainAgent?.IsEnemyOf(agent) ??
                   Mission.Current.PlayerTeam?.IsEnemyOf(agent.Team) ?? false;
        }

        public static bool IsEnemy(Formation formation)
        {
            return Mission.Current.PlayerTeam?.IsEnemyOf(formation.Team) ?? false;
        }

        private static readonly FieldInfo CameraAddedElevation =
            typeof(MissionScreen).GetField("_cameraAddedElevation", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CameraTargetAddedHeight =
            typeof(MissionScreen).GetField("_cameraTargetAddedHeight", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CameraAddSpecialMovement =
            typeof(MissionScreen).GetField("_cameraAddSpecialMovement", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CameraApplySpecialMovementsInstantly =
            typeof(MissionScreen).GetField("_cameraApplySpecialMovementsInstantly", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SetLastFollowedAgent =
            typeof(MissionScreen).GetProperty("LastFollowedAgent", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(true);

        private static readonly FieldInfo CameraSpecialCurrentAddedElevation =
            typeof(MissionScreen).GetField("_cameraSpecialCurrentAddedElevation", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CameraSpecialCurrentAddedBearing =
            typeof(MissionScreen).GetField("_cameraSpecialCurrentAddedBearing", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CameraSpecialCurrentPositionToAdd =
            typeof(MissionScreen).GetField("_cameraSpecialCurrentPositionToAdd", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SetCameraElevation =
            typeof(MissionScreen).GetProperty("CameraElevation", BindingFlags.Instance | BindingFlags.Public)
                ?.GetSetMethod(true);

        private static readonly MethodInfo SetCameraBearing =
            typeof(MissionScreen).GetProperty("CameraBearing", BindingFlags.Instance | BindingFlags.Public)
                ?.GetSetMethod(true);

        private static readonly FieldInfo IsPlayerAgentAdded =
            typeof(MissionScreen).GetField("_isPlayerAgentAdded", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool ShouldSmoothMoveToAgent = true;

        public static bool BeforeSetMainAgent()
        {
            if (ShouldSmoothMoveToAgent)
            {
                ShouldSmoothMoveToAgent = false;
                return true;
            }

            return false;
        }

        public static void AfterSetMainAgent(bool shouldSmoothMoveToAgent, MissionScreen missionScreen, bool rotateCamera = true)
        {
            if (shouldSmoothMoveToAgent)
            {
                ShouldSmoothMoveToAgent = true;
                SmoothMoveToAgent(missionScreen, false, rotateCamera);
            }
        }

        public static void SmoothMoveToAgent(MissionScreen missionScreen, bool forceMove = false, bool changeCameraRotation = true)
        {
            try
            {
                var spectatingData = missionScreen.GetSpectatingData(missionScreen.CombatCamera.Position);
                if (spectatingData.AgentToFollow != null)
                {
                    CameraAddSpecialMovement?.SetValue(missionScreen, true);
                    CameraApplySpecialMovementsInstantly?.SetValue(missionScreen, false);
                    if (missionScreen.LastFollowedAgent != spectatingData.AgentToFollow || forceMove)
                    {
                        var targetFrame =
                            GetCameraFrameWhenLockedToAgent(missionScreen, spectatingData.AgentToFollow);
                        CameraSpecialCurrentPositionToAdd?.SetValue(missionScreen,
                            missionScreen.CombatCamera.Position - targetFrame.origin);
                    }
                    if (changeCameraRotation)
                    {
                        CameraSpecialCurrentAddedElevation?.SetValue(missionScreen, missionScreen.CameraElevation);
                        CameraSpecialCurrentAddedBearing?.SetValue(missionScreen,
                            MBMath.WrapAngle(missionScreen.CameraBearing - spectatingData.AgentToFollow.LookDirectionAsAngle));
                        SetCameraElevation?.Invoke(missionScreen, new object[] { 0.0f });
                        SetCameraBearing?.Invoke(missionScreen,
                            new object[] { spectatingData.AgentToFollow.LookDirectionAsAngle });
                    }

                    SetLastFollowedAgent.Invoke(missionScreen, new object[] { spectatingData.AgentToFollow });
                }
                // Avoid MissionScreen._cameraSpecialCurrentAddedBearing reset to 0.
                SetIsPlayerAgentAdded(missionScreen, false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static MatrixFrame GetCameraFrameWhenLockedToAgent(MissionScreen missionScreen, Agent agentToFollow)
        {
            MatrixFrame result = MatrixFrame.Identity;
            float cameraBaseDistance = 0.6f;
            float agentScale = agentToFollow.AgentScale;
            if (missionScreen.IsViewingCharacter())
            {
                cameraBaseDistance += 0.5f;
            }
            result.rotation.RotateAboutSide(1.570796f);
            result.rotation.RotateAboutForward(missionScreen.CameraBearing);
            result.rotation.RotateAboutSide(missionScreen.CameraElevation);
            MatrixFrame matrixFrame = result;
            float num8 = Math.Max(cameraBaseDistance + Mission.CameraAddedDistance, 0.48f) * agentScale;
            result.rotation.RotateAboutSide((float?)CameraAddedElevation?.GetValue(missionScreen) ?? 0);
            bool flag5 = agentToFollow.AgentVisuals != null && (uint)agentToFollow.AgentVisuals.GetSkeleton().GetCurrentRagdollState() > 0;
            var agentVisualPosition = agentToFollow.VisualPosition;
            var cameraTarget = flag5 ? agentToFollow.AgentVisuals.GetFrame().origin : agentVisualPosition;
            if (agentToFollow.MountAgent != null)
            {
                var vec3_4 = agentToFollow.MountAgent.GetMovementDirection().ToVec3() * agentToFollow.MountAgent.Monster.RiderBodyCapsuleForwardAdder;
                cameraTarget += vec3_4;
            }
            cameraTarget.z += (float)CameraTargetAddedHeight.GetValue(missionScreen);
            cameraTarget += matrixFrame.rotation.f * agentScale * (0.7f * MathF.Pow(MathF.Cos((float)(1.0 / ((num8 / (double)agentScale - 0.200000002980232) * 30.0 + 20.0))), 3500f));
            result.origin = cameraTarget + matrixFrame.rotation.u * missionScreen.CameraResultDistanceToTarget;
            return result;
        }

        public static void SetIsPlayerAgentAdded(MissionScreen missionScreen, bool value)
        {
            IsPlayerAgentAdded?.SetValue(missionScreen, value);
            if (value)
                CameraSpecialCurrentPositionToAdd?.SetValue(missionScreen, Vec3.Zero);
        }

        private static readonly PropertyInfo HasPlayer =
            typeof(Formation).GetProperty(nameof(HasPlayer), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SetHasPlayerMethod = HasPlayer?.GetSetMethod(true);

        public static void SetHasPlayer(Formation formation, bool hasPlayer)
        {
            try
            {
                SetHasPlayerMethod?.Invoke(formation, new object[] { hasPlayer });
            }
            catch (Exception e)
            {
                DisplayMessage(e.ToString());
            }
        }

        public static void Reset(this GameKey gameKey)
        {

            Key controllerKey = gameKey.ControllerKey;
            if (controllerKey != null)
            {
                Key defaultControllerKey = gameKey.DefaultControllerKey;
                controllerKey.ChangeKey((object)defaultControllerKey != null ? defaultControllerKey.InputKey : InputKey.Invalid);
            }
            Key keyboardKey = gameKey.KeyboardKey;
            if (keyboardKey != null)
            {
                Key defaultKeyboardKey = gameKey.DefaultKeyboardKey;
                keyboardKey.ChangeKey((object)defaultKeyboardKey != null ? defaultKeyboardKey.InputKey : InputKey.Invalid);
            }
        }
    }
}
