using System;
using System.Reflection;
using RTSCamera.Config;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;
using TaleWorlds.MountAndBlade.View.Screen;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RTSCamera
{
    public class Utility
    {
        private static readonly MethodInfo OnUnitJoinOrLeave =
            typeof(MovementOrder).GetMethod("OnUnitJoinOrLeave", BindingFlags.Instance | BindingFlags.NonPublic);
        public static void DisplayLocalizedText(string id, string variation = null)
        {
            try
            {
                if (!RTSCameraConfig.Get().DisplayMessage)
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
                if (!RTSCameraConfig.Get().DisplayMessage)
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
                if (!RTSCameraConfig.Get().DisplayMessage)
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
                if (!RTSCameraConfig.Get().DisplayMessage)
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
            InformationManager.DisplayMessage(new InformationMessage("RTS Camera: " + str));
        }

        private static void DisplayMessageImpl(string str, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage("RTS Camera: " + str, color));
        }

        public static void PrintUsageHint()
        {
            var keyName = TextForKey(GameKeyConfig.Get().GetKey(GameKeyEnum.OpenMenu));
            GameTexts.SetVariable("KeyName", keyName);
            var hint = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_open_menu_hint").ToString();
            DisplayMessageOutOfMission(hint);
            keyName = TextForKey(GameKeyConfig.Get().GetKey(GameKeyEnum.FreeCamera));
            GameTexts.SetVariable("KeyName", keyName);
            hint = Module.CurrentModule.GlobalTextManager.FindText("str_rts_camera_switch_camera_hint").ToString();
            DisplayMessageOutOfMission(hint);
        }

        public static void PrintOrderHint()
        {
            if (RTSCameraConfig.Get().ClickToSelectFormation)
            {
                DisplayLocalizedText("str_rts_camera_click_to_select_formation_hint");
            }

            if (RTSCameraConfig.Get().AttackSpecificFormation)
            {
                DisplayLocalizedText("str_rts_camera_attack_specific_formation_hint");
            }
        }

        private static void DisplayMessageOutOfMission(string text)
        {
            if (Mission.Current == null)
                DisplayMessageImpl(text);
            else
                DisplayMessage(text);
        }

        public static TextObject TextForKey(InputKey key)
        {
           return  Module.CurrentModule.GlobalTextManager.FindText("str_game_key_text",
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
                    formation.IsAIControlled = isAIControlled;
                }
            }
        }

        public static void CancelPlayerAsCommander()
        {
        }

        public static void SetPlayerFormation(FormationClass formationClass)
        {
            var mission = Mission.Current;
            if (mission.MainAgent != null && Mission.Current.PlayerTeam != null &&
                mission.MainAgent.Formation?.FormationIndex != formationClass)
            {
                var formation = mission.PlayerTeam.GetFormation(formationClass);
                if (formation.CountOfUnits == 0)
                {
                    if (formation.IsAIControlled)
                        formation.IsAIControlled = false;
                    // fix crash when begin a battle and assign player to an empty formation, then give it an shield wall order.
                    formation.MovementOrder = MovementOrder.MovementOrderMove(mission.MainAgent.GetWorldPosition());
                }

                if (mission.MainAgent.Formation != null)
                    SetHasPlayer(mission.MainAgent.Formation, false);
                mission.MainAgent.Formation = formation;
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
        }

        public static void AIControlMainAgent(bool changeAlarmed, bool alarmed = false)
        {
            var mission = Mission.Current;
            if (mission?.MainAgent == null)
                return;
            try
            {
                mission.GetMissionBehaviour<MissionMainAgentController>()?.InteractionComponent.ClearFocus();
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

                    if (mission.MainAgent.Formation != null)
                    {
                        OnUnitJoinOrLeave?.Invoke(mission.MainAgent.Formation.MovementOrder, new object[]
                        {
                            mission.MainAgent.Formation, mission.MainAgent, true
                        });
                    }
                }
            }
            catch (Exception e)
            {
                DisplayMessage(e.ToString());
            }

            // avoid crash after victory. After victory, team ai decision won't be made so that current tactics won't be updated.
            if (mission.MissionEnded())
                mission.AllowAiTicking = false;
        }

        public static void SetMainAgentAlarmed(bool alarmed)
        {
            Mission.Current.MainAgent?.SetWatchState(alarmed
                ? AgentAIStateFlagComponent.WatchState.Alarmed
                : AgentAIStateFlagComponent.WatchState.Patroling);
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
            if (missionScreen.IsViewingChar())
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
                var vec3_4 = agentToFollow.MountAgent.GetMovementDirection() * agentToFollow.MountAgent.Monster.RiderBodyCapsuleForwardAdder;
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
                Utility.DisplayMessage(e.ToString());
            }
        }
    }
}
