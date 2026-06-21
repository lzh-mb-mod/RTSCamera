using HarmonyLib;
using MissionSharedLibrary.Utilities;
using System;
using System.Collections;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Patch.TOR_fix
{
    public class Fix_WizardAIComponent
    {
        private static Type abilityManagerMissionLogicType;
        private static MethodInfo isCastingMissionMethod;
        private static MethodInfo isAbilityUserMethod;
        private static Type abilityComponentType;
        private static PropertyInfo knownAbilitySystemProperty;
        private static Type wizardAIComponentType;

        public static void OnAgentControllerChanged(
            Agent agent,
            AgentControllerType oldController)
        {
            // From AbilityManagerMissionLogic.OnAgentCreated
            //if (!this.IsCastingMission() || !agent.IsAbilityUser())
            //    return;
            //AbilityComponent abilityComponent = new AbilityComponent(agent);
            //agent.AddComponent((AgentComponent)abilityComponent);
            //if (agent.IsAIControlled && abilityComponent.KnownAbilitySystem.Count > 0)
            //    agent.AddComponent((AgentComponent)new WizardAIComponent(agent));
            abilityManagerMissionLogicType ??= AccessTools.TypeByName("AbilityManagerMissionLogic");
            isCastingMissionMethod ??= AccessTools.Method(abilityManagerMissionLogicType, "IsCastingMission");
            isAbilityUserMethod ??= AccessTools.Method("TOR_Core.Extensions.AgentExtensions:IsAbilityUser");
            var abilityManagerMissionLogic = GetAbilityManagerMissionLogic(Mission.Current);

            var isCastingMission = (bool)isCastingMissionMethod.Invoke(abilityManagerMissionLogic, null);
            var isAbilityUser = (bool)isAbilityUserMethod.Invoke(null, new object[] { agent });
            if (!isCastingMission || !isAbilityUser)
                return;

            abilityComponentType ??= AccessTools.TypeByName("AbilityComponent");
            var abilityComponent = Utilities.Utility.GetAgentComponent(agent, abilityComponentType);
            knownAbilitySystemProperty = AccessTools.Property(abilityComponentType, "KnownAbilitySystem");
            var knownAbilitySystem = (IList)knownAbilitySystemProperty.GetValue(abilityComponent);

            if (agent.IsAIControlled && knownAbilitySystem.Count > 0)
            {
                wizardAIComponentType ??= AccessTools.TypeByName("WizardAIComponent");
                var newWidzardAIComponent = (AgentComponent)Activator.CreateInstance(wizardAIComponentType, new object[] { agent });
                agent.AddComponent(newWidzardAIComponent);
            }
        }

        public static MissionBehavior GetAbilityManagerMissionLogic(Mission mission)
        {
            return Utility.GetMissionBehaviorOfType(mission, abilityManagerMissionLogicType);
        }
    }
}
