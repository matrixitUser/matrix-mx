//#define MAQ_NEXTHOUR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.IO;
using System.Dynamic;
using System.Configuration;
using NLog;

namespace Matrix.Web.Host.Handlers
{
    class MaquetteHandler : IHandler
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static readonly MaquetteHandler instance = new MaquetteHandler();
        public static MaquetteHandler Instance
        {
            get
            {
                return instance;
            }
        }

        public bool CanAccept(string what)
        {
            return what.StartsWith("maquette");
        }

        public bool IsSent(DateTime date, Guid maquetteId, bool nullAllowed)
        {
            //Поиск уже отправленных 
            var alreadySent = false;
            var cache = Cache.Instance.GetRecords(date, date, "Maquette80020", new Guid[] { maquetteId });

            foreach (var o in cache)
            {
                if (nullAllowed == true) //любые данные 
                {
                    if (o.I2 != null) //была ли хоть одна рассылка?
                    {
                        alreadySent = true;
                        break;
                    }
                }
                else
                {
                    if (o.I2 == 0) //были ли УСПЕШНЫЕ рассылки?
                    {
                        alreadySent = true;
                        break;
                    }
                }
            }

            return alreadySent;
        }


        private string GetChannelDesc(string code)
        {
            switch (code)
            {
                case "01": return "Активный прием";
                case "02": return "Реактивный прием";
                case "03": return "Активная отдача";
                case "04": return "Реактивная отдача";
                default: return "";
            }
        }

        /// <summary>
        /// Вызывается при запросе клиентом с веба.
        /// </summary>
        /// <param name="maq"></param>
        /// <param name="days"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private dynamic Build0(dynamic maq, IEnumerable<DateTime> days, Guid userId)
        {
            dynamic ret = new ExpandoObject();
            ret.success = false;
            ret.error = string.Empty;

            var infos = new List<Matrix.Common.Maquette.Maquette80020>();

            var dmaq = maq as IDictionary<string, object>;

            //// сборка макета отключена 
            //if (dmaq.ContainsKey("disable") && maq.disable == true) return infos;

            var startNumber = dmaq.ContainsKey("lastNumber") ? (int)maq.lastNumber : 1;

            var tubes = new List<dynamic>();
            var tubeIds = new List<Guid>();
            foreach (var tube in maq.Tube)
            {
                tubes.Add(tube);
                tubeIds.Add(Guid.Parse((string)tube.id));
            }

            if(tubeIds.Count() == 0)
            {
                ret.error = "Макет не связан с точками учёта";
                return ret;
            }

            var cnt = 0;

            var hours = new List<dynamic>();
            foreach (var day in days.OrderBy(d => d))
            {
                var start = day.Date;
#if MAQ_NEXTHOUR
                var end = day.Date.AddDays(2);
#else
                var end = day.Date.AddDays(1);
#endif
                var data = Data.Cache.Instance.GetRecords(start, end, "Hour", tubeIds.ToArray());
                cnt = data.Count();
                hours.AddRange(data);
            }

            foreach (var day in days)
            {
                var m = new Matrix.Common.Maquette.Maquette80020();
                m.DateTime.DayAsDateTime = day;
                m.DateTime.TimestampAsDateTime = DateTime.Now;
                m.Number = startNumber++;
                m.Sender.Inn = maq.Inn;
                m.Sender.Name = maq.organization;

                var a = new Matrix.Common.Maquette.Area();
                m.Areas.Add(a);
                a.Inn = maq.Inn;
                a.Name = maq.organization;

                foreach (var tube in tubes)
                {
                    var dtube = tube as IDictionary<string, object>;
                    var tubeId = Guid.Parse((string)tube.id);
                    var rows = StructureGraph.Instance.GetRows(new Guid[] { tubeId }, userId);
                    var area = rows.FirstOrDefault().Area[0];

                    var mp = new Matrix.Common.Maquette.MeasuringPoint();
                    mp.Code = tube.code;
                    mp.Name = string.Format("{0} {1}", area.name, tube.name);
                    var parameters = StructureGraph.Instance.GetParameters(tubeId, userId);

                    var findDuplicate = new Dictionary<string, bool>();

                    foreach (var tubeParameter in parameters)
                    {
                        var dtp = tubeParameter as IDictionary<string, object>;
                        if (!dtp.ContainsKey("tag")) continue;

                        string tag = tubeParameter.tag;
                        if (findDuplicate.ContainsKey(tag)) continue;
                        findDuplicate[tag] = true;

                        double kiu = 1.0;
                        if (dtube.ContainsKey("KTr"))
                        {
                            double.TryParse(tube.KTr.ToString(), out kiu);
                        }

                        if (new string[] { "01", "02", "03", "04", "11", "12", "13", "14" }.Contains(tag))
                        {
                            //* Было до 6/06/2017 7:25 нет нулевого часа следующего дня
                            var dayHours = hours.Where(h => h.S1 == tubeParameter.name && h.Date.Date == day && h.ObjectId == tubeId).OrderBy(h => h.Date).ToList();  
                            //var dayHours = hours.Where(h => h.S1 == tubeParameter.name && h.Date.Date >= day && h.Date.Date <= day.AddDays(2)  && h.ObjectId == tubeId).OrderBy(h => h.Date).ToList();
                            //var dayHoursNext = hours.Where(h => h.S1 == tubeParameter.name && h.Date.Date == day.AddDays(1) && h.ObjectId == tubeId).OrderBy(h => h.Date).ToList(); //z

                            //var cntHours = dayHoursNext.Count;
                            var mc = new Matrix.Common.Maquette.MeasuringChannel();
                            mp.MeasuringChannels.Add(mc);
                            mc.Code = tag;
                            mc.Name = GetChannelDesc(tag);
                            
                            for (DateTime hour = day; hour < day.AddDays(1); hour = hour.AddHours(1))
                            {
#if MAQ_NEXTHOUR
                                var hourData = dayHours.FirstOrDefault(h => h.Date == hour.AddHours(1));    // Начинается с нуля часов, на данные надо брать за +1 час 

                                //zvar tmp = "нет";
                                if (hour.Hour == 23)
                                {
                                    hourData = dayHoursNext.FirstOrDefault(h => h.Date == hour.AddHours(1));
                                    //ztmp = hour.AddHours(1).ToString("<dd.mm.yy HHmm>");
                                }
#else
                                //* Было до 6/06/2017 6:51 
                                var hourData = dayHours.FirstOrDefault(h => h.Date == hour);    
#endif
                                var p = new Matrix.Common.Maquette.Period();
                                //zp.Start = hour.ToString("dd.mm.yy HHmm") + tmp+" cntHours:"+ cntHours.ToString()+" ";                              //*  Начало периода 
                                p.Start = hour.ToString("HHmm");                              //*  Начало периода 
                                p.End = hour.AddHours(1).ToString("HHmm");                    //*  Конец периода
                                p.Value.Data = 0;
                                p.Value.Status = 1;

                                if (hourData != null)
                                {
                                    var val = hourData.D1 * kiu;
                                    p.Value.Data = Math.Round(val, 5);
                                    p.Value.Status = 0;
                                }
                                mc.Periods.Add(p);
                            }
                        }
                    }

                    if (mp.MeasuringChannels.Count == 0)
                    {
                        ret.error = "Измерительные каналы не найдены";
                        return ret;
                    }

                    a.MeasuringPoints.Add(mp);
                }
                infos.Add(m);
            }
            maq.lastNumber = startNumber;
            StructureGraph.Instance.SaveMaquette(maq);

            ret.success = true;
            ret.infos = infos;
            return ret;
        }

        /// <summary>
        /// Вызывается при автоматической рассылке.
        /// </summary>
        /// <param name="maquetteId"></param>
        /// <param name="days"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public dynamic Build1(Guid maquetteId, IEnumerable<DateTime> days, Guid userId)
        {
            log.Trace("Начало отправки макета [maquetteId:{0}] за {1:dd.MM.yyyy HH:mm} от [userId:{2}]", maquetteId, days.OrderBy(d => d).FirstOrDefault(), userId);

            dynamic ret = new ExpandoObject();
            ret.success = true;
            ret.error = string.Empty;

            ret.maquetteName = string.Format("[maquetteId:{0}]", maquetteId);

            var infos = new List<Matrix.Common.Maquette.Maquette80020>();
            
            var maq = StructureGraph.Instance.GetMaquetteById(maquetteId, userId);
            if (maq == null)
            {
                ret.error = "Макет не существует/не найден";
                ret.success = false;
            }
            else
            {
                var dmaq = maq as IDictionary<string, object>;

                var maquetteName = dmaq.ContainsKey("name") ? maq.name : "<макет без названия>";
                ret.maquetteName = maquetteName;

                //// сборка макета отключена 
                //if (dmaq.ContainsKey("disable") && maq.disable == true) return infos;

                var startNumber = dmaq.ContainsKey("lastNumber") ? (int)maq.lastNumber : 1;

                var tubes = new List<dynamic>();
                var tubeIds = new List<Guid>();
                foreach (var tube in maq.Tube)
                {
                    tubes.Add(tube);
                    tubeIds.Add(Guid.Parse((string)tube.id));
                }

                if (tubeIds.Count() == 0)
                {
                    ret.success = false;
                    ret.error = "Макет не связан с точками учёта";
                }
                else
                {
                    var hours = new List<dynamic>();
                    foreach (var day in days.OrderBy(d => d))
                    {
                        var start = day.Date;
                        var end = day.Date.AddDays(1);
                        var data = Data.Cache.Instance.GetRecords(start, end, "Hour", tubeIds.ToArray());
                        hours.AddRange(data);
                    }

                    foreach (var day in days)
                    {
                        var m = new Matrix.Common.Maquette.Maquette80020();
                        m.DateTime.DayAsDateTime = day;
                        m.DateTime.TimestampAsDateTime = DateTime.Now;
                        m.Number = startNumber++;
                        m.Sender.Inn = maq.Inn;
                        m.Sender.Name = maq.organization;

                        var a = new Matrix.Common.Maquette.Area();
                        m.Areas.Add(a);
                        a.Inn = maq.Inn;
                        a.Name = maq.organization;

                        foreach (var tube in tubes)
                        {
                            var dtube = tube as IDictionary<string, object>;
                            var tubeId = Guid.Parse((string)tube.id);
                            var rows = StructureGraph.Instance.GetRows(new Guid[] { tubeId }, userId);
                            var area = rows.FirstOrDefault().Area[0];

                            var mp = new Matrix.Common.Maquette.MeasuringPoint();
                            mp.Code = tube.code;
                            mp.Name = string.Format("{0} {1}", area.name, tube.name);
                            var parameters = StructureGraph.Instance.GetParameters(tubeId, userId);

                            var findDuplicate = new Dictionary<string, bool>();

                            foreach (var tubeParameter in parameters)
                            {
                                var dtp = tubeParameter as IDictionary<string, object>;
                                if (!dtp.ContainsKey("tag")) continue;

                                string tag = tubeParameter.tag;
                                if (findDuplicate.ContainsKey(tag)) continue;
                                findDuplicate[tag] = true;

                                double kiu = 1.0;
                                if (dtube.ContainsKey("KTr"))
                                {
                                    double.TryParse(tube.KTr.ToString(), out kiu);
                                }

                                if (new string[] { "01", "02", "03", "04", "11", "12", "13", "14" }.Contains(tag))
                                {
                                    var dayHours = hours.Where(h => h.S1 == tubeParameter.name && h.Date.Date == day && h.ObjectId == tubeId).OrderBy(h => h.Date).ToList();
                                    var mc = new Matrix.Common.Maquette.MeasuringChannel();
                                    mp.MeasuringChannels.Add(mc);
                                    mc.Code = tag;
                                    mc.Name = GetChannelDesc(tag);
                                    
                                    for (DateTime hour = day; hour < day.AddDays(1); hour = hour.AddHours(1))
                                    {
                                        var hourData = dayHours.FirstOrDefault(h => h.Date == hour);
                                        var p = new Matrix.Common.Maquette.Period();
                                        p.Start = hour.ToString("HHmm");
                                        p.End = hour.AddHours(1).ToString("HHmm");
                                        p.Value.Data = 0;
                                        p.Value.Status = 1;

                                        if (hourData != null)
                                        {
                                            var val = hourData.D1 * kiu;
                                            p.Value.Data = Math.Round(val, 5);
                                            p.Value.Status = 0;
                                        }
                                        mc.Periods.Add(p);
                                    }
                                }
                            }

                            if (mp.MeasuringChannels.Count == 0)
                            {
                                ret.error = "Измерительные каналы не найдены";
                                ret.success = false;
                                break;
                            }
                            else
                            {
                                a.MeasuringPoints.Add(mp);
                            }
                        }

                        if(ret.success)
                        {
                            infos.Add(m);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (ret.success)
                    {
                        maq.lastNumber = startNumber;
                        StructureGraph.Instance.SaveMaquette(maq);
                    }
                }

            }

            log.Debug("Результат отправки макетов {0} за {1:dd.MM.yyyy} - {2}", ret.maquetteName, days.OrderBy(d => d).FirstOrDefault(), ret.success ? "успех" : ret.error);
            log.Trace("Окончание отправки макетов");

            ret.infos = infos;
            return ret;
        }


        public dynamic SendBuilt(Guid maquetteId, dynamic built, Guid userId)
        {
            dynamic ret = new ExpandoObject();
            var maquetteData = new List<dynamic>();

            ret.data = maquetteData;
            ret.success = false;
            ret.error = string.Empty;

            if (!built.success)
            {
                ret.error = built.error;
                return ret;
            }

            var maq = StructureGraph.Instance.GetMaquetteById(maquetteId, userId);
            if (maq == null)
            {
                ret.error = "Макет не существует/не найден";
                return ret; //запрет отправки
            }

            var dmaq = maq as IDictionary<string, object>;

            if ((dmaq.ContainsKey("kind") && maq.kind == "disabled") || (dmaq.ContainsKey("disable") && maq.disable == true))
            {
                ret.error = "Запрещено к отправке";
                return ret; //запрет отправки
            }
            if (!dmaq.ContainsKey("receiver") || (maq.receiver == ""))
            {
                ret.error = "Рассылка не содержит адреса получателя";
                return ret; // нет адреса получателя
            }
            
            //

            var maquettes = built.infos;

            //

            var smtpHost = ConfigurationManager.AppSettings["senderServer"];
            var smtpPort = ConfigurationManager.AppSettings["senderPort"];
            var mailLogin = ConfigurationManager.AppSettings["sender"];
            var mailPass = ConfigurationManager.AppSettings["senderPassword"];

            var maqName = dmaq.ContainsKey("name") ? maq.name : "<макет без названия>";
            var nullAllowed = dmaq.ContainsKey("nullAllowed") ? (bool)maq.nullAllowed : false;

            //

            var mailTo = maq.receiver;

            foreach (var maquette in maquettes)
            {
                var Subject = string.Format("{0}_{1:yyyyMMdd}_{2}", maquette.Sender.Inn, maquette.DateTime.DayAsDateTime, maquette.Number);

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtpHost);
                mail.From = new MailAddress(mailLogin);
                mail.To.Add(mailTo);
                mail.Subject = Subject;
                mail.Body = maquette.FileName;
                var maquetteContent = "";
                //var bytes = Encoding.GetEncoding(1251).GetBytes(maquetteContent);
                byte[] bytes;
                if (maquette.Sender.Inn == "0278152599") // если это "Открытые инвестиции", то отправить с кодировкой windows-1251
                {
                    maquetteContent = maquette.Save1251();
                    bytes = Encoding.GetEncoding(1251).GetBytes(maquetteContent);
                }
                else   // иначе Utf8
                {
                    maquetteContent = maquette.Save();
                    bytes = Encoding.UTF8.GetBytes(maquetteContent);
                }
                MemoryStream stream = new MemoryStream(bytes);

                System.Net.Mail.Attachment attachment;
                attachment = new System.Net.Mail.Attachment(stream, maquette.FileName);
                mail.Attachments.Add(attachment);

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(mailLogin, mailPass);
                SmtpServer.EnableSsl = true;

                try
                {
                    SmtpServer.Send(mail);

                    dynamic mData = new ExpandoObject();
                    mData.id = Guid.NewGuid().ToString();
                    mData.date = maquette.DateTime.DayAsDateTime;
                    mData.objectId = maquetteId.ToString();
                    mData.type = DataRecordTypes.Maquette80020Type;
                    mData.i1 = maquette.Number;
                    mData.i2 = maquette.HasNonCommercials() ? 1 : 0;
                    mData.s1 = maquetteContent;
                    mData.s2 = maquette.FileName;
                    mData.s3 = mailTo;
                    mData.dt1 = DateTime.Now;
                    maquetteData.Add(mData);
                }
                catch (Exception ex)
                {
                    ret.error = string.Format("Ошибка при отправке макетов {0}", ex);
                    return ret;
                }
            }

            var records = new List<DataRecord>();
            foreach (var raw in maquetteData)
            {
                records.Add(EntityExtensions.ToRecord(raw));
            }

            if (records.Count > 0)
            {
                RecordAcceptor.Instance.Save(records);
            }

            ret.success = true;
            return ret;
        }

        /// <summary>
        /// Отправка макета вручную.
        /// </summary>
        /// <param name="maquetteId"></param>
        /// <param name="days"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public dynamic Send(Guid maquetteId, List<DateTime> days, Guid userId)
        {
            dynamic ret = new ExpandoObject();
            var maquetteData = new List<dynamic>();

            ret.data = maquetteData;
            ret.success = false;
            ret.error = string.Empty;
            
            var maq = StructureGraph.Instance.GetMaquetteById(maquetteId, userId);
            if(maq == null)
            {
                ret.error = "Макет не существует/не найден";
                return ret; //запрет отправки
            }
            
            var dmaq = maq as IDictionary<string, object>;

            if ((dmaq.ContainsKey("kind") && maq.kind == "disabled") || (dmaq.ContainsKey("disable") && maq.disable == true))
            {
                ret.error = "Запрещено к отправке";
                return ret; //запрет отправки
            }
            if (!dmaq.ContainsKey("receiver") || (maq.receiver == ""))
            {
                ret.error = "Рассылка не содержит адреса получателя";
                return ret; // нет адреса получателя
            }

            var buildResult = Build0(maq, days, userId);
            if (!buildResult.success)
            {
                ret.error = buildResult.error;
                return ret;
            }

            //

            var maquettes = buildResult.infos;

            //

            var smtpHost = ConfigurationManager.AppSettings["senderServer"];    
            var smtpPort = ConfigurationManager.AppSettings["senderPort"];      
            var mailLogin = ConfigurationManager.AppSettings["sender"];         
            var mailPass = ConfigurationManager.AppSettings["senderPassword"];
            
            var maqName = dmaq.ContainsKey("name") ? maq.name : "<макет без названия>";
            var nullAllowed = dmaq.ContainsKey("nullAllowed") ? (bool)maq.nullAllowed : false;

            //

            var mailTo = maq.receiver;
            
            foreach (var maquette in maquettes)
            {
                var Subject = string.Format("{0}_{1:yyyyMMdd}_{2}", maquette.Sender.Inn, maquette.DateTime.DayAsDateTime, maquette.Number);

                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient(smtpHost);
                mail.From = new MailAddress(mailLogin);
                mail.To.Add(mailTo);
                mail.Subject = Subject;
                mail.Body = maquette.FileName;

                dynamic maquetteContent;
                byte[] bytes;
                if (maquette.Sender.Inn == "0278152599") // если это "Открытые инвестиции", то отправить с кодировкой windows-1251
                {
                    maquetteContent = maquette.Save1251();
                    bytes = Encoding.GetEncoding(1251).GetBytes(maquetteContent);
                }
                else   // иначе Utf8
                {
                    maquetteContent = maquette.Save();
                    bytes = Encoding.UTF8.GetBytes(maquetteContent);
                }

                MemoryStream stream = new MemoryStream(bytes);

                System.Net.Mail.Attachment attachment;
                attachment = new System.Net.Mail.Attachment(stream, maquette.FileName);
                mail.Attachments.Add(attachment);

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential(mailLogin, mailPass);
                SmtpServer.EnableSsl = true;

                try
                {
                    SmtpServer.Send(mail);

                    dynamic mData = new ExpandoObject();
                    mData.id = Guid.NewGuid().ToString();
                    mData.date = maquette.DateTime.DayAsDateTime;
                    mData.objectId = maquetteId.ToString();
                    mData.type = DataRecordTypes.Maquette80020Type;
                    mData.i1 = maquette.Number;
                    mData.i2 = maquette.HasNonCommercials() ? 1 : 0;
                    mData.s1 = maquetteContent;
                    mData.s2 = maquette.FileName;
                    mData.s3 = mailTo;
                    mData.dt1 = DateTime.Now;
                    maquetteData.Add(mData);
                }
                catch (Exception ex)
                {
                    ret.error = string.Format("Ошибка при отправке макетов {0}", ex);
                    return ret;
                }
            }

            var records = new List<DataRecord>();
            foreach (var raw in maquetteData)
            {
                records.Add(EntityExtensions.ToRecord(raw));
            }

            if (records.Count > 0)
            {
                RecordAcceptor.Instance.Save(records);
            }

            ret.success = true;
            return ret;
        }


        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            if (what == "maquette-list")
            {
                var objs = StructureGraph.Instance.GetMaquettes(Guid.Parse(session.user.id));
                var ans = Helper.BuildMessage(what);
                ans.body.maquettes = objs;
                return ans;
            }

            if (what == "maquette-get")
            {
                var id = Guid.Parse((string)message.body.id);
                var userId = Guid.Parse(session.user.id);
                //
                var m = StructureGraph.Instance.GetMaquetteById(id, userId);
                //
                var ans = Helper.BuildMessage(what);
                ans.body.maquette = m;
                return ans;
            }

            if (what == "maquette-send")
            {
                var maquetteId = Guid.Parse((string)message.body.id);
                var userId = Guid.Parse(session.user.id);

                var days = new List<DateTime>();
                foreach (var day in message.body.days)
                {
                    days.Add(day);
                }

                var sent = Send(maquetteId, days, userId);
                
                var ans = Helper.BuildMessage(what);
                ans.body.sent = sent.data;
                ans.body.error = sent.error;
                ans.body.success = sent.success;
                return ans;
            }

            return Helper.BuildMessage(what);
        }

    }
}
