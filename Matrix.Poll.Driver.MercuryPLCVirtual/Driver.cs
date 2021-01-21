using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.MercuryPLCVirtual
{
    public partial class Driver
    {
#if OLD_DRIVER
        bool debugMode = false;
#endif
        public const string ParameName1 = "Электроэнергия (тариф 1)";
        public const string ParameName2 = "Электроэнергия (тариф 2)";
        public const string ParameName3 = "Электроэнергия (тариф 3)";
        public const string ParameName4 = "Электроэнергия (тариф 4)";
        public const string ParameNameAll = "Электроэнергия (все тарифы)";
        
        private UInt32 NetworkAddress;
        
        public DateTime startDate;
        public DateTime endDate;

        public string strHubIds;

        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
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

        private void log(string message, int level = 2)
        {
#if OLD_DRIVER
            if ((level < 3) || ((level == 3) && debugMode))
            {
                logger(message);
            }
#else
            logger(message, level);
#endif
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

        private dynamic MakeDayRecord(string parameter, double value, string unit, DateTime date, string s3)
        {
            dynamic record = new ExpandoObject();
            record.type = "Day";
            record.date = date;
            record.d1 = value;
            record.d2 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.s3 = (s3 == "") ? null : s3;
            record.dt1 = DateTime.Now;
            return record;
        }
        private dynamic MakeFixedRecord(string parameter, double value, string unit, DateTime date, string s3, string id)
        {
            dynamic record = new ExpandoObject();
            record.type = "Fixed";
            record.date = date;
            record.d1 = value;
            record.i1 = 0;
            record.s1 = parameter;
            record.s3 = (s3 == "") ? null : s3;
            record.dt1 = DateTime.Now;
            record.g1 = Guid.Parse(id);
            return record;
        }
        private dynamic MakeAbnormalRecord(string name, int duration, DateTime date, int eventId)
        {
            dynamic record = new ExpandoObject();
            record.type = "Abnormal";
            record.date = date;
            record.i1 = duration;
            record.i2 = eventId;
            record.s1 = name;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeCurrentRecord(string parameter, double d1, string s2, DateTime date, string s3)
        {
            dynamic record = new ExpandoObject();
            record.type = "Current";
            record.date = date;
            record.d1 = d1;
            record.s1 = parameter;
            record.s2 = s2;
            record.s3 = (s3 == "") ? null : s3;
            record.dt1 = DateTime.Now;
            return record;
        }

        private dynamic MakeMonthRecord(string parameter, double value, string unit, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Month"; //потому как нет данных помесячных
            record.d1 = value;
            record.s1 = parameter;
            record.s2 = unit;
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }


        private dynamic MakeResult(int code, DeviceError errorcode, string description)
        {
            dynamic result = new ExpandoObject();

            switch (errorcode)
            {
                case DeviceError.NO_ANSWER:
                    result.code = 310;
                    break;

                default:
                    result.code = code;
                    break;
            }

            result.description = description;
            result.success = code == 0 ? true : false;
            return result;
        }
        #endregion

        #region ImportExport
        /// <summary>
        /// Регистр выбора стрраницы
        /// </summary>
        private const short RVS = 0x0084;

#if OLD_DRIVER
        [Import("log")]
        private Action<string> logger;
#else
        [Import("logger")]
        private Action<string, int> logger;
#endif

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

        [Import("setTimeDifference")]
        private Action<TimeSpan> setTimeDifference;

        [Import("setContractHour")]
        private Action<int> setContractHour;

        [Import("recordLoad")]
        private Func<DateTime, DateTime, string, List<dynamic>> recordLoad;
        
        [Import("loadRecordsPowerful")]
        private Func<DateTime, DateTime, string, string, string, List<dynamic>> LoadRecordsPowerful;

        [Import("setArchiveDepth")]
        private Action<string, int> setArchiveDepth;

        [Import("setIndicationForRowCache")]
        private Action<double, string, DateTime> setIndicationForRowCache;

        [Export("do")]
        public dynamic Do(string what, dynamic arg)
        {
            var param = (IDictionary<string, object>)arg;

            #region networkAddress
            if (!param.ContainsKey("networkAddress") || !UInt32.TryParse(arg.networkAddress.ToString(), out NetworkAddress))
            {
               
                log("Отсутствуют сведения о сетевом адресе", level: 1);

                return MakeResult(202, DeviceError.NO_ERROR, "сетевой адрес");
            }
            #endregion
            if (!param.ContainsKey("hubIds"))
            {
                log("Отсутствуют id концентраторов", level: 1);
                return MakeResult(202, DeviceError.NO_ERROR, "нет id концентраторов");
            }
            strHubIds = arg.hubIds.ToString();

            #region components
            var components = "Hour;Day;Constant;Abnormal;Current";
            if (param.ContainsKey("components"))
            {
                components = arg.components;
                log(string.Format("указаны архивы {0}", components));
            }
            else
            {
                log(string.Format("архивы не указаны, будут опрошены все"));
            }
            #endregion

            #region start
            if (param.ContainsKey("start") && arg.start is DateTime)
            {
                startDate = (DateTime)arg.start;
                getStartDate = (type) => (DateTime)arg.start;
                log(string.Format("указана дата начала опроса {0:dd.MM.yyyy HH:mm}", arg.start));
            }
            else
            {
                startDate = DateTime.Now.AddMonths(-2);
                getStartDate = (type) => getLastTime(type);
                log(string.Format("дата начала опроса не указана, опрос начнется с последней прочитанной записи"));
            }
            #endregion

            #region end
            if (param.ContainsKey("end") && arg.end is DateTime)
            {
                endDate = (DateTime)arg.end;
                getEndDate = (type) => (DateTime)arg.end;
                log(string.Format("указана дата окончания опроса {0:dd.MM.yyyy HH:mm}", arg.end));
            }
            else
            {
                endDate = DateTime.MaxValue;
                getEndDate = null;
                log(string.Format("дата окончания опроса не указана, опрос продолжится до последней записи в вычислителе"));
            }
            #endregion
            
            #region dayRanges
            List<dynamic> dayRanges;
            if (param.ContainsKey("dayRanges") && arg.dayRanges is IEnumerable<dynamic>)
            {

                dayRanges = arg.dayRanges;
                foreach (var range in dayRanges)
                {
                    log(string.Format("принят суточный диапазон {0:dd.MM.yyyy}-{1:dd.MM.yyyy}", range.start, range.end));
                }
            }
            else
            {
                dayRanges = new List<dynamic>();
                dynamic defaultrange = new ExpandoObject();
                defaultrange.start = getStartDate("Day");
                defaultrange.end = getEndDate == null ? DateTime.MaxValue : getEndDate("Day");
                dayRanges.Add(defaultrange);
            }
            #endregion

            
             
            
            dynamic result;

            try
            {
                switch (what.ToLower())
                {
                    case "all":
                        {
                            result = Wrap(() => All(components));
                        }
                        break;
                    default:
                        {
                            var description = string.Format("неопознаная команда {0}", what);
                            log(description, level: 1);
                            result = MakeResult(201, DeviceError.NO_ERROR, description);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                log(string.Format("{1}; {0}", ex.StackTrace, ex.Message), level: 1);
                result = MakeResult(201, DeviceError.NO_ERROR, ex.Message);
            }

            return result;
        }

        private dynamic Wrap(Func<dynamic> func)
        {
            return func();
        }
        #endregion
        
        private dynamic GetCurrents()
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();

            DateTime dt10MonthBeforeNow = DateTime.Now.AddMonths(-10);
            
            List<Packet> packetListAll = new List<Packet>();
            List<string> tarifList = new List<string>();
            string parameters1 = string.Format("na{0}_{1}", NetworkAddress, ParameName1);
            var recordList1 = LoadRecordsPowerful(startDate, endDate, "Current", parameters1, "ids:" + strHubIds);
            foreach(var rec in recordList1)
            {
                recs.Add(MakeCurrentRecord(parameters1, rec.d1, "кВт", rec.date, rec.s3));
            } 
            string parameters2 = string.Format("na{0}_{1}", NetworkAddress, ParameName2);
            var recordList2 = LoadRecordsPowerful(startDate, endDate, "Current", parameters2, "ids:" + strHubIds);
            foreach (var rec in recordList2)
            {
                recs.Add(MakeCurrentRecord(parameters2, rec.d1, "кВт", rec.date, rec.s3));
            }
            string parameters3 = string.Format("na{0}_{1}", NetworkAddress, ParameName3);
            var recordList3 = LoadRecordsPowerful(startDate, endDate, "Current", parameters3, "ids:" + strHubIds);
            foreach (var rec in recordList3)
            {
                recs.Add(MakeCurrentRecord(parameters3, rec.d1, "кВт", rec.date, rec.s3));
            }

            string parameters4 = string.Format("na{0}_{1}", NetworkAddress, ParameName4);
            var recordList4 = LoadRecordsPowerful(startDate, endDate, "Current", parameters4, "ids:" + strHubIds);
            foreach (var rec in recordList4)
            {
                recs.Add(MakeCurrentRecord(parameters4, rec.d1, "кВт", rec.date, rec.s3));
            }

            string parametersAll = string.Format("na{0}_{1}", NetworkAddress, ParameNameAll);
            var recordListAll = LoadRecordsPowerful(startDate, endDate, "Current", parametersAll, "ids:" + strHubIds);

            foreach (var rec in recordListAll)
            {
                recs.Add(MakeCurrentRecord(parametersAll, rec.d1, "кВт", rec.date, rec.s3));
            }
           
            if (recordList4.Any()) // логика Ильмира для PLC2 так как у PLC1 нет 4-го тарифа
            {
                var valueSum = recordListAll.Max(x => x.d1);
                var valueSumDate = recordListAll.Max(x => x.date);
                setIndicationForRowCache(valueSum, "кВт", valueSumDate);
            }


            answer.records = recs;
            return answer;
        }


        private dynamic GetMonths()
        {
            dynamic answer = new ExpandoObject();
            answer.success = true;
            answer.error = string.Empty;
            answer.errorcode = DeviceError.NO_ERROR;
            List<dynamic> recs = new List<dynamic>();
            log($"Чтение данных последних месяцев для сетевого адреса: {NetworkAddress}", 1);

            double valueT1 = 0, valueT2 = 0, valueTAll = 0;

            DateTime packetMinDate = DateTime.MaxValue;
            log($"Обнаружены данные последних месяцев для сетевого адреса: {NetworkAddress}");

            string parameters1 = string.Format("na{0}_{1}", NetworkAddress, ParameName1);
            var recordList1 = LoadRecordsPowerful(startDate, endDate, "Day", parameters1, "ids:" + strHubIds);
            var recordList1FixedData = recordLoad(startDate.AddMonths(-12), endDate, "Fixed");
            var recordList1FixedDataVerification = recordList1FixedData.FindAll(x => x.s1 == parameters1).FindAll(x => x.i1 == 1);

            var recTmp1 = RecordForGetMonths(recordList1FixedDataVerification, recordList1, parameters1);
            DateTime date = DateTime.MinValue;
            if(recTmp1.Count > 0)
            {
                valueT1 = recTmp1.Max(x => x.d1);
                date = recTmp1.Max(x => x.date);
                recs.AddRange(recTmp1);
            }
           
            string parameters2 = string.Format("na{0}_{1}", NetworkAddress, ParameName2);
            var recordList2 = LoadRecordsPowerful(startDate, endDate, "Day", parameters2, "ids:" + strHubIds);
            var recordList2FixedData = recordLoad(startDate.AddMonths(-12), endDate, "Fixed");
            var recordList2FixedDataVerification = recordList2FixedData.FindAll(x => x.s1 == parameters2).FindAll(x => x.i1 == 1);

            var recTmp2 = RecordForGetMonths(recordList2FixedDataVerification, recordList2, parameters2);
            if(recTmp2.Count > 0)
            {
                valueT2 = recTmp2.Max(x => x.d1);
                if(date == DateTime.MinValue)
                {
                    date = recTmp2.Max(x => x.date);
                }
                recs.AddRange(recTmp2);
            }
           
            string parametersAll = string.Format("na{0}_{1}", NetworkAddress, ParameNameAll);
            var recordListAll = LoadRecordsPowerful(startDate, endDate, "Day", parametersAll, "ids:" + strHubIds);
            var recordListAllFixedData = recordLoad(startDate.AddMonths(-12), endDate, "Fixed");
            var recordListAllFixedDataVerification = recordListAllFixedData.FindAll(x => x.s1 == parametersAll).FindAll(x => x.i1 == 1);

            var recTmpAll = RecordForGetMonths(recordListAllFixedDataVerification, recordListAll, parametersAll);
            if(recTmpAll.Count > 0)
            {
                valueTAll = recTmpAll.Max(x => x.d1);
                date = recTmpAll.Max(x => x.date);
                recs.AddRange(recTmpAll);
            }

            if (valueTAll == 0)
            {
                valueTAll = valueT1 + valueT2;
            }
            if (valueTAll != 0)
            {
                setIndicationForRowCache(valueTAll, "кВт", date);
            }
            answer.records = recs;
            return answer;
        }
        public List<dynamic> RecordForGetMonths( List<dynamic> recordListFixedDataVerification, List<dynamic> recordList, string parameters)
        {
            List<dynamic> recs = new List<dynamic>();
            dynamic valueTAll = 0;
            foreach (var rec in recordListFixedDataVerification)
            {
                recs.Add(MakeDayRecord(parameters, rec.d1, "кВт", rec.date, rec.s3));
            }
            
            foreach (var rec in recordList)
            {
                var tmp1 = recordListFixedDataVerification.Find(x => x.date == rec.date);
                var tmp2 = recordListFixedDataVerification.FindAll(x => x.date < rec.date).Max(x => x.date);

                if (tmp1 != null) continue;

                List<dynamic> recTmp = InsideRecordForGetMonths(recordListFixedDataVerification, parameters, rec, tmp2);
                if (recTmp.Count > 0) recs.AddRange(recTmp);
            }

            var tmp3 = recordList.Find(x => x.date > DateTime.Now);
            if (tmp3 != null)
            {
                //var tmp1 = recordListFixedDataVerification.Find(x => x.date == tmp3.date);
                var tmp2 = recordListFixedDataVerification.FindAll(x => x.date < tmp3.date).Max(x => x.date);
                var recTmp = InsideRecordForGetMonths(recordListFixedDataVerification, parameters, tmp3, tmp2);
                if (recTmp.Count > 0) recs.AddRange(recTmp);
                //if (tmp1 != null)
                //{
                //    if (tmp1.d1 != tmp3.d1)
                //    {
                //        tmp3.date = ((DateTime)tmp3.date).AddHours(1);

                //        var recTmp = InsideRecordForGetMonths(recordListFixedDataVerification, parameters, tmp3, tmp2);
                //        if (recTmp.Count > 0) recs.AddRange(recTmp);
                //    }
                //}
                //else
                //{
                    
                //}
            }
            return recs;
        }
        public List<dynamic> InsideRecordForGetMonths(List<dynamic> recordListFixedDataVerification, string parameters, dynamic rec, DateTime? maxDataDate)
        {
            List<dynamic> recs = new List<dynamic>();
            if (maxDataDate != null)
            {
                var tmp22 = recordListFixedDataVerification.Find(x => x.date == maxDataDate);
                if (tmp22.d1 <= rec.d1)
                {
                    recs.Add(MakeFixedRecord(parameters, rec.d1, "кВт", rec.date, rec.s3, rec.objectId));
                }
            }
            else
            {
                recs.Add(MakeFixedRecord(parameters, rec.d1, "кВт", rec.date, rec.s3, rec.objectId));
            }
            return recs;
        }
        private dynamic All(string components)
        {
            if (components.Contains("Current"))
            {
                var currents = new List<dynamic>();

                var current = GetCurrents();
                if (!current.success)
                {
                    log(string.Format("Ошибка при считывании текущих и констант: {0}", current.error), level: 1);
                    return MakeResult(102, current.errorcode, current.error);
                }

                currents = current.records;
                log(string.Format("Текущие прочитаны: всего {0}", currents.Count), level: 1);
                records(currents);
            }

            if (components.Contains("Day"))
            {
                var months = new List<dynamic>();

                var month = GetMonths();
                if (!month.success)
                {
                    log(string.Format("Ошибка при считывании данных последних месяцев: {0}", month.error), level: 1);
                    return MakeResult(102, month.errorcode, month.error);
                }

                months = month.records;
                log(string.Format(" Данных последних месяцев прочитаны: всего {0}", months.Count), level: 1);
                records(months);
            }

           
            return MakeResult(0, DeviceError.NO_ERROR, "опрос успешно завершен");
        }
    }
}
