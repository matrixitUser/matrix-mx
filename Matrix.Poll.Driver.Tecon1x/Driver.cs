using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.Tecon1x
{
    /// <summary>
    /// Драйвер для ТЭКОН-10 и ТЭКОН-17
    /// </summary>
    public partial class Driver
    {
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        enum ProtoFormat
        {
            FT1_2_CS,
            //FT1_2_CRCCS,
            //FT1_2_CRC,
            FT1_1
        }

        enum Function
        {
            FailsManagement = 0x00,     //управление отказами; - сброс и считывание новых
            ReadParameter,              //чтение параметра;
            ReadMemoryExt,              //чтение внешней памяти;
            ReadMemoryInt,              //чтение внутренней памяти;
            ReadMemoryProg,             //чтение памяти программ;
            WriteParameter,             //запись параметра;
            WriteMemoryExt,             //запись во внешнюю память;
            WriteMemoryInt,             //запись во внутреннюю память;
            CmdStart,                   //пуск;
            CmdStop,                    //останов;
            CmdNop,                     //команда исключена из протокола
            SuEnd,                      //конец полного доступа;
            CmdBitParam,                //работа с битовым параметром;
            ReprogramData,              //репрограммация данных;
            EraseMemory,                //очистка памяти;
            ChangeProgram,              //смена программы;
            ExchangeSuperflo,           //произвести обмен с Superflo; - только в "спецприборах"
            ReadParameterSlaveTecon,    //чтение параметра из ведомого ТЭКОН; - только в "спецприборах"
            ReadArchiveEvents,          //чтение архива событий;    - существует не во всех приборах
            ReadParameterPackage,       //чтение пакета параметров; - нет в старом протоколе
            WriteParameterSlaveTecon    //запись параметра в ведомый ТЭКОН; - только в "спецприборах"
        }

        ProtoFormat Format = ProtoFormat.FT1_2_CS;
        byte NetworkAddress = 1;

        #region ImportExport
        /// <summary>
        /// Регистр выбора стрраницы
        /// </summary>
        private const short RVS = 0x0084;
        [Import("log")]
        private Action<string> log;

        [Import("request")]
        private Action<byte[]> request;

        [Import("response")]
        private Func<byte[]> response;

        [Import("records")]
        private Action<IEnumerable<dynamic>> records;

        [Import("cancel")]
        private Func<bool> cancel;

        [Import("getLastTime")]
        private Func<string, DateTime> getLastTime;

        [Import("getLastRecords")]
        private Func<string, IEnumerable<dynamic>> getLastRecords;

        [Import("getRange")]
        private Func<string, DateTime, DateTime, IEnumerable<dynamic>> getRange;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            //Параметры вычислителя
            if (!param.ContainsKey("networkAddress") || !byte.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
                log(string.Format("Отсутствуют сведения о канале, выбран по умолчанию {0}", NetworkAddress));
            }

            //Параметры опроса
            if (param.ContainsKey("start") && arg.start is DateTime)
            {
                getStartDate = (type) => (DateTime)arg.start;
                log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            }
            else
            {
                getStartDate = (type) => getLastTime(type);
                log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            }

            if (param.ContainsKey("end") && arg.end is DateTime)
            {
                getEndDate = (type) => (DateTime)arg.end;
                log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            }
            else
            {
                getEndDate = null;
                log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }

            //Начало опроса
            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        result = All();
                        break;
                    case "ping":
                        result = Ping();
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description);
                            result = MakeResult(201, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                //log(ex.Message);
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message));
                result = MakeResult(201, ex.Message);
            }

            return result;
        }
        #endregion

        #region Common
        private byte[] SendSimple(byte[] data)
        {
            var buffer = new List<byte>();

            //log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))));

            response();
            request(data);

            var timeout = 7500;
            var sleep = 250;
            var isCollecting = false;
            var waitCollected = 0;
            var isCollected = false;
            while ((timeout -= sleep) > 0 && !isCollected)
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
            //log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));

            return buffer.ToArray();
        }

        private dynamic Send(Function func, byte[] data = null)
        {
            dynamic answer = new ExpandoObject();

            if (data == null)
            {
                data = new byte[] { };
            }

            //обязательные поля
            answer.success = false;
            answer.error = string.Empty;

            answer.result = null;
            answer.data = null;

            answer.Length = 0;   //длина 
            answer.C = null;//управляющий байт
            answer.A = null;//адрес

            byte[] buffer = null;

            var bytes = new List<byte>();

            byte C = 0x40;
            byte A = NetworkAddress;
            switch (Format)
            {
                case ProtoFormat.FT1_1:
                    bytes.Add((byte)(data.Length + 1));
                    bytes.Add(C);
                    bytes.Add(A);
                    bytes.AddRange(data);
                    break;

                case ProtoFormat.FT1_2_CS:
                //break;

                default:
                    if (data.Length <= 4)//формат с фиксированной длиной
                    {
                        bytes.Add(C);
                        bytes.Add(A);
                        bytes.Add(data[0]);
                        bytes.Add(data.Length > 1 ? data[1] : (byte)0x00);
                        bytes.Add(data.Length > 2 ? data[2] : (byte)0x00);
                        bytes.Add(data.Length > 3 ? data[3] : (byte)0x00);
                        var CS = (byte)bytes.Sum(r => r);
                        bytes.Add(CS);
                        bytes.Insert(0, 0x10);  //startSmall
                        bytes.Add(0x16);        //end
                    }
                    else
                    {
                        var L = (byte)(data.Length + 2);
                        var pre = new List<byte>()
                        {
                            0x68,//startFull
                            L,
                            L,
                            0x68
                        };

                        bytes.Add(C);
                        bytes.Add(A);
                        bytes.AddRange(data);
                        var CS = (byte)bytes.Sum(r => r);
                        bytes.Add(CS);
                        bytes.InsertRange(0, pre);
                        bytes.Add(0x16);        //end
                    }
                    break;

            }

            for (var attempts = 0; attempts < 3 && answer.success == false; attempts++)
            {
                buffer = SendSimple(bytes.ToArray());
                if (buffer.Length == 0)
                {
                    answer.error = "Нет ответа";
                }
                else
                {
                    switch (Format)
                    {
                        case ProtoFormat.FT1_1:
                            {
                                if (buffer.Length == 1) //квитанция
                                {
                                    if (buffer[0] == 0)
                                    {
                                        answer.success = true;
                                        answer.data = new byte[] { };
                                        answer.result = true;
                                    }
                                    else
                                    {
                                        answer.error = "квитанция имеет неверный формат";
                                    }
                                }
                                else if (buffer.Length < 3)
                                {
                                    answer.error = "в кадре ответа не может содержаться менее 3 байт";
                                }
                                else
                                {
                                    answer.Length = buffer[0] - 2;
                                    answer.C = buffer[1];
                                    answer.A = buffer[2];

                                    if (buffer[0] != (buffer.Length - 1))
                                    {
                                        answer.error = "кадр с переменной длиной имеет неверный формат";
                                    }
                                    else if ((answer.C & 0x40) > 0)
                                    {
                                        answer.error = "возможно, ответ не получен (установлен бит PRM)";
                                    }
                                    else if (answer.A != NetworkAddress)
                                    {
                                        answer.error = "возможно, получен ответ от другого прибора";
                                    }
                                    else
                                    {
                                        answer.success = true;
                                        answer.data = buffer.Skip(3);
                                    }
                                }
                            }
                            break;
                        case ProtoFormat.FT1_2_CS:
                        //break;
                        default:
                            {
                                if (buffer.Length == 1) //квитанция
                                {
                                    if (buffer[0] == 0xA2 || buffer[0] == 0xE5)
                                    {
                                        answer.success = true;
                                        answer.data = new byte[] { };
                                        answer.result = (buffer[0] == 0xA2);
                                    }
                                    else
                                    {
                                        answer.error = "квитанция имеет неверный формат";
                                    }
                                }
                                else if (buffer.Length < 9)
                                {
                                    answer.error = "в кадре ответа не может содержаться менее 9 байт";
                                }
                                else if ((buffer.Skip(1).Take(buffer.Length - 3).Sum(r => r) & 0xff) != buffer.Skip(buffer.Length - 2).FirstOrDefault())
                                {
                                    answer.error = "контрольная сумма кадра не сошлась";
                                }
                                else if (buffer.FirstOrDefault() == 0x10 && buffer.LastOrDefault() == 0x16) //кадр с постоянной длиной
                                {
                                    answer.Length = 4;
                                    answer.C = buffer[1];
                                    answer.A = buffer[2];

                                    if (buffer.Length != 7)
                                    {
                                        answer.error = "кадр с постоянной длиной имеет неверный формат";
                                    }
                                    else if ((answer.C & 0x40) > 0)
                                    {
                                        answer.error = "возможно, ответ не получен (установлен бит PRM)";
                                    }
                                    else if (answer.A != NetworkAddress)
                                    {
                                        answer.error = "возможно, получен ответ от другого прибора";
                                    }
                                    else
                                    {
                                        answer.success = true;
                                        //
                                        answer.data = buffer.Skip(3).Take((byte)answer.Length);
                                    }
                                }
                                else if (buffer.FirstOrDefault() == 0x68 && buffer.LastOrDefault() == 0x16) //кадр с переменной длиной
                                {
                                    answer.Length = buffer[1] - 2;
                                    answer.C = buffer[4];
                                    answer.A = buffer[5];

                                    if ((buffer[1] != buffer[2]) || (buffer[3] != 0x68) || (buffer[1] != (buffer.Length - 6)))
                                    {
                                        answer.error = "кадр с переменной длиной имеет неверный формат";
                                    }
                                    else if ((answer.C & 0x40) > 0)
                                    {
                                        answer.error = "возможно, ответ не получен (установлен бит PRM)";
                                    }
                                    else if (answer.A != NetworkAddress)
                                    {
                                        answer.error = "возможно, получен ответ от другого прибора";
                                    }
                                    else
                                    {
                                        answer.success = true;
                                        //
                                        answer.data = buffer.Skip(6).Take((byte)answer.Length);
                                    }
                                }
                                else
                                {
                                    answer.error = "не найдено соответствие для кадра";
                                }
                            }
                            break;
                    }
                }
            }

            if (answer.success && answer.result == null)
            {
                //.C, .A, .data={bytes}


                //    switch ((TFG_Proto)answer.proto)
                //    {
                //        case TFG_Proto.SHORT:                     //<channel|func|data|crc>
                //            answer.Func = buffer[1];
                //            answer.Body = buffer.Skip(2).Take(buffer.Count() - 3).ToArray();
                //            break;
                //        case TFG_Proto.LONG:                     //<func|logicnumber|channel|data|crc>
                //            answer.Func = buffer[0];
                //            answer.Body = buffer.Skip(3).Take(buffer.Count() - 4).ToArray();
                //            break;
                //    }
            }

            return answer;
        }


        private dynamic MakeConstRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayOrHourRecord(string type, string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = type;
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeHourRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Hour";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.i1 = duration;
            record.s1 = name;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }

        public dynamic MakeResult(int code, string description = "")
        {
            dynamic result = new ExpandoObject();
            result.code = code;
            result.success = code == 0 ? true : false;
            result.description = description;
            return result;
        }
        #endregion

        #region Base
        
        #endregion

        #region Интерфейс

        private dynamic Ping()
        {
            return MakeResult(0);
        }

        private dynamic All()
        {
            return MakeResult(0);
        }
        #endregion
    }
}
