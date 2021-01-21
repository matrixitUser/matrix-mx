using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="na"></param>
        /// <param name="password"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private dynamic GetBOD53(byte na, short password, DateTime date)
        {
            var answer = ParseModbusResponse(SendWithCrc(Make16Request(na, RVS, new short[] { 0x0038 })));
            if (!answer.success) return answer;

           // return ParseBODFlash7Response(SendWithCrc(Make3Request(na, 0x0000, 0x0081 / 2)), date);
            //ri4 показал что именно такой начальный адрес стоит использовать
            return ParseBODFlash7Response(SendWithCrc(Make3Request(na, 0x0000, 0x0032)), date);
        }
    }
}
