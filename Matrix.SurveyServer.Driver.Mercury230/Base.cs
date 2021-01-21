using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.Mercury230
{
    public partial class Driver
    {
        public byte[] MakeBaseRequest(byte RequestCode, List<byte> Data)// = null
        {
            var bytes = new List<byte>();
            bytes.Add(NetworkAddress);
            bytes.Add(RequestCode);

            if (Data != null)
            {
                bytes.AddRange(Data);
            }

            var crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
            bytes.Add(crc.CrcData[0]);
            bytes.Add(crc.CrcData[1]);

            return bytes.ToArray();
        }

        //public dynamic ParseBaseResponse(byte[] data)
        //{
        //    dynamic answer = new ExpandoObject();
        //    answer.success = false;
        //    answer.error = string.Empty;

        //    data = data.SkipWhile(b => b == 0xff).ToArray();

        //    if (data.Length < 4)
        //    {
        //        answer.error = "в кадре ответа не может содежаться менее 4 байт";
        //        return answer;
        //    }

        //    if (data[0] != NetworkAddress)
        //    {
        //        answer.error = "Несовпадение сетевого адреса";
        //        return answer;
        //    }

        //    if (!Crc.Check(data, new Crc16Modbus()))
        //    {
        //        answer.error = "контрольная сумма кадра не сошлась";
        //        return answer;
        //    }

        //    answer.Body = data.Skip(1).Take(data.Count() - 3).ToArray();

        //    answer.NetworkAddress = data[0];

        //    //modbus error
        //    if (data.Length == 4)
        //    {
        //        switch (data[1])
        //        {
        //            case 0x00:
        //                answer.success = true;
        //                answer.error = "все нормально";
        //                break;
        //            case 0x01:
        //                answer.error = "недопустимая команда или параметр";
        //                break;
        //            case 0x02:
        //                answer.error = "внутренняя ошибка счетчика";
        //                break;
        //            case 0x03:
        //                answer.error = "не достаточен уровень доступа для удовлетворения запроса";
        //                break;
        //            case 0x04:
        //                answer.error = "внутренние часы счетчика уже корректировались в течении текущих суток";
        //                break;
        //            case 0x05:
        //                answer.error = "не открыт канал связи";
        //                break;
        //            default:
        //                answer.error = "неизвестная ошибка";
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        answer.success = true;
        //    }

        //    return answer;
        //}

    }
}
