using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public class Utility
    {
        public static void DisplayLocalizedText(string id, string variation = null)
        {
            if (!RTSCameraConfig.Get().DisplayMessage)
                return;
            DisplayMessageImpl(GameTexts.FindText(id, variation).ToString());
        }
        public static void DisplayLocalizedText(string id, string variation, Color color)
        {
            if (!RTSCameraConfig.Get().DisplayMessage)
                return;
            DisplayMessageImpl(GameTexts.FindText(id, variation).ToString(), color);
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
            if (Mission.Current.MainAgent != null && Mission.Current.PlayerTeam != null &&
                !Mission.Current.PlayerTeam.IsPlayerSergeant &&
                Mission.Current.MainAgent.Formation?.FormationIndex != formationClass)
            {
                var mission = Mission.Current;
                var controller = mission.MainAgent.Controller;
                // to add player to unit card in order UI, the controller need to be set to AI.
                mission.MainAgent.Controller = Agent.ControllerType.AI;
                var previousFormation = mission.MainAgent.Formation;
                mission.MainAgent.Formation =
                    mission.PlayerTeam.GetFormation(formationClass);
                if (controller != Agent.ControllerType.AI)
                    mission.MainAgent.Controller = controller;
                //if (previousFormation != null)
                //{
                //    mission.PlayerTeam.MasterOrderController.ClearSelectedFormations();
                //    mission.PlayerTeam.MasterOrderController.SelectFormation(previousFormation);
                //    mission.PlayerTeam.MasterOrderController.SetOrderWithFormationAndNumber(OrderType.Transfer,
                //        mission.MainAgent.Formation, 0);
                //}
                //mission.PlayerTeam.ExpireAIQuerySystem();
            }
        }

        public static bool IsInPlayerParty(Agent agent)
        {
            if (Campaign.Current != null)
            {

                var mainPartyName = Campaign.Current.MainParty.Name;
                if (agent.Origin is SimpleAgentOrigin simpleAgentOrigin && Equals(simpleAgentOrigin.Party.Name, mainPartyName) ||
                    agent.Origin is PartyAgentOrigin partyAgentOrigin && Equals(partyAgentOrigin.Party.Name, mainPartyName) ||
                    agent.Origin is PartyGroupAgentOrigin partyGroupAgentOrigin && Equals(partyGroupAgentOrigin.Party.Name, mainPartyName))
                    return true;
            }
            else
            {
                return agent.Team == Mission.Current.PlayerTeam;
            }
            return false;
        }

        public static void AIControlMainAgent(FormationClass playerFormation)
        {
            var mission = Mission.Current;
            mission.MainAgent.Controller = Agent.ControllerType.AI;
            mission.MainAgent.SetWatchState(AgentAIStateFlagComponent.WatchState.Alarmed);
            if (mission.MainAgent.Formation == null || mission.MainAgent.Formation.FormationIndex >=
                FormationClass.NumberOfRegularFormations)
            {
                Utility.SetPlayerFormation(playerFormation);
            }
            // avoid crash after victory. After victory, team ai decision won't be made so that current tactics won't be updated.
            if (mission.MissionEnded())
                mission.AllowAiTicking = false;
        }
    }
}
