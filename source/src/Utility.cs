using RTSCamera.Config;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;
using ManagedParameters = TaleWorlds.Core.ManagedParameters;
using ManagedParametersEnum = TaleWorlds.Core.ManagedParametersEnum;
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
                DisplayMessageImpl(new TaleWorlds.Localization.TextObject(msg).ToString());
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
                DisplayMessageImpl(new TaleWorlds.Localization.TextObject(msg).ToString(), color);
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
            DisplayMessageOutOfMission((hint));
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

                mission.MainAgent.Formation = formation;
            }
        }

        public static bool IsInPlayerParty(Agent agent)
        {
            if (Campaign.Current != null)
            {

                var mainPartyName = Campaign.Current.MainParty.Name;
                if (agent.Origin is SimpleAgentOrigin simpleAgentOrigin && Equals(simpleAgentOrigin.Party?.Name, mainPartyName) ||
                    agent.Origin is PartyAgentOrigin partyAgentOrigin && Equals(partyAgentOrigin.Party?.Name, mainPartyName) ||
                    agent.Origin is PartyGroupAgentOrigin partyGroupAgentOrigin && Equals(partyGroupAgentOrigin.Party?.Name, mainPartyName))
                    return true;
            }
            else
            {
                return agent.Team == Mission.Current.PlayerTeam;
            }
            return false;
        }

        public static void AIControlMainAgent(bool alarmed)
        {
            var mission = Mission.Current;
            if (mission?.MainAgent == null)
                return;
            try
            {
                if (mission.MainAgent.Controller == Agent.ControllerType.Player)
                {
                    mission.MainAgent.Controller = Agent.ControllerType.AI;
                    if (alarmed)
                    {
                        if ((mission.MainAgent.AIStateFlags & Agent.AIStateFlag.Alarmed) == Agent.AIStateFlag.None)
                            SetMainAgentAlarmed(true);
                    }
                    else
                    {
                        SetMainAgentAlarmed(false);
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
                Utility.DisplayMessage(e.ToString());
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

        public static void AfterSetMainAgent(bool shouldSmoothMoveToAgent, MissionScreen missionScreen)
        {
            if (shouldSmoothMoveToAgent)
            {
                ShouldSmoothMoveToAgent = true;
                SmoothMoveToAgent(missionScreen);
            }
        }
        public static void SmoothMoveToAgent(MissionScreen missionScreen, bool forceMove = false)
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
                            GetCameraPositionWhenLockedToAgent(missionScreen, spectatingData.AgentToFollow);
                        CameraSpecialCurrentPositionToAdd?.SetValue(missionScreen,
                            missionScreen.CombatCamera.Position - targetFrame.origin +
                            (Vec3)CameraSpecialCurrentPositionToAdd.GetValue(missionScreen));
                        CameraSpecialCurrentAddedElevation?.SetValue(missionScreen,
                            missionScreen.CameraElevation +
                            (float)CameraSpecialCurrentAddedElevation.GetValue(missionScreen));
                        CameraSpecialCurrentAddedBearing?.SetValue(missionScreen,
                            MBMath.WrapAngle(missionScreen.CameraBearing - spectatingData.AgentToFollow.LookDirectionAsAngle +
                                             (float)CameraSpecialCurrentAddedBearing.GetValue(missionScreen)));
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

        public static MatrixFrame GetCameraPositionWhenLockedToAgent(MissionScreen missionScreen, Agent agentToFollow)
        {
            MatrixFrame cameraFrame1 = MatrixFrame.Identity;
            Vec3 vec3_1;
            float num1 = 0.6f;
            float num2 = 0.0f;
            bool flag3 = agentToFollow.MountAgent != null;
            float num4 = agentToFollow.AgentScale;
            bool flag2 = false;
            float num5;
            float num6;

            if (flag3)
            {
                num1 += 0.1f;
                num6 = (float) (((double) agentToFollow.MountAgent.Monster.RiderCameraHeightAdder +
                                 (double) agentToFollow.MountAgent.Monster.BodyCapsulePoint1.z +
                                 (double) agentToFollow.MountAgent.Monster.BodyCapsuleRadius) *
                                (double) agentToFollow.MountAgent.AgentScale +
                                (double) agentToFollow.Monster.CrouchEyeHeight * (double) num4);
            }
            else
                num6 = agentToFollow.AgentVisuals.GetSkeleton().GetCurrentRagdollState() != RagdollState.Active
                    ? ((agentToFollow.GetCurrentAnimationFlag(0) & AnimFlags.anf_reset_camera_height) == (AnimFlags) 0
                        ? (agentToFollow.CrouchMode ||
                           agentToFollow.GetCurrentActionType(0) == Agent.ActionCodeType.Sit ||
                           agentToFollow.GetCurrentActionType(0) == Agent.ActionCodeType.SitOnTheFloor
                            ? (agentToFollow.Monster.CrouchEyeHeight + 0.2f) * num4
                            : (agentToFollow.Monster.StandingEyeHeight + 0.2f) * num4)
                        : 0.5f)
                    : 0.5f;
            if (missionScreen.IsViewingChar())
            {
                num6 *= 0.5f;
                num1 += 0.5f;
            }
            num5 = num1;
            cameraFrame1.rotation.RotateAboutSide(1.570796f);
            cameraFrame1.rotation.RotateAboutForward(missionScreen.CameraBearing);
            cameraFrame1.rotation.RotateAboutSide(missionScreen.CameraElevation);
            MatrixFrame matrixFrame = cameraFrame1;
            float num8 = Math.Max(num5 + Mission.CameraAddedDistance, 0.48f) * num4;
            if (agentToFollow.IsActive() && BannerlordConfig.EnableVerticalAimCorrection)
            {
                WeaponComponentData currentUsageItem = agentToFollow.WieldedWeapon.CurrentUsageItem;
                if (currentUsageItem != null && currentUsageItem.IsRangedWeapon)
                {
                    MatrixFrame frame = missionScreen.CombatCamera.Frame;
                    float num7 = !flag3 ? agentToFollow.Monster.StandingEyeHeight * num4 : (float)(((double)agentToFollow.MountAgent.Monster.RiderCameraHeightAdder + (double)agentToFollow.MountAgent.Monster.BodyCapsulePoint1.z + (double)agentToFollow.MountAgent.Monster.BodyCapsuleRadius) * (double)agentToFollow.MountAgent.AgentScale + (double)agentToFollow.Monster.CrouchEyeHeight * (double)num4);
                    if (currentUsageItem.WeaponFlags.HasAnyFlag<WeaponFlags>(WeaponFlags.UseHandAsThrowBase))
                        num7 *= 1.25f;
                    float num9;
                    double z = (double)frame.origin.z;
                    double num10 = -(double)frame.rotation.u.z;
                    Vec2 asVec2_1 = frame.origin.AsVec2;
                    vec3_1 = agentToFollow.Position;
                    Vec2 asVec2_2 = vec3_1.AsVec2;
                    double length = (double)(asVec2_1 - asVec2_2).Length;
                    double num11 = num10 * length;
                    num9 = (float)(z + num11 - ((double)agentToFollow.Position.z + (double)num7));
                    num2 = (double)num9 <= 0.0 ? 0.0f : Math.Max(-0.15f, -(float)Math.Asin(Math.Min(1.0, Math.Sqrt(19.6000003814697 * (double)num9) / (double)currentUsageItem.MissileSpeed)));
                }
                else
                    num2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.MeleeAddedElevationForCrosshair);
            }

            cameraFrame1.rotation.RotateAboutSide((float?)CameraAddedElevation?.GetValue(missionScreen) ?? 0);
            bool flag4 = missionScreen.IsViewingChar() && !GameNetwork.IsSessionActive;
            bool flag5 = agentToFollow.AgentVisuals != null && (uint)agentToFollow.AgentVisuals.GetSkeleton().GetCurrentRagdollState() > 0;
            bool flag6 = agentToFollow.IsActive() && agentToFollow.GetCurrentActionType(0) == Agent.ActionCodeType.Mount;
            Vec3 vec3_4 = Vec3.Zero;
            var vec3_5 = agentToFollow.VisualPosition;
            var vec3_6 = flag5 ? agentToFollow.AgentVisuals.GetFrame().origin : vec3_5;
            if (agentToFollow.MountAgent != null)
            {
                vec3_4 = agentToFollow.MountAgent.GetMovementDirection() * agentToFollow.MountAgent.Monster.RiderBodyCapsuleForwardAdder;
                vec3_6 += vec3_4;
            }
            Vec3 vec3_7 = vec3_5;
            Vec3 vec3_8 = vec3_6;
            vec3_8.z += (float)CameraTargetAddedHeight.GetValue(missionScreen);
            int num12 = 0;
            bool flag7;
            float num13 = num8 - Mission.CameraAddedDistance;
            var cameraTarget = vec3_8;
            cameraTarget += matrixFrame.rotation.f * num4 * (0.7f * MathF.Pow(MathF.Cos((float)(1.0 / (((double)num8 / (double)num4 - 0.200000002980232) * 30.0 + 20.0))), 3500f));
            cameraFrame1.origin = cameraTarget + matrixFrame.rotation.u * missionScreen.CameraResultDistanceToTarget;
            return cameraFrame1;
        }

        public static void SetIsPlayerAgentAdded(MissionScreen missionScreen, bool value)
        {
            IsPlayerAgentAdded?.SetValue(missionScreen, value);
            if (value)
                CameraSpecialCurrentPositionToAdd?.SetValue(missionScreen, Vec3.Zero);
        }
    }
}
