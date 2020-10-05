using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            this._cachedValue = default(T);
            this._expireTime = 0.0f;
            this._lifetime = lifetime;
            this._valueFunc = valueFunc;
            this._syncGroup = (IEnumerable<IQueryData>)null;
            this.TelemetryScopeName = "QueryDataNameUninitialized";
        }

        public void Evaluate(float currentTime)
        {
            this.SetValue(this._valueFunc(), currentTime);
        }

        public void SetValue(T value, float currentTime)
        {
            this._cachedValue = value;
            this._expireTime = currentTime + this._lifetime;
        }

        public T GetCachedValue()
        {
            return this._cachedValue;
        }

        public T GetCachedValueWithMaxAge(float age)
        {
            if ((double)MBCommon.GetTime(MBCommon.TimeType.Application) <= (double)this._expireTime - (double)this._lifetime + (double)Math.Min(this._lifetime, age))
                return this._cachedValue;
            this.Expire();
            return this.Value;
        }

        public T Value
        {
            get
            {
                float time = MBCommon.GetTime(MBCommon.TimeType.Application);
                if ((double)time >= (double)this._expireTime)
                {
                    if (this._syncGroup != null)
                    {
                        foreach (IQueryData queryData in this._syncGroup)
                            queryData.Evaluate(time);
                    }
                    this.Evaluate(time);
                }
                return this._cachedValue;
            }
        }

        public void Expire()
        {
            this._expireTime = 0.0f;
        }

        public static void SetupSyncGroup(params IQueryData[] groupItems)
        {
            List<IQueryData> queryDataList = new List<IQueryData>((IEnumerable<IQueryData>)groupItems);
            foreach (IQueryData groupItem in groupItems)
                groupItem.SetSyncGroup((IEnumerable<IQueryData>)queryDataList);
        }

        public void SetSyncGroup(IEnumerable<IQueryData> syncGroup)
        {
            this._syncGroup = syncGroup;
        }
    }
}
