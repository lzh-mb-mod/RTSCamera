using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public abstract class RTSCameraExtension
    {
        public static event Action OnExtensionMenuClosed;

        public static void ExtensionMenuClosed()
        {
            OnExtensionMenuClosed?.Invoke();
        }
        private static readonly List<RTSCameraExtension> _extensions = new List<RTSCameraExtension>();
        public static IEnumerable<RTSCameraExtension> Extensions => _extensions;
        public static void AddExtension(RTSCameraExtension extension)
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
