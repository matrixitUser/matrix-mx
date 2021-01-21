using log4net;
using Matrix.PollServer.Handlers;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    /// <summary>
    /// отвечает за отправку сообщений на сервер, 
    /// следит за сессией
    /// </summary>
    class ApiConnector : IConnector
    {
        private const int FORCE_RECONNECT_INTERVAL = 10 * 60 * 1000;
        private const int CONNECTION_TIMEOUT = 5 * 60 * 1000;

        private const string PING = "ping";

        private const string SESSION_ID = "sessionId";
        private const string LOGIN = "login";
        private const string PASSWORD = "password";
        private const string URL = "serverUrl";

        private const string API_PATH = "api/transport";
        private const string SIGNALR_CONNECTOR = "messageacceptor";

        private static readonly ILog log = LogManager.GetLogger(typeof(ApiConnector));

        private bool isPingOk = false;

        public ApiConnector()
        {
            // таймер, перезапускающий соединение api, если не было пинга в течении некоторого времени
            var timer = new System.Timers.Timer();
            timer.Elapsed += (se, ea) =>
            {
                Relogin();

                //

                bool pok = isPingOk;
                isPingOk = false;
                log.Trace($"проверка наличия пинга: connection={(connection?.State.ToString() ?? "<NULL>")} пинг {(pok ? "OK" : "error")}");

                if (connection == null) return;
                if (connection.State == ConnectionState.Connected)
                {
                    if (pok)
                    {
                        return;
                    }
                }
                Subscribe();
            };

            int connectionTimeoutSec;
            if (!int.TryParse(ConfigurationManager.AppSettings["connection-timeout"]?.ToLower().Trim(), out connectionTimeoutSec) || connectionTimeoutSec < 0)
            {
                connectionTimeoutSec = CONNECTION_TIMEOUT / 1000;
            }

            if (connectionTimeoutSec == 0) return;
            timer.Interval = connectionTimeoutSec * 1000;
            timer.Start();
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
                var raw = ConfigurationManager.AppSettings.Get(SESSION_ID);
                Guid sessionId = Guid.Empty;
                Guid.TryParse(raw, out sessionId);
                return sessionId;
            }
            set
            {
                UpdateSetting(SESSION_ID, value.ToString());
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
        
        public dynamic SendMessage(dynamic message, Guid sessionId)
        {
            message.head.sessionId = sessionId;
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
                // log.Debug(string.Format("отправлено сообщение : {0}", message.head.what));
                dynamic answer = JsonConvert.DeserializeObject<ExpandoObject>(response.Content);
                //  log.Debug(string.Format("получено сообщение : {0}", answer == null ? "null" : answer.head.what));
                return answer;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
            return null;
        }

        private Connection connection = null;

        private void TryToConnect()
        {
            if (connection.State == ConnectionState.Connected)
            {
                connection.Stop();
            }
            else
            {
                connection.Start().ContinueWith(task =>
                {
                    if (!task.IsFaulted)
                    {
                        log.Warn(string.Format("старт соединения приема сообщений cid={0}", connection.ConnectionId));
                        var msg = Helper.BuildMessage("signal-bind");
                        msg.body.connectionId = connection.ConnectionId;
                        SendMessage(msg);
                    }
                });
            }
        }

        public void Subscribe()
        {
            if (connection == null)
            {
                connection = new Connection(string.Format("{0}/{1}", ServerUrl, SIGNALR_CONNECTOR));

                connection.Reconnected += () =>
                {
                    log.Warn(string.Format("рестарт соединения приема сообщений cid={0}", connection.ConnectionId));
                    var msg = Helper.BuildMessage("signal-bind");
                    msg.body.connectionId = connection.ConnectionId;
                    SendMessage(msg);
                };

                connection.Closed += () =>
                {
                    log.Error("соединение приёма сообщений закрыто");
                    TryToConnect();
                };

                connection.Error += (ex) =>
                {
                    log.Error(string.Format("ошибка соединения приёма сообщений cid={0}: {1}", connection.ConnectionId, ex));
                    TryToConnect();
                };

                connection.Received += (obj) => Task.Factory.StartNew(() => OnDataReceived(obj));
            }

            TryToConnect();
        }

        private void OnDataReceived(string obj)
        {
            try
            {
                #region PING
                if (obj == PING) return;
                //log.Error(string.Format("получен какой-то запрос {0}", obj));
                dynamic message = JsonConvert.DeserializeObject<ExpandoObject>(obj);
                string what = message.head.what;
                if (what == "ping")
                {
                    log.Trace("пришел пинг от сервера");
                    isPingOk = true;
                    return;
                }
                #endregion
                var handler = HandlerManager.Instance.Get(what);
                if (handler == null) return;
                handler.Handle(message);
            }
            catch (Exception ex)
            {
                log.Error("ошибка при приеме сообщения", ex);
            }
        }

        public bool Relogin()
        {
            if (SessionId != Guid.Empty)
            {
                var authBySession = Helper.BuildMessage("auth-by-session");
                authBySession.body.sessionId = SessionId;
                var sessionAns = SendByAPI(authBySession);
                if (sessionAns == null) return false;

                if (sessionAns.head.what == "auth-success")
                {
                    SessionId = Guid.Parse((string)sessionAns.body.sessionId);
                    return true;
                }
            }

            var authByLogin = Helper.BuildMessage("auth-by-login");
            authByLogin.body.login = Login;
            authByLogin.body.password = Password;
            var loginAns = SendByAPI(authByLogin);

            if (loginAns == null) return false;

            if (loginAns.head.what == "auth-success")
            {
                SessionId = Guid.Parse((string)loginAns.body.sessionId);
                return true;
            }

            return false;

            //Subscribe();
        }

        public void Dispose()
        {
            connection.Stop();
            connection.Dispose();
            connection = null;
        }

        public override string ToString()
        {
            return $"id: {connection.ConnectionId}; state: {connection.State}; lastError: {connection.LastError?.Message ?? "No error"}";
        }
    }
}
