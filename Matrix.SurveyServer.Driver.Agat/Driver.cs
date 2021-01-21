using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.SurveyServer.Driver.Common;

namespace Matrix.SurveyServer.Driver.Agat
{
    public class Driver : BaseDriver
    {
        private byte[] ReadResponse()
        {
            isDataReceived = false;
            receivedBuffer = null;
            Wait(4000);

            isDataReceived = false;
            receivedBuffer = null;
            Wait(4000);

            return receivedBuffer;
        }

        private static double Real48ToDouble(byte[] real48)
        {

            if (real48[0] == 0)
                return 0.0; // Null exponent = 0

            double exponent = real48[0] - 129.0;
            double mantissa = 0.0;

            for (int i = 1; i < 5; i++) // loop through bytes 1-4
            {
                mantissa += real48[i];
                mantissa *= 0.00390625; // mantissa /= 256
            }


            mantissa += (real48[5] & 0x7F);
            mantissa *= 0.0078125; // mantissa /= 128
            mantissa += 1.0;

            if ((real48[5] & 0x80) == 0x80) // Sign bit check
                mantissa = -mantissa;

            return mantissa * Math.Pow(2.0, exponent);
        }

        public override SurveyResultData ReadCurrentValues()
        {
            try
            {
                var data = ReadResponse();

                if (data == null && data.Length < 11) return new SurveyResultData { State = Matrix.Common.Agreements.SurveyResultState.NoResponse };

                var value = Real48ToDouble(data.Skip(1).Take(6).ToArray());
                var record = new Data("Расход", Matrix.Common.Agreements.MeasuringUnitType.m3, DateTime.Now, value);

                OnSendMessage(string.Format("прочитан расход {0}", value));

                return new SurveyResultData { State = Matrix.Common.Agreements.SurveyResultState.Success, Records = new Data[] { record } };
            }
            catch (Exception ex)
            {
                OnSendMessage(string.Format("ошибка {0}", ex.Message));
            }
            return new SurveyResultData { State = Matrix.Common.Agreements.SurveyResultState.NoResponse };
        }
    }
}
