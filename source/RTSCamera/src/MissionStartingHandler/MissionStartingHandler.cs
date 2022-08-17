using MissionLibrary.Controller;
using MissionLibrary.Extension;
using MissionSharedLibrary.Controller;
using RTSCamera.Logic;
using RTSCamera.View;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.MissionStartingHandler
{
    public class MissionStartingHandler : AMissionStartingHandler
    {
        public override void OnCreated(MissionView entranceView)
        {

            List<MissionBehavior> list = new List<MissionBehavior>
            {
                new RTSCameraSelectCharacterView(),

                new RTSCameraLogic(),

                new HideHUDView(),
                new FlyCameraMissionView()
            };


            foreach (var missionBehaviour in list)
            {
                MissionStartingManager.AddMissionBehaviour(entranceView, missionBehaviour);
            }
        }

        public override void OnPreMissionTick(MissionView entranceView, float dt)
        {
            var spectatorControl = entranceView.Mission.GetMissionBehavior<MissionGauntletSpectatorControl>();
            if (spectatorControl != null)
            {
                entranceView.Mission.RemoveMissionBehavior(spectatorControl);
            }
            
        }
    }
}
