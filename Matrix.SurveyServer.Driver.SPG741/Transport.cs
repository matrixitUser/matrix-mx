using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SPG741
{
    public partial class Driver
    {
        private int mid = 0;
        private int lastMid = -1;
        private byte[] lastReceived = null;

        private enum DeviceError
        {
            NO_ERROR = 0,
            NO_ANSWER,
            TOO_SHORT_ANSWER,
            ANSWER_LENGTH_ERROR,
            ADDRESS_ERROR,
            CRC_ERROR,
            DEVICE_EXCEPTION
        };

        private byte[] SendSimple(byte[] data, int bytesToRead = 0)
        {
            var buffer = new List<byte>();

            //log(string.Format("{2:X}:Sys-({1})->Dev {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length, mid & 0xF));
            log(string.Format("Sys-({1})->Dev {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), level: 3);

            response();
            request(data);

            var timeout = 10000;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;

            while ((timeout -= sleep) > 0 && !isCollected && ((bytesToRead == 0) || (buffer.Count < bytesToRead)))
            {
                Thread.Sleep(sleep);

                var buf = response();
                if (buf.Any())
                {
                    isCollecting = true;
                    buffer.AddRange(buf);
                    waitCollected = 0;
                }
                else
                {
                    if (isCollecting)
                    {
                        waitCollected++;
                        if (waitCollected == 6)
                        {
                            isCollected = true;
                        }
                    }
                }
            }

            //log(string.Format("{2:X}:Sys<-({1})-Dev {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count, mid & 0xF));
            log(string.Format("Sys<-({1})-Dev {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count), level: 3);

            mid++;

            return buffer.ToArray();
        }

        private dynamic SendShort(byte[] data, byte cmd)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            answer.code = -1;

            byte[] buffer = null;

            for (var attempts = 0; attempts < 4 && answer.success == false; attempts++)
            {
                buffer = SendSimple(data);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer[0] != 0x10)
                    {
                        var x = buffer.ToList();
                        x.Insert(0, 0x10);
                        buffer = x.ToArray();
                    }

                    if (buffer.Length < 4)
                    {
                        answer.error = "в кадре ответа не может содежаться менее 4 байт";
                        answer.errorcode = DeviceError.TOO_SHORT_ANSWER;
                    }
                    else if (!CheckCrc(buffer))
                    {
                        answer.error = "контрольная сумма кадра не сошлась";
                        answer.errorcode = DeviceError.CRC_ERROR;
                        answer.body = buffer;
                    }
                    else
                    {
                        answer.success = true;
                        answer.error = string.Empty;
                        answer.errorcode = DeviceError.NO_ERROR;
                    }
                }
            }

            if (answer.success)
            {
                int index = -1;
                for (int i = 0; i < 3; i++)
                {
                    if (buffer[i] == cmd)
                    {
                        index = i;
                        break;
                    }

                    if (buffer[i] == 0x21)
                    {
                        answer.success = false;
                        answer.code = buffer[i];
                        answer.errorcode = DeviceError.DEVICE_EXCEPTION;

                        if (Errors.ContainsKey(buffer[i + 1]))
                        {
                            answer.error = Errors[buffer[i + 1]];
                        }
                        else
                        {
                            answer.error = "неопознанная ошибка";
                        }
                        answer.body = buffer;

                        return answer;
                    }
                }

                if (index == -1)
                {
                    answer.success = false;
                    answer.error = "ответ не корректный";
                    answer.body = buffer;
                    return answer;
                }

                answer.code = buffer[index];
                answer.body = (buffer as byte[]).Skip(index + 1).Take((int)buffer.Length - (2 + (index + 1))).ToArray();
            }

            return answer;
        }


        private dynamic SendFlash(byte[] data, byte n, HashSet<string> driverParameters)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            answer.code = -1;

            byte[] buffer = null;

            for (var attempts = 0; (attempts < 4) && (answer.success == false); attempts++)
            {
                buffer = SendSimple(data, 69 * n);
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                    answer.errorcode = DeviceError.NO_ANSWER;
                }
                else
                {
                    if (buffer[0] != 0x10)
                    {
                        var x = buffer.ToList();
                        x.Insert(0, 0x10);
                        buffer = x.ToArray();
                    }

                    if (buffer.Length % (69) != 0)
                    {
                        answer.error = "данные получены не полностью";
                        answer.errorcode = DeviceError.ANSWER_LENGTH_ERROR;
                    }
                    else
                    {
                        var pages = new List<byte[]>();
                        int count = buffer.Length / 69;
                        int i;
                        dynamic page = new ExpandoObject();
                        page.success = false;
                        page.error = "Нулевая длина";

                        for (i = 0; i < count; i++)
                        {
                            page = ParseShortResponse(buffer.Skip(i * 69).Take(69).ToArray(), 0x45);
                            if (!page.success) break;
                            pages.Add(page.body);
                        }

                        if (!page.success)
                        {
                            answer.error = page.error;
                            answer.errorcode = page.errorcode;
                        }
                        else
                        {
                            answer.n = pages.Count;
                            answer.body = pages;
                            answer.success = true;
                            answer.error = string.Empty;
                            answer.errorcode = DeviceError.NO_ERROR;
                        }
                    }
                }
            }

            return answer;
        }

        //

        private byte[] Send(byte[] data)
        {
            response();
            request(data);

            //log(string.Format("{2:X}:Sys-({1})->Dev* {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length, mid & 0xF));
            log(string.Format("Sys-({1})->Dev* {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), level: 3);

            byte[] buffer = new byte[] { };
            var timeout = 7000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !buffer.Any())
            {
                Thread.Sleep(sleep);
                buffer = response();
            }

            // часто ответ приходит без начального байта, добавляем при необходимости
            if (buffer.Any() && buffer[0] != 0x10)
            {
                var x = buffer.ToList();

                x.Insert(0, 0x10);
                buffer = x.ToArray();
            }

            bool filtered = false;

            if (buffer.Any())
            {
                //иногда в ответ приходит повтор предыдущего сообщения - фильтруем только первое
                if ((lastReceived != null) && (buffer.Length == lastReceived.Length) && (lastMid == (mid - 1)))
                {
                    filtered = true;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] != lastReceived[i])
                        {
                            filtered = false;
                            break;
                        }
                    }
                }

                //

                if (!filtered)
                {
                    lastReceived = buffer;
                    lastMid = mid;
                }
                else
                {
                    buffer = new byte[] { };
                }
            }
            else
            {
                //перегрузка lastMid, т.к "нет ответа" не считается за ответ
                lastMid = mid;
            }

            //log(string.Format("{2:X}:Sys<-({1})-Dev* {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Length, mid & 0xF));
            log(string.Format("Sys<-({1})-Dev* {0}", filtered ? "повтор" : string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Length), level: 3);


            mid++;
            // log(string.Format("пришло {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
            return buffer;
        }


        /// <summary>
        /// короткий кадр
        /// </summary>
        /// <param name="na">Групповой номер прибора (NT) / Сетевой адрес</param>
        /// <param name="cmd">Код запроса</param>
        /// <param name="d1">Поле 1</param>
        /// <param name="d2">Поле 2</param>
        /// <param name="d3">Поле 3</param>
        /// <param name="d4">Поле 4</param>
        /// <returns></returns>
        private byte[] MakeShortRequest(byte na, byte cmd, byte d1, byte d2, byte d3, byte d4)
        {
            var data = new byte[9];
            data[0] = 0x10; //код начала кадра
            data[1] = na;
            data[2] = cmd;
            data[3] = d1;
            data[4] = d2;
            data[5] = d3;
            data[6] = d4;
            data[7] = CalcCrc(data, 1, data.Length - 3);
            data[8] = 0x16;	//код конца кадра
            return data;
        }

        private dynamic ParseShortResponse(byte[] bytes, byte cmd)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            answer.code = -1;

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на команду";
                answer.errorcode = DeviceError.NO_ANSWER;
                return answer;
            }
            if (!CheckCrc(bytes))
            {
                //for (int i = 0; i < 3; i++)
                //{
                //    if (bytes[i] == 0x21) { return ParseErrorResponse(bytes, i); }
                //}

                answer.success = false;
                answer.error = "не сошлась контрольная сумма";
                answer.errorcode = DeviceError.CRC_ERROR;
                answer.body = bytes;
                return answer;
            }

            int index = -1;
            for (int i = 0; i < 3; i++)
            {
                if (bytes[i] == cmd) { index = i; break; }
                if (bytes[i] == 0x21) { return ParseErrorResponse(bytes, i); }
            }

            if (index == -1)
            {
                answer.success = false;
                answer.error = "ответ не корректный";
                answer.body = bytes;
                return answer;
            }
            answer.code = bytes[index];

            answer.body = (bytes as byte[]).Skip(index + 1).Take((int)bytes.Length - (2 + (index + 1))).ToArray();
            return answer;
        }

        private dynamic ParseErrorResponse(byte[] bytes, int index)
        {
            dynamic answer = new ExpandoObject();
            answer.success = false;
            answer.code = bytes[index];

            string error = string.Empty;
            if (Errors.ContainsKey(bytes[index + 1]))
                error = Errors[bytes[index + 1]];
            else
                error = "неопознаная ошибка";
            answer.error = string.Format("{0}", error);
            answer.body = bytes;

            return answer;
        }

        private Dictionary<int, string> Errors = new Dictionary<int, string>()
        {
            {0,"нарушение структуры запроса. Нарушена контрольная сумма принятого кадра запроса или код конца кадра. Код запроса не опознан."},
            {1,"защита от ввода параметра. Обработка запроса ввода параметра базы данных при включенной защите данных "},
            {2,"недопустимые значения параметров запроса. Запрос содержит недостоверные данные "},
            {3,"нет данных  Архивная запись не найдена (при запросе поиска записи в архиве)"}
        };

        private byte[] MakeRamRequest(byte na, Int16 address, byte n)
        {
            return MakeShortRequest(na, 0x52, Helper.GetLowByte(address), Helper.GetHighByte(address), n, 0x00);
        }
        private dynamic ParseRamResponse(byte[] bytes)
        {
            return ParseShortResponse(bytes, 0x52);
        }

        private dynamic ReadFlash(byte na, Int16 address, byte n)
        {
            var data = MakeFlashRequest(na, address, n);

            response();
            request(data);

            //log(string.Format("{2:X}:Sys-({1})->Dev/Flash {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length, mid & 0xF));
            log(string.Format("Sys-({1})->Dev/Flash {0}", string.Join(",", data.Select(b => b.ToString("X2"))), data.Length), level: 3);


            List<byte> buffer = new List<byte>();
            var timeout = 10000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && buffer.Count < 69 * n)
            {
                Thread.Sleep(sleep);
                var resp = response();
                if (!resp.Any()) continue;

                var sector = resp.ToArray();
                buffer.AddRange(sector);
            }

            //log(string.Format("{2:X}:Sys<-({1})-Dev/Flash {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count, mid & 0xF));
            log(string.Format("Sys<-({1})-Dev/Flash {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))), buffer.Count), level: 3);

            mid++;

            // log(string.Format("прочитано с flash {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
            return ParseFlashResponse(buffer.ToArray());
        }

        private byte[] MakeFlashRequest(byte na, Int16 address, byte n)
        {
            return MakeShortRequest(na, 0x45, Helper.GetLowByte(address), Helper.GetHighByte(address), n, 0x00);
        }
        private dynamic ParseFlashResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "ответ не получен";
                return answer;
            }

            if (bytes.Length % (69) != 0)
            {
                answer.success = false;
                answer.error = "данные получены не полностью";
                return answer;
            }

            List<byte[]> pages = new List<byte[]>();

            int count = bytes.Length / 69;

            for (int i = 0; i < count; i++)
            {
                var page = ParseShortResponse(bytes.Skip(i * 69).Take(69).ToArray(), 0x45);
                if (!page.success)
                    return page;
                pages.Add(page.body);
            }

            answer.n = pages.Count;
            answer.body = pages;
            answer.success = true;
            return answer;
        }
    }
}
