using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.Engine.Options;

namespace RTSCamera
{
    public class SelectionOptionData
    {
        private Action<int> _setValue;
        private readonly Func<int> _getValue;
        private int _value;
        private readonly int _limit;
        private readonly IEnumerable<SelectionItem> _data;

        public SelectionOptionData(Action<int> setValue, Func<int> getValue, int limit, IEnumerable<SelectionItem> data)
        {
            _setValue = setValue;
            _getValue = getValue;
            _value = getValue();
            _limit = limit;
            _data = data;
        }
        public float GetDefaultValue()
        {
            return _getValue();
        }

        public void Commit()
        {
            _setValue(_value);
        }

        public float GetValue()
        {
            return _value;
        }

        public void SetValue(float value)
        {
            this._value = (int)value;
        }

        public object GetOptionType()
        {
            return null;
        }

        public bool IsNative()
        {
            return false;
        }

        public int GetSelectableOptionsLimit()
        {
            return _limit;
        }

        public IEnumerable<SelectionItem> GetSelectableOptionNames()
        {
            return _data;
        }
    }
}
