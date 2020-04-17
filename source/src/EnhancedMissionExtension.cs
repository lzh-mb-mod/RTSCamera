using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.MountAndBlade;

namespace EnhancedMission
{
    public abstract class EnhancedMissionExtension
    {
        public static event Action OnExtensionMenuClosed;

        public static void ExtensionMenuClosed()
        {
            OnExtensionMenuClosed?.Invoke();
        }
        private static readonly List<EnhancedMissionExtension> _extensions = new List<EnhancedMissionExtension>();
        public static IEnumerable<EnhancedMissionExtension> Extensions => _extensions;
        public static void AddExtension(EnhancedMissionExtension extension)
        {
            _extensions.Add(extension);
        }

        public static void Clear()
        {
            _extensions.Clear();
        }

        public abstract void OpenModMenu(Mission mission);
        public abstract void CloseModMenu(Mission mission);
        public abstract void OpenExtensionMenu(Mission mission);

        public abstract string ExtensionName { get; }
        public abstract string ButtonName { get; }

        public abstract List<MissionBehaviour> CreateMissionBehaviours(Mission mission);
    }
}
