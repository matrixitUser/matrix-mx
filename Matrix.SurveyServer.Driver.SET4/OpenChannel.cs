using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        byte[] MakeOpenChannelRequest(string password)
        {
            var Data = new List<byte>();
            Data.Add(0x01);//код запроса на тестирование
            byte[] Password = Encoding.ASCII.GetBytes(password);
            Data.AddRange(Password);
            return MakeBaseRequest(Data);
        }
        
        dynamic ParseOpenChannelResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            //if (data.Length != 4)
            //{
            //    answer.error = "данные не распознаны";
            //    answer.success = false;
            //}
            //else 
            if (answer.Body[0] >= 16)
            {
                answer.errorcode = DeviceError.DEVICE_EXCEPTION;
                answer.error = ">=16";
                answer.success = false;
            }

            return answer;
        }
    }
}
