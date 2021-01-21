using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Web.Host.Data;
using Matrix.Web.Host.Transport;
using System.Configuration;

namespace Matrix.Web.Host.Handlers
{
    class ManagmentHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ManagmentHandler));

        public bool CanAccept(string what)
        {
            return what.StartsWith("managment");
        }

        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            Guid userId = Guid.Parse(session.userId.ToString());

            if (false && !session.IsSuperUser)
            {
                var ans = Helper.BuildMessage(what);
                ans.body.message = "недостаточно прав";
                return ans;
            }

            if (what == "managment-get-sessions")
            {
                var ans = Helper.BuildMessage(what);
                ans.body.sessions = new List<dynamic>();

                return ans;
            }

            if (what == "managment-pollserver-reset")
            {
                var serverName = message.body.serverName;
                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    var dsession = sess as IDictionary<string, object>;
                    if (!dsession.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = dsession[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);
                }
                var ans = Helper.BuildMessage(what);
                ans.body.message = string.Format("перезагрузка сервера {0} уведомлено {1} клиентов", serverName, sessions.Count());
                return ans;
            }

            if (what == "managment-service-operation")
            {
                var ans = Helper.BuildMessage(what);
                ans.body.message = "недостаточно прав";

                if ((message is IDictionary<string, object>) && (message as IDictionary<string, object>).ContainsKey("body") && (message.body is IDictionary<string, object>))
                {
                    IDictionary<string, object> dbody = message.body as IDictionary<string, object>;
                    string password = ConfigurationManager.AppSettings["servicePassword"];

                    string operationName = null;
                    string operationAction = null;
                    string successMessage = null;
                    if (dbody.ContainsKey("operation") && (message.body.operation is string) && (message.body.operation != null) && (message.body.operation != ""))
                    {
                        operationName = message.body.operation;
                        operationAction = ConfigurationManager.AppSettings["operation_" + operationName];
                        successMessage = ConfigurationManager.AppSettings["message_" + operationName];

                        if (operationAction == null)
                        {
                            ans.body.message = "не найдена соответствующая операция в конфигурационном файле";
                        }
                        else if (successMessage == null || successMessage == "")
                        {
                            successMessage = string.Format("выполняется сервисная операция: {0}", ans.body.operation);
                        }
                    }
                    else
                    {
                        operationName = "serviceOperation";
                        operationAction = ConfigurationManager.AppSettings["serviceOperation"];
                        successMessage = "производится рестарт сервера";
                    }

                    if (password != null
                        && operationAction != null
                        && operationAction != ""
                        && dbody.ContainsKey("password")
                        && (message.body.password is string)
                        && (password == message.body.password))
                    {

                        ans.body.message = successMessage;
                        Process.Start(operationAction);

                        //Process myProcess = new Process();
                        //myProcess.StartInfo.FileName = "cmd.exe";
                        //myProcess.StartInfo.Arguments = string.Format(@"/C cd {0} & {1}", Environment.CurrentDirectory, operation);
                        //myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        //myProcess.StartInfo.CreateNoWindow = true;
                        //myProcess.Start();
                    }
                }

                return ans;
            }

            if (what == "managment-rebuild-cache")
            {
                var diagnos = "";
                var sw = new Stopwatch();
                //параметры
                sw.Start();
                var parameters = StructureGraph.Instance.GetAllParameters(userId);
                foreach (var par in parameters)
                {
                    CacheRepository.Instance.SaveParameter(par.tubeId, par);
                }
                sw.Stop();
                diagnos += string.Format("параметры перестроены за {0} мс;", sw.ElapsedMilliseconds);


                var result = new List<dynamic>();

                sw.Restart();
                var ids = StructureGraph.Instance.GetRowIds("", new Guid[] { }, userId);
                var rows = new List<dynamic>();
                const int step = 100;
                for (var offset = 0; offset < ids.Count(); offset += step)
                {
                    rows.AddRange(StructureGraph.Instance.GetRows(ids.Skip(offset).Take(step), userId));
                }
                foreach (var row in rows)
                {
                    var id = Guid.Parse(row.id.ToString());
                    CacheRepository.Instance.Set("row", id, row);
                    //CacheRepository.Instance.SetLocal("row", id, row);
                }
                sw.Stop();
                diagnos += string.Format("строки перестроены за {0} мс;", sw.ElapsedMilliseconds);

                var ans = Helper.BuildMessage(what);
                ans.body.message = diagnos;
                return ans;
            }

            if (what == "managment-kill-com")
            {
                string port = message.body.port;

                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    var dsession = sess as IDictionary<string, object>;
                    if (!dsession.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = dsession[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(message, connectionId);
                }
                var ans = Helper.BuildMessage(what);
                ans.body.message = string.Format("убийство ком порта {0}", port);
                return ans;
            }

            if (what == "managment-ping")
            {
                var ans = Helper.BuildMessage("managment-pong");
                if ((message as IDictionary<string, object>).ContainsKey("body") && (message.body is IDictionary<string, object>) && (message.body as IDictionary<string, object>).ContainsKey("message") && (message.body.message is string) && (message.body.message != ""))
                {
                    log.Info(string.Format("сообщение пинг: {0}", message.body.message));
                }
                return ans;
            }

            if (what == "managment-test")
            {
                string[] objectIds = new string[] { "15739db6-d683-45fa-92ff-7d6ff37ae2a1" };
                dynamic test = new ExpandoObject();
                test.head = new ExpandoObject();
                test.head.what = "poll";
                test.body = new ExpandoObject();
                test.body.objectIds = objectIds;
                test.body.auto = false;
                test.body.arg = new ExpandoObject();
                test.body.arg.cmd = "";
                test.body.arg.components = "Current:3;";
                test.body.arg.logLevel = "1";
                test.body.arg.onlyHoles = false;

                var sessions = CacheRepository.Instance.GetSessions();
                foreach (var sess in sessions)
                {
                    if (sess.id == session.id) continue;
                    var bag = sess as IDictionary<string, object>;
                    if (!bag.ContainsKey(SignalRConnection.SIGNAL_CONNECTION_ID)) continue;
                    var connectionId = bag[SignalRConnection.SIGNAL_CONNECTION_ID].ToString();
                    SignalRConnection.RaiseEvent(test, connectionId);
                }
            }

            return Helper.BuildMessage(what);
        }
    }
}
