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

namespace Matrix.Scheduler
{
    /// <summary>
    /// шина сообщений для расписания
    /// 
    /// </summary>
    public class Bus
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string MAILER_QUEUE = "mailer";
        private const string POLL_QUEUE = "poll";
        private const string CHECK_QUEUE = "check";
        private const string NOTIFY_EXCHANGE = "notify";
        private const string CHECK_EXCHANGE = "check";
        private readonly Dictionary<string, TaskCompletionSource<dynamic>> waiters = new Dictionary<string, TaskCompletionSource<dynamic>>();

        public void SendPoll(dynamic message)
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: NOTIFY_EXCHANGE,
                                  routingKey: POLL_QUEUE,
                                  basicProperties: null,
                                  body: body);
        }
        public void SendCheck(dynamic message)
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: CHECK_EXCHANGE,
                                  routingKey: CHECK_QUEUE,
                                  basicProperties: null,
                                  body: body);
        }
        public async Task<dynamic> Send(dynamic message)
        {
            var foo = new TaskCompletionSource<dynamic>();

            string id = message.head.id;

            var body = Serialize(message);

            channel.BasicPublish(exchange: NOTIFY_EXCHANGE,
                                  routingKey: POLL_QUEUE,
                                  basicProperties: null,
                                  body: body);

            if (message.head.isSync)
            {
                logger.Debug("сообщение {0} синхронное", message.head.what);
                if (waiters.ContainsKey(id))
                {
                    waiters[id] = foo;
                }
                else
                {
                    waiters.Add(id, foo);
                    logger.Debug("добавление ожидания ответа, всего {0}", waiters.Count);
                }
            }
            else
            {
                logger.Debug("сообщение {0} НЕ синхронное", message.head.what);
                foo.TrySetResult("yep");
            }

            return foo.Task;
        }

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private void RaiseMessageReceived(dynamic message)
        {
            if (OnMessageReceived != null)
            {
                OnMessageReceived(this, new MessageReceivedEventArgs(message));
            }
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
        private IModel channel;//, channel1;

        public void Start()
        {
            var host = ConfigurationManager.AppSettings["rabbit-host"];
            var login = ConfigurationManager.AppSettings["rabbit-login"];
            var passsword = ConfigurationManager.AppSettings["rabbit-password"];

            var factory = new ConnectionFactory() { HostName = host, UserName = login, Password = passsword };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.ExchangeDeclare("notify", "fanout");

            channel.QueueDeclare(queue: POLL_QUEUE,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(POLL_QUEUE, "notify", "");
            
            logger.Info("подключен к шине сообщений, очередь: {0}", POLL_QUEUE);
            //channel1 = connection.CreateModel();
            channel.ExchangeDeclare(CHECK_EXCHANGE, "fanout");
            channel.QueueDeclare(queue: CHECK_QUEUE,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(CHECK_QUEUE, CHECK_EXCHANGE, "");
            logger.Info("подключен к шине сообщений, очередь: {0}", CHECK_QUEUE);
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
}
