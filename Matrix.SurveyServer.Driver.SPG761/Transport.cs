using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {
        private byte[] SendWithCrc(byte[] data)
        {
            request(data);
                log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            byte[] buffer = new byte[] { };
            var timeout = 10000;
            var sleep = 100;
            List<byte> range = new List<byte>();
            while ((timeout -= sleep) > 0 && !CrcCheck(range.ToArray()))
            {
                Thread.Sleep(sleep);
                buffer = response();
                if (!buffer.Any()) continue;

                range.AddRange(buffer);
            }
            
                log(string.Format("пришло {0}", string.Join(",", range.Select(b => b.ToString("X2")))), level: 3);
            return range.ToArray();
        }

        /// <summary>
        /// заголовок с учетом DLE-стафинга
        /// </summary>
        /// <param name="dad">байт адреса приёмника</param>
        /// <param name="sad">байт адреса источника</param>
        /// <param name="fnc">байт кода функции</param>
        /// <returns></returns>
        private byte[] MakeHeader(byte dad, byte sad, byte fnc, bool needDad)
        {
            var bytes = new List<byte>();

            bytes.Add(DLE);
            bytes.Add(SOH);

            if (needDad)
            {
                bytes.Add(dad);
                bytes.Add(sad);
            }

            bytes.Add(DLE);
            bytes.Add(ISI);
            bytes.Add(fnc);

            return bytes.ToArray();
        }


        /// <summary>
        /// общая структура сообщения
        /// </summary>
        /// <param name="dad">байт адреса приёмника</param>
        /// <param name="sad">байт адреса источника</param>
        /// <param name="fnc">байт кода функции</param>
        /// <param name="body"></param>
        /// <returns></returns>

        private byte[] MakeBaseRequest(byte dad, byte sad, byte fnc, IEnumerable<byte> body, bool needDad)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(MakeHeader(dad, sad, fnc, needDad));

            bytes.Add(DLE);
            bytes.Add(STX);

            bytes.AddRange(body);

            bytes.Add(DLE);
            bytes.Add(ETX);

            var crc = CrcCalc(bytes.ToArray(), 2, bytes.Count - 2);
            bytes.AddRange(crc);
            return bytes.ToArray();
        }
        private dynamic ParseBaseResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();

            answer.success = true;
            answer.error = string.Empty;
            answer.body = new ExpandoObject();

            if (bytes == null || !bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на запрос";
                return answer;
            }

            if (!CrcCheck(bytes))
            {
                answer.success = false;
                answer.error = "контрольная сумма не сошлась";
                return answer;
            }

            var start = 0;
            var end = bytes.Length;
            for (int i = 0; i < bytes.Length - 1; i++)
            {
                if (bytes[i] == DLE && bytes[i + 1] == STX)
                {
                    start = i + 2;
                }

                if (start != 0 && bytes[i] == DLE && bytes[i + 1] == ETX)
                {
                    end = i - start - 1;
                    break;
                }
            }

            var body = Unstuff((bytes as IEnumerable<byte>).Skip(start).Take(end).ToArray());
            answer.body = Decode(body);

            return answer;
        }

        private dynamic GetParameters(byte dad, byte sad, Dictionary<string, string> categories, bool needDad)
        {
            List<byte> dataSet = new List<byte>();
            foreach (var category in categories)
            {
                dataSet.Add(HT);
                dataSet.AddRange(Encode(category.Value));
                dataSet.Add(HT);
                dataSet.AddRange(Encode(category.Key));
                dataSet.Add(FF);
            }

            var bytes = MakeBaseRequest(dad, sad, 0x1D, Stuffing(dataSet), needDad);
            return ParseParameterResponse(SendWithCrc(bytes.ToArray()));
        }

        private dynamic ParseParameterResponse(byte[] bytes)
        {
            dynamic answer = ParseBaseResponse(bytes);

            if (!answer.success)
                return answer;

            var categories = new List<string[]>();

            //   var body = Decode(answer.body);
            var cats = answer.body.Split((char)FF);

            for (int i = 0; i < cats.Length - 1; i += 2)
            {
                IEnumerable<string> ptrs = cats[i + 1].Split((char)HT);
                categories.Add(ptrs.Skip(1).ToArray());
                // log(string.Format("категория {0}", string.Join(";", categories.Last())));
            }

            //log(string.Format("категории {0}", string.Join(",", categories.Select(c => c.First()))));

            dynamic parameter = new ExpandoObject();
            parameter.success = true;
            parameter.error = string.Empty;
            parameter.categories = categories;
            return parameter;
        }

        private dynamic GetIndexArray(byte dad, byte sad, bool needDad, byte ch, byte narray, byte start, byte count)
        {
            List<byte> dataSet = new List<byte>();

            dataSet.Add(HT);
            dataSet.AddRange(Encode(ch.ToString()));
            dataSet.Add(HT);
            dataSet.AddRange(Encode(narray.ToString()));
            dataSet.Add(HT);
            dataSet.AddRange(Encode(start.ToString()));
            dataSet.Add(HT);
            dataSet.AddRange(Encode(count.ToString()));
            dataSet.Add(FF);

            var bytes = MakeBaseRequest(dad, sad, 0x0C, Stuffing(dataSet), needDad);
            return ParseArrayResponse(SendWithCrc(bytes));
        }

        private dynamic ParseArrayResponse(byte[] bytes)
        {
            dynamic answer = ParseBaseResponse(bytes);

            if (!answer.success)
                return answer;

            var categories = new List<string[]>();
            var cats = answer.body.Split((char)FF);

            for (int i = 1; i < cats.Length; i++)
            {
                var ptrs = cats[i].Split((char)HT);
                categories.Add((ptrs as IEnumerable<string>).Skip(1).ToArray());
            }

            dynamic array = new ExpandoObject();
            array.success = true;
            array.error = string.Empty;
            array.categories = categories;

            return array;
        }

        private dynamic GetArhiveArray(byte dad, byte sad, bool needDad, string ch, string narray, DateTime end, DateTime start)
        {
            /// Третий указатель всегда должен содержать 
            /// предшествующий момент времени по отношению ко второму указателю
            /// т.е. сначла конец, затем начало
            List<byte> dataSet = new List<byte>();

            dataSet.Add(HT);
            dataSet.AddRange(Encode(ch));
            dataSet.Add(HT);
            dataSet.AddRange(Encode(narray));
            dataSet.Add(FF);
            dataSet.Add(HT);
            dataSet.AddRange(DateConvert(end));
            dataSet.Add(FF);
            dataSet.Add(HT);
            dataSet.AddRange(DateConvert(start));
            dataSet.Add(FF);

            var bytes = MakeBaseRequest(dad, sad, 0x0e, Stuffing(dataSet), needDad);
            return ParseArrayResponse(SendWithCrc(bytes));
        }

        private dynamic GetArhive(byte dad, byte sad, bool needDad, string ch, string narray, DateTime date)
        {
            List<byte> dataSet = new List<byte>();

            dataSet.Add(HT);
            dataSet.AddRange(Encode(ch));
            dataSet.Add(HT);
            dataSet.AddRange(Encode(narray));
            dataSet.Add(FF);
            dataSet.Add(HT);
            dataSet.AddRange(DateConvert(date));
            dataSet.Add(FF);

            var bytes = MakeBaseRequest(dad, sad, 0x18, Stuffing(dataSet), needDad);
            return ParseArrayResponse(SendWithCrc(bytes));
        }


        private byte[] DateConvert(DateTime date)
        {
            var bytes = new List<byte>();
            byte HT = 0x09;
            bytes.AddRange(Encode(date.Day.ToString("00")));

            bytes.Add(HT);
            bytes.AddRange(Encode(date.Month.ToString("00")));

            bytes.Add(HT);
            bytes.AddRange(Encode(date.Year.ToString("0000")));

            bytes.Add(HT);
            bytes.AddRange(Encode(date.Hour.ToString("00")));

            bytes.Add(HT);
            bytes.AddRange(Encode(date.Minute.ToString("00")));

            bytes.Add(HT);
            bytes.AddRange(Encode(date.Second.ToString("00")));

            return bytes.ToArray();
        }

        private byte[] Stuffing(IEnumerable<byte> raw)
        {
            var result = new List<byte>();
            foreach (var b in raw)
            {
                result.Add(b);
                if (b == DLE) result.Add(DLE);
            }
            return result.ToArray();
        }

        private byte[] Unstuff(byte[] stuffed)
        {
            var result = new List<byte>();
            for (int i = 0; i < stuffed.Length; i++)
            {
                var b = stuffed[i];
                if (b == DLE)
                {
                    i++;
                }
                result.Add(b);
            }
            return result.ToArray();
        }

        private string Decode(byte[] bytes)
        {
            var encoding = Encoding.GetEncoding(866);
            return encoding.GetString(bytes);
        }

        private byte[] Encode(string bytes)
        {
            var encoding = Encoding.GetEncoding(866);
            return encoding.GetBytes(bytes);
        }

        private byte[] MakeDataSet(IEnumerable<string[]> categories)
        {
            var result = new List<byte>();
            foreach (var category in categories)
            {
                foreach (var pointer in category)
                {
                    result.Add(HT);
                    result.AddRange(Encode(pointer));
                }
                result.Add(FF);
            }
            return result.ToArray();
        }

        private byte[] MakeDataHead(IEnumerable<string[]> categories)
        {
            var result = new List<byte>();
            foreach (var category in categories)
            {
                foreach (var pointer in category)
                {
                    result.Add(HT);
                    result.AddRange(Encode(pointer));
                }
                result.Add(FF);
            }
            return result.ToArray();
        }
    }
}
