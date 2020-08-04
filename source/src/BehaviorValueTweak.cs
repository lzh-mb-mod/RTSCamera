using System;
using System.Globalization;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace RTSCamera
{
    public class BehaviorValues : ViewModel
    {
        private string _value;
        private bool _error;
        public AISimpleBehaviorKind Kind { get; }

        public BehaviorValues(AISimpleBehaviorKind kind, string value)
        {
            Kind = kind;
            Name = kind.ToString();
            Value = value;
        }

        public string Name { get; set; }

        [DataSourceProperty]
        public string Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;
                _value = value;
                Apply();
                OnPropertyChanged(nameof(Value));
            }
        }

        [DataSourceProperty]
        public bool Error
        {
            get => _error;
            set
            {
                if (_error == value)
                    return;
                _error = value;
                OnPropertyChanged(nameof(Error));
            }
        }

        public void Apply()
        {
            try
            {
                var values = Value.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                var floats = values.Select(str => Single.Parse(str, NumberStyles.Float)).ToArray();
                Error = floats.Length != 5;
                if (Error)
                    return;

                foreach (var formation in Mission.Current.PlayerTeam.Formations)
                {
                    formation.ApplyActionOnEachUnit(agent =>
                    {
                        agent.SetAIBehaviorValues(Kind, floats[0], floats[1], floats[2], floats[3], floats[4]);
                    });
                }
                foreach (var formation in Mission.Current.PlayerEnemyTeam.Formations)
                {
                    formation.ApplyActionOnEachUnit(agent =>
                    {
                        agent.SetAIBehaviorValues(Kind, floats[0], floats[1], floats[2], floats[3], floats[4]);
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //Utility.DisplayMessage(e.ToString());
                Error = true;
            }
        }
    }

    public class BehaviorValueTweakVM : MissionMenuVMBase
    {
        private static MBBindingList<BehaviorValues> _allValues = new MBBindingList<BehaviorValues>
        {
            new BehaviorValues(AISimpleBehaviorKind.GoToPos, "3, 7, 5, 20, 6"),
            new BehaviorValues(AISimpleBehaviorKind.Melee, "8, 7, 5, 20, 0.01"),
            new BehaviorValues(AISimpleBehaviorKind.Ranged, "0.02, 7, 0.04, 20, 0.03"),
            new BehaviorValues(AISimpleBehaviorKind.ChargeHorseback, "10, 7, 5, 30, 0.05"),
            new BehaviorValues(AISimpleBehaviorKind.RangedHorseback, "0.02, 15, 0.065, 30, 0.055"),
            new BehaviorValues(AISimpleBehaviorKind.AttackEntityMelee, "5, 12, 7.5, 30, 4"),
            new BehaviorValues(AISimpleBehaviorKind.AttackEntityRanged, "0.55, 12, 0.8, 30, 0.45")
        };


        public BehaviorValueTweakVM(Action onClose)
            : base(onClose)
        {
            foreach (var value in _allValues)
            {
                value.Apply();
            }
        }

        [DataSourceProperty]
        public MBBindingList<BehaviorValues> AllValues
        {
            get => _allValues;
            set
            {
                if (_allValues == value)
                    return;
                _allValues = value;
                OnPropertyChanged(nameof(AllValues));
            }
        }
    }

    public class BehaviorValueTweak : MissionMenuViewBase
    {


        public BehaviorValueTweak()
            : base(24, nameof(BehaviorValueTweak))
        {
            this.GetDataSource = () => new BehaviorValueTweakVM(this.OnCloseMenu);
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();

            Utility.PrintOpenMenuHint();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (IsActivated)
            {
                if (this.GauntletLayer.Input.IsKeyReleased(InputKey.K))
                    DeactivateMenu();
            }
            else if (this.Input.IsKeyReleased(InputKey.K))
                ActivateMenu();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();

            GameKeyConfig.Clear();
            RTSCameraConfig.Clear();
        }
    }
}
