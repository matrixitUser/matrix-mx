using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        const int MAX_2318_BLOCKS = 80;

        private Func<byte, byte, byte, byte, int, byte[]> GetBlocks;

        private byte[] GetBlocks2318(byte na, byte command, byte start, byte end, int blockLength)
        {
            if (end > MAX_2318_BLOCKS)
            {
                log(string.Format("запрошено большое число блоков, ограничение {0}", MAX_2318_BLOCKS));
                end = MAX_2318_BLOCKS;
            }

            var ans = Send(MakeCommandRequest(na, command));
            if (ans == null || !ans.Any()) return null;

            if (ans[0] == 0x62)
            {
                log(string.Format("команда {0:X2} не распознана", command));
                return null;
            }

            if (ans[0] != 0x6e)
            {
                log(string.Format("неопознаный ответ"));
                return null;
            }
            log(string.Format("команда {0:X2} принята", command));

            var count = end;

            log(string.Format("ушел запрос блоков c 0 до {0}", end));
            request(new byte[] { (byte)count, GetHighByte(blockLength), GetLowByte(blockLength) });

            var length = count * blockLength;

            var buffer = new List<byte>();

            var timeout = 20000 + 5000 * end;
            while (buffer.Count < length && timeout > 0)
            {
                if (fullCancel())
                {
                    return new byte[] { };
                }

                Thread.Sleep(100);
                timeout -= 100;
                var resp = response();
                if (resp.Any())
                {
                    buffer.AddRange(resp);

                    if (buffer.Count == 1)
                    {
                        if (buffer[0] == 0x65)
                        {
                            log("адаптер ответил: приняты не все данные");
                            return null;
                        }
                        if (buffer[0] == 0x61)
                        {
                            log("адаптер ответил: отсутствие ответа от прибора");
                            return null;
                        }
                    }

                    if (timeout < 6000) timeout = 6000;
                    log(string.Format("получена порция {0} из {1}", buffer.Count, length));
                }
            }
            if (buffer.Any())
            {
                buffer.RemoveAt(0);
            }
            return buffer.ToArray();
        }

        private bool stelCancel = false;

        private bool fullCancel()
        {
            return cancel() || stelCancel;
        }

        private byte[] GetBlocksStel(byte na, byte command, byte start, byte end, int blockLength)
        {
            //if (end - start > 1)
            //{
            //    var allazaur = new List<byte>();
            //    for (var st = start; st < end; st++)
            //    {
            //        allazaur.AddRange(GetBlocksStel(na, command, st, (byte)(st + 1), blockLength));
            //    }
            //    return allazaur.ToArray();
            //}

            if (start >= 100) start = 0;

            var allBuffer = new List<byte>();
            
            var count = end - start+1;

            if (count > 1 )//|| new byte[] {0xcb,0xd4,0xd5 }.Contains(command))
            {
                var ans = Send(MakeStelRangeRequest(start, end));
            }

            var cmdReq = MakeCommandRequest(na, command);
            log(string.Format("ушел запрос блоков {0} с {1} по {2}", string.Join(",", cmdReq.Select(r => r.ToString("X2"))), start, end));        
            request(cmdReq);
            
            var length = count * blockLength;
            log(string.Format("ожидаемое число блоков {0} (длина {0}x{1}={2})", count, blockLength, length));

            var buffer = new List<byte>();

            var timeout = 20000 + 2000 * start;
            while (buffer.Count < length && timeout > 0)
            {
                if (fullCancel())
                {
                    return new byte[] { };
                }
                Thread.Sleep(100);
                timeout -= 100;
                var resp = response();

                if (Encoding.UTF8.GetString(resp).Contains("+++"))
                {
                    log("контроллер СТЕЛ разорвал связь");
                    stelCancel = true;
                }

                buffer.AddRange(resp);

                if (resp.Any())
                {
                    if (timeout < 7000) timeout = 7000;
                    log(string.Format("получена порция {0} из {1}", buffer.Count, length));
                }
            }
            //allBuffer.AddRange(buffer);
            //}

            //return allBuffer.ToArray();
            return buffer.ToArray();
        }

        private List<byte>[] blocksBuffer = null;

        private List<byte>[] cacheFill(byte na, int count, int blockLength)
        {
            if (blocksBuffer == null || blocksBuffer.Length < count)
            {
                blocksBuffer = new List<byte>[count];

                //Сбор в модеме
                var body = new byte[]
                {
                    na,
                    0x9B,
                    GetHighByte(blockLength),
                    GetLowByte(blockLength),
                    GetHighByte(count),
                    GetLowByte(count)
                };

                log(string.Format("собирается архив из {0} блоков длиной по {1} байт", count, blockLength));
                var answer = Send(MakeMatrixRequest(0x12, body), count * 1000 + 20000);
                //log(string.Format("пришло {0}", string.Join(",", answer.Select(b => b.ToString("X2")))));
                if (answer.Length > 0 && answer[1] == 0x12)
                {
                    var lArchive = System.BitConverter.ToUInt16(answer, 2);
                    log(string.Format("архив в матриксе собран, длина архива={0}", lArchive));
                }
                else
                {
                    log(string.Format("архив в матриксе не собран"));
                    blocksBuffer = null;
                    return blocksBuffer;
                }

                // запрос блоков по-отдельности
                for (var i = 0; i < count; i++)
                {
                    var cmd = new byte[] { (byte)i, 1 };

                    request(MakeMatrixRequest(0x13, cmd));
                    request(MakeMatrixRequest(0x14, cmd));

                    //log(string.Format("запрос {1} блоков, начиная с {0}", i, 1));

                    //log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
                    List<byte> buffer = new List<byte>();
                    var timeout = 10000 + count * 250;
                    var sleep = 100;
                    var length = 1 * blockLength;
                    while ((timeout -= sleep) > 0 && buffer.Count < length)
                    {
                        Thread.Sleep(sleep);
                        var receive = response();
                        if (receive.Any())
                        {
                            buffer.AddRange(receive);
                            log(string.Format("получен блок {2}: {0} из {1}", buffer.Count, length, i));//, string.Join(",", buffer.Skip(length - 4).Select(b => b.ToString("X2")))));
                        }
                    }
                    //log(string.Format("({1})< {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count));

                    blocksBuffer[i] = new List<byte>(buffer);
                    //blocksBuffer[start].AddRange();
                }
            }

            return blocksBuffer;
        }

        private byte[] GetBlocksMatrix(byte na, byte command, byte start, byte end, int blockLength)
        {
            byte count = (byte)(end - start + 1);
            if (count <= 0)
            {
                return new byte[] { };
            }

            if (count == 1)
                return Send(MakeMatrixRequest(0x12, MakeCommandRequest(na, command)));

            var buffer = new List<byte>();
            var cache = cacheFill(na, end + 1, blockLength);
            if (cache == null || cache.Length == 0)
            {
                return new byte[] { };
            }

            for (var i = 0; i < end + 1; i++)   //start не используются, тк из-за него вылезают ошибки контрольной суммы
            {
                var c = cache[i];
                //log(string.Format("выборка из кэша: #{0} длиной {1} хвостик {2}", i, c.Count, string.Join(",", c.ToList().Skip(c.Count - 8).Select(b => b.ToString("X2")))));
                buffer.AddRange(c);
            }
            return buffer.ToArray();
        }

        private byte[] MakeMatrixRequest(byte command, byte[] body)
        {
            var password = Encoding.ASCII.GetBytes("matrix");
            var bytes = new List<byte>();
            bytes.AddRange(password);
            bytes.Add(command);
            bytes.AddRange(body);
            return bytes.ToArray();
        }

        private byte[] MakeCommandRequest(byte na, byte command)
        {
            return new byte[] { na, command };
        }

        private byte[] Send(byte[] data, int timeout = 20000)
        {
            request(data);
            //  log(string.Format("ушло {1} байт: {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length));
            byte[] buffer = new byte[] { };
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !buffer.Any())
            {
                Thread.Sleep(sleep);
                buffer = response();
            }
            //  log(string.Format("пришло {1} байт: {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Length));
            return buffer;
        }
    }
}
