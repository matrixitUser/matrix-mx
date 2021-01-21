using Matrix.SurveyServer.Driver.Common.Crc;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Matrix.SurveyServer.Driver.TSRV043
{
    //public partial class Driver
    //{
    //    #region Send

    //    private byte[] SendSimple(byte[] data, int timeout = 7500, int wait = 6)
    //    {
    //        var buffer = new List<byte>();

    //        //log(string.Format("Попытка {0}", attempts + 1));
    //        //log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))));

    //        response();
    //        request(data);

    //        var to = timeout;
    //        var sleep = 250;
    //        var isCollecting = false;
    //        var waitCollected = 0;
    //        var isCollected = false;
    //        while ((to -= sleep) > 0 && !isCollected)
    //        {
    //            Thread.Sleep(sleep);

    //            var buf = response();
    //            if (buf.Any())
    //            {
    //                isCollecting = true;
    //                buffer.AddRange(buf);
    //                waitCollected = 0;
    //            }
    //            else
    //            {
    //                if (isCollecting)
    //                {
    //                    waitCollected++;
    //                    if (waitCollected == wait)
    //                    {
    //                        isCollected = true;
    //                    }
    //                }
    //            }
    //        }
    //        //log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));

    //        return buffer.ToArray();
    //    }

    //    private dynamic Send(byte[] dataSend)
    //    {
    //        dynamic answer = new ExpandoObject();
    //        answer.success = false;
    //        answer.error = string.Empty;

    //        byte[] data = null;

    //        for (var attempts = 0; attempts < 3 && answer.success == false; attempts++)
    //        {
    //            if (attempts == 0)
    //            {
    //                data = SendSimple(dataSend, 1500, 0);
    //            }
    //            else if (attempts == 1)
    //            {
    //                data = SendSimple(dataSend, 5000, 2);
    //            }
    //            else
    //            {
    //                data = SendSimple(dataSend, 10000, 6);
    //            }

    //            if (data.Length == 0)
    //            {
    //                answer.error = "Нет ответа";
    //            }
    //            else
    //            {
    //                if (data.Length < 5)
    //                {
    //                    answer.error = "в кадре ответа не может содежаться менее 5 байт";
    //                }
    //                else if (!Crc.Check(data, new Crc16Modbus()))
    //                {
    //                    answer.error = "контрольная сумма кадра не сошлась";
    //                }
    //                else
    //                {
    //                    answer.success = true;
    //                }
    //            }
    //        }

    //        if (answer.success)
    //        {
    //            answer.NetworkAddress = data[0];
    //            answer.Function = data[1];
    //            answer.Length = data[3];
    //            answer.Body = data.Skip(3).Take(data.Length - 5).ToArray();
    //            answer.data = data;

    //            //modbus error
    //            if (answer.Function > 0x80)
    //            {
    //                var exceptionCode = (ModbusExceptionCode)data[2];
    //                answer.success = false;
    //                answer.error = string.Format("устройство вернуло ошибку: {0}", exceptionCode);
    //            }
    //        }

    //        return answer;
    //    }

    //    enum ModbusExceptionCode : byte
    //    {
    //        ILLEGAL_FUNCTION = 0x01,
    //        ILLEGAL_DATA_ADDRESS = 0x02,
    //        ILLEGAL_DATA_VALUE = 0x03,
    //        SLAVE_DEVICE_FAILURE = 0x04,
    //        ACKNOWLEDGE = 0x05,
    //        SLAVE_DEVICE_BUSY = 0x06,
    //        MEMORY_PARITY_ERROR = 0x07,
    //        GATEWAY_PATH_UNAVAILABLE = 0x0a,
    //        GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
    //    }

    //    #endregion

    //    #region Request

    //    byte[] MakeRequest(byte function, List<byte> Data = null)
    //    {
    //        var data = new List<byte>();
    //        data.Add(NetworkAddress);
    //        data.Add(function);

    //        if (Data != null)
    //        {
    //            data.AddRange(Data);
    //        }

    //        var crc = Crc.Calc(data.ToArray(), new Crc16Modbus());
    //        data.Add(crc.CrcData[0]);
    //        data.Add(crc.CrcData[1]);
    //        return data.ToArray();
    //    }

    //    byte[] MakeRequest17()
    //    {
    //        return MakeRequest(17);
    //    }

    //    #endregion

    //    #region Response

    //    dynamic ParseResponse17(dynamic answer)
    //    {
    //        if(!answer.success) return answer;

    //        var x = Encoding.ASCII.GetString(answer.Body);
    //        var regex = new Regex(@"VZLJOT (..\.?){4}");
    //        var match = regex.Match(x);

    //        answer.Version = "???";
    //        if (match.Success)
    //        {
    //            answer.Version = match.Value;
    //        }

    //        return answer;
    //    }

    //    #endregion
    //}
}
