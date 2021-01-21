using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Configuration;

namespace Matrix.CheckServer
{
    /// <summary>
    /// подписка на сообщения из очереди poll (задания на опрос)
    /// </summary>
    public class Bus
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string POLL_QUEUE = "poll";
        private const string CHECK_QUEUE = "check";
        private const string CHECK_EXCHANGE = "check";

        private readonly Dictionary<string, TaskCompletionSource<dynamic>> waiters = new Dictionary<string, TaskCompletionSource<dynamic>>();

        public async Task<dynamic> Send(dynamic message)
        {
            var foo = new TaskCompletionSource<dynamic>();

            string id = message.head.id;

            var body = Serialize(message);

            channel.BasicPublish(exchange: "",
                                  routingKey: CHECK_QUEUE,
                                  basicProperties: null,
                                  body: body);

            if (message.head.isSync)
            {
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
                foo.TrySetResult("yep");
            }

            return foo.Task;
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

            channel.ExchangeDeclare(CHECK_EXCHANGE, "fanout");


            channel.QueueDeclare(queue: CHECK_QUEUE,
                         durable: true,
                         exclusive: false,
                         autoDelete: true,
                         arguments: null);

            channel.QueueBind(CHECK_QUEUE, CHECK_EXCHANGE, "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var message = Deserealize(ea.Body);
                    logger.Debug("получено сообщение из шины, тип: {0}", message.head.what);
                    string id = message.head.id;
                    if (waiters.ContainsKey(id))
                    {
                        waiters[id].TrySetResult(message);
                        waiters.Remove(id);
                    }
                    else
                    {
                        RaiseMessageReceived(message);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "сообщение из шины обработано с ошибкой");
                }
            };
            
            channel.BasicConsume(queue: CHECK_QUEUE,
                                 noAck: true,
                                 consumer: consumer);

            logger.Info("подключен к шине сообщений, очередь: {0}", CHECK_QUEUE);
        }

        public void Stop()
        {
            if (channel != null)
            {
                try
                {
                    channel.Dispose();
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    channel = null;
                }
            }
            if (connection != null)
            {
                try
                {
                    connection.Dispose();
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    connection = null;
                }
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

        public string GetStatus()
        {
            StringBuilder text = new StringBuilder();
            text.AppendFormat("соединение {0} ", connection.ToString());
            text.Append(connection.IsOpen ? "открыто" : "закрыто");
            text.Append(" ");
            if (connection.CloseReason != null)
            {
                text.AppendFormat(" причина: {0}", connection.CloseReason.ToString());
            }
            text.Append("; ");
            text.AppendFormat("канал {0} ", channel.ToString());
            text.Append(channel.IsOpen ? "открыт" : "закрыт");
            return text.ToString();
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
