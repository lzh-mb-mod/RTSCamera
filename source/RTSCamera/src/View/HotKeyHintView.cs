using MissionSharedLibrary.HotKey;
using RTSCamera.Config;
using RTSCamera.Config.HotKey;
using RTSCamera.Logic;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace RTSCamera.View
{
    public class HotKeyHintView: MissionView
    {
        private readonly string _movieName;
        private HotKeyHintCollectionVM _dataSource;
        private GauntletLayer GauntletLayer;
        private GauntletMovieIdentifier _movie;
        private RTSCameraLogic _rtsCameraLogic;

        public bool IsActivated { get; set; }

        public HotKeyHintView(int viewOrderPriority)
        {
            ViewOrderPriority = viewOrderPriority;
            _movieName = nameof(HotKeyHintView);
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            _rtsCameraLogic = Mission.GetMissionBehavior<RTSCameraLogic>();
        }


        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            GauntletLayer = null;
            _dataSource?.OnFinalize();
            _dataSource = null;
            _movie = null;
        }

        public void ToggleMenu()
        {
            if (IsActivated)
                Deactivate();
            else
                Activate();
        }

        public void Activate()
        {
            if (IsActivated)
                return;
            IsActivated = true;
            _dataSource = new HotKeyHintCollectionVM(OnClose, GetHintList());
            if (_dataSource == null)
                return;
            GauntletLayer = new GauntletLayer(ViewOrderPriority) { IsFocusLayer = false };
            _movie = GauntletLayer.LoadMovie(_movieName, _dataSource);
            MissionScreen.AddLayer(GauntletLayer);
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (!(_rtsCameraLogic?.SwitchFreeCameraLogic.IsSpectatorCamera ?? false) || Mission.Mode == MissionMode.Deployment)
            {
                if (IsActivated)
                    Deactivate();
                return;
            }

            if (RTSCameraConfig.Get().ShowHotKeyHint != IsActivated)
            {
                if (IsActivated)
                {
                    Deactivate();
                }
                else
                {
                    Activate();
                }
            }
        }

        public void Deactivate()
        {
            if (!IsActivated)
                return;
            _dataSource?.Close();
        }

        private void OnClose()
        {
            IsActivated = false;
            MissionScreen.RemoveLayer(GauntletLayer);
            _dataSource.OnFinalize();
            _dataSource = null;
            if (_movie != null)
            {
                GauntletLayer.ReleaseMovie(_movie);
            }
            _movie = null;
            GauntletLayer = null;
        }

        private List<HotKeyHint> GetHintList()
        {
            return new List<HotKeyHint>
            {
                new HotKeyHint
                {
                    Key = GeneralGameKeyCategory.GetKey(GeneralGameKey.OpenMenu).ToSequenceString(),
                    Description = GameTexts.FindText("str_rts_camera_open_menu_hotkey_hint")
                },
                new HotKeyHint
                {
                    Key = RTSCameraGameKeyCategory.GetKey(GameKeyEnum.FreeCamera).ToSequenceString(),
                    Description = GameTexts.FindText("str_rts_camera_switch_camera_hotkey_hint")
                },
                new HotKeyHint
                {
                    Key = RTSCameraGameKeyCategory.GetKey(GameKeyEnum.ControlTroop).ToSequenceString(),
                    Description = GameTexts.FindText("str_rts_camera_control_troop_hotkey_hint")
                },
                new HotKeyHint
                {
                    Key = RTSCameraGameKeyCategory.GetKey(GameKeyEnum.SelectCharacter).ToSequenceString(),
                    Description = GameTexts.FindText("str_rts_camera_select_character_hotkey_hint")
                },
                new HotKeyHint
                {
                    Key = $"{RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveForward).ToSequenceString()} " +
                        $"{RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveBackward).ToSequenceString()} " +
                        $"{RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveLeft).ToSequenceString()} " +
                        $"{RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveRight).ToSequenceString()} " +
                        $"{RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveUp).ToSequenceString()} " +
                        $"{RTSCameraGameKeyCategory.GetKey(GameKeyEnum.CameraMoveDown).ToSequenceString()} ",
                    Description = GameTexts.FindText("str_rts_camera_move_camera_hotkey_hint")
                }
            };
        }

    }

}