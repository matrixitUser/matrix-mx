using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using log4net;

/*
namespace Matrix.Common.Infrastructure.Protocol
{
	/// <summary>
	/// приемщик точек соединения
	/// </summary>
	/// <typeparam name="T">Тип контекста прокси клиента</typeparam>
	public class ConnectionPointAcceptor<T> : IDisposable
	{
		private readonly ILog log = LogManager.GetLogger(typeof(ConnectionPointAcceptor<T>));
		private readonly ISerializer serializer = new JsonSerilizer();

		private Socket acceptSocket;



		//public event Action<ClientProxyConnectionPoint<T>, BaseMessage> MessageRecieved;
		//private void RaiseMessageRecieved(ClientProxyConnectionPoint<T> sender, BaseMessage message)
		//{
		//    if (MessageRecieved != null)
		//        MessageRecieved(sender, message);
		//}

		//public bool IsStarted { get; private set; }
		//public IEnumerable<ClientProxyConnectionPoint<T>> Clients
		//{
		//    get { return clients; }
		//}
		//private readonly List<ClientProxyConnectionPoint<T>> clients = new List<ClientProxyConnectionPoint<T>>();
		//private readonly object addClsLocker = new object();

		//private bool loop = true;
		public ConnectionPointAcceptor()
		{
			//IsStarted = false;
		}
		public void Start(int port)
		{
			//if (IsStarted) return;
			//IsStarted = true;
			var listenThread = new Thread(Accept);
			listenThread.Start(port);
			//lastUsedPort = port;
		}

		public void Stop()
		{
			try
			{
				//loop = false;
				//IsStarted = false;
				acceptSocket.Shutdown(SocketShutdown.Both);
				acceptSocket.Close();
			}
			catch (Exception e)
			{
				log.Error("Ошибка при остановке сервера", e);
			}
		}

		/// <summary>
		/// принимает входящие соединения
		/// (поток B)
		/// </summary>
		/// <param name="parameter"></param>
		private void Accept(object parameter)
		{
			int port;
			if (parameter is int)
			{
				port = (int)parameter;
			}
			else
			{
				log.Error("Ошибка при старте сервера", new ArgumentNullException("parameter", "Ожидался аргумент типа int"));
				return;
			}

			var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var localEndPoint = new IPEndPoint(IPAddress.Any, port);
			serverSocket.Bind(localEndPoint);
			serverSocket.Listen(100);
			log.InfoFormat("начало приема входящих точек соединения на порту {0}", port);
			while (true)
			{
				var clientSocket = serverSocket.Accept();

				var remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
				if (remoteEndPoint != null)
				{
					log.InfoFormat("Принято подключение от {0}", remoteEndPoint.Address);
				}
				var clientProxyConnectionPoint = new ClientProxyConnectionPoint<T>(clientSocket, serializer);
				AddClient(clientProxyConnectionPoint);
			}

			serverSocket.Dispose();
		}

		private void AddClient(ClientProxyConnectionPoint<T> client)
		{
			try
			{
				client.Disconnected += ClientOnDisconnected;
				client.MessageRecieved += ClientDataRecieved;
				lock (addClsLocker)
				{
					clients.Add(client);
				}
			}
			catch (Exception e)
			{
				log.Error("Ошибка при  добавлении клиента в список", e);
			}
		}

		private void RemoveClient(ClientProxyConnectionPoint<T> client)
		{
			try
			{
				if (client == null) return;

				client.Disconnected -= ClientOnDisconnected;
				client.MessageRecieved -= ClientDataRecieved;

				lock (addClsLocker)
				{
					clients.Remove(client);
				}
			}
			catch (Exception e)
			{
				log.Error("Ошибка при удалении клиента из списка", e);
			}
		}

		void ClientDataRecieved(ConnectionPoint sender, BaseMessage message)
		{
			if (!(sender is ClientProxyConnectionPoint<T>)) return;
			RaiseMessageRecieved(sender as ClientProxyConnectionPoint<T>, message);
		}

		private void ClientOnDisconnected(object sender, EventArgs eventArgs)
		{
			RemoveClient(sender as ClientProxyConnectionPoint<T>);
		}

		public void Dispose()
		{
			Stop();
			//listenThread = null;
		}
	}
}
*/