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

namespace Matrix.ModemPool
{
    public class Bus  
    {
        private const string POLL_PORT_EXCHANGE = "poll-port";
        private const string POLL_PORT_AE_EXCHANGE = "poll-port-ae";
        private const string TASK_MANAGER_QUEUE = "task-manager";
        private const string RECORDS_SAVE_EXCHANGE = "records";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string queueName;
        private readonly string portName;

        /// <summary>
        /// отправка сообщения таск-менеджеру
        /// </summary>
        /// <param name="message"></param>
        public void Send(dynamic message)
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: "",
                                  routingKey: TASK_MANAGER_QUEUE,
                                  basicProperties: null,
                                  body: body);
        }

        public void SendRejectPoll(Guid targetId, string reason)
        {
            var message = MakeMessageStub("", "poll-reject");
            message.body.targetId = targetId;
            message.body.reason = reason;
            var body = Serialize(message);
            channel.BasicPublish(exchange: "",
                                  routingKey: TASK_MANAGER_QUEUE,
                                  basicProperties: null,
                                  body: body);
        }

        public void SendRecords(IEnumerable<dynamic> records)
        {
            var message = MakeMessageStub("", "records-save");
            message.body.records = records;            
            var body = Serialize(message);
            channel.BasicPublish(exchange: RECORDS_SAVE_EXCHANGE,
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);
        }

        public void SendBeginPoll(Guid targetId, string reason)
        {
            var message = MakeMessageStub("", "poll-begin");
            message.body.targetId = targetId;
            message.body.reason = reason;
            var body = Serialize(message);
            channel.BasicPublish(exchange: "",
                                  routingKey: TASK_MANAGER_QUEUE,
                                  basicProperties: null,
                                  body: body);
        }

        public void SendCompletePoll(Guid targetId, int code, string reason)
        {
            var message = MakeMessageStub("", "poll-complete");
            message.body.targetId = targetId;
            message.body.code = code;
            message.body.reason = reason;
            var body = Serialize(message);
            channel.BasicPublish(exchange: "",
                                  routingKey: TASK_MANAGER_QUEUE,
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

            channel.ExchangeDeclare(POLL_PORT_EXCHANGE, "topic", false, false, new Dictionary<string, object> { { "alternate-exchange", POLL_PORT_AE_EXCHANGE } });

            channel.QueueDeclare(queue: queueName,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(queueName, POLL_PORT_EXCHANGE, string.Format("poll.{0}", portName.Replace(" ", "")));

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);
                RaiseMessageReceived(message);
            };

            channel.BasicConsume(queue: queueName,
                               noAck: true,
                               consumer: consumer);

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
            portName = ConfigurationManager.AppSettings["port-name"];
            queueName = string.Format("poll-port-{0}", portName);
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
