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

namespace Matrix.Spotter
{
    public class Bus
    {
        private const string SAVER_EXCHANGE = "records";

        private const string EXPORT_EXCHANGE = "export";
        private const string EXPORT_QUEUE = "export";

        private const string POLL_PORT_EXCHANGE = "poll-port";
        private const string POLL_PORT_AE_EXCHANGE = "poll-port-ae";

        private const string TASK_MANAGER_QUEUE = "task-manager";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string queueName;

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
            var portName = ConfigurationManager.AppSettings["port-name"];

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

        public dynamic MakeMessageStub(string what)
        {
            dynamic message = new ExpandoObject();
            message.head = new ExpandoObject();
            message.head.what = what;
            message.body = new ExpandoObject();

            return message;
        }

        public Bus()
        {
            queueName = string.Format("{0}-{1}", EXPORT_QUEUE, Guid.NewGuid());
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
