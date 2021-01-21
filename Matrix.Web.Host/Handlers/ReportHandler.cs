//#define NEWREPORTS

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Newtonsoft.Json.Linq;
using NLog;
using TuesPechkin;
using System.Configuration;
using System.Collections.Specialized;
using System.Net.Mail;
using System.IO;

namespace Matrix.Web.Host.Handlers
{
    class ReportHandler : IHandler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool CanAccept(string what)
        {
            return what.StartsWith("report");
        }

        public dynamic Build(Guid reportId, List<Guid> targets, DateTime start, DateTime end, Guid userId, dynamic session)
        {
            dynamic model = new ExpandoObject();

            model.targets = StructureGraph.Instance.GetRows(targets, userId).ToArray();
            model.start = start;
            model.end = end;

            var report = StructureGraph.Instance.GetNodeById(reportId, userId);
            var result = Reports.Mapper.Instance.Map(model, report.template, session);

            return result;
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;

            if (what == "reports-list")
            {
                var answer = Helper.BuildMessage(what);
                //answer.body.
                var reports = StructureGraph.Instance.GetNodesByType("Report", Guid.Parse(session.userId.ToString()));
                answer.body.reports = reports;
                return answer;
            }

            //if (what == "reports-names")
            //{
            //    var answer = Helper.BuildMessage(what);
            //    answer.body.reports = new JArray();
            //    var entities = Cache.Instance.GetEntities(session.User);
            //    foreach (var report in entities.OfType<Report>())
            //    {
            //        dynamic rep = new JObject();
            //        rep.id = report.Id;
            //        rep.name = report.Name;
            //        answer.body.reports.Add(rep);
            //    }
            //    return answer;
            //}

            if (what == "reports-save")
            {
                //var tokens = new List<dynamic>();
                var userId = Guid.Parse((string)session.userId);
                foreach (var report in message.body.reports)
                {
                    //dynamic token = new ExpandoObject();
                    //token.action = "save";
                    report.type = "Report";
                    //token.start = report;
                    //token.userId = userId;
                    //tokens.Add(token);

                    StructureGraph.Instance.SaveSingle(report, userId);
                }

                //Data.NodeBackgroundProccessor.Instance.AddTokens(tokens);
                return Helper.BuildMessage(what);
            }

            if (what == "report-build")
            {
#if (NEWREPORTS)
                try
                {
                    message.session = session;
                    var r = await bus.SyncSend(Bus.REPORTS_EXCHANGE, message);
                    logger.Debug("ушел отчет");
                    return r;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "отчет не построен");
                    return null;
                }
#endif

                Guid reportId = Guid.Parse((string)message.body.report);

                //dynamic model = new ExpandoObject();
                var targets = new List<Guid>();
                foreach (var t in message.body.targets)
                {
                    if(t is string)
                    {
                        targets.Add(Guid.Parse((string)t));
                    }
                    else if ((t is IDictionary<string, object>) && (t as IDictionary<string, object>).ContainsKey("id") && (t.id is string))
                    {
                        targets.Add(Guid.Parse((string)t.id));
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                }
                //model.targets = StructureGraph.Instance.GetRows(targets, Guid.Parse((string)session.userId)).ToArray();
                //model.start = message.body.start;
                //model.end = message.body.end;


                var result = Build(reportId, targets, (DateTime)message.body.start, (DateTime)message.body.end, Guid.Parse((string)session.userId), session);

                //var report = StructureGraph.Instance.GetNodeById(reportId, Guid.Parse((string)session.userId));// (Report)Cache.Instance.GetById(reportId, session.User);
                //var result = Reports.Mapper.Instance.Map(model, report.template, session);

                var answer = Helper.BuildMessage(what);
                answer.body.report = result.render;
                answer.body.options = result.build;
                return answer;
            }

            if (what == "report-export")
            {
#if (NEWREPORTS)
                return await bus.SyncSend(Bus.REPORTS_EXCHANGE, message);
#else
                string type = message.body.type;
                string wayToPictures = ConfigurationManager.AppSettings["way_pictures"]; // путь для рисунков в отчетах(печать и тд)

                string text = message.body.text;
                string fullText = "";
                bool isOrientationAlbum = ((message.body as IDictionary<string, object>).ContainsKey("isOrientationAlbum")) && (message.body.isOrientationAlbum is bool) && (message.body.isOrientationAlbum == true);
                if (text.Contains("\\img\\image"))
                {
                    int indexImg = text.IndexOf("\\img\\image");
                    fullText = text.Insert(indexImg, wayToPictures);
                }
                else
                {
                    fullText = text;
                }
                byte[] bytes = Html2PdfConvertor.Instance.Convert(fullText, isOrientationAlbum: isOrientationAlbum);

                logger.Debug("отчет конвертирован в pdf {0} байт", bytes.Length);

                //var p = new Pechkin.SimplePechkin(new Pechkin.GlobalConfig() { });
                //var bytes = p.Convert(text);
                var answer = Helper.BuildMessage(what);
                answer.body.bytes = bytes;
                return answer;
#endif
            }

            if (what == "report-mail-send")
            {
                var ans = Helper.BuildMessage(what);
                try
                {
                    var reportsId = Guid.Parse((string)message.body.reportid);
                    var userId = Guid.Parse(session.user.id);
                    List<Guid> tubeIds = new List<Guid>();
                    foreach (var tubeid in message.body.tubeIds)
                    {
                        tubeIds.Add(Guid.Parse((string)tubeid));
                    }
                    var dateStart = (DateTime)message.body.dateStart;
                    var dateEnd = (DateTime)message.body.dateEnd;
                    var pdfOrExcel = (string)message.body.type;
                    string text = message.body.text;
                    bool isOrientationAlbum = ((message.body as IDictionary<string, object>).ContainsKey("isOrientationAlbum")) && (message.body.isOrientationAlbum is bool) && (message.body.isOrientationAlbum == true);

                    var sent = Send(reportsId, tubeIds, dateStart, dateEnd, userId, pdfOrExcel, isOrientationAlbum, text);
                    //var sent = Send(mailerId, start, end, userId, session, period);
                    
                    ans.body.sent = sent.data;
                    ans.body.error = sent.error;
                    ans.body.success = sent.success;
                }
                catch
                {
                    ans.body.error = "Ошибка при рассылки отчета";
                    ans.body.success = false;
                }
                return ans;
            }

            return Helper.BuildMessage(what);
        }
        #region report send mail
        
        private dynamic MakeMailerRecord(Guid mailerId, DateTime start, DateTime end, int number, bool resultIsNull, string reportNames, string htmlText, string mailTo)
        {
            dynamic mData = new ExpandoObject();
            mData.id = Guid.NewGuid().ToString();
            mData.date = DateTime.Now;
            mData.objectId = mailerId.ToString();
            mData.type = DataRecordTypes.MailerType + "ByDate";
            mData.i1 = number;
            mData.i2 = resultIsNull ? 1 : 0;
            mData.s1 = htmlText; //reportNames;
            mData.s2 = reportNames; //htmlText;
            mData.s3 = mailTo;
            mData.dt1 = start;
            mData.dt2 = end;
            return mData;
        }
        public dynamic Send(Guid reportsId, List<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, Guid userId, string pdfOrExcel, bool isOrientationAlbum, string text)
        {

            dynamic ret = new ExpandoObject();
            var mailerData = new List<dynamic>();

            //подготовка ответа
            ret.data = mailerData;
            ret.success = false;
            ret.error = string.Empty;
            
            //получение объекта рассылки по id
            //var mailer = StructureGraph.Instance.GetMailerById(mailerId, userId);
            var mailer = StructureGraph.Instance.GetMailerByTubeIds(reportsId, tubeIds, userId);
            if (mailer == null)
            {
                ret.error = "Рассылка не существует/не найдена";
            }
            else
            {
                Guid mailerId = Guid.Parse((string)mailer.id);
                ret.mailerName = string.Format("[mailerId:{0}]", mailerId);

                var dmailer = mailer as IDictionary<string, object>;
                
                //ошибки до формирования отчётов
                if (!dmailer.ContainsKey("kind") || mailer.kind == "disabled")
                {
                    ret.error = "Отчет запрещен к отправке";
                }
                else if (!dmailer.ContainsKey("Report") || mailer.Report == null)
                {
                    ret.error = "Рассылка не содержит отчёта";
                }
                else if (!dmailer.ContainsKey("receiver") || (mailer.receiver == ""))
                {
                    ret.error = "Рассылка не содержит адреса получателя";
                }
                //нет ошибок
                else
                {
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
                    string wayToPictures = ConfigurationManager.AppSettings["way_pictures"]; // путь для рисунков в отчетах(печать и тд)

                    string tmpText = text.Replace("\r\n", "");
                    text = tmpText.Replace("\t", "");
                    while (text.Contains("  "))
                    {
                        tmpText = text.Replace("  ", " ");
                        text = tmpText;
                    }
                    string fullText = "";
                    if (text.Contains("\\img\\image"))
                    {
                        int indexImg = text.IndexOf("\\img\\image");
                        fullText = text.Insert(indexImg, wayToPictures);
                        
                    }
                    else
                    {
                        fullText = text;
                    }
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
                    //foreach (var report in mailer.Report)
                    {
                        reportNames.Add(mailer.Report.name.ToString());
                       
                        string periodText;
                        periodText = string.Format("{0:dd-MM-yyyy}", dateStart);
                        
                        //Прикрепление к письму отчёта в виде файла - формат excel
                        if (pdfOrExcel == "excel")
                        {
                            var template = "<html xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:excel\" xmlns=\"http://www.w3.org/TR/REC-html40\"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{0}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--></head><body>{1}</body></html>";
                            var bytes = Encoding.UTF8.GetBytes(string.Format(template, "Отчёт", fullText));
                            MemoryStream stream = new MemoryStream(bytes);
                            Attachment attachment = new Attachment(stream, string.Format("{0} - {1} ({2}).xls", mailerName, mailer.Report.name, periodText));
                            mail.Attachments.Add(attachment);
                        }
                        
                        //Прикрепление к письму отчёта в виде файла - формат pdf
                        if(pdfOrExcel == "pdf")
                        {
                            var bytes = Html2PdfConvertor.Instance.Convert(fullText, isOrientationAlbum: isOrientationAlbum);
                            MemoryStream stream = new MemoryStream(bytes);
                            Attachment attachment = new Attachment(stream, string.Format("{0} - {1} ({2}).pdf", mailerName, mailer.Report.name, periodText));
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
                        mail.Subject = string.Format("{0} ({1}) за {2}", mailerName, tubeCount, string.Format("{0:dd.MM.yyyy}", dateStart));
                        mail.Body = string.Format("Рассылка \"{0}\" ({1})\r\nОтчёт: {2}", mailerName, tubeCount, string.Join(", ", reportNames));

                        // подготовка к отправке письма
                        SmtpClient SmtpServer = new SmtpClient(smtpHost);
                        SmtpServer.Port = 587;
                        SmtpServer.Credentials = new System.Net.NetworkCredential(mailLogin, mailPass);
                        SmtpServer.EnableSsl = true;

                        try
                        {
                            //отправка письма
                            SmtpServer.Send(mail);
                            
                            //формирование результатов отправки - ежесуточные рассылки
                            mailerData.Add(MakeMailerRecord(mailerId, dateStart, dateEnd, number, resultIsNull, string.Join(", ", reportNames), text, mailTo));

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
            
            return ret;
        }
        #endregion
#if (NEWREPORTS)
        private static ReportHandler instance;

        public static void Init(Bus bus)
        {
            instance = new ReportHandler(bus);
        }

        private Bus bus;

        private ReportHandler(Bus bus)
        {
            this.bus = bus;
        }
#else
        private static readonly ReportHandler instance = new ReportHandler();
#endif
        public static ReportHandler Instance
        {
            get
            {
                return instance;
            }
        }


    }

    class Html2PdfConvertor
    {
        public byte[] Convert(string html, string title = "Report", bool isOrientationAlbum = false)
        {
            //замечание от разработчиков библиотеки
            // Keep the converter somewhere static, or as a singleton instance!
            // Do NOT run the above code more than once in the application lifecycle!

            var document = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    ProduceOutline = true,
                    DocumentTitle = title,
                    PaperSize = isOrientationAlbum? new PechkinPaperSize("297", "210") : new PechkinPaperSize("210", "297"),//PaperKind.A4, // Implicit conversion to PechkinPaperSize
                    Margins =
                    {
                        All = 1.375,
                        Unit = Unit.Centimeters
                    }
                },
                Objects = {
                        new ObjectSettings { HtmlText = html },
                    }
            };

            return converter.Convert(document);
        }

        private static readonly IConverter converter;

        private Html2PdfConvertor()
        {

        }

        static Html2PdfConvertor()
        {
            converter =
                       new ThreadSafeConverter(
                           new PdfToolset(
                               new Win32EmbeddedDeployment( //или 64 бита где как
                                   new TempFolderDeployment()))); // если крашится, то установите vc++ redist 2013 x86
        }
        private static Html2PdfConvertor instance = new Html2PdfConvertor();
        public static Html2PdfConvertor Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
