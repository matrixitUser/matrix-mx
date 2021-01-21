using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Net;

namespace Matrix.SurveyServer.Driver.SPS
{

    public partial class Driver
    {
        int NetworkAddress = 0;
        private Func<string, DateTime> getStartDate;
        private Func<string, DateTime> getEndDate;

        #region Common
        private byte[] Send(byte[] data)
        {
            request(data);
            //log(string.Format("> {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            var buffer = new List<byte>();
            var timeout = 10000;
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
                        if (waitCollected == 2)
                        {
                            isCollected = true;
                        }
                    }
                }
            }
            //log(string.Format("< {0}", string.Join(",", buffer.Select(b => b.ToString("X2")))));
            return buffer.ToArray();
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
            record.date = date.Date.AddHours(date.Hour);
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
        #endregion

        #region ImportExport
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
        private dynamic Do(string what, dynamic arg)
        {
            var parameters = (IDictionary<string, object>)arg;

            int idmodem = 0;

            if (!parameters.ContainsKey("idmodem") || !int.TryParse(arg.idmodem.ToString(), out idmodem))
            {
                log(string.Format("отсутствутют сведения о СПС ИД"));
                return MakeResult(202, "СПС ИД");
            }
            else
            {
                log(string.Format("используется ID сканера СПС {0}", idmodem));
            }

            var cli = new WebClient();
            var str = cli.DownloadString(string.Format("http://www.spsclient.ru/sgs/all_photo_xml3.php?idmodem={0}", idmodem));

            var doc = new XmlDocument();
            doc.LoadXml(str);
            var nodes = doc.SelectNodes("//row");

            var rcds = new List<dynamic>();
            foreach (XmlNode node in nodes)
            {
                try
                {
                    var dateStr = node.Attributes["date"].Value;

                    var date = DateTime.ParseExact(dateStr, "yyyyMMddHHmmss", null);
                    if (date < DateTime.Now.Date.AddMonths(-6))
                        continue;
                    
                    var imgUrl = node.Attributes["img"].Value;
                    var imgFullUrl = string.Format("http://www.spsclient.ru/sgs/{0}", imgUrl);
                    var imgData = cli.DownloadData(imgFullUrl);
                    var imgBase64 = Convert.ToBase64String(imgData);

                    float val = 0;
                    float.TryParse(node.Attributes["pok"].Value.Replace("*", "0").Replace(".", ","), out val);

                    var foo = MakeHourRecord("показание", val, imgBase64, date);
                    foo.s3 = imgFullUrl;
                    rcds.Add(foo);


                    var battery = 0;
                    int.TryParse(node.Attributes["bat"].Value, out battery);
                    rcds.Add(MakeHourRecord("батарея", battery, "%", date));


                    var level = int.Parse(node.Attributes["csq"].Value);
                    rcds.Add(MakeHourRecord("сигнал", level, "%", date));


                    var ballance = 0f;
                    float.TryParse(node.Attributes["balans"].Value.Replace(".", ","), out ballance);
                    rcds.Add(MakeHourRecord("баланс", ballance, "руб", date));


                    var err = int.Parse(node.Attributes["er"].Value);
                    rcds.Add(MakeHourRecord("ошибка", err, "", date));

                    log(string.Format("запись за {0:dd.MM.yy HH:mm:ss} показание {1} уровень {2}", date, val, level));
                }
                catch (Exception ex)
                {
                    log(string.Format("запись не разобрана {0}", ex.Message));
                }
            }
            records(rcds);

            return MakeResult(0);
        }

        private dynamic MakeResult(int code, string description = "")
        {
            dynamic res = new ExpandoObject();
            res.code = code;
            res.description = description;
            return res;
        }

        #endregion        
    }
}
