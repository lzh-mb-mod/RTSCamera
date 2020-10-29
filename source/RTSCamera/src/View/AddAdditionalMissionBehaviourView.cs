using MissionLibrary.Extension;
using RTSCamera.Config;
using RTSCamera.Logic;
using RTSCamera.Patch;
using RTSCamera.Patch.CircularFormation;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Missions;

namespace RTSCamera.View
{
    [DefaultView]
    class AddAdditionalMissionBehaviourView : MissionView
    {
        public override void OnCreated()
        {
            base.OnCreated();
            var config = RTSCameraConfig.Get();
            if (config.AttackSpecificFormation)
            {
                PatchChargeToFormation.Patch();
            }

            if (config.FixCircularArrangement)
            {
                PatchCircularFormation.Patch();
            }
            List<MissionBehaviour> list = new List<MissionBehaviour>
            {
                new RTSCameraSelectCharacterView(),

                new RTSCameraLogic(),

                new HideHUDView(),
                new RTSCameraMenuView(),
                new FlyCameraMissionView(),
                new RTSCameraGameKeyConfigView(),
                new FormationColorMissionView(),
                new RTSCameraOrderTroopPlacer()
            };


            foreach (var missionBehaviour in list)
            {
                if (missionBehaviour is AddAdditionalMissionBehaviourView)
                    continue; // avoid accidentally add itself infinitely.
                AddMissionBehaviour(missionBehaviour);
            }

            foreach (var extension in RTSCameraExtension.Extensions)
            {
                foreach (var missionBehaviour in extension.CreateMissionBehaviours(Mission))
                {
                    AddMissionBehaviour(missionBehaviour);
                }
            }

            foreach (var extension in MissionExtensionCollection.Extensions)
            {
                foreach (var missionBehaviour in extension.CreateMissionBehaviours(Mission))
                {
                    AddMissionBehaviour(missionBehaviour);
                }
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            var orderTroopPlacer = Mission.GetMissionBehaviour<OrderTroopPlacer>();
            if (orderTroopPlacer != null)
                Mission.RemoveMissionBehaviour(orderTroopPlacer);
        }

        private void AddMissionBehaviour(MissionBehaviour behaviour)
        {
            behaviour.OnAfterMissionCreated();
            Mission.AddMissionBehaviour(behaviour);
        }
    }
}
