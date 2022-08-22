using MissionSharedLibrary.View;
using System;
using MissionSharedLibrary.View.ViewModelCollection.Basic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace CinematicCamera
{
    public class CinematicCameraMenuVM : MissionMenuVMBase
    {
        private readonly CinematicCameraConfig _config = CinematicCameraConfig.Get();

        private readonly SetPlayerHealthLogic _setPlayerHealthLogic =
            Mission.Current.GetMissionBehavior<SetPlayerHealthLogic>();

        private NumericVM _verticalFov;
        //private NumericVM _zoom;

        private NumericVM _speedFactor;

        private NumericVM _verticalSpeedFactor;

        private NumericVM _depthOfFieldDistance, _depthOfFieldStart, _depthOfFieldEnd;

        public string PlayerInvulnerableString { get; } = GameTexts.FindText("str_cinematic_camera_player_invulnerable").ToString();
        public string ResetString { get; } = GameTexts.FindText("str_cinematic_camera_reset").ToString();

        public string ZoomString { get; } = GameTexts.FindText("str_cinematic_camera_zoom").ToString();

        public string RotateSmoothModeString { get; } = GameTexts.FindText("str_cinematic_camera_rotate_smooth_mode").ToString();

        public bool PlayerInvulnerable
        {
            get => _config.PlayerInvulnerable;
            set
            {
                if (_config.PlayerInvulnerable == value)
                    return;
                _config.PlayerInvulnerable = value;
                _setPlayerHealthLogic?.UpdateInvulnerable(_config.PlayerInvulnerable);
                OnPropertyChanged(nameof(PlayerInvulnerable));
            }
        }

        public NumericVM VerticalFov
        {
            get => _verticalFov;
            set
            {
                if (_verticalFov == value)
                    return;
                _verticalFov = value;
                OnPropertyChanged(nameof(VerticalFov));
            }
        }

        public void ResetFov()
        {
            VerticalFov.OptionValue = 65.0f;
        }

        //public NumericVM Zoom
        //{
        //    get => _zoom;
        //    set
        //    {
        //        if (_zoom == value)
        //            return;
        //        _zoom = value;
        //        OnPropertyChanged(nameof(Zoom));
        //    }
        //}

        //public void ResetZoom()
        //{
        //    Zoom.OptionValue = 1;
        //}

        public bool RotateSmoothMode
        {
            get => _config.RotateSmoothMode;
            set
            {
                if (_config.RotateSmoothMode == value)
                    return;
                _config.RotateSmoothMode = value;
                ModifyCameraHelper.UpdateRotateSmoothMode();
                OnPropertyChanged(nameof(RotateSmoothMode));
            }
        }

        public NumericVM SpeedFactor
        {
            get => _speedFactor;
            set
            {
                if (_speedFactor == value)
                    return;
                _speedFactor = value;
                OnPropertyChanged(nameof(SpeedFactor));
            }
        }

        public void ResetSpeedFactor()
        {
            SpeedFactor.OptionValue = 1.0f;
        }

        public NumericVM VerticalSpeedFactor
        {
            get => _verticalSpeedFactor;
            set
            {
                if (_verticalSpeedFactor == value)
                    return;
                _verticalSpeedFactor = value;
                OnPropertyChanged(nameof(VerticalSpeedFactor));
            }
        }

        public void ResetVerticalSpeedFactor()
        {
            VerticalSpeedFactor.OptionValue = 1.0f;
        }

        public NumericVM DepthOfFieldDistance
        {
            get => _depthOfFieldDistance;
            set
            {
                if (_depthOfFieldDistance == value)
                    return;
                _depthOfFieldDistance = value;
                OnPropertyChanged(nameof(DepthOfFieldDistance));
            }
        }

        public NumericVM DepthOfFieldStart
        {
            get => _depthOfFieldStart;
            set
            {
                if (_depthOfFieldStart == value)
                    return;
                _depthOfFieldStart = value;
                OnPropertyChanged(nameof(DepthOfFieldStart));
            }
        }

        public NumericVM DepthOfFieldEnd
        {
            get => _depthOfFieldEnd;
            set
            {
                if (_depthOfFieldEnd == value)
                    return;
                _depthOfFieldEnd = value;
                OnPropertyChanged(nameof(DepthOfFieldEnd));
            }
        }

        public CinematicCameraMenuVM(Action closeMenu) : base(closeMenu)
        {
            VerticalFov = new NumericVM(GameTexts.FindText("str_cinematic_camera_vertical_fov").ToString(), _config.CameraFov, 1, 179, true,
                fov =>
                {
                    _config.CameraFov = fov;
                    ModifyCameraHelper.UpdateFov();
                });
            //Zoom = new NumericVM(GameTexts.FindText("str_cinematic_camera_zoom").ToString(), _config.Zoom, 0.01f, 10, false,
            //    zoom =>
            //    {
            //        _config.Zoom = zoom;
            //        ModifyCameraHelper.UpdateZoom();
            //    });
            SpeedFactor = new NumericVM(GameTexts.FindText("str_cinematic_camera_speed_factor").ToString(), _config.SpeedFactor, 0.01f,9.99f, false,
                factor =>
                {
                    _config.SpeedFactor = factor;
                    ModifyCameraHelper.UpdateSpeed();
                });
            VerticalSpeedFactor = new NumericVM(GameTexts.FindText("str_cinematic_camera_vertical_speed_factor").ToString(), _config.VerticalSpeedFactor, 0.01f, 9.99f, false,
                factor =>
                {
                    _config.VerticalSpeedFactor = factor;
                    ModifyCameraHelper.UpdateSpeed();
                });

            var scene = Mission.Current.Scene;
            DepthOfFieldDistance = new NumericVM(GameTexts.FindText("str_cinematic_camera_depth_of_field_distance").ToString(), _config.DepthOfFieldDistance, 0, 100f, false,
                v =>
                {
                    _config.DepthOfFieldDistance = v;
                    ModifyCameraHelper.UpdateDepthOfFieldDistance();
                    ModifyCameraHelper.UpdateDepthOfFieldParameters();
                });
            DepthOfFieldStart = new NumericVM(GameTexts.FindText("str_cinematic_camera_depth_of_field_start").ToString(), _config.DepthOfFieldStart, 0, 100f, false,
                v =>
                {
                    _config.DepthOfFieldStart = v;
                    ModifyCameraHelper.UpdateDepthOfFieldParameters();
                });
            DepthOfFieldEnd = new NumericVM(GameTexts.FindText("str_cinematic_camera_depth_of_field_End").ToString(), _config.DepthOfFieldEnd, 0, 1000f, false,
                v =>
                {
                    _config.DepthOfFieldEnd = v;
                    ModifyCameraHelper.UpdateDepthOfFieldParameters();
                });
        }

        public override void CloseMenu()
        {
            _config.Serialize();

            base.CloseMenu();
        }
    }
}
