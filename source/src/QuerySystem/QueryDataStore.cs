using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class QueryDataStore
    {
        public static QueryDataStore Instance { get; private set; }

        public List<TeamQuery> Teams = new List<TeamQuery>();

        public static void EnsureInitialized()
        {
            if (Instance == null)
                Instance = new QueryDataStore();
        }

        public static FormationQuery Get(Formation formation)
        {
            return Instance.Teams[formation.Team.TeamIndex].Formations[(int) formation.FormationIndex];
        }

        public static void AddTeam(Team team)
        {
            EnsureInitialized();
            Instance.Teams.Add(new TeamQuery(team));
        }

        public static void Clear()
        {
            Instance = null;
        }
    }
}
