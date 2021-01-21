using Gurux.DLMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.DLMS
{
    /// <summary>
    /// List of devices.
    /// </summary>
    [Serializable]
    public class GXDLMSDeviceCollection : List<GXDLMSDevice>
    {
    }

    /// <summary>
    /// List of meters.
    /// </summary>
    [Serializable]
    public class GXDLMSMeterCollection : List<GXDLMSMeter>
    {
    }
}
