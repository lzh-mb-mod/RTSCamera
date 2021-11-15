namespace MissionLibrary.Controller.Camera
{
    public interface ICameraController
    {
        float ViewAngle { get; set; }
        bool SmoothRotationMode { get; set; }

        float MovementSpeedFactor { get; set; }
        float VerticalMovementSpeedFactor { get; set; }

        float DepthOfFieldDistance { get; set; }

        float DepthOfFieldStart { get; set; }

        float DepthOfFieldEnd { get; set; }
    }
}
