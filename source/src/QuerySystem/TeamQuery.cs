using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class TeamQuery
    {
        public FormationQuery[] Formations;

        public TeamQuery(Team team)
        {
            Formations = new FormationQuery[(int)FormationClass.NumberOfAllFormations];
            for (FormationClass formationClass = 0;
                formationClass < FormationClass.NumberOfAllFormations;
                ++formationClass)
            {
                Formations[(int)formationClass] = new FormationQuery(team.FormationsIncludingSpecialAndEmpty[(int)formationClass]);
            }
        }
    }
}
