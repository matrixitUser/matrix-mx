using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Matrix.Common.Infrastructure.Protocol
{
	/// <summary>
	/// принимает подключения от клиентских точек подключений
	/// </summary>
	public class ServerConnectionPointAcceptor
	{
		private readonly ILog log = LogManager.GetLogger(typeof(ServerConnectionPointAcceptor));
		private readonly ISerializer serializer = new JsonSerilizer();

		private Socket acceptSocket;
		private readonly int port;

		/// <summary>
		/// срабатывает при подключении нового клиента
		/// </summary>
		public event EventHandler<ServerConnectionPointEventArgs> ConnectionPointConnected;
		private void RaiseConnectionPointConnected(ServerConnectionPoint connectionPoint)
		{
			if (ConnectionPointConnected != null)
			{
				ConnectionPointConnected(this, new ServerConnectionPointEventArgs(connectionPoint));
			}
		}

		public ServerConnectionPointAcceptor(int port)
		{
			this.port = port;
		}

		public void Start()
		{
			acceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			var localEndPoint = new IPEndPoint(IPAddress.Any, port);
			acceptSocket.Bind(localEndPoint);
			acceptSocket.Listen(100);

			var listenThread = new Thread(Accept);
			listenThread.Start();
		}

		public void Stop()
		{
			try
			{
				acceptSocket.Shutdown(SocketShutdown.Both);
				acceptSocket.Close();
				log.InfoFormat("[{0}] остановка приема входящих точек соединения на порту {1}", this, port);
			}
			catch (Exception e)
			{
				log.Error(string.Format("[{0}] ошибка при остановке сервера", this), e);
			}
		}

		/// <summary>
		/// принимает входящие соединения
		/// (поток B)
		/// </summary>
		/// <param name="parameter"></param>
		private void Accept(object parameter)
		{
			try
			{
				log.InfoFormat("[{0}] начало приема входящих точек соединения на порту {1}", this, port);
				while (true)
				{
					var clientSocket = acceptSocket.Accept();					

					var remoteEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
					if (remoteEndPoint != null)
					{
						log.InfoFormat("[{0}] принято подключение от {1}", this, remoteEndPoint.Address);
					}

					var serverConnectionPoint = new ServerConnectionPoint(clientSocket, "PROXY");
					RaiseConnectionPointConnected(serverConnectionPoint);
				}
			}
			catch (Exception)
			{
			}
		}

		public void Dispose()
		{
			Stop();
		}

		public override string ToString()
		{
			return string.Format("приемщик точек соединения, порт={0}", port);
		}
	}

	public class ServerConnectionPointEventArgs : EventArgs
	{
		public ServerConnectionPoint ConnectionPoint { get; private set; }

		public ServerConnectionPointEventArgs(ServerConnectionPoint connectionPoint)
		{
			ConnectionPoint = connectionPoint;
		}
	}
}
