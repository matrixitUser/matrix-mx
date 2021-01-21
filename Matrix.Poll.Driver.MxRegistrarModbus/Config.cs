using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.MxRegistrarModbus
{
    public partial class Driver
    {
        private byte[] MakeConfigRequest()
        {
            return MakeBaseRequest(96, new List<byte>() { 0 });
        }

        private dynamic ParseConfigResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            if (answer.Function != 96)
            {
                answer.success = false;
                answer.error = "Неожиданный ответ на запрос";
                answer.errorcode = DeviceError.UNEXPECTED_RESPONSE;
                return answer;
            }

            byte[] body = answer.Body;

            answer.SubFunction = (Request96Type)(0x7f & body[0]);
            answer.Result = body[1];

            if ((answer.Result == 0x00) && (answer.SubFunction == Request96Type.Read))
            {
                answer.Config = body.Skip(2).ToArray();
            }

            return answer;
        }
        
        enum Request96Type
        {
            Read = 0,
            Write,
            Default
        }
    }
}
