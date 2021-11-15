using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace MissionLibrary.Extension
{
    public interface IMissionExtension
    {
        void OpenExtensionMenu(Mission mission);

        string ExtensionName { get; }
        string ButtonName { get; }

        List<MissionBehaviour> CreateMissionBehaviours(Mission mission);
    }
}
