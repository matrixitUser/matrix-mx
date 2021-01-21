using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Matrix.TaskManager
{
    /// <summary>
    /// шина сообщений для расписания
    /// 
    /// </summary>
    public class Bus
    {
        private const string POLL_PORT_EXCHANGE = "poll-port";
        private const string POLL_PORT_AE_EXCHANGE = "poll-port-ae";
        private const string POLL_PORT_AE_QUEUE = "poll-port-unrouted";
        private const string TASK_MANAGER_QUEUE = "task-manager";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// отправка сообщения порту
        /// </summary>
        /// <param name="message"></param>
        public void SendToPort(dynamic message, string portName)
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: POLL_PORT_EXCHANGE,
                                  routingKey: string.Format("poll.{0}", portName.Replace(" ", "")),
                                  basicProperties: null,
                                  body: body);
        }

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private void RaiseMessageReceived(dynamic message)
        {
            if (OnMessageReceived != null)
            {
                OnMessageReceived(this, new MessageReceivedEventArgs(message));
            }
        }

        public event EventHandler<MessageReceivedEventArgs> OnMessageRollback;
        private void RaiseMessageRollbacked(dynamic message)
        {
            if (OnMessageRollback != null)
            {
                OnMessageRollback(this, new MessageReceivedEventArgs(message));
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
        private IModel channel;

        public void Start()
        {
            var host = ConfigurationManager.AppSettings["rabbit-host"];
            var login = ConfigurationManager.AppSettings["rabbit-login"];
            var password = ConfigurationManager.AppSettings["rabbit-password"];

            var factory = new ConnectionFactory() { HostName = host, UserName = login, Password = password };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            //очередь таск менеджера
            //получает заявки на опрос, ответы от портов опроса о статусе опроса
            channel.QueueDeclare(queue: TASK_MANAGER_QUEUE,
                         durable: true,
                         exclusive: true,
                         autoDelete: true,
                         arguments: null);

            //подписка на сообщения из очереди
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);
                RaiseMessageReceived(message);
            };
            channel.BasicConsume(queue: TASK_MANAGER_QUEUE,
                               noAck: true,
                               consumer: consumer);


            //точка обмена для портов опроса
            channel.ExchangeDeclare(POLL_PORT_EXCHANGE, "topic", false, false, new Dictionary<string, object> { { "alternate-exchange", POLL_PORT_AE_EXCHANGE } });
            channel.ExchangeDeclare(POLL_PORT_AE_EXCHANGE, "fanout");

            channel.QueueDeclare(queue: POLL_PORT_AE_QUEUE,
                         durable: true,
                         exclusive: true,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(POLL_PORT_AE_QUEUE, POLL_PORT_AE_EXCHANGE, "");

            //подписка на непринятые портами опроса заявки
            var aeConsumer = new EventingBasicConsumer(channel);
            aeConsumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, непринятое портом опроса: {0}", message.head.what);
                RaiseMessageRollbacked(message);
            };
            channel.BasicConsume(queue: POLL_PORT_AE_QUEUE,
                               noAck: true,
                               consumer: aeConsumer);

            logger.Info("подключен к шине сообщений, очередь: {0}", TASK_MANAGER_QUEUE);
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
}
