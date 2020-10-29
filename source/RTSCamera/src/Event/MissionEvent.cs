using System;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.Event
{
    // Legacy. Use MissionLibrary.Event.MissionEvent instead.
    public static class MissionEvent
    {
        public static event Action<Agent> MainAgentWillBeChangedToAnotherOne;

        public static event Action<bool> ToggleFreeCamera;

        public delegate void SwitchTeamDelegate();

        public static event SwitchTeamDelegate PreSwitchTeam;
        public static event SwitchTeamDelegate PostSwitchTeam;

        public static void Clear()
        {
            MainAgentWillBeChangedToAnotherOne = null;
            ToggleFreeCamera = null;
            PreSwitchTeam = null;
            PostSwitchTeam = null;
        }

        public static void OnMainAgentWillBeChangedToAnotherOne(Agent newAgent)
        {
            MainAgentWillBeChangedToAnotherOne?.Invoke(newAgent);
        }

        public static void OnToggleFreeCamera(bool obj)
        {
            ToggleFreeCamera?.Invoke(obj);
        }

        public static void OnPreSwitchTeam()
        {
            PreSwitchTeam?.Invoke();
        }

        public static void OnPostSwitchTeam()
        {
            PostSwitchTeam?.Invoke();
        }
    }
}
