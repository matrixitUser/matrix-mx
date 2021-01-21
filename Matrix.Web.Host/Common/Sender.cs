using Matrix.Common.Maquette;
using Matrix.Web.Host.Data;
using Matrix.Web.Host.Handlers;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Common
{
    class Sender : IDisposable
    {

        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private Sender() { }


        private static readonly Sender instance = new Sender();
        public static Sender Instance
        {
            get
            {
                return instance;
            }
        }

        public void Dispose()
        {
            //    foreach (var rule in rules)
            //    {
            //        rule.Dispose();
            //    }
        }


        /// <summary>
        /// Точка входа автоматической рассылки
        /// </summary>
        /// <param name="mailerIdsSpecial">укажите ids для точечной рассылки</param>
        public void SendMail(List<Guid> mailerIdsSpecial)
        {
            log.Trace("Начало автоматической рассылки отчётов{0}", (mailerIdsSpecial.Count > 0 ? string.Format(" по {0} спец.рассылкам", mailerIdsSpecial.Count) : ""));

            //user
            var rootuserId = StructureGraph.Instance.GetRootUser();

            dynamic rootsession = new ExpandoObject();
            dynamic rootuser = new ExpandoObject();
            rootuser.id = rootuserId.ToString();
            rootsession.user = rootuser;

            //список mailerId, по которым будет осуществляться рассылка
            List<Guid> mailerIds;

            if (mailerIdsSpecial.Count > 0) // точечная рассылка только по выбранным
            {
                mailerIds = mailerIdsSpecial;
            }
            else                            // рассылка на общих условиях
            {
                mailerIds = new List<Guid>();
                var mailers = StructureGraph.Instance.GetMailers(rootuserId);
                foreach (var m in mailers)
                {
                    mailerIds.Add(Guid.Parse(m.id.ToString()));
                }
            }

            //Статистика по письмам
            var sent = 0;       //отправлено
            var witherror = 0;  //не отправлено - ошибка отправки
            var ignored = 0;    //не отправлено и не должно было

            var targetDate = DateTime.Now.Date;

            var i = 0;
            foreach (var mailerId in mailerIds)
            {
                i++;

                var mailer = StructureGraph.Instance.GetMailerById(mailerId, rootuserId);
                var dmailer = mailer as IDictionary<string, object>;

                //по умолчанию
                if (!dmailer.ContainsKey("kind"))
                {
                    mailer.kind = "manual";
                }

                //настройки рассылки
                bool booleanParse = false;
                mailer.nullAllowed = (dmailer.ContainsKey("nullAllowed") && bool.TryParse(mailer.nullAllowed.ToString(), out booleanParse)) ? booleanParse : false;
                mailer.special = (dmailer.ContainsKey("special") && bool.TryParse(mailer.special.ToString(), out booleanParse)) ? booleanParse : false;

                //ФИЛЬТРАЦИЯ
                if ((mailer.special == true) && (mailerIdsSpecial.Count == 0))
                {
                    log.Debug("[{1}/{2}] рассылка {0} специализированная, игнорируем", mailer.name, i, mailerIds.Count);
                    ignored++;
                }
                else if (mailer.kind != "auto")  //игнор НЕ автоматической отправки
                {
                    log.Debug("[{1}/{2}] рассылка {0} НЕ по расписанию, игнорируем", mailer.name, i, mailerIds.Count);
                    ignored++;
                }
                else
                {
                    //рассылка
                    try
                    {
                        // уже была (успешная) рассылка
                        if (MailerHandler.Instance.IsSent(targetDate, mailerId, mailer.nullAllowed))
                        {
                            log.Debug("[{1}/{2}] рассылка {0} уже была произведена ранее", mailer.name, i, mailerIds.Count);
                            ignored++;
                        }
                        else
                        {
                            //отправка
                            var cursent = MailerHandler.Instance.Send(mailerId, targetDate, rootuserId, rootsession);
                            if (cursent.success == false)
                            {
                                log.Debug("[{2}/{3}] рассылка {0} не отправлена по причине: {1}", mailer.name, cursent.error, i, mailerIds.Count);
                                ignored++;
                            }
                            else
                            {
                                log.Debug("[{1}/{2}] рассылка {0} успешно отправлена", mailer.name, i, mailerIds.Count);
                                sent++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("ошибка при рассылке почты {0}: {1}", mailer.name, ex);
                        witherror++;
                    }
                }
            }

            log.Info("[{4:dd-MM-yyyy}] Писем отправлено {0}, пропущено {1}, не удалось отправить {2}, всего {3}", sent, ignored, witherror, mailerIds.Count(), targetDate);
            log.Trace("Окончание рассылки");
        }




        /// <summary>
        /// Точка входа автоматической рассылки макетов
        /// </summary>
        public void SendMaquette()
        {
            log.Trace("Начало автоматической рассылки макетов");

            //user
            var rootuserId = StructureGraph.Instance.GetRootUser();

            //dynamic rootsession = new ExpandoObject();
            //dynamic rootuser = new ExpandoObject();
            //rootuser.id = rootuserId.ToString();
            //rootsession.user = rootuser;

            //список maquetteId, по которым будет осуществляться рассылка
            var maquettes = StructureGraph.Instance.GetMaquettes(rootuserId);

            //Макеты 80020
            var day = DateTime.Now.AddDays(-1).Date;

            //Статистика по письмам
            var sent = 0;       //отправлено
            var witherror = 0;  //не отправлено - ошибка отправки
            var ignored = 0;    //не отправлено и не должно было

            var i = 0;
            foreach (var maq in maquettes)
            {
                i++;

                var dmaq = maq as IDictionary<string, object>;
                var maqId = Guid.Parse(maq.id.ToString());

                //по умолчанию
                if (!dmaq.ContainsKey("disable"))
                {
                    maq.disable = false;
                }
                if (!dmaq.ContainsKey("name"))
                {
                    maq.name = "";
                }

                if (maq.disable != false)  //игнор НЕ автоматической отправки
                {
                    log.Debug("[{1}/{2}] отправка макета {0} запрещена, игнорируем", maq.name, i, maquettes.Count());
                    ignored++;
                }
                else
                {
                    //bool booleanParse = false;
                    //maq.nullAllowed = (dmaq.ContainsKey("nullAllowed") && bool.TryParse(maq.nullAllowed.ToString(), out booleanParse)) ? booleanParse : false;

                    try
                    {
                        // пропуск, если была успешная рассылка
                        if (MaquetteHandler.Instance.IsSent(day, maqId, false))
                        {
                            log.Debug("[{1}/{2}] макет {0} уже был отправлен", maq.name, i, maquettes.Count());
                            ignored++;
                        }
                        else
                        {
                            // постройка
                            var curbuild = MaquetteHandler.Instance.Build1(maqId, new List<DateTime>() { day }, rootuserId);
                            if (curbuild.success == false)
                            {
                                log.Debug("[{2}/{3}] макет {0} не построен по причине: {1}", maq.name, curbuild.error, i, maquettes.Count());
                                ignored++;
                            }
                            else
                            {
                                var infos = (curbuild.infos as List<Maquette80020>);
                                var hasNonCommercials = infos.Where(f => f.HasNonCommercials() == true).Count() > 0;

                                // пропуск, если построен некоммерческий макет //old:  уже была рассылка на эту дату и 
                                if (hasNonCommercials)  //old: &&  MaquetteHandler.Instance.IsSent(day, maqId, true))
                                {
                                    log.Debug("[{1}/{2}] макет {0} пропущен, т.к он некоммерческий", maq.name, i, maquettes.Count());
                                    ignored++;
                                }
                                else
                                {
                                    //отправка
                                    var cursent = MaquetteHandler.Instance.SendBuilt(maqId, curbuild, rootuserId);
                                    if (cursent.success == false)
                                    {
                                        log.Debug("[{2}/{3}] макет {0} не отправлен по причине: {1}", maq.name, cursent.error, i, maquettes.Count());
                                        ignored++;
                                    }
                                    else
                                    {
                                        log.Debug("[{1}/{2}] макет {0} успешно отправлен", maq.name, i, maquettes.Count());
                                        sent++;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("ошибка при рассылке макетов {0}: {1}", maq.name, ex);
                        witherror++;
                    }
                }
            }
            log.Info("[{4:dd-MM-yyyy}] Макетов отправлено {0}, пропущено {1}, не удалось отправить {2}, всего {3}", sent, ignored, witherror, maquettes.Count(), day);
        }
    }
}
