using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RTSCamera.QuerySystem
{
    public class UiQueryData<T> : IQueryData
    {
        private T _cachedValue;
        private float _expireTime;
        private readonly float _lifetime;
        private readonly Func<T> _valueFunc;
        private IEnumerable<IQueryData> _syncGroup;

        public string TelemetryScopeName { get; set; }

        public UiQueryData(Func<T> valueFunc, float lifetime)
        {
            _cachedValue = default;
            _expireTime = 0.0f;
            _lifetime = lifetime;
            _valueFunc = valueFunc;
            _syncGroup = null;
            TelemetryScopeName = "QueryDataNameUninitialized";
        }

        public void Evaluate(float currentTime)
        {
            SetValue(_valueFunc(), currentTime);
        }

        public void SetValue(T value, float currentTime)
        {
            _cachedValue = value;
            _expireTime = currentTime + _lifetime;
        }

        public T GetCachedValue()
        {
            return _cachedValue;
        }

        public T GetCachedValueWithMaxAge(float age)
        {
            if (MBCommon.GetTime(MBCommon.TimeType.Application) <= _expireTime - (double)_lifetime + Math.Min(_lifetime, age))
                return _cachedValue;
            Expire();
            return Value;
        }

        public T Value
        {
            get
            {
                float time = MBCommon.GetTime(MBCommon.TimeType.Application);
                if (time >= (double)_expireTime)
                {
                    if (_syncGroup != null)
                    {
                        foreach (IQueryData queryData in _syncGroup)
                            queryData.Evaluate(time);
                    }
                    Evaluate(time);
                }
                return _cachedValue;
            }
        }

        public void Expire()
        {
            _expireTime = 0.0f;
        }

        public static void SetupSyncGroup(params IQueryData[] groupItems)
        {
            List<IQueryData> queryDataList = new List<IQueryData>(groupItems);
            foreach (IQueryData groupItem in groupItems)
                groupItem.SetSyncGroup(queryDataList);
        }

        public void SetSyncGroup(IEnumerable<IQueryData> syncGroup)
        {
            _syncGroup = syncGroup;
        }
    }
}
