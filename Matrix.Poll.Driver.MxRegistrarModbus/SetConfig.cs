using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        #region Set config

        public void SetConfig(List<byte> byteConfig, UInt32 register)
        {
            log(string.Format("Config  (ushort)byteConfig.Count= {0}", (ushort)byteConfig.Count), level: 1);
            var result = Send(MakeWriteHoldingRegisterRequest(register, (ushort)byteConfig.Count, byteConfig));
            if (!result.success)
            {
                log(string.Format("Config не введён: {0}", result.error), level: 1);
            }
        }
            
        #endregion

        
    }
}
