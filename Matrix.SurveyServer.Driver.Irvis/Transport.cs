using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.Irvis
{
    public partial class Driver
    {
        //private byte[] Send(byte[] data)
        //{
        //    request(data);
        //    log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
        //    byte[] buffer = new byte[] { };
        //    var timeout = 7000;
        //    var sleep = 100;
        //    while ((timeout -= sleep) > 0 && !buffer.Any())
        //    {
        //        Thread.Sleep(sleep);
        //        buffer = response();
        //    }
        //    log(string.Format("пришло {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
        //    return buffer;
        //}

        private byte[] SendWithCrc(byte[] data)
        {
            request(data);

            if (debugMode)
            {
                log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            }

            byte[] buffer = new byte[] { };
            var timeout = 10000;
            var sleep = 100;
            List<byte> range = new List<byte>();
            while ((timeout -= sleep) > 0 && !CheckCrc16(range.ToArray()))
            {
                Thread.Sleep(sleep);
                buffer = response();
                if (!buffer.Any()) continue;

                range.AddRange(buffer);
            }

            if (debugMode)
            {
                log(string.Format("пришло {0}", string.Join(",", range.Select(b => b.ToString("X2")))));
            }

            return range.ToArray();
        }

        private byte[] Make70Request(byte na, byte cmd, byte[] body)
        {
            var bytes = new List<byte>();
            bytes.Add(cmd);
            bytes.AddRange(body);
            return MakeModbusRequest(na, 70, bytes.ToArray());
        }

        private dynamic Parse70Response(byte[] bytes)
        {
            var x = ParseModbusResponse(bytes);
            if (!x.success) return x;

            x.body = (x.body as byte[]).Skip(1).ToArray();
            return x;
        }

        /// <summary>
        /// Функция ModBus
        /// Запись группы 16-ти разрядных регистров
        /// </summary>
        /// <param name="na"></param>
        /// <param name="register"></param>
        /// <param name="registers"></param>
        /// <returns></returns>
        private byte[] Make16Request(byte na, short register, short[] registers)
        {
            var bytes = new List<byte>();
            bytes.Add(Helper.GetHighByte(register));	//стартовый регистр
            bytes.Add(Helper.GetLowByte(register));

            bytes.Add(Helper.GetHighByte(registers.Count()));	//стартовый регистр
            bytes.Add(Helper.GetLowByte(registers.Count()));

            bytes.Add((byte)(registers.Count() * 2));

            foreach (var reg in registers)
            {
                bytes.Add(Helper.GetHighByte(reg));
                bytes.Add(Helper.GetLowByte(reg));
            }
            return MakeModbusRequest(na, 16, bytes.ToArray());
        }

        /// <summary>
        /// Функция ModBus
        /// Чтение группы 16-ти разрядных регистров 
        /// </summary>
        /// <param name="na"></param>
        /// <param name="startRegister"></param>
        /// <param name="registerCount"></param>
        /// <returns></returns>
        private byte[] Make3Request(byte na, short startRegister, short registerCount)
        {
            var bytes = new List<byte>();
            bytes.Add(Helper.GetHighByte(startRegister));	//стартовый регистр
            bytes.Add(Helper.GetLowByte(startRegister));

            bytes.Add(Helper.GetHighByte(registerCount));	//стартовый регистр
            bytes.Add(Helper.GetLowByte(registerCount));

            return MakeModbusRequest(na, 3, bytes.ToArray());
        }
    }
}
