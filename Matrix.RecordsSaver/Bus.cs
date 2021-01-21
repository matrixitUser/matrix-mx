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

namespace Matrix.RecordsSaver
{
    public class Bus
    {
        private const string SAVER_EXCHANGE = "records";
        private const string SAVER_QUEUE = "records";        

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// отправка сообщения порту
        /// </summary>
        /// <param name="message"></param>
        //public void SendToPort(dynamic message, string portName)
        //{
        //    var body = Serialize(message);
        //    channel.BasicPublish(exchange: POLL_PORT_EXCHANGE,
        //                          routingKey: string.Format("poll.{0}", portName.Replace(" ", "")),
        //                          basicProperties: null,
        //                          body: body);
        //}

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

            //точка обмена для портов опроса
            channel.ExchangeDeclare(SAVER_EXCHANGE, "fanout");

            channel.QueueDeclare(queue: SAVER_QUEUE,
                         durable: true,
                         exclusive: true,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(SAVER_QUEUE, SAVER_EXCHANGE, "");

            //подписка на сообщения из очереди
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);
                RaiseMessageReceived(message);
            };
            channel.BasicConsume(queue: SAVER_QUEUE,
                               noAck: true,
                               consumer: consumer);

            logger.Info("подключен к шине сообщений, очередь: {0}", "");
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
