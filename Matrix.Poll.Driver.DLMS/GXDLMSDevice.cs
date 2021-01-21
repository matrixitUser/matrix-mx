using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.ComponentModel;
using System.Xml.Serialization;
using Gurux.Common;
using System.Reflection;
using Gurux.DLMS;
using Gurux.DLMS.Objects;
using Gurux.DLMS.Secure;

namespace Matrix.Poll.Driver.DLMS
{
    [Serializable]
    public class GXDLMSDevice : GXDLMSMeter
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public GXDLMSDevice()
        {
            GXDLMSSecureClient client = new GXDLMSSecureClient();
            Objects = client.Objects;
        }
        
    }
}