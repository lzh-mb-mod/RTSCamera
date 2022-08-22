using MissionLibrary;
using MissionLibrary.Controller.Camera;
using TaleWorlds.MountAndBlade;

namespace CinematicCamera
{
    public class ModifyCameraHelper
    {
        private static readonly CinematicCameraConfig Config = CinematicCameraConfig.Get();

        public static void OnBehaviorInitialize()
        {
            if (ACameraControllerManager.Get().Instance != null)
            {
                ACameraControllerManager.Get().Instance.ViewAngle = Config.CameraFov;
                ACameraControllerManager.Get().Instance.SmoothRotationMode = Config.RotateSmoothMode;
                UpdateDepthOfFieldParameters();
                UpdateDepthOfFieldDistance();
            }
        }

        public static void UpdateFov()
        {
            if (ACameraControllerManager.Get().Instance == null)
                return;
            ACameraControllerManager.Get().Instance.ViewAngle = Config.CameraFov;
        }

        public static void UpdateRotateSmoothMode()
        {
            if (ACameraControllerManager.Get().Instance == null)
                return;
            ACameraControllerManager.Get().Instance.SmoothRotationMode = Config.RotateSmoothMode;
        }

        public static void UpdateSpeed()
        {
            if (ACameraControllerManager.Get().Instance == null)
                return;
            ACameraControllerManager.Get().Instance.MovementSpeedFactor = Config.SpeedFactor;
            ACameraControllerManager.Get().Instance.VerticalMovementSpeedFactor = Config.VerticalSpeedFactor;
        }

        public static void UpdateDepthOfFieldDistance()
        {
            if (ACameraControllerManager.Get().Instance == null)
                return;
            ACameraControllerManager.Get().Instance.DepthOfFieldDistance = Config.DepthOfFieldDistance;
        }

        public static void UpdateDepthOfFieldParameters()
        {
            if (ACameraControllerManager.Get().Instance == null)
                return;
            ACameraControllerManager.Get().Instance.DepthOfFieldStart = Config.DepthOfFieldStart;
            ACameraControllerManager.Get().Instance.DepthOfFieldEnd = Config.DepthOfFieldEnd;
        }

        //public void UpdateZoom()
        //{
        //    if (CameraController.Instance == null)
        //        return;
        //    CameraController.Instance.Zoom = _config.Zoom;
        //}
    }
}
