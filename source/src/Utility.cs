using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    class Utility
    {
        public static void DisplayLocalizedText(string id, string variation = null)
        {
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText(id, variation).ToString()));
        }
        public static void DisplayLocalizedText(string id, string variation, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText(id, variation).ToString(), color));
        }
        public static void DisplayMessage(string msg)
        {
            InformationManager.DisplayMessage(new InformationMessage(new TaleWorlds.Localization.TextObject(msg).ToString()));
        }
        public static void DisplayMessage(string msg, Color color)
        {
            InformationManager.DisplayMessage(new InformationMessage(new TaleWorlds.Localization.TextObject(msg).ToString(), color));
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
                bool isAIControlled = formation.IsAIControlled;
                if (formation.PlayerOwner != null) 
                    formation.PlayerOwner = mission.MainAgent;
                formation.IsAIControlled = isAIControlled;
            }
        }

        public static void CancelPlayerAsCommander()
        {
        }

        public static void SetPlayerFormation(FormationClass formationClass)
        {
            if (Mission.Current.MainAgent != null && Mission.Current.PlayerTeam != null &&
                Mission.Current.MainAgent.Formation?.FormationIndex != formationClass)
            {
                var mission = Mission.Current;
                var controller = mission.MainAgent.Controller;
                mission.MainAgent.Controller = Agent.ControllerType.AI;
                var previousFormation = mission.MainAgent.Formation;
                mission.MainAgent.Formation =
                    mission.PlayerTeam.GetFormation(formationClass);
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
    }
}
