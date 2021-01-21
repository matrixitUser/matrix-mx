using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.SurveyServer.Driver.SPG742
{
    public partial class Driver
    {
        /// <summary>
        /// код формата сообщения
        /// </summary>
        private const byte FRM = 0x90;
        /// <summary>
        /// управляющий код начала сообщения
        /// </summary>
        private const byte SOH = 0x10;

        /// <summary>
        /// байт атрибутов сообщения
        /// зарезервировано для использования в последующих версиях протокола
        /// </summary>
        private const byte ATR = 0x00;

        private byte[] Send(byte[] data)
        {
            request(data);

            log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))), level: 3);

            byte[] buffer = new byte[] { };
            var timeout = 7000;
            var sleep = 100;
            while ((timeout -= sleep) > 0 && !buffer.Any())
            {
                Thread.Sleep(sleep);
                buffer = response();
            }
            //   string path = @"D:\SPG742.txt";
            //System.IO.File.AppendAllText(path, string.Format("\r\n"));
            //System.IO.File.AppendAllText(path, string.Join(",", buffer.Select(b => b.ToString("X2"))));

            log(string.Format("пришло {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))), level: 3);

            return buffer;
        }

        /// <summary>
        /// Базовый формат сообщений, стр 4
        /// </summary>
        /// <param name="nt">сетевой номер абонента-адресата сообщения</param>
        /// <returns></returns>
        private byte[] MakeBaseRequest(byte nt, byte[] msgbody)
        {
            ///байт идентификатора сообщения
            byte id = 0x00;

            var data = new List<byte>();
            data.Add(SOH);
            data.Add(nt);
            data.Add(FRM);
            data.Add(id);
            data.Add(ATR);
            data.AddRange(BitConverter.GetBytes((Int16)msgbody.Length));
            data.AddRange(msgbody);
            data.AddRange(BitConverter.GetBytes(Crc16Calc(data.Skip(1).ToArray())));
            return data.ToArray();
        }

        private dynamic ParseBaseResponse(byte[] bytes)
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.msgcode = -1;

            if (!bytes.Any())
            {
                answer.success = false;
                answer.error = "не получен ответ на команду";
                return answer;
            }
            if (!Crc16Check(bytes))
            {
                answer.success = false;
                answer.error = "не сошлась контрольная сумма";
                answer.body = bytes;
                return answer;
            }

            answer.msgcode = bytes[7];
            if (answer.msgcode == (byte)Codes.Error)
            {
                return ParseErrorResponse(bytes[8]);
            }
            answer.length = BitConverter.ToInt16(bytes, 5);
            answer.body = bytes.Skip(8).Take((int)bytes.Length - (8 + 2)).ToArray();
            return answer;
        }

        private dynamic ParseErrorResponse(byte code)
        {
            dynamic error = new ExpandoObject();
            error.success = false;
            switch (code)
            {
                case 0: error.error = "нарушение структуры запроса"; return error;
                case 1: error.error = "защита от записи"; return error;
                case 2: error.error = "недопустимые значения параметров запроса"; return error;
                default: error.error = "нет сведений об ошибке"; return error;
            }
        }

        #region Архивы

        private dynamic ReadArchive(byte na, byte ch, ArchiveType type, DateTime start, DateTime end, byte count = 1)
        {
            var bytes = MakeArchiveRequest(na, ch, type, count, start, end);
            dynamic answer = ParseBaseResponse(Send(bytes));

            if (!answer.success) return answer;

            if (IsIntervalArchive(type))
                answer.archives = ParseIntervalArchive(answer);
            else
                answer.archives = ParseInductionArchive(answer);
            return answer;
        }

        /// <summary>
        /// Интервальные архивы
        /// </summary>
        private List<dynamic> ParseIntervalArchive(dynamic data)
        {
            int number = 0;
            List<dynamic> archives = new List<dynamic>();
            while (number < data.length - 1)
            {
                dynamic archive = new ExpandoObject();

                byte archtag = data.body[number];
                if (archtag != (byte)Tags.ARCHDATE)
                {
                    log("ошибка в Tags.ARCHDATE");
                    break;
                }
                byte ln = data.body[number + 1];
                if (ln == 0) break;
                var archdate = (data.body as byte[]).Skip(number + 2).Take(ln).ToArray();
                archive.date = ParseParameter(archtag, archdate);

                number += ln + 2;

                byte sequencetag = data.body[number];
                if (sequencetag != (byte)Tags.SEQUENCE)
                {
                    log("ошибка в Tags.SEQUENCE");
                    break;
                }

                if ((byte)data.body[number + 1] == 0x00) break;


                ln = data.body[number + 2];
                var archbody = (data.body as byte[]).Skip(number + 3).Take(ln).ToArray();
                archive.body = ParseParameters(archbody);

                archives.Add(archive);
                number += ln + 3;
            }
            return archives;
        }

        /// <summary>
        /// Асинхронные архивы
        /// </summary>
        private List<dynamic> ParseInductionArchive(dynamic data)
        {
            int number = 0;
            List<dynamic> archives = new List<dynamic>();
            while (number < data.length - 1)
            {
                dynamic archive = new ExpandoObject();

                byte archtag = data.body[number];
                if (archtag != (byte)Tags.ARCHDATE)
                {
                    log("ошибка в Tags.ARCHDATE");
                    break;
                }
                byte ln = data.body[number + 1];
                if (ln == 0) break;
                var archdate = (data.body as byte[]).Skip(number + 2).Take(ln).ToArray();
                archive.date = ParseParameter(archtag, archdate);
                number += ln + 2;

                byte tag = data.body[number];
                if (tag != (byte)Tags.ASCIIString)
                {
                    log("ошибка в Tags.ASCIIString");
                    break;
                }
                if (data.body[number + 1] == 0x00)
                {
                    break;
                }

                ln = data.body[number + 1];
                var archbody = (data.body as byte[]).Skip(number + 3).Take(ln).ToArray();
                archive.body = ParseParameter(tag, archbody);
                archives.Add(archive);
                number += ln + 2;
            }
            return archives;
        }

        private byte[] MakeArchiveRequest(byte na, byte ch, ArchiveType type, byte count, DateTime start, DateTime end)
        {
            var msgbody = new List<byte>();
            msgbody.Add((byte)Codes.Archive);
            msgbody.Add((byte)Tags.OCTET_STRING);

            var octet = new List<byte>();
            octet.Add(0xFF);
            octet.Add(0xFF);
            octet.Add(ch);
            octet.Add((byte)type);
            octet.Add(count);

            msgbody.Add((byte)octet.Count);
            msgbody.AddRange(octet);
            msgbody.AddRange(GetDateTimeBytes(start));
            msgbody.AddRange(GetDateTimeBytes(end));
            return MakeBaseRequest(na, msgbody.ToArray());
        }

        private byte[] GetDateTimeBytes(DateTime date)
        {
            byte YY = (byte)(date.Year % 100);
            byte MH = (byte)date.Month;
            byte DD = (byte)date.Day;
            byte HH = (byte)date.Hour;
            byte MM = (byte)date.Minute;
            byte SS = (byte)date.Second;
            //byte ms_l = 0x00;
            //byte ms_h = 0x00;
            var bytes = new byte[] { YY, MH, DD, HH, MM, SS };
            var data = new List<byte>();
            data.Add((byte)Tags.ARCHDATE);
            data.Add((byte)bytes.Length);
            data.AddRange(bytes);
            return data.ToArray();
        }

        #endregion

        #region Параметры

        private dynamic ReadParameters(byte na, byte ch, short[] codes)
        {
            var parameters = new List<dynamic>();

            int count = 20;
            for (int index = 0; index < codes.Length; index += count)
            {
                var partcodes = codes.Skip(index).Take(count).ToArray();
                dynamic answer = null;
                for (int i = 0; i < TRY_COUNT; i++)
                {
                    if (cancel())
                    {
                        answer = new ExpandoObject();
                        answer.success = false;
                        answer.error = "вызвана остановка процесса опроса";
                        return answer;
                    }

                    var bytes = MakeParameterRequest(na, ch, partcodes);
                    answer = ParseBaseResponse(Send(bytes));
                    if (answer.success) break;
                    log(string.Format("неудача при считывании параметров: {0}", answer.error));
                }
                if (!answer.success) return answer;

                parameters.AddRange(ParseParameters(answer.body));
            }

            dynamic parameter = new ExpandoObject();
            parameter.success = true;
            parameter.error = string.Empty;
            parameter.parameters = parameters;
            return parameter;
        }

        private byte[] MakeParameterRequest(byte na, byte ch, short[] codes)
        {
            var msgbody = new List<byte>();
            msgbody.Add((byte)Codes.ParameterRead);
            foreach (var code in codes)
            {
                msgbody.AddRange(MakeBaseParameterRequest(ch, code));
            }
            return MakeBaseRequest(na, msgbody.ToArray());
        }

        private byte[] MakeBaseParameterRequest(byte ch, short Pn)
        {
            var parameter = new List<byte>();
            parameter.Add((byte)Tags.PNUM);
            var PnBytes = BitConverter.GetBytes(Pn);
            parameter.Add((byte)(PnBytes.Length + 1));
            parameter.Add(ch);
            parameter.AddRange(PnBytes);
            return parameter.ToArray();
        }

        private List<dynamic> ParseParameters(byte[] bytes)
        {
            var parameters = new List<dynamic>();
            int number = 0;
            while (number < bytes.Length - 1)
            {
                byte tag = bytes[number];
                byte ln = bytes[number + 1];
                var body = bytes.Skip(number + 2).Take(ln).ToArray();
                parameters.Add(ParseParameter(tag, body));
                number += ln + 2;
            }
            return parameters;
        }

        private dynamic ParseParameter(byte tag, byte[] body)
        {
            switch ((Tags)tag)
            {
                case Tags.OCTET_STRING: return Encoding.ASCII.GetString(body); ;
                case Tags.NULL: return "нет данных";
                case Tags.ASCIIString: return Encoding.ASCII.GetString(body);
                case Tags.SEQUENCE: return "";
                case Tags.IntU: return BitConverter.ToUInt32(body, 0);
                case Tags.IntS: return BitConverter.ToInt32(body, 0);
                case Tags.IEEFloat: return (double)BitConverter.ToSingle(body, 0);
                case Tags.TIME:
                    {
                        if (body.Length != 4) return new TimeSpan();
                        int sec = body[1];
                        int min = body[2];
                        int hour = body[3];
                        return new TimeSpan(hour, min, sec);
                    }
                case Tags.DATE:
                    {
                        if (body.Length != 4) return new DateTime();
                        int day = body[0];
                        int month = body[1];
                        int year = body[2] + 2000;
                        return new DateTime(year, month, day);
                    }
                case Tags.ARCHDATE:
                    {
                        if (body.Length == 3 || body.Length == 4)
                        {
                            int year = 2000 + body[0];
                            int month = body[1];
                            int day = body[2];
                            int hour = 0;
                            int min = 0;
                            int sec = 0;
                            return new DateTime(year, month, day, hour, min, sec);
                        }

                        if (body.Length == 6)
                        {
                            int year = 2000 + body[0];
                            int month = body[1];
                            int day = body[2];
                            int hour = body[3];
                            int min = body[4];
                            int sec = body[5];
                            return new DateTime(year, month, day, hour, min, sec);
                        }
                        return new DateTime(2000, 01, 01);
                    }
                case Tags.ERR: return Encoding.ASCII.GetString(body);
                default: return string.Format("не реализован обработчик ({0})", tag);
            }
        }

        #endregion
    }
}
