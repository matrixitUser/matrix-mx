using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Erz2000
{
    public partial class Driver
    {
        private byte[] MakeModbus3Request(byte na, short startRegister, short count)
        {
            var body = new byte[]
            {
                GetHighByte(startRegister),
                GetLowByte(startRegister),
                GetHighByte(count),
                GetLowByte(count)
            };

            return MakeModbusRequest(na, 0x03, body);
        }

        private byte[] MakeModbusRequest(byte na, byte fn, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(na);
            bytes.Add(fn);

            bytes.AddRange(body);

            var crc = CalcCrc16(bytes.ToArray());
            bytes.Add(crc[0]);
            bytes.Add(crc[1]);
            return bytes.ToArray();
        }

        private dynamic ParseModbus(byte[] bytes)
        {
            dynamic modbus = new ExpandoObject();

            if (!bytes.Any())
            {
                modbus.success = false;
                modbus.error = "не получен ответ на запрос";
                return modbus;
            }

            if (!CheckCrc16(bytes))
            {
                modbus.success = false;
                modbus.error = "не сошлась контрольная сумма";
                modbus.body = bytes;
                return modbus;
            }

            byte function = bytes[1];

            if (function > 0x80)
            {
                var exceptionCode = (ModbusExceptionCode)bytes[2];

                modbus.success = false;
                modbus.error = string.Format("устройство вернуло ошибку: {0}", exceptionCode);
                modbus.body = bytes;
                return modbus;
            }

            modbus.success = true;
            modbus.error = string.Empty;
            modbus.body = (bytes as byte[]).Skip(2).Take((int)bytes.Length - (2 + 2)).ToArray();
            return modbus;         
        }

        private byte[] CalcCrc16(byte[] bytes)
        {
            ushort polynomial = 0xA001;
            ushort[] table = new ushort[256];
            ushort value;
            ushort temp;

            for (ushort i = 0; i < table.Length; i++)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; j++)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }

            ushort crc = 0xFFFF;

            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            var crcBytes = new byte[] { (byte)(crc & 0x00ff), (byte)(crc >> 8) };
            return crcBytes;
        }

        private bool CheckCrc16(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 3) return false;

            var crc = CalcCrc16(bytes.Take(bytes.Length - 2).ToArray());
            var crcMsg = bytes.Skip(bytes.Length - 2).ToArray();

            for (int i = 0; i < 2; i++)
            {
                if (crc[i] != crcMsg[i]) return false;
            }
            return true;
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
    }
}
