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
    class MailerHandler : IHandler
    {
        private int contractHour = 10;
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static readonly MailerHandler instance = new MailerHandler();
        public static MailerHandler Instance
        {
            get
            {
                return instance;
            }
        }

        public bool CanAccept(string what)
        {
            return what.StartsWith("mailer");
        }

        public bool IsSent(DateTime date, Guid mailerId, bool nullAllowed)
        {
            var dailyStart = date.AddDays(-1).Date.AddHours(contractHour);
            var dailyEnd = date.Date.AddHours(contractHour);
            var monthlyStart = date.Date.AddDays(1 - date.Day).AddMonths(-1);
            var monthlyEnd = date.Date.AddDays(1 - date.Day);

            //Поиск уже отправленных писем
            var alreadySent = false;
            var cache = Cache.Instance.GetRecords(dailyStart, dailyStart, "MailerDaily", new Guid[] { mailerId });

            if(date.Day == 1)
            {
                cache.ToList().AddRange(Cache.Instance.GetRecords(monthlyStart, monthlyEnd, "MailerMonthly", new Guid[] { mailerId }));
            }

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
                else //только ПОЛНЫЕ данные
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
        
        private dynamic MakeMailerRecord(Guid mailerId, string period, DateTime start, DateTime end, int number, bool resultIsNull, string reportNames, string mailSubject, string mailTo)
        {
            dynamic mData = new ExpandoObject();
            mData.id = Guid.NewGuid().ToString();
            mData.date = start;
            mData.objectId = mailerId.ToString();
            mData.type = DataRecordTypes.MailerType + period;
            mData.i1 = number;
            mData.i2 = resultIsNull ? 1 : 0;
            mData.s1 = reportNames;
            mData.s2 = mailSubject;// string.Join("\r\n", targetNameList);
            mData.s3 = mailTo;
            mData.dt1 = DateTime.Now;
            mData.dt2 = end;
            return mData;
        }

        /// <summary>
        /// Рассылка по дате; если указано 1-е число месяца, то рассылка считается месячной, иначе отправляется за сутки до указанной даты
        /// </summary>
        /// <param name="mailerId">идентификатор рассылки</param>
        /// <param name="date">день, по которому нужно осуществить рассылку; если указано 1-е число, то рассылка осуществляется за предыдущий месяц, иначе - за "вчера"</param>
        /// <param name="userId">пользователь</param>
        /// <param name="session">сессия</param>
        /// <returns></returns>
        public dynamic Send(Guid mailerId, DateTime date, Guid userId, dynamic session)
        {
            log.Trace("Начало отправки рассылки [mailerId:{0}] за {1:dd.MM.yyyy HH:mm} от [userId:{2}]", mailerId, date, userId);

            dynamic ret = new ExpandoObject();
            var mailerData = new List<dynamic>();

            //подготовка ответа
            ret.data = mailerData;
            ret.success = false;
            ret.error = string.Empty;
            ret.mailerName = string.Format("[mailerId:{0}]", mailerId);

            //получение объекта рассылки по id
            var mailer = StructureGraph.Instance.GetMailerById(mailerId, userId);
            if (mailer == null)
            {
                ret.error = "Рассылка не существует/не найдена";
            }
            else
            {
                var dmailer = mailer as IDictionary<string, object>;

                //настройки рассылки по отчётам
                var dailyReports = dmailer.ContainsKey("reportDaily") ? dmailer["reportDaily"].ToString() : "";
                var monthlyReports = dmailer.ContainsKey("reportMonthly") ? dmailer["reportMonthly"].ToString() : "";
                var specificDayReports = dmailer.ContainsKey("reportSpecificDay") ? dmailer["reportSpecificDay"].ToString() : "";
                var pdfReports = dmailer.ContainsKey("reportPdf") ? dmailer["reportPdf"].ToString() : "";
                var xlsReports = dmailer.ContainsKey("reportXls") ? dmailer["reportXls"].ToString() : "";
                var strDateWithIdsSpecificDay = dmailer.ContainsKey("dateSpecificDay") ? dmailer["dateSpecificDay"].ToString() : "";

                //флаг ежемесячной рассылки
                var isMonthly = (date.Day == 1);

                //ошибки до формирования отчётов
                if (!dmailer.ContainsKey("kind") || mailer.kind == "disabled")
                {
                    ret.error = "Запрещено к отправке";
                }
                else if (!dmailer.ContainsKey("Report") || mailer.Report == null || mailer.Report.Length == 0)
                {
                    ret.error = "Рассылка не содержит отчётов";
                }
                else if ((dailyReports == "") && (!isMonthly || (monthlyReports == "")) && (specificDayReports == ""))
                {
                    ret.error = "Рассылка не содержит отчётов нужного типа (суточный/месячный)";
                }
                else if (!dmailer.ContainsKey("receiver") || (mailer.receiver == ""))
                {
                    ret.error = "Рассылка не содержит адреса получателя";
                }
                //нет ошибок
                else
                {
                    // настройка периодов отправки для суточных, ежемесячных, суточных-по-месяцу
                    var dailyStart = date.AddDays(-1).Date.AddHours(contractHour);      //вчера, контрактный час
                    var dailyEnd = date.Date.AddHours(contractHour);                    //сегодня, контрактный час

                    var dailyMonthlyStart = date.Date.AddDays(-1).AddHours(contractHour); //отчётный час на вчера
                    dailyMonthlyStart = dailyMonthlyStart.AddDays(1 - dailyMonthlyStart.Day); //1-е число месяца
                    var dailyMonthlyEnd = dailyEnd;

                    var monthlyStart = date.Date.AddDays(1 - date.Day).AddMonths(-1);   //1-е число пред. месяца
                    var monthlyEnd = date.Date.AddDays(1 - date.Day);                   //вчера

                    //сбор данных по рассылке
                    var startNumber = dmailer.ContainsKey("lastNumber") ? (int)mailer.lastNumber : 1;
                    var number = startNumber++;

                    var nullAllowed = dmailer.ContainsKey("nullAllowed") ? (bool)mailer.nullAllowed : false;

                    var mailerName = dmailer.ContainsKey("name") ? mailer.name : "<рассылка без названия>";
                    ret.mailerName = mailerName;

                    // списки точек учёта по id и именам
                    var targetIdList = new List<Guid>();
                    var targetNameList = new List<string>();

                    if (dmailer.ContainsKey("Tube") && mailer.Tube != null)
                    {
                        foreach (var tube in mailer.Tube)
                        {
                            var dtube = tube as IDictionary<string, object>;
                            if (!dtube.ContainsKey("isDisabled") || ((tube.isDisabled is bool) && ((bool)tube.isDisabled == false)) || ((tube.isDisabled is string) && ((tube.isDisabled as string).ToLower() != "true")))
                            {
                                targetIdList.Add(Guid.Parse((string)tube.id));
                                targetNameList.Add(dtube.ContainsKey("name") ? tube.name : "?");
                            }
                        }
                    }

                    // почта - отправитель
                    var smtpHost = ConfigurationManager.AppSettings["senderServer"];    //mailer.senderServer;
                    var smtpPort = ConfigurationManager.AppSettings["senderPort"];      //mailer.senderPort;
                    var mailLogin = ConfigurationManager.AppSettings["sender"];         //mailer.sender;
                    var mailPass = ConfigurationManager.AppSettings["senderPassword"];  //mailer.senderPassword;

                    // почта - получатель
                    var mailTo = mailer.receiver;

                    //создание письма - заполнение адресов отправителя и получателя 
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(mailLogin);
                    mail.To.Add(mailTo);

                    //результат формирования отчётов
                    var resultIsNull = false;

                    var reportNames = new List<string>();

                    //формирование отчётов - вложений для письма
                    foreach (var report in mailer.Report)
                    {
                        reportNames.Add(report.name.ToString());
                        var reportId = Guid.Parse((string)report.id);
                        bool isOrientationAlbum = ((report as IDictionary<string, object>).ContainsKey("isOrientationAlbum")) && (report.isOrientationAlbum is bool) && (report.isOrientationAlbum == true);

                        dynamic result;
                        string periodText;

                        // Проверка на необходимость отправки отчёта 
                        if (!dailyReports.Contains((string)report.id) && !monthlyReports.Contains((string)report.id) && !specificDayReports.Contains((string)report.id) && !strDateWithIdsSpecificDay.Contains((string)report.id)) continue; // посуточная или ежемесячная рассылка?
                        if (!isMonthly && !dailyReports.Contains((string)report.id) && !specificDayReports.Contains((string)report.id) && !strDateWithIdsSpecificDay.Contains((string)report.id)) continue;  // следующий месяц еще не наступил?
                        var arrDateWithIdsSpecificDay = strDateWithIdsSpecificDay.Split(';');
                        int tmpDay = 1;
                        foreach (var dateWithId in arrDateWithIdsSpecificDay)
                        {
                            if (dateWithId.Contains((string)report.id)) tmpDay = Convert.ToInt32(dateWithId.Split(',')[1]);
                        }
                        //Постройка отчёта - ежемесячная рассылка
                        if (isMonthly && monthlyReports.Contains((string)report.id))
                        {
                            result = ReportHandler.Instance.Build(reportId, targetIdList, monthlyStart, monthlyEnd, userId, session);
                            periodText = string.Format("{0:MMM-yyyy}", monthlyStart);
                        }
                        else if(tmpDay > 1 && specificDayReports.Contains((string)report.id))
                        {
                            DateTime tmpDate = (tmpDay > date.Day) ? date.AddMonths(-1) : date;
                            var tmpEndDate = new DateTime(tmpDate.Year, tmpDate.Month, tmpDay, 0, 0, 0); // на указанную дату
                            var tmpStartDate = tmpEndDate.AddMonths(-1);                                 //на указанную дату минус месяц
                            result = ReportHandler.Instance.Build(reportId, targetIdList, tmpStartDate, tmpEndDate, userId, session);
                            periodText = string.Format("{0:MMM-yyyy}", tmpStartDate);
                        }
                        //Постройка отчёта - сутки за месяц
                        else if (dailyReports.Contains((string)report.id) && monthlyReports.Contains((string)report.id))
                        {
                            result = ReportHandler.Instance.Build(reportId, targetIdList, dailyMonthlyStart, dailyMonthlyEnd, userId, session);
                            periodText = string.Format("{0:dd-MM-yyyy}", dailyStart);
                        }
                        //Постройка отчёта - посуточная рассылка
                        else
                        {
                            result = ReportHandler.Instance.Build(reportId, targetIdList, dailyStart, dailyEnd, userId, session);
                            periodText = string.Format("{0:dd-MM-yyyy}", dailyStart);
                        }

                        var reportContent = result.render;

                        //проверка результатов постройки отчёта - наличие неполных данных
                        if (result.build.nullCount != 0)
                        {
                            resultIsNull |= true;
                            if (!nullAllowed)
                            {
                                break;
                            }
                        }

                        //проверка результатов постройки отчёта - наличие пустых (незначимых) отчётов
                        if (result.build.nullReport != 0)
                        {
                            continue;
                        }

                        //Прикрепление к письму отчёта в виде файла - формат excel
                        if (xlsReports.Contains((string)report.id))
                        {
                            var template = "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{0}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body>{1}</body></html>";
                            var bytes = Encoding.UTF8.GetBytes(string.Format(template, "Отчёт", (string)reportContent));
                            MemoryStream stream = new MemoryStream(bytes);
                            Attachment attachment = new Attachment(stream, string.Format("{0} - {1} ({2}).xls", mailerName, report.name, periodText));
                            mail.Attachments.Add(attachment);
                        }

                        //Прикрепление к письму отчёта в виде файла - формат pdf
                        if (!xlsReports.Contains((string)report.id) || pdfReports.Contains((string)report.id))
                        {
                            var bytes = Html2PdfConvertor.Instance.Convert(reportContent, isOrientationAlbum: isOrientationAlbum);
                            MemoryStream stream = new MemoryStream(bytes);
                            Attachment attachment = new Attachment(stream, string.Format("{0} - {1} ({2}).pdf", mailerName, report.name, periodText));
                            mail.Attachments.Add(attachment);
                        }
                    }

                    // ошибки после формирования отчётов
                    if (!nullAllowed && resultIsNull)
                    {
                        ret.error = string.Format("Рассылка остановлена, данные не полные", mailerName);
                    }
                    else if (mail.Attachments.Count == 0)
                    {
                        ret.error = string.Format("Письмо по рассылке не содержит вложений", mailerName);
                    }
                    // ошибок нет
                    else
                    {
                        // формирование письма - заполнение темы и тела письма
                        var tubeCount = string.Format("{0} точ{1} учёта", targetIdList.Count, targetIdList.Count == 1 ? "ка" : (targetIdList.Count < 5 ? "ки" : "ек"));
                        mail.Subject = string.Format("{0} ({1}) за {2}", mailerName, tubeCount, isMonthly ? string.Format("{0:MMMM yyyy}", monthlyStart) : string.Format("{0:dd.MM.yyyy}", dailyStart));
                        mail.Body = string.Format("Рассылка \"{0}\" ({1})\r\nОтчёт{3}: {2}", mailerName, tubeCount, string.Join(", ", reportNames), mailer.Report.Length > 1 ? "ы" : "");//, string.Join("\r\n", targetNameList));

                        // подготовка к отправке письма
                        SmtpClient SmtpServer = new SmtpClient(smtpHost);
                        SmtpServer.Port = 587;
                        SmtpServer.Credentials = new System.Net.NetworkCredential(mailLogin, mailPass);
                        SmtpServer.EnableSsl = true;

                        try
                        {
                            //отправка письма
                            SmtpServer.Send(mail);

                            //формирование результатов отправки - ежемесячные рассылки
                            if (isMonthly)
                            {
                                mailerData.Add(MakeMailerRecord(mailerId, "Monthly", monthlyStart, monthlyEnd, number, resultIsNull, string.Join(", ", reportNames), mail.Subject, mailTo));
                            }

                            //формирование результатов отправки - ежесуточные рассылки
                            mailerData.Add(MakeMailerRecord(mailerId, "Daily", dailyStart, dailyEnd, number, resultIsNull, string.Join(", ", reportNames), mail.Subject, mailTo));

                            ret.success = true;
                        }
                        catch (Exception ex)
                        {
                            ret.error = string.Format("Ошибка при отправке писем {0}", ex);
                        }

                        // сохранение результатов рассылки в бд
                        if (ret.success == true)
                        {
                            var records = new List<DataRecord>();
                            foreach (var raw in mailerData)
                            {
                                records.Add(EntityExtensions.ToRecord(raw));
                            }

                            if (records.Count > 0)
                            {
                                RecordAcceptor.Instance.Save(records);
                            }

                            mailer.lastNumber = startNumber;
                        }
                    }
                }
            }

            log.Debug("Результат рассылки {0} за {1:dd.MM.yyyy} - {2}", ret.mailerName, date, ret.success? "успех" : ret.error);
            log.Trace("Окончание рассылки");

            return ret;
        }

        //public List<dynamic> Send1(Guid mailerId, DateTime start, DateTime end, Guid userId, dynamic session, string period)
        //{
        //    if (period != "Daily" && period != "Monthly") return null; // неизвестный период

        //    var mailer = StructureGraph.Instance.GetMailerById(mailerId, userId);
        //    if (mailer == null) return null;

        //    var dmailer = mailer as IDictionary<string, object>;
        //    if (!dmailer.ContainsKey("kind") || mailer.kind == "disabled") return null; //запрет отправки

        //    var startNumber = dmailer.ContainsKey("lastNumber") ? (int)mailer.lastNumber : 1;
        //    var number = startNumber++;

        //    var nullAllowed = dmailer.ContainsKey("nullAllowed") ? (bool)mailer.nullAllowed : false;

        //    var mailerName = dmailer.ContainsKey("name") ? mailer.name : "<рассылка без названия>";

        //    var mailerReports = dmailer.ContainsKey("report" + period) ? dmailer["report" + period].ToString() : "";
        //    if (mailerReports == "") return null; //нет отчётов с соответствующим периодом

        //    //

        //    var targetIdList = new List<Guid>();
        //    var targetNameList = new List<string>();

        //    if (dmailer.ContainsKey("Tube") && mailer.Tube != null)
        //    {
        //        foreach (var tube in mailer.Tube)
        //        {
        //            targetIdList.Add(Guid.Parse((string)tube.id));
        //            var dtube = tube as IDictionary<string, object>;
        //            targetNameList.Add(dtube.ContainsKey("name") ? tube.name : "?");
        //        }
        //    }

        //    var smtpHost = ConfigurationManager.AppSettings["senderServer"];    //mailer.senderServer;
        //    var smtpPort = ConfigurationManager.AppSettings["senderPort"];      //mailer.senderPort;
        //    var mailLogin = ConfigurationManager.AppSettings["sender"];         //mailer.sender;
        //    var mailPass = ConfigurationManager.AppSettings["senderPassword"];  //mailer.senderPassword;

        //    if (!dmailer.ContainsKey("receiver") || mailer.receiver == "") return null;
        //    var mailTo = mailer.receiver;

        //    var mailerData = new List<dynamic>();

        //    //

        //    //var Subject = string.Format("{0}_{1:yyyyMMdd}_{2}", maquette.Sender.Inn, maquette.DateTime.DayAsDateTime, maquette.Number);
        //    var reportNames = "";

        //    MailMessage mail = new MailMessage();
        //    mail.From = new MailAddress(mailLogin);
        //    mail.To.Add(mailTo);

        //    var resultIsNull = false;

        //    if (!dmailer.ContainsKey("Report") || mailer.Report == null || mailer.Report.Length == 0) return null;

        //    foreach (var report in mailer.Report)
        //    {
        //        if (!mailerReports.Contains((string)report.id)) continue;
        //        //
        //        reportNames += (reportNames == "" ? "" : ", ") + report.name;
        //        var reportId = Guid.Parse((string)report.id);
        //        var result = ReportHandler.Instance.Build(reportId, targetIdList, start, end, userId, session);
        //        var reportContent = result.render;
        //        if (result.build.nullCount != 0)
        //        {
        //            resultIsNull |= true;
        //        }
        //        var bytes = Html2PdfConvertor.Instance.Convert(reportContent);//var bytes = Encoding.UTF8.GetBytes(reportContent);                
        //        MemoryStream stream = new MemoryStream(bytes);

        //        Attachment attachment = new Attachment(stream, string.Format("{0} - {1}.pdf", mailerName, report.name));
        //        mail.Attachments.Add(attachment);
        //    }

        //    if (mail.Attachments.Count == 0) return null;
        //    if (!nullAllowed && resultIsNull) return null;

        //    var tubeCount = string.Format("{0} точ{1} учёта", targetIdList.Count, targetIdList.Count == 1 ? "ка" : (targetIdList.Count < 5 ? "ки" : "ек"));
        //    mail.Subject = string.Format("{2}{0} ({1})", mailerName, tubeCount, period == "Monthly" ? "[МЕСЯЦ] " : "");
        //    mail.Body = string.Format("Рассылка \"{0}\" ({1})\r\nОтчёт{3}: {2}", mailerName, tubeCount, reportNames, mailer.Report.Length > 1 ? "ы" : "");//, string.Join("\r\n", targetNameList));

        //    // 

        //    SmtpClient SmtpServer = new SmtpClient(smtpHost);
        //    SmtpServer.Port = 587;
        //    SmtpServer.Credentials = new System.Net.NetworkCredential(mailLogin, mailPass);
        //    SmtpServer.EnableSsl = true;

        //    try
        //    {
        //        SmtpServer.Send(mail);

        //        dynamic mData = new ExpandoObject();
        //        mData.id = Guid.NewGuid().ToString();
        //        mData.date = start;
        //        mData.objectId = mailerId.ToString();
        //        mData.type = DataRecordTypes.MailerType + period;
        //        mData.i1 = number;
        //        mData.i2 = resultIsNull ? 1 : 0;
        //        mData.s1 = reportNames;
        //        mData.s2 = mail.Subject;// string.Join("\r\n", targetNameList);
        //        mData.s3 = mailTo;
        //        mData.dt1 = DateTime.Now;
        //        mData.dt2 = end;
        //        mailerData.Add(mData);
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ErrorFormat("Ошибка при отправке писем {0}", ex);
        //    }

        //    var records = new List<DataRecord>();
        //    foreach (var raw in mailerData)
        //    {
        //        records.Add(EntityExtensions.ToRecord(raw));
        //    }

        //    if (records.Count > 0)
        //    {
        //        RecordAcceptor.Instance.Save(records);
        //    }

        //    mailer.lastNumber = startNumber;
        //    //StructureGraph.Instance.UpdNode(mailerId, "Mailer", mailer, userId);
        //    return mailerData;
        //}

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            if (what == "mailer-list")
            {
                var objs = StructureGraph.Instance.GetMailers(Guid.Parse(session.user.id));
                var ans = Helper.BuildMessage(what);
                ans.body.mailers = objs;
                return ans;
            }

            if (what == "mailer-get")
            {
                var mailerId = Guid.Parse((string)message.body.id);
                var userId = Guid.Parse(session.user.id);
                //
                var mailer = StructureGraph.Instance.GetMailerById(mailerId, userId);
                //
                var ans = Helper.BuildMessage(what);
                ans.body.mailer = mailer;
                return ans;
            }

            if (what == "mailer-send")
            {
                var mailerId = Guid.Parse((string)message.body.id);
                var userId = Guid.Parse(session.user.id);

                var date = (DateTime)message.body.date;
                //var start = (DateTime)message.body.start;
                //var end = (DateTime)message.body.end;
                //var period = message.body.period.ToString();

                var sent = Send(mailerId, date, userId, session);
                //var sent = Send(mailerId, start, end, userId, session, period);

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
