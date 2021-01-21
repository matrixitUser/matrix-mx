using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Matrix.SurveyServer.Driver.Common.Crc;
using System.Dynamic;

namespace Matrix.SurveyServer.Driver.SET4
{
    public partial class Driver
    {
        //enum CrcType
        //{
        //    KS = 0,
        //    CRC = 1
        //};

        byte[] MakeBaseRequest(List<byte> Data)
        {
            //var crcType = CrcType.CRC;

            var bytes = new List<byte> {};
            if(NetworkAddress > 0xFF)
            {
                bytes.Add((byte)0xFC);
                bytes.AddRange(BitConverter.GetBytes(NetworkAddress).Reverse());
            }
            else
            {
                bytes.Add((byte)NetworkAddress);
            };
            bytes.AddRange(Data);

            //if (crcType == CrcType.CRC)
            {
                Crc crc = Crc.Calc(bytes.ToArray(), new Crc16Modbus());
                bytes.Add(crc.CrcData[0]);
                bytes.Add(crc.CrcData[1]);
            }
            //else if (crcType == CrcType.KS)
            //{
            //    Crc crc = Crc.Calc(bytes.ToArray(), new SetCrc8());
            //    bytes.Add(crc.CrcData[0]);
            //}

            return bytes.ToArray();
        }

        //dynamic ParseBaseResponse(byte[] data)
        //{
        //    dynamic answer = new ExpandoObject();
        //    answer.success = false;
        //    answer.error = "";
            
        //    data = data.SkipWhile(b => b == 0xff).ToArray();
        //    answer.Body = data.Skip(1).Take(data.Count() - 3).ToArray();
        //    if (data.Length < 4)
        //    {
        //        answer.error = "в кадре ответа не может содержаться менее 4 байт";
        //        return answer;
        //    }

        //    if (!Crc.Check(data, new Crc16Modbus()))
        //    {
        //        answer.error = "контрольная сумма кадра не сошлась";
        //        return answer;
        //    }


        //    answer.NetworkAddress = data[0];

        //    //modbus error
        //    if (data.Length == 4)
        //    {
        //        switch (data[1] & 0x0F)
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
        //            case 0x0F:
        //                answer.error = "счётчик не отвечает (коммуникатор)";
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
