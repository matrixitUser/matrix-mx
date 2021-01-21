using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    class Bus
    {
        private const string REPORTS_EXCHANGE = "matrix.reports";
        private const string REPORTS_QUEUE_ROUND_ROBIN = "matrix.reports";
        private const string REPORTS_RESULT_EXCHANGE = "matrix.reports.result";

        public const string REPLY_QUEUE = "amq.rabbitmq.reply-to";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private void RaiseMessageReceived(dynamic message, IBasicProperties properties)
        {
            OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message, properties));
        }

        /// <summary>
        /// отправка сообщения-ответа в шину
        /// </summary>
        /// <param name="message"></param>
        /// <param name="properties"></param>
        public void Answer(dynamic message,IBasicProperties properties)
        {
            var body = Serialize(message);
            channel.BasicPublish(exchange: "",
                                  routingKey: properties.ReplyTo,
                                  basicProperties: properties,
                                  body: body);
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
            var str = JsonConvert.SerializeObject(message, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
            logger.Trace("сериализация сообщения {0}", str);
            return Encoding.UTF8.GetBytes(str);
        }

        private dynamic Deserealize(byte[] raw)
        {
            var str = Encoding.UTF8.GetString(raw);
            logger.Trace("десериализация сообщения {0}", str);
            return JsonConvert.DeserializeObject<ExpandoObject>(str, new JsonSerializerSettings { DateTimeZoneHandling = DateTimeZoneHandling.Local });
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

            channel.ExchangeDeclare(REPORTS_RESULT_EXCHANGE, "fanout");

            channel.ExchangeDeclare(REPORTS_EXCHANGE, "direct");
            channel.QueueDeclare(REPORTS_QUEUE_ROUND_ROBIN, true, false, false, null);

            

            channel.QueueBind(REPORTS_QUEUE_ROUND_ROBIN, REPORTS_EXCHANGE, "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Trace("получено сообщение из шины, тип: {0}", message.head.what);                
                RaiseMessageReceived(message, ea.BasicProperties);
            };

            channel.BasicConsume(queue: REPORTS_QUEUE_ROUND_ROBIN,
                               noAck: true,
                               consumer: consumer);

            logger.Info("подключен к шине сообщений, очередь: {0}", REPORTS_QUEUE_ROUND_ROBIN);
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
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public dynamic Message { get; private set; }
        public IBasicProperties Properties { get; private set; }

        public MessageReceivedEventArgs(dynamic message, IBasicProperties properties)
        {
            Message = message;
            Properties = properties;
        }
    }
}
