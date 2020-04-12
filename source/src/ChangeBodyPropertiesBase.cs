using System;
using System.Collections.Generic;
using System.Text;

namespace EnhancedMission
{
    public abstract class ChangeBodyPropertiesBase
    {
        private static ChangeBodyPropertiesBase _current;

        public static void SetInstance(ChangeBodyPropertiesBase instance)
        {
            _current = instance;
        }
        public static ChangeBodyPropertiesBase Get()
        {
            return _current;
        }
        public abstract bool UseRealisticBlocking { get; set; }
        public abstract bool ChangeMeleeAI { get; set; }

        public abstract int MeleeAI { get; set; }
        public abstract bool ChangeRangedAI { get; set; }

        public abstract int RangedAI { get; set; }

        public abstract void SaveConfig();
    }
}
