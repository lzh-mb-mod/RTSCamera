using System;
using TaleWorlds.Library;
using MathF = TaleWorlds.Library.MathF;

namespace RTSCamera
{
    public class NumericVM : ViewModel
    {

        private readonly float _initialValue;
        private float _min;
        private float _max;
        private float _optionValue;
        private bool _isDiscrete;
        private Action<float> _updateAction;
        private int _roundScale;
        private bool _isVisible;

        public NumericVM(string name, float initialValue, float min, float max, bool isDiscrete, Action<float> updateAction, int roundScale = 100, bool isVisible = true)
        {
            Name = name;
            _initialValue = initialValue;
            _min = min;
            _max = max;
            _optionValue = initialValue;
            _isDiscrete = isDiscrete;
            _updateAction = updateAction;
            _roundScale = roundScale;
            _isVisible = isVisible;
        }
        public string Name { get; }

        [DataSourceProperty]
        public float Min
        {
            get => this._min;
            set
            {
                if (Math.Abs(value - this._min) < 0.01f)
                    return;
                this._min = value;
                this.OnPropertyChanged(nameof(Min));
            }
        }

        [DataSourceProperty]
        public float Max
        {
            get => this._max;
            set
            {
                if (Math.Abs(value - this._max) < 0.01f)
                    return;
                this._max = value;
                this.OnPropertyChanged(nameof(Max));
            }
        }

        [DataSourceProperty]
        public float OptionValue
        {
            get => this._optionValue;
            set
            {
                if (Math.Abs((double)value - (double)this._optionValue) < 0.01f)
                    return;
                this._optionValue = MathF.Round(value * _roundScale) / (float)_roundScale;
                this.OnPropertyChanged(nameof(OptionValue));
                this.OnPropertyChanged(nameof(OptionValueAsString));
                this._updateAction(OptionValue);
            }
        }

        [DataSourceProperty]
        public bool IsDiscrete
        {
            get => this._isDiscrete;
            set
            {
                if (value == this._isDiscrete)
                    return;
                this._isDiscrete = value;
                this.OnPropertyChanged(nameof(IsDiscrete));
            }
        }

        [DataSourceProperty]
        public string OptionValueAsString => !this.IsDiscrete ? this._optionValue.ToString("F") : ((int)this._optionValue).ToString();



        [DataSourceProperty]
        public bool IsVisible
        {
            get => this._isVisible;
            set
            {
                if (value == this._isVisible)
                    return;
                this._isVisible = value;
                this.OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
}
