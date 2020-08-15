using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public class Utility
    {
        public static void DisplayLocalizedText(string id, string variation = null)
        {
            if (!RTSCameraConfig.Get().DisplayMessage)
                return;
            try
            {
                DisplayMessageImpl(GameTexts.FindText(id, variation).ToString());
            }
            catch
            {
                // ignored
            }
        }
        public static void DisplayLocalizedText(string id, string variation, Color color)
        {
            if (!RTSCameraConfig.Get().DisplayMessage)
                return;
            try
            {
                DisplayMessageImpl(GameTexts.FindText(id, variation).ToString(), color);
            }
            catch
            {
                // ignored
            }
        }
        public static void DisplayMessage(string msg)
        {
            if (!RTSCameraConfig.Get().DisplayMessage)
                return;
            DisplayMessageImpl(new TaleWorlds.Localization.TextObject(msg).ToString());
        }
        public static void DisplayMessage(string msg, Color color)
        {
            if (!RTSCameraConfig.Get().DisplayMessage)
                return;
            DisplayMessageImpl(new TaleWorlds.Localization.TextObject(msg).ToString(), color);
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
            Mission.Current.MainAgent.SetWatchState(alarmed
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
    }
}
