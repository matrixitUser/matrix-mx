using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        #region Set light
        public void SetLight(byte byteLight)
        {
            List<byte> listByteLight = new List<byte>();
            listByteLight.Add(0);

            listByteLight.Add(byteLight);
            var result = Send(MakeLightRequest((Int32)0x0100, (ushort)listByteLight.Count, listByteLight)); 
        }
            
        #endregion

        
    }
}
