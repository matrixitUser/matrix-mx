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

namespace Matrix.ExportData
{
    public class Bus
    {
        private const string EXPORT_EXCHANGE = "export";
        private const string EXPORT_QUEUE = "export";

        private const string TO_SESSION_EXCHANGE = "to-session";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        ///private readonly string queueName;

        /// <summary>
        /// отправка сообщения таск-менеджеру
        /// </summary>
        /// <param name="message"></param>
        public void SendToSession(dynamic message)
        {
            logger.Trace("сообщение отправлено для сессии");
            var body = Serialize(message);            
            channel.BasicPublish(exchange: TO_SESSION_EXCHANGE,
                                  routingKey: "",
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

            channel.ExchangeDeclare(EXPORT_EXCHANGE, "fanout");

            //channel.QueueDeclare(queue: queueName,
            //             durable: true,
            //             exclusive: false,
            //             autoDelete: true,
            //             arguments: null);
            var da = channel.QueueDeclare();

            channel.QueueBind(da.QueueName, EXPORT_EXCHANGE, "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);
                RaiseMessageReceived(message);
            };

            channel.BasicConsume(queue: da.QueueName,
                               noAck: true,
                               consumer: consumer);

            logger.Info("подключен к шине сообщений, очередь: {0}", da.QueueName);
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
            //queueName = string.Format("{0}-{1}", EXPORT_QUEUE, Guid.NewGuid());
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
