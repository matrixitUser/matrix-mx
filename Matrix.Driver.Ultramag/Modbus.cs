using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Driver.Ultramag
{
    public partial class Driver
    {
        private byte[] MakeModbusRequest(byte na, byte func, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(na);
            bytes.Add(func);
            bytes.AddRange(body);
            var crc = CalcCrc16(bytes.ToArray());
            bytes.AddRange(crc);
            return bytes.ToArray();
        }

        private byte[] MakeReadRequest(byte na, short register, short count)
        {
            return MakeModbusRequest(na, 4, new byte[] { 
                GetHighByte(register),
                GetLowByte(register),
                GetHighByte(count),
                GetLowByte(count)               
            });
        }

        private byte[] MakeWriteRequest(byte na, short register, byte[] data)
        {
            var wrap = new List<byte>();
            wrap.AddRange(data);
            if (wrap.Count % 2 != wrap.Count / 2)
            {
                wrap.Insert(0, 0);
            }

            var bytes = new List<byte> { 
                GetHighByte(register),
                GetLowByte(register),
                GetHighByte((short)(wrap.Count()/2)),
                GetLowByte((short)(wrap.Count()/2)),
                GetLowByte((byte)wrap.Count())               
            };

            bytes.AddRange(wrap);

            return MakeModbusRequest(na, 16, bytes.ToArray());
        }

        public static byte GetLowByte(int b)
        {
            return (byte)(b & 0xFF);
        }

        public static byte GetHighByte(int b)
        {
            return (byte)((b >> 8) & 0xFF);
        }

        private dynamic ParseModbusResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на запрос";
                return answer;
            }

            var clear = new List<byte>();
            for (var i = 0; i < bytes.Length; i++)
            {
                if (i < bytes.Length - 1 && bytes[i] == 0x0d && bytes[i + 1] == 0x0a)
                {
                    i++;
                    continue;
                }
                clear.Add(bytes[i]);
            }
            //bytes = clear.ToArray();

            if (!CheckCrc16(bytes))
            {
                answer.success = false;
                answer.error = "не сошлась контрольная сумма";
                log(string.Format("crc {0}", string.Join(",", bytes.Select(b => b.ToString("X2")))));

                //string path = @"D:\Irvis.txt";
                //System.IO.File.AppendAllText(path, string.Format("crc {0}\r\n", string.Join(",", bytes.Select(b => b.ToString("X2")))));

                answer.body = bytes;
                return answer;
            }

            byte function = bytes[1];

            if (function > 0x80)
            {
                var exceptionCode = (ModbusExceptionCode)bytes[2];

                answer.success = false;
                answer.error = string.Format("устройство вернуло ошибку: {0}", GetModbusException(exceptionCode));
                answer.body = bytes;
                return answer;
            }

            answer.success = true;
            answer.error = string.Empty;

            //пропускаем байты 
            //0 - сетевой адрес
            //1 - функция
            answer.body = (bytes as byte[]).Skip(2).Take((int)bytes.Length - (2 + 2)).ToArray();
            return answer;
        }

        enum ModbusExceptionCode : byte
        {
            ILLEGAL_FUNCTION = 0x01,
            ILLEGAL_DATA_ADDRESS = 0x02,
            ILLEGAL_DATA_VALUE = 0x03,
            FAILURE_IN_ASSOCIATED_DEVICEE = 0x04,
            ACKNOWLEDGE = 0x05,
            SLAVE_DEVICE_BUSY = 0x06,
            MEMORY_PARITY_ERROR = 0x07,
            GATEWAY_PATH_UNAVAILABLE = 0x0a,
            GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND = 0x0b
        }

        private string GetModbusException(ModbusExceptionCode code)
        {
            switch (code)
            {
                case ModbusExceptionCode.ILLEGAL_FUNCTION: return "ILLEGAL_FUNCTION";
                case ModbusExceptionCode.ILLEGAL_DATA_ADDRESS: return "ILLEGAL_DATA_ADDRESS";
                case ModbusExceptionCode.ILLEGAL_DATA_VALUE: return "ILLEGAL_DATA_VALUE";
                case ModbusExceptionCode.FAILURE_IN_ASSOCIATED_DEVICEE: return "FAILURE_IN_ASSOCIATED_DEVICEE";
                case ModbusExceptionCode.ACKNOWLEDGE: return "ACKNOWLEDGE";
                case ModbusExceptionCode.SLAVE_DEVICE_BUSY: return "SLAVE_DEVICE_BUSY";
                case ModbusExceptionCode.MEMORY_PARITY_ERROR: return "MEMORY_PARITY_ERROR";
                case ModbusExceptionCode.GATEWAY_PATH_UNAVAILABLE: return "GATEWAY_PATH_UNAVAILABLE";
                case ModbusExceptionCode.GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND: return "GATEWAY_TARGET_DEVICE_FAILED_TO_RESPOND";
                default: return "ошибка не известна";
            }
        }
    }
}
