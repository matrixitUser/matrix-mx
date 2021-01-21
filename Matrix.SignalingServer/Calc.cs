using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.SignalingServer
{
    class Calc
    {
        private const string SESSION_ID = "sessionId";
        private const string LOGIN = "login";
        private const string PASSWORD = "password";
        private const string URL = "serverUrl";
        private const string API_PATH = "api/transport";

        static Calc() { }

        private static readonly Calc instance = new Calc();
        public static Calc Instance
        {
            get
            {
                return instance;
            }
        }
        public void Calculate(Object stateInfo)
        {
            List<Guid> tubeids = new List<Guid>();
            List<dynamic> tubes = new List<dynamic>();
            List<dynamic> tubesWithSetPoint = new List<dynamic>();
            List<Guid> tubeIdsWithSetPoint = new List<Guid>();
            
            List<dynamic> rowsActiveEvent = GetTubeEventRows();
            List<dynamic> rowsCache = GetRowCache();
            foreach(var row in rowsCache)
            {
                tubeids.Add(Guid.Parse(row.id));
                if (tubeids.Count() > Math.Round(rowsCache.Count()/5d))
                {
                    tubes.AddRange(GetParameters(tubeids));
                    tubeids.Clear();
                }
            }
            tubes.AddRange(GetParameters(tubeids));
            foreach(var tube in tubes)
            {
                List<dynamic> listParam = new List<dynamic>();
                foreach (var parameter in tube.parameters)
                {
                    var parTmp = (IDictionary<string, object>)parameter;
                    if (parTmp.ContainsKey("max") && parameter.max != null)
                    {
                        listParam.Add(parameter);
                        //tubesWithSetPoint.Add(tube);
                    }
                }

                if (listParam.Any())
                {
                    dynamic tubeObj = new ExpandoObject();
                    tubeObj.id = tube.tubeId;
                    tubeObj.parameters = listParam;
                    tubeIdsWithSetPoint.Add(Guid.Parse(tube.tubeId.ToString()));
                    tubesWithSetPoint.Add(tubeObj);
                }
            }
            DateTime start = DateTime.Now.AddDays(-2).AddHours(-5);
            List<dynamic> tmp = GetRecordsLoadPretty(tubeIdsWithSetPoint, start);
            List<dynamic> rowsByDateStart = GetByDateStart(start);
            List<dynamic> rowsForInsertInDB = new List<dynamic>();
            List<dynamic> rowsForUpdateInDB = new List<dynamic>();
            foreach (var tube in tubesWithSetPoint)
            {
                foreach(var param in tube.parameters)
                {
                    List<dynamic> recordsAllDate = tmp.FindAll(x => ((x.objectId == tube.id) && (x.s1 == param.tag)));
                    if (!recordsAllDate.Any()) continue;

                    int startDay = (Int32.TryParse(param.startDay.ToString(), out int tmpStartDay)) ? tmpStartDay : 2;
                    int endDay = (Int32.TryParse(param.endDay.ToString(), out int tmpEndDay)) ? tmpEndDay : 5;
                    
                    if(Double.TryParse(param.max.ToString(), out double max))
                    {
                        List<dynamic> recordsMaxMoreSetPoint = recordsAllDate.FindAll(x => (((double)x.d1 >= max) && ((((DateTime)x.date).Hour > startDay) || (((DateTime)x.date).Hour < endDay))));
                        List<dynamic> recordsMaxLessSetPoint = recordsAllDate.FindAll(x => (((double)x.d1 < max) && ((((DateTime)x.date).Hour > startDay) || (((DateTime)x.date).Hour < endDay))));
                        Records(rowsCache, tube, param, max, "Дневной", recordsMaxMoreSetPoint, recordsMaxLessSetPoint, rowsByDateStart, rowsActiveEvent);
                    }
                    if(Double.TryParse(param.maxNight.ToString(), out double maxNight))
                    {
                        List<dynamic> recordsMaxNightMoreSetPoint = recordsAllDate.FindAll(x => (((double)x.d1 >= maxNight) && ((((DateTime)x.date).Hour <= startDay) && (((DateTime)x.date).Hour >= endDay))));
                        List<dynamic> recordsMaxNightLessSetPoint = recordsAllDate.FindAll(x => (((double)x.d1 < maxNight) && ((((DateTime)x.date).Hour <= startDay) && (((DateTime)x.date).Hour >= endDay))));
                        Records(rowsCache, tube, param, maxNight, "Ночной", recordsMaxNightMoreSetPoint, recordsMaxNightLessSetPoint, rowsByDateStart, rowsActiveEvent);
                    }
                }
            }
            var ooo = tubesWithSetPoint[0];
        }
        public void Records(List<dynamic> rowsCache, dynamic tube, dynamic param, double max, string nameParameter, List<dynamic> recordsMaxMoreSetPoint, List<dynamic> recordsMaxLessSetPoint, List<dynamic> rowsByDateStart, List<dynamic> rowsActiveEvent)
        {
            List<dynamic> rowsForInsertInDB = new List<dynamic>();
            List<dynamic> rowsForUpdateInDB = new List<dynamic>();

            List<dynamic> rowsTmp = rowsByDateStart.FindAll(x => ((x.objectId == tube.id) && (x.tag == param.tag) && (x.parameter == nameParameter)));
            List<dynamic> rowsAETmp = rowsActiveEvent.FindAll(x => ((x.objectId == tube.id) && (x.tag == param.tag) && (x.parameter == nameParameter)));
            if (rowsAETmp.Any()) // если два списка не пустые, значит есть активное событие и это событие должен быть в событиях за период, значит в этом условие работаем только с активным событием
            {
                List<dynamic> recsLessTmp = recordsMaxLessSetPoint.FindAll(x => x.date > rowsAETmp[0].dateStart);
                dynamic rowLastStart = rowsAETmp[0];
                while (recsLessTmp.Any())
                {
                    var recMinDateFromMaxLessSetPoint = recsLessTmp.Find(x => x.date == recsLessTmp.Min(y => y.date));

                    rowsForUpdateInDB.Add(RowForUpdateInDB(Guid.Parse(rowLastStart.id.ToString()), (DateTime)recMinDateFromMaxLessSetPoint.date));

                    List<dynamic> recsMoreTmp = recordsMaxMoreSetPoint.FindAll(x => x.date > recMinDateFromMaxLessSetPoint.date);
                    if (recsMoreTmp.Any())
                    {
                        var recMinDateFromMaxMoreSetPoint = recsMoreTmp.Find(x => x.date == recsMoreTmp.Min(y => y.date));
                        rowLastStart = RowForInsertInDB(rowsCache, recMinDateFromMaxMoreSetPoint, tube, param, max, nameParameter);
                        rowsForInsertInDB.Add(rowLastStart);
                        recsLessTmp.RemoveAll(x => x.date < rowLastStart.dateStart);
                    }
                    else
                    {
                        recsLessTmp.Clear();
                    }
                }
            }
            else if (rowsTmp.Any()) // активного события нет, проверяем была ли 
            {
                var rowsMaxDateFromNotActiveEvent = rowsTmp.Find(x => x.dateStart == rowsTmp.Max(y => y.dateStart));
                List<dynamic> recsMaxMoreSPAfterRowsNotAE = recordsMaxMoreSetPoint.FindAll(x => x.date > rowsMaxDateFromNotActiveEvent.dateEnd);
                dynamic rowLastStart = null;
                while (recsMaxMoreSPAfterRowsNotAE.Any())
                {
                    var recMinDateFromMaxMoreSP = recsMaxMoreSPAfterRowsNotAE.Find(x => x.date == recsMaxMoreSPAfterRowsNotAE.Min(y => y.date));
                    rowLastStart = RowForInsertInDB(rowsCache, recMinDateFromMaxMoreSP, tube, param, max, nameParameter);
                    rowsForInsertInDB.Add(rowLastStart);
                    List<dynamic> recsMaxLessSPAfterRowsNotAE = recordsMaxLessSetPoint.FindAll(x => x.date > recMinDateFromMaxMoreSP.date);
                    if (recsMaxLessSPAfterRowsNotAE.Any())
                    {
                        var recMinDateFromMaxLessSP = recsMaxLessSPAfterRowsNotAE.Find(x => x.date == recsMaxLessSPAfterRowsNotAE.Min(y => y.date));
                        rowsForUpdateInDB.Add(RowForUpdateInDB(Guid.Parse(rowLastStart.id.ToString()), (DateTime)recMinDateFromMaxLessSP.date));
                        recsMaxMoreSPAfterRowsNotAE.RemoveAll(x => x.date < recMinDateFromMaxLessSP.date);
                    }
                    else
                    {
                        recsMaxMoreSPAfterRowsNotAE.Clear();
                    }
                }
            }
            else
            {
                dynamic rowLastStart = null;
                while (recordsMaxMoreSetPoint.Any())
                {
                    var recMinDateFromMaxMoreSP = recordsMaxMoreSetPoint.Find(x => x.date == recordsMaxMoreSetPoint.Min(y => y.date));
                    rowLastStart = RowForInsertInDB(rowsCache, recMinDateFromMaxMoreSP, tube, param, max, nameParameter);
                    rowsForInsertInDB.Add(rowLastStart);
                    List<dynamic> recsMaxLessSPAfterRowsNotAE = recordsMaxLessSetPoint.FindAll(x => x.date > recMinDateFromMaxMoreSP.date);
                    if (recsMaxLessSPAfterRowsNotAE.Any())
                    {
                        var recMinDateFromMaxLessSP = recsMaxLessSPAfterRowsNotAE.Find(x => x.date == recsMaxLessSPAfterRowsNotAE.Min(y => y.date));
                        rowsForUpdateInDB.Add(RowForUpdateInDB(Guid.Parse(rowLastStart.id.ToString()), (DateTime)recMinDateFromMaxLessSP.date));
                        recordsMaxMoreSetPoint.RemoveAll(x => x.date < recMinDateFromMaxLessSP.date);
                    }
                    else
                    {
                        recordsMaxMoreSetPoint.Clear();
                    }
                }
            }
            RecordingRowsInDB(rowsForInsertInDB);
            UpdatingRowsInDB(rowsForUpdateInDB);
        }
        public dynamic RowForInsertInDB(List<dynamic> rowsCache, dynamic records, dynamic tube, dynamic parameter, double value, string nameParameter)
        {
            var rowCache = rowsCache.Find(x => x.id == tube.id);

            dynamic row = new ExpandoObject();
            row.id = Guid.NewGuid();
            row.objectId = tube.id;
            row.value = Math.Round((double)records.d1, 3);
            row.name = rowCache.name.ToString();
            row.parameter = nameParameter;
            row.tag = parameter.tag;
            row.message = $"{nameParameter}: {Math.Round((double)records.d1, 3)} > {value}";
            row.dateStart = records.date;
            return row;
        }
        public dynamic RowForUpdateInDB(Guid id, DateTime dateEnd)
        {
            dynamic row = new ExpandoObject();
            row.id = id;
            row.dateEnd = dateEnd;
            return row;
        }
        public void RecordingRowsInDB(List<dynamic> rows)
        {
            dynamic msg = Helper.BuildMessage("setpoint-recording-rows");
            msg.body.rows = rows;
            var answer = ApiConnector.Instance.SendMessage(msg);
        }
        public void UpdatingRowsInDB(List<dynamic> rows)
        {
            dynamic msg = Helper.BuildMessage("setpoint-updating-rows");
            msg.body.rows = rows;
            var answer = ApiConnector.Instance.SendMessage(msg);
        }
        public List<dynamic> GetParameters(List<Guid> tubeIds)
        {
            dynamic msg = Helper.BuildMessage("parameters-get-3");
            msg.body.tubeIds = tubeIds;
            var answer = ApiConnector.Instance.SendMessage(msg);
            return answer.body.tubes;
        }
        public List<dynamic> GetRowCache()
        {
            dynamic msg = Helper.BuildMessage("rows-get-2");
            msg.body.filter = new ExpandoObject();
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetTubeEventRows()
        {
            dynamic msg = Helper.BuildMessage("setpoint-get-active-events-dateEnd");
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetRecords(List<Guid> ids)
        {
            dynamic msg = Helper.BuildMessage("records-get");
            DateTime start = DateTime.Now.AddHours(-10);
            msg.body.targets = ids;
            msg.body.start = start;
            msg.body.end = DateTime.Now;
            msg.body.type = "Hour";
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> records = answer.body.records;
            return records;
        }
        public List<dynamic> GetByObjectIdAndDateStart(List<Guid> objectIds, DateTime dateStart)
        {
            dynamic msg = Helper.BuildMessage("setpoint-get-ids-datestart");
            msg.body.objectIds = objectIds;
            msg.body.dateStart = dateStart;
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetByDateStart(DateTime dateStart)
        {
            dynamic msg = Helper.BuildMessage("setpoint-get-datestart");
            msg.body.dateStart = dateStart;
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> rows = answer.body.rows;
            return rows;
        }
        public List<dynamic> GetRecordsLoadPretty(List<Guid> ids, DateTime start)
        {
            dynamic msg = Helper.BuildMessage("records-get-load-pretty");
            msg.body.targets = ids;
            msg.body.start = start;
            msg.body.end = DateTime.Now;
            msg.body.type = "Hour";
            var answer = ApiConnector.Instance.SendMessage(msg);
            List<dynamic> records = answer.body.records;
            return records;
        }
        private static void UpdateSetting(string key, string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (configuration.AppSettings.Settings[key] == null)
            {
                configuration.AppSettings.Settings.Add(key, value);
            }
            else
            {
                configuration.AppSettings.Settings[key].Value = value;
            }

            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }
        public Guid SessionId
        {
            get
            {
                var raw = ConfigurationManager.AppSettings.Get("sessionId-User");
                Guid sessionId = Guid.Empty;
                Guid.TryParse(raw, out sessionId);
                return sessionId;
            }
            set
            {
                UpdateSetting("sessionId-User", value.ToString());
            }
        }
        public string Login
        {
            get
            {
                var login = ConfigurationManager.AppSettings.Get(LOGIN);
                return login;
            }
        }

        public string Password
        {
            get
            {
                var password = ConfigurationManager.AppSettings.Get(PASSWORD);
                return password;
            }
        }

        public string ServerUrl
        {
            get
            {
                var serverUrl = ConfigurationManager.AppSettings.Get(URL);
                return serverUrl;
            }
        }
        public dynamic SendMessage(dynamic message)
        {
            if (SessionId != Guid.Empty)
            {
                message.head.sessionId = SessionId;
            }
            return SendByAPI(message);
        }

        public dynamic SendByAPI(dynamic message)
        {
            try
            {
                var client = new RestClient(ServerUrl);
                RestRequest request = new RestRequest(API_PATH, RestSharp.Method.POST);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(message);
                var response = client.Execute(request);
                dynamic answer = JsonConvert.DeserializeObject<ExpandoObject>(response.Content);
                return answer;
            }
            catch (Exception ex)
            {

            }
            return null;
        }
    }
}
