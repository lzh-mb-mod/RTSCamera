using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screen;
using Module = TaleWorlds.MountAndBlade.Module;

namespace RTSCamera
{
    public class Utility
    {
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

        public static void SetPlayerAsCommander()
        {
            var mission = Mission.Current;
            if (mission?.PlayerTeam == null)
                return;
            mission.PlayerTeam.PlayerOrderController.Owner = mission.MainAgent;
            foreach (var formation in mission.PlayerTeam.FormationsIncludingEmpty)
            {
                if (formation.PlayerOwner != null)
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
            Utility.DisplayMessage("SmoothMoveToAgent");
            var spectatingData = missionScreen.GetSpectatingData(missionScreen.CombatCamera.Position);
            if (spectatingData.AgentToFollow != null)
            {
                CameraAddSpecialMovement?.SetValue(missionScreen, true);
                CameraApplySpecialMovementsInstantly?.SetValue(missionScreen, false);
                if (missionScreen.LastFollowedAgent != spectatingData.AgentToFollow || forceMove)
                {
                    CameraSpecialCurrentPositionToAdd?.SetValue(missionScreen,
                        missionScreen.CombatCamera.Position - spectatingData.AgentToFollow.VisualPosition +
                        (Vec3)CameraSpecialCurrentPositionToAdd.GetValue(missionScreen));
                }
                SetLastFollowedAgent.Invoke(missionScreen, new object[] { spectatingData.AgentToFollow });
                CameraSpecialCurrentAddedElevation?.SetValue(missionScreen,
                    missionScreen.CameraElevation + (float) CameraSpecialCurrentAddedElevation.GetValue(missionScreen));
                CameraSpecialCurrentAddedBearing?.SetValue(missionScreen,
                    MBMath.WrapAngle(missionScreen.CameraBearing - spectatingData.AgentToFollow.LookDirectionAsAngle +
                                     (float) CameraSpecialCurrentAddedBearing.GetValue(missionScreen)));
                SetCameraElevation?.Invoke(missionScreen, new object[]{0.0f});
                SetCameraBearing?.Invoke(missionScreen,
                    new object[] {spectatingData.AgentToFollow.LookDirectionAsAngle});
            }
            // Avoid MissionScreen._cameraSpecialCurrentAddedBearing reset to 0.
            IsPlayerAgentAdded?.SetValue(missionScreen, false);
        }
    }
}
