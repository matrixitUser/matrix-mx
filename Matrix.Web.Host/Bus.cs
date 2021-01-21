//#define NEWREPORTS

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Matrix.Web.Host
{
#if (NEWREPORTS)
    /// <summary>
    /// шина сообщений
    /// todo можно выделить интерфейс
    /// </summary>
    public class Bus
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string SAVER_EXCHANGE = "matrix.records";
        private const string NOTIFY_EXCHANGE = "matrix.notify";
        public const string TASK_MANAGER_EXCHANGE = "matrix.taskmanager";
        public const string REPORTS_EXCHANGE = "matrix.reports";
        public const string RIGHTS_EXCHANGE = "matrix.rights";
        public const string LOG_SUBSCRIBE_EXCHANGE = "matrix.log.subscribe";

        private const string REPLY_QUEUE = "amq.rabbitmq.reply-to";

        //private const string TO_SESSION_EXCHANGE = "to-session";
        //private const string TO_SESSION_QUEUE = "to-session";

        //private const string REGISTER_HANDLER_EXCHANGE = "register-handler";
        //private const string REGISTER_HANDLER_QUEUE = "register-handler";

        private const int SYNC_MESSAGE_TIMEOUT = 1 * 60 * 1000;

        private readonly Dictionary<string, TaskCompletionSource<dynamic>> waiters = new Dictionary<string, TaskCompletionSource<dynamic>>();

        public async Task<dynamic> SyncSend(string exchange, dynamic message)
        {
            var props = channel.CreateBasicProperties();
            props.ReplyTo = REPLY_QUEUE;
            props.CorrelationId = Guid.NewGuid().ToString();

            var id = props.CorrelationId;

            var tcs = new TaskCompletionSource<dynamic>();

            var c = new CancellationTokenSource(SYNC_MESSAGE_TIMEOUT);
            c.Token.Register((ts) =>
            {
                var t = ts as TaskCompletionSource<dynamic>;
                if (t != null && t.Task.Status == TaskStatus.WaitingForActivation)
                {
                    if (waiters.ContainsKey(id))
                        waiters.Remove(id);
                    t.SetException(new Exception(string.Format("ответ на сообщение не получен за {0} мс", SYNC_MESSAGE_TIMEOUT)));
                }
            }, tcs, useSynchronizationContext: false);

            logger.Trace("синхронное сообщение {0} отправлено в шину", id);

            var body = Serialize(message);
            channel.BasicPublish(exchange: exchange,
                                  routingKey: "",
                                  basicProperties: props,
                                  body: body);

            if (waiters.ContainsKey(id))
            {
                waiters[id] = tcs;
            }
            else
            {
                waiters.Add(id, tcs);
            }

            return await tcs.Task;
        }
        //public async Task<dynamic> SyncSend(string exchange, dynamic message)
        //{
        //    var msgId = Guid.NewGuid().ToString();
        //    message.head.id = msgId;
        //    var tcs = new TaskCompletionSource<dynamic>();

        //    logger.Trace("синхронное сообщение {0} отправлено в шину", msgId);

        //    var body = Serialize(message);
        //    channel.BasicPublish(exchange: exchange,
        //                          routingKey: "",
        //                          basicProperties: null,
        //                          body: body);

        //    if (waiters.ContainsKey(msgId))
        //    {
        //        waiters[msgId] = tcs;
        //    }
        //    else
        //    {
        //        waiters.Add(msgId, tcs);
        //    }

        //    return await tcs.Task;
        //    //return tcs.Task;
        //}

        public void SendRecords(IEnumerable<dynamic> records)
        {
            var message = MakeMessageStub("records-save");
            message.body.records = records;
            var body = Serialize(message);
            channel.BasicPublish(exchange: SAVER_EXCHANGE,
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);
        }

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private void RaiseMessageReceived(dynamic message)
        {
            OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        public event EventHandler<MessageReceivedEventArgs> OnNotifyMessageReceived;
        private void RaiseNotifyMessageReceived(dynamic message)
        {
            OnNotifyMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        public event EventHandler<MessageReceivedEventArgs> OnHandlerRegister;
        private void RaiseHandlerRegister(dynamic message)
        {
            if (OnHandlerRegister != null)
            {
                OnHandlerRegister(this, new MessageReceivedEventArgs(message));
            }
        }

        public void Send(string exchange, dynamic message, string route = "")
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: exchange,
                                  routingKey: route,
                                  basicProperties: null,
                                  body: body);
        }

        private byte[] Serialize(dynamic message)
        {
            var str = JsonConvert.SerializeObject(message);
            logger.Trace("сериализация сообщения {0}", str);
            return Encoding.UTF8.GetBytes(str);
        }

        private dynamic Deserealize(byte[] raw)
        {
            var str = Encoding.UTF8.GetString(raw);
            logger.Trace("десериализация сообщения {0}", str);
            return JsonConvert.DeserializeObject<ExpandoObject>(str);
        }

        private IConnection connection;
        private IModel channel;
        public void Start()
        {
            var host = ConfigurationManager.AppSettings["rabbit-host"];
            var login = ConfigurationManager.AppSettings["rabbit-login"];
            var password = ConfigurationManager.AppSettings["rabbit-password"];

            var factory = new ConnectionFactory() { HostName = host, UserName = login, Password = password };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.ExchangeDeclare(TASK_MANAGER_EXCHANGE, "direct");

            channel.ExchangeDeclare(LOG_SUBSCRIBE_EXCHANGE, "topic");

            channel.ExchangeDeclare(REPORTS_EXCHANGE, "direct");

            ///--------------
            channel.ExchangeDeclare(NOTIFY_EXCHANGE, "fanout");
            var notifyQueue = channel.QueueDeclare();

            channel.QueueBind(notifyQueue.QueueName, NOTIFY_EXCHANGE, "");

            var notifyConsumer = new EventingBasicConsumer(channel);
            notifyConsumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение-уведомление из шины");

                RaiseNotifyMessageReceived(message);
            };

            channel.BasicConsume(queue: notifyQueue.QueueName,
                                 noAck: true,
                                 consumer: notifyConsumer);
            ///----------------           

            var replyConsumer = new EventingBasicConsumer(channel);
            replyConsumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                var id = ea.BasicProperties.CorrelationId;
                if (waiters.ContainsKey(id))
                {
                    waiters[id].SetResult(message);
                    waiters.Remove(id);
                }
            };

            channel.BasicConsume(queue: REPLY_QUEUE,
                               noAck: true,
                               consumer: replyConsumer);
            
            //channel.ExchangeDeclare(TO_SESSION_EXCHANGE, "fanout");
            //channel.QueueDeclare(queue: queueName,
            //             durable: true,
            //             exclusive: false,
            //             autoDelete: true,
            //             arguments: null);

            //channel.QueueBind(queueName, TO_SESSION_EXCHANGE, "");

            /////---------------
            //channel.ExchangeDeclare(NOTIFY_EXCHANGE, "fanout");
            //channel.QueueDeclare(queue: NOTIFY_QUEUE,
            //             durable: true,
            //             exclusive: false,
            //             autoDelete: true,
            //             arguments: null);

            //channel.QueueBind(NOTIFY_QUEUE, NOTIFY_EXCHANGE, "");

            //var notifyConsumer = new EventingBasicConsumer(channel);
            //notifyConsumer.Received += (model, ea) =>
            //{
            //    var message = Deserealize(ea.Body);
            //    logger.Debug("получено сообщение-уведомление из шины");

            //    RaiseNotifyMessageReceived(message);
            //};

            //channel.BasicConsume(queue: NOTIFY_QUEUE,
            //                     noAck: true,
            //                     consumer: notifyConsumer);
            /////----------------

            //var consumer = new EventingBasicConsumer(channel);
            //consumer.Received += (model, ea) =>
            //{
            //    var message = Deserealize(ea.Body);
            //    logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);

            //    var dmsg = message as IDictionary<string, object>;

            //    if (!dmsg.ContainsKey("head"))
            //    {
            //        logger.Trace("сообщение не содержит заголовок");
            //        RaiseMessageReceived(message);
            //    }

            //    var head = message.head;
            //    var dhead = head as IDictionary<string, object>;
            //    if (!dhead.ContainsKey("id"))
            //    {
            //        logger.Trace("заголовок не содержит идентификатор (сообщение асинхронное)");
            //        RaiseMessageReceived(message);
            //    }

            //    string id = message.head.id;
            //    if (waiters.ContainsKey(id))
            //    {
            //        logger.Trace("получен ответ на сообщение {0}", id);
            //        waiters[id].TrySetResult(message);
            //        waiters.Remove(id);
            //    }
            //    else
            //    {
            //        logger.Trace("идентификатор {0} не содержится в списке ожидаемых", id);
            //        RaiseMessageReceived(message);
            //    }
            //};

            //channel.BasicConsume(queue: queueName,
            //                     noAck: true,
            //                     consumer: consumer);

            //channel.ExchangeDeclare(REGISTER_HANDLER_EXCHANGE, "fanout");
            //channel.QueueDeclare(queue: REGISTER_HANDLER_QUEUE,
            //             durable: true,
            //             exclusive: false,
            //             autoDelete: true,
            //             arguments: null);

            //channel.QueueBind(REGISTER_HANDLER_QUEUE, REGISTER_HANDLER_EXCHANGE, "");

            //var registerConsumer = new EventingBasicConsumer(channel);
            //registerConsumer.Received += (model, ea) =>
            //{
            //    var message = Deserealize(ea.Body);
            //    logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);


            //};

            //channel.BasicConsume(queue: REGISTER_HANDLER_QUEUE,
            //                     noAck: true,
            //                     consumer: registerConsumer);

            logger.Info("подключен к шине сообщений");
        }

        public void Stop()
        {
            if (channel != null)
            {
                channel.Dispose();
                channel = null;
            }
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            logger.Info("отключен от шины сообщений");
        }

        public dynamic MakeMessageStub(string what)
        {
            dynamic message = new ExpandoObject();
            message.head = new ExpandoObject();
            message.head.what = what;
            message.head.version = "0";
            message.head.isSync = false;
            message.body = new ExpandoObject();

            return message;
        }
        
        public Bus()
        {
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public dynamic Message { get; private set; }

        public MessageReceivedEventArgs(dynamic message)
        {
            Message = message;
        }
    }
#else

    /// <summary>
    /// шина сообщений
    /// todo можно выделить интерфейс
    /// </summary>
    public class Bus
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string SAVER_EXCHANGE = "records";

        private const string TO_SESSION_EXCHANGE = "to-session";
        private const string TO_SESSION_QUEUE = "to-session";

        private const string REGISTER_HANDLER_EXCHANGE = "register-handler";
        private const string REGISTER_HANDLER_QUEUE = "register-handler";

        private const string NOTIFY_EXCHANGE = "notify";
        private const string NOTIFY_QUEUE = "notify";

        private const int SYNC_MESSAGE_TIMEOUT = 5000;

        private readonly string queueName;

        private readonly Dictionary<string, TaskCompletionSource<dynamic>> waiters = new Dictionary<string, TaskCompletionSource<dynamic>>();

        public async Task<dynamic> SyncSend(string exchange, dynamic message)
        {
            var msgId = Guid.NewGuid().ToString();
            message.head.id = msgId;
            var tcs = new TaskCompletionSource<dynamic>();

            logger.Trace("синхронное сообщение {0} отправлено в шину", msgId);

            var body = Serialize(message);
            channel.BasicPublish(exchange: exchange,
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);

            if (waiters.ContainsKey(msgId))
            {
                waiters[msgId] = tcs;
            }
            else
            {
                waiters.Add(msgId, tcs);
            }

            return await tcs.Task;
            //return tcs.Task;
        }

        public void SendRecords(IEnumerable<dynamic> records)
        {
            var message = MakeMessageStub("records-save");
            message.body.records = records;
            var body = Serialize(message);
            channel.BasicPublish(exchange: SAVER_EXCHANGE,
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);
        }

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private void RaiseMessageReceived(dynamic message)
        {
            OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        public event EventHandler<MessageReceivedEventArgs> OnNotifyMessageReceived;
        private void RaiseNotifyMessageReceived(dynamic message)
        {
            OnNotifyMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        public event EventHandler<MessageReceivedEventArgs> OnHandlerRegister;
        private void RaiseHandlerRegister(dynamic message)
        {
            if (OnHandlerRegister != null)
            {
                OnHandlerRegister(this, new MessageReceivedEventArgs(message));
            }
        }

        public void Send(string exchange, dynamic message, string route = "")
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: exchange,
                                  routingKey: route,
                                  basicProperties: null,
                                  body: body);
        }

        private byte[] Serialize(dynamic message)
        {
            var str = JsonConvert.SerializeObject(message);
            logger.Trace("сериализация сообщения {0}", str);
            return Encoding.UTF8.GetBytes(str);
        }

        private dynamic Deserealize(byte[] raw)
        {
            var str = Encoding.UTF8.GetString(raw);
            logger.Trace("десериализация сообщения {0}", str);
            return JsonConvert.DeserializeObject<ExpandoObject>(str);
        }

        private IConnection connection;
        private IModel channel;
        public void Start()
        {
            var host = ConfigurationManager.AppSettings["rabbit-host"];
            var login = ConfigurationManager.AppSettings["rabbit-login"];
            var password = ConfigurationManager.AppSettings["rabbit-password"];

            bool isRecoveryEnable;
            int recoveryInterval;
            if (!bool.TryParse(ConfigurationManager.AppSettings["rabbit-recovery-enable"]?.ToLower().Trim(), out isRecoveryEnable))
            {
                isRecoveryEnable = false;
            }
            if (!int.TryParse(ConfigurationManager.AppSettings["rabbit-recovery-interval"]?.ToLower().Trim(), out recoveryInterval))
            {
                recoveryInterval = 0;
            }

            var factory = new ConnectionFactory() { HostName = host, UserName = login, Password = password };
            factory.AutomaticRecoveryEnabled = isRecoveryEnable;
            if (recoveryInterval > 0) factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(recoveryInterval);

            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.ExchangeDeclare(TO_SESSION_EXCHANGE, "fanout");
            channel.QueueDeclare(queue: queueName,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(queueName, TO_SESSION_EXCHANGE, "");

            ///---------------
            channel.ExchangeDeclare(NOTIFY_EXCHANGE, "fanout");
            channel.QueueDeclare(queue: NOTIFY_QUEUE,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(NOTIFY_QUEUE, NOTIFY_EXCHANGE, "");

            var notifyConsumer = new EventingBasicConsumer(channel);
            notifyConsumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение-уведомление из шины");

                RaiseNotifyMessageReceived(message);
            };

            channel.BasicConsume(queue: NOTIFY_QUEUE,
                                 noAck: true,
                                 consumer: notifyConsumer);
            ///----------------

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);

                var dmsg = message as IDictionary<string, object>;

                if (!dmsg.ContainsKey("head"))
                {
                    logger.Trace("сообщение не содержит заголовок");
                    RaiseMessageReceived(message);
                }

                var head = message.head;
                var dhead = head as IDictionary<string, object>;
                if (!dhead.ContainsKey("id"))
                {
                    logger.Trace("заголовок не содержит идентификатор (сообщение асинхронное)");
                    RaiseMessageReceived(message);
                }

                string id = message.head.id;
                if (waiters.ContainsKey(id))
                {
                    logger.Trace("получен ответ на сообщение {0}", id);
                    waiters[id].TrySetResult(message);
                    waiters.Remove(id);
                }
                else
                {
                    logger.Trace("идентификатор {0} не содержится в списке ожидаемых", id);
                    RaiseMessageReceived(message);
                }
            };

            channel.BasicConsume(queue: queueName,
                                 noAck: true,
                                 consumer: consumer);

            channel.ExchangeDeclare(REGISTER_HANDLER_EXCHANGE, "fanout");
            channel.QueueDeclare(queue: REGISTER_HANDLER_QUEUE,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(REGISTER_HANDLER_QUEUE, REGISTER_HANDLER_EXCHANGE, "");

            var registerConsumer = new EventingBasicConsumer(channel);
            registerConsumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);


            };

            channel.BasicConsume(queue: REGISTER_HANDLER_QUEUE,
                                 noAck: true,
                                 consumer: registerConsumer);

            logger.Info("подключен к шине сообщений, очередь: {0}", queueName);
        }

        public void Stop()
        {
            if (channel != null)
            {
                channel.Dispose();
                channel = null;
            }
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
            logger.Info("отключен от шины сообщений");
        }

        public dynamic MakeMessageStub(string what)
        {
            dynamic message = new ExpandoObject();
            message.head = new ExpandoObject();
            message.head.what = what;
            message.head.version = "0";
            message.head.isSync = false;
            message.body = new ExpandoObject();

            return message;
        }


        public dynamic MakeMessageStub(string id, string what)
        {
            dynamic message = new ExpandoObject();
            message.head = new ExpandoObject();
            message.head.id = id;
            message.head.what = what;
            message.head.version = "0";
            message.head.isSync = false;
            message.body = new ExpandoObject();

            return message;
        }

        public Bus()
        {
            queueName = string.Format("{0}-{1}", TO_SESSION_QUEUE, Guid.NewGuid());
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public dynamic Message { get; private set; }

        public MessageReceivedEventArgs(dynamic message)
        {
            Message = message;
        }
    }
#endif
}