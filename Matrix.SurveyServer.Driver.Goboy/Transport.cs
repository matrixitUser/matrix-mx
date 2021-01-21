using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.Goboy
{
    public partial class Driver
    {
        private byte[] Send(byte[] data)
        {
            request(data);
            log(string.Format(">({1}) {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Count()), level: 3);

            var buffer = new List<byte>();
            var timeout = 10000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !(buffer.Any() && CheckGoboiCrc(buffer.ToArray())))
            {
                Thread.Sleep(sleep);
                buffer.AddRange(response());
            }

            log(string.Format("<({1}) {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count()), level: 3);
            return buffer.ToArray();
        }

        private void DoRaccord(bool isMatrix)
        {
            //DateTime start = DateTime.Now;
            log("раккорд, ожидание 20 сек");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            if (isMatrix)
            {
                Int16 time = 210; //количество интервалов по 100мс
                request(MakeMatrixRequest(0x1F, MakeCommandRequest(0x55, time)));
                Thread.Sleep(time * 100);
            }
            else
            {
                var length = 101; //длина посылки, байт

                // время за которое посылка отправляется
                var interval = 100; //мс
                
                //число пакетов, отправленных за 21 сек
                var parts = 21 * 1000 / interval;
                var req = MakeRaccordRequest(length);
                log(string.Format("отправляется раккорд интервал: {0} мс, размер порции: {1}, количество: {2}", interval, length, parts));
                for (int i = 0; i < parts; i++)
                {
                    if (sw.ElapsedMilliseconds >= 21000) break;
                    request(req);
                    Thread.Sleep(interval);
                }                
            }

            sw.Stop();
            log(string.Format("раккорд завершен за {0:0} сек", sw.ElapsedMilliseconds / 1000), level: 3);//(DateTime.Now - start).TotalSeconds));
        }

        private byte[] MakeMatrixRequest(byte command, byte[] body)
        {
            var password = Encoding.ASCII.GetBytes("matrix");
            var bytes = new List<byte>();
            bytes.AddRange(password);
            bytes.Add(command);
            bytes.Add(0x01);
            bytes.AddRange(body);
            return bytes.ToArray();
        }

        private byte[] MakeCommandRequest(byte symbol, Int16 time)
        {
            var body = new List<byte>();
            body.Add(symbol);
            body.AddRange(BitConverter.GetBytes(time));
            return body.ToArray();
        }

        private byte[] MakeRaccordRequest(int length)
        {
            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                bytes[i] = 0x55;
            }
            return bytes;
        }

        private dynamic ParseResponse(byte[] data)
        {
            dynamic response = new ExpandoObject();
            response.success = false;
            response.error = "";

            if ((data == null) || (data.Length < 11))
            {
                response.error = "недостаточно данных";
                return response;
            }

            if (!CheckGoboiCrc(data))
            {
                response.error = "не сошлась контрольная сумма";
                return response;
            }

            response.type = data[1];
            response.serialNumber = BitConverter.ToInt16(data, 2);
            response.command = data[6];
            
            if (data.Length == 11)
            {
                response.error = string.Format("ошибка при выполнении команды, код {0:X2}", response.command);
                return response;
            }

            response.success = true;
            response.length = data[7] + data[8] * 255;
            response.body = data.Skip(9).Take(data.Length - 11).ToArray();

            return response;
        }

        private byte[] MakeRequest(int sn, byte cmd, byte[] body)
        {
            var bytes = new List<byte> { 0xa5, 0x00 };
            bytes.AddRange(BitConverter.GetBytes(sn));
            bytes.Add(cmd);

            bytes.Add(GetLowByte(body.Length));
            bytes.Add(GetHighByte(body.Length));
            bytes.AddRange(body);
            var crc = CalcGoboiCrc(bytes.ToArray());
            bytes.AddRange(crc);
            return bytes.ToArray();
        }

        private byte[] MakeMemoryRequest(int sn, short start, short count)
        {
            return MakeRequest(sn, 0x02, new byte[] {
                GetLowByte(start),
                GetHighByte(start),
                GetLowByte(count),
                GetHighByte(count)
            });
        }
    }
}
