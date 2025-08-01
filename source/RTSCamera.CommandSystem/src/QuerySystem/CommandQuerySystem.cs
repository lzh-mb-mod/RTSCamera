using RTSCamera.CommandSystem.Logic;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.CommandSystem.QuerySystem
{
    public class CommandQuerySystem
    {
        public static Dictionary<Formation, CommandFormationQuerySystem> FormationQuerySystem = new Dictionary<Formation, CommandFormationQuerySystem>();


        public static void OnBehaviorInitialize()
        {
            FormationQuerySystem = new Dictionary<Formation, CommandFormationQuerySystem>();
        }

        public static void OnRemoveBehavior()
        {
            FormationQuerySystem = null;
        }

        public static CommandFormationQuerySystem GetQueryForFormation(Formation formation)
        {
            if (!FormationQuerySystem.TryGetValue(formation, out var query))
            {
                FormationQuerySystem[formation] = query = new CommandFormationQuerySystem(formation);
            }
            return query;
        }
    }
}
