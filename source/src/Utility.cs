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
        public static void DisplayMessage(string msg)
        {
            InformationManager.DisplayMessage(new InformationMessage(new TaleWorlds.Localization.TextObject(msg).ToString()));
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

    }
}
