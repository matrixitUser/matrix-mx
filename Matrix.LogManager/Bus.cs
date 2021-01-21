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

namespace Matrix.LogManager
{
    public class Bus
    {
        private const string SAVER_EXCHANGE = "records";
        private const string SAVER_QUEUE = "records";

        private const string LOG_SUBSCRIBE_EXCHANGE = "log-subscribe";
        private const string LOG_SUBSCRIBE_QUEUE = "log-subscribe";

        private const string NOTIFY_EXCHANGE = "notify";

        private static readonly Logger logger = NLog.LogManager.GetCurrentClassLogger();
        

        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;
        private void RaiseMessageReceived(dynamic message)
        {
            OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
        }

        public event EventHandler<MessageReceivedEventArgs> OnSubscribeMessageReceived;
        private void RaiseSubscribeMessageReceived(dynamic message)
        {
            OnSubscribeMessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
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











        public void SendNotify(dynamic message)
        {                        

            var body = Serialize(message);
            channel.BasicPublish(exchange: NOTIFY_EXCHANGE,
                                  routingKey: "",
                                  basicProperties: null,
                                  body: body);

        }

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

            var da = channel.QueueDeclare();            
            //channel.QueueDeclare(queue: SAVER_QUEUE,
            //             durable: true,
            //             exclusive: true,
            //             autoDelete: true,
            //             arguments: null);

            channel.QueueBind(da.QueueName, SAVER_EXCHANGE, "fanout");

            //подписка на сообщения из очереди
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Trace("получено сообщение из шины, тип: {0}", message.head.what);
                RaiseMessageReceived(message);
            };
            channel.BasicConsume(queue: da.QueueName,
                               noAck: true,
                               consumer: consumer);

            //------------------
            channel.ExchangeDeclare(LOG_SUBSCRIBE_EXCHANGE,"topic");
            channel.QueueDeclare(queue: LOG_SUBSCRIBE_QUEUE,
                         durable: true,
                         exclusive: true,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(LOG_SUBSCRIBE_QUEUE, LOG_SUBSCRIBE_EXCHANGE, "");            
            var logConsumer = new EventingBasicConsumer(channel);
            logConsumer.Received += (model, ea) =>
            {
                var message = Deserealize(ea.Body);
                logger.Trace("получено сообщение из шины, тип: {0}", message.head.what);
                RaiseSubscribeMessageReceived(message);
            };
            channel.BasicConsume(queue: LOG_SUBSCRIBE_QUEUE,
                               noAck: true,
                               consumer: logConsumer);
            //------------------



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
