using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTSCamera
{
    public static class RTSCameraConfigExtension
    {
        public static bool ShouldHighlightWithOutline(this RTSCameraConfig config)
        {
            return config.ClickToSelectFormation || config.AttackSpecificFormation;
        }
    }
}
