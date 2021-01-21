using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;
using System.Diagnostics;
using Matrix.Common.Infrastructure.Protocol.Messages;
using Matrix.Domain.Entities;
using Ionic.Zip;
using System.IO;

namespace Matrix.Common.Infrastructure.Protocol
{
    /// <summary>
    /// точка соединения, 
    /// принимает и отправляет сообщения
    /// использую при этом два сокета
    /// </summary>
    public abstract class ConnectionPoint : IDisposable
    {
        /// <summary>
        /// количество байт заголовка пакета (длина)
        /// </summary>
        public const int HEADER_LENGTH = 4;
        /// <summary>
        /// максимальная длина пакета
        /// </summary>
        public const int MAX_MESSAGE_LENGTH = 1024 * 1024;//1 МБ
        /// <summary>
        /// максимальная длина данны для кусочного пакета чтобы весь пакет не превышал <see cref="MAX_MESSAGE_LENGTH"/>
        /// </summary>
        public const int MAX_DATA_LENGTH = 1024 * 1024 - 300;//1 МБ

        public Guid ConnectionId { get; protected set; }

        /// <summary>
        /// имя потока ожидающего данные от сокета
        /// </summary>
        const string IDLE_THREAD_NAME = "ConnectionPointIdleThread";

        /// <summary>
        /// размер буфера
        /// </summary>
        const int BUFFER_SIZE = 1024 * 1024;

        private static readonly ILog log = LogManager.GetLogger(typeof(ConnectionPoint));

        protected Socket socket;

        /// <summary>
        /// ждущие ответ запросы
        /// </summary>
        readonly ResponseWaiterSet responseWaiterSet = new ResponseWaiterSet(SYNC_MESSAGE_TIMEOUT);

        private readonly ISerializer serializer;
        private readonly string idleThreadName;
        internal ConnectionPoint(ISerializer serializer, string idleThreadName)
        {
            this.serializer = serializer;
            this.idleThreadName = idleThreadName;
        }

        /// <summary>
        /// происходит при разрыве соединения
        /// </summary>
        public event EventHandler Disconnected;
        private void RaiseDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        public event EventHandler<MessageReceivedEventArgs> MessageRecieved;
        protected void RaiseMessageRecieved(DoMessage message)
        {
            if (MessageRecieved != null)
            {
                //обработка поступившего сообщения происходит в фоновом потоке				
                ThreadPool.QueueUserWorkItem((arg) =>
                {
                    try
                    {
                        MessageRecieved(this, new MessageReceivedEventArgs(message));
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("ошибка при обработке сообщения {0}", message), ex);
                    }
                });
            }
        }

        public event EventHandler<MessageReceiveProgressEventArgs> MessageReceiveProgress;

        public void RaiseMessageReceiveProgress(MessageReceiveProgressEventArgs e)
        {
            if (MessageReceiveProgress != null)
                MessageReceiveProgress(this, e);
        }

        /// <summary>
        /// получает адрес удаленной точки
        /// </summary>
        /// <returns></returns>
        public string GetRemoteAddress()
        {
            var remoteAddress = "неизвестный адрес";
            try
            {
                if (socket != null)
                {
                    if (socket.RemoteEndPoint is IPEndPoint)
                    {
                        remoteAddress = (socket.RemoteEndPoint as IPEndPoint).Address.ToString();
                    }
                    else if (socket.RemoteEndPoint != null)
                    {
                        remoteAddress = socket.RemoteEndPoint.ToString();
                    }
                }
                return remoteAddress;
            }
            catch (Exception)
            {

            }
            return remoteAddress;
        }

        /// <summary>
        /// открывает соединение
        /// начинает принимать сообщения
        /// </summary>
        public void OpenConnection()
        {
            try
            {
                var idleThread = new Thread(Idle) { Name = idleThreadName };
                idleThread.Start();
            }
            catch (Exception e)
            {
                log.Error(string.Format("[{0}] не удалось начать прием сообщений", this), e);
            }
        }

        /// <summary>
        /// закрывает соединение
        /// прекращает прием сообщений
        /// </summary>
        public void CloseConnection()
        {
            try
            {
                if (socket != null)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    socket.Dispose();
                }
                socket = null;
            }
            catch (Exception e)
            {
                log.Error(string.Format("[{0}] ошибка при закрытии соединения", this), e);
            }
        }

        /// <summary>
        /// преобразует массив байт в сообщение
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected DoMessage ToMessage(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            try
            {
                return serializer.DeserializeMessage(data);
            }
            catch (Exception e)
            {
                log.Error(string.Format("[{0}] ошибка при десериализации сообщения", this), e);
            }
            return null;
        }


        #region messaging
        /// <summary>
        /// отправка сообщения с получением ответа в калбэке
        /// ВАЖНО: событие не будет райзиться
        /// </summary>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        /// <param name="onProgress"></param>
        public void SendMessage(DoMessage message, Action<DoMessage> callback, Action<double> onProgress = null)
        {
            if (message == null) return;

            //todo check message access

            if (callback != null)
            {
                responseWaiterSet.Add(message, callback);
            }
            SendMessage(message);
        }

        private const int SYNC_MESSAGE_TIMEOUT = 30000;

        /// <summary>
        /// отправка сообщения с получением ответа в возвращаемом значении
        /// ВАЖНО: событие не будет райзиться
        /// </summary>
        /// <param name="message"></param>
        /// <param name="onProgress"> </param>
        public DoMessage SendSyncMessage(DoMessage message, int timeout = -1, Action<double> onProgress = null)
        {
            if (message == null) return null;

            DoMessage result = null;
            bool hasResult = false;
            SendMessage(message, response =>
            {
                result = response;
                hasResult = true;
            }, onProgress);

            var opTimeout = SYNC_MESSAGE_TIMEOUT;
            if (timeout > 0) opTimeout = timeout;
            var step = 100;
            log.Debug(string.Format("таймаут операции {0} мс", opTimeout));
            while ((opTimeout -= step) > 0 && !hasResult)
            {
                Thread.Sleep(step);
            }

            return result;
        }
        /// <summary>
        /// отправляет сообщение на сервер
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(DoMessage message)
        {
            if (message == null) return;

            byte[] data = null;
            try
            {
                data = serializer.SerializeMessage(message);
                SendData(data);
                //log.Debug(string.Format("[{0}] сообщение {1} отправлено на точку {2}", this, message, GetRemoteAddress()));
            }
            catch (Exception e)
            {
                log.Error(string.Format("[{0}] ошибка при сериализации сообщения. Сообщение типа {1}", this, message.GetType().Name), e);
            }
        }
        #endregion

        #region transport
        private const long COMPRESS_SIZE = 1024 * 5;

        /// <summary>
        /// отправка данных на удаленый сокет
        /// </summary>
        /// <param name="data"></param>
        private void SendData(byte[] data)
        {
            if (data == null) return;
            if (socket == null) return;

            try
            {
                byte compressed = 0;
                if (data.Length > COMPRESS_SIZE)
                {
                    using (var stream = new MemoryStream())
                    {
                        using (ZipFile zipFile = new ZipFile())
                        {
                            zipFile.AddEntry("body", data);
                            zipFile.Save(stream);
                        }
                        data = stream.GetBuffer();
                        compressed = 1;
                    }
                }

                int length = data.Length;
                var lengthBytes = BitConverter.GetBytes(length);
                var checkSum = (byte)lengthBytes.Sum(b => b);
                var fulldata = lengthBytes.Concat(new byte[] { checkSum, compressed }).Concat(data).ToArray();
                socket.Send(fulldata, SocketFlags.None);
            }
            catch (Exception e)
            {
                log.Error(string.Format("[{0}] ошибка при отправке сообщения", this), e);
                RaiseDisconnected();
            }
        }

        /// <summary>
        /// ожидание входящего трафика
        /// (поток B)
        /// </summary>
        private void Idle()
        {
            log.Info(string.Format("[{0}] начало приема сообщений", this));
            //поток пришедших байт
            var stream = new List<byte>();
            try
            {
                while (true)
                {
                    Thread.Sleep(300);
                    var buffer = new byte[BUFFER_SIZE];
                    var readed = socket.Receive(buffer, SocketFlags.None);
                    if (readed == 0)
                    {
                        //todo возможно это говорит о потере соединения, нужно проверить					
                        break;
                    }

                    var readedBuffer = new byte[readed];
                    Array.Copy(buffer, readedBuffer, readed);
                    stream.AddRange(readedBuffer);
                    //stream.AddRange(buffer.Take(readed).ToList());
                    stream = GetPackage(stream).ToList();
                }
            }
            catch (Exception)
            {
                log.Info(string.Format("[{0}] соединение с {1} разорвано", this, GetRemoteAddress()));

            }
            CloseConnection();
            RaiseDisconnected();
        }

        /// <summary>
        /// вычленяет пакет из потока байт
        /// </summary>
        /// <param name="stream"></param>		
        /// <returns>возвращает остаток от неполного пакета</returns>
        private IEnumerable<byte> GetPackage(IEnumerable<byte> stream)
        {
            try
            {
                //1. проверка на наличие полных пакетов в потоке
                if (stream.Count() <= HEADER_LENGTH + 1) return stream;
                byte[] header = stream.Take(HEADER_LENGTH).ToArray();

                byte checkSum = stream.ElementAt(HEADER_LENGTH);
                byte calcCheckSum = (byte)header.Sum(b => b);
                if (checkSum != calcCheckSum) return new byte[] { };

                int packageLength = (int)BitConverter.ToUInt32(header, 0);
                if (stream.Count() < packageLength + 1) return stream;

                var compressed = stream.ElementAt(HEADER_LENGTH + 1) == 1;

                var messageBody = stream.Skip(HEADER_LENGTH + 1 + 1).Take(packageLength).ToArray();

                if (compressed)
                {
                    using (var msgStream = new MemoryStream(messageBody))
                    {
                        using (ZipFile zipFile = ZipFile.Read(msgStream))
                        {
                            foreach (var zipEntry in zipFile.Entries)
                            {
                                if (zipEntry.FileName == "body")
                                {
                                    MemoryStream entryStream = new MemoryStream();
                                    zipEntry.Extract(entryStream);
                                    messageBody = entryStream.GetBuffer();
                                    break;
                                }
                            }
                        }
                    }
                }

                //2. обработка пакета
                DoMessage message = ToMessage(messageBody);
                //System.IO.File.AppendAllText(@"d:\foo.txt", string.Format("{0:HH:mm:ss}\t{1}\t{2}\tIN\t{3}\n", DateTime.Now, idleThreadName, instance, message));
                //log.DebugFormat("[{0}] на точку соединения поступило сообщение {1}", this, message);

                ThreadPool.QueueUserWorkItem(arg =>
                {
                    //if (message is ServiceMessage)
                    //{
                    //    ProcessServerMessage(message as ServiceMessage);
                    //}
                    //else
                    //{
                    ProcessMessage(message);
                    //}
                });

                //3. поиск остальных пакетов в потоке
                return GetPackage(stream.Skip(HEADER_LENGTH + 1 + 1 + packageLength));
            }
            catch (Exception ex)
            {
                return new byte[] { };
            }
        }

        private void ProcessMessage(DoMessage message)
        {
            if (message == null) return;

            if (responseWaiterSet.Has(message.Id))
            {
                //вызываем callback ожидающего
                var callback = responseWaiterSet.Get(message.Id);
                if (callback != null) callback(message);
            }
            else
            {
                RaiseMessageRecieved(message);
            }
        }

        #endregion

        #region IDisposable
        public virtual void Dispose()
        {
            CloseConnection();
        }
        #endregion

        public override string ToString()
        {
            return string.Format("точка соединения с {0} [{1}]", GetRemoteAddress(), idleThreadName);
        }

    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public DoMessage Message { get; private set; }

        public MessageReceivedEventArgs(DoMessage message)
        {
            Message = message;
        }
    }

    public class MessageReceiveProgressEventArgs : EventArgs
    {
        public double Percent { get; private set; }
        public Guid MessageId { get; private set; }

        public MessageReceiveProgressEventArgs(double percent, Guid messageId)
        {
            Percent = percent;
            MessageId = messageId;
        }
    }

    /// <summary>
    /// ожидающие ответа запросы
    /// </summary>
    class ResponseWaiterSet
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ResponseWaiterSet));

        private readonly int timeout;

        private Dictionary<Guid, Action<DoMessage>> callbacks = new Dictionary<Guid, Action<DoMessage>>();

        public ResponseWaiterSet(int timeout)
        {
            this.timeout = timeout;
        }

        /// <summary>
        /// добавляет запрос в число ожидающих ответа
        /// </summary>
        /// <param name="request"></param>
        /// <param name="callback"></param>
        public void Add(DoMessage request, Action<DoMessage> callback)
        {
            lock (callbacks)
            {
                if (callbacks.ContainsKey(request.Id)) throw new Exception("дублирующийся код запроса");

                callbacks.Add(request.Id, callback);

                //выполняется 1 раз через TOKEN_LIFE_TIME мс. Удаляет запрос из числа ожидающих
                Timer timer = new Timer(arg =>
                {
                    lock (callbacks)
                    {
                        if (callbacks.ContainsKey(request.Id))
                        {
                            callbacks[request.Id](null);
                            callbacks.Remove(request.Id);
                            log.Debug(string.Format("каллбак для {0} был удален по таймауту", request));
                        }
                    }
                }, null, timeout, System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// возвращает каллбек ожидающего запроса
        /// и удаляет его из ожидающих
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Action<DoMessage> Get(Guid id)
        {
            lock (callbacks)
            {
                Action<DoMessage> result = null;
                if (callbacks.ContainsKey(id))
                {
                    result = callbacks[id];
                    callbacks.Remove(id);
                }
                return result;
            }
        }

        /// <summary>
        /// проверяет имеется ли ожидающий запрос
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Has(Guid id)
        {
            return callbacks.ContainsKey(id);
        }
    }
}
