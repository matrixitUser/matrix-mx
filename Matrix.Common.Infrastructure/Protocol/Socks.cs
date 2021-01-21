using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Matrix.Common.Infrastructure.Protocol
{
	/// <summary>
	/// Реализация протокола SOCKS4 и SOSKS5
	/// </summary>
	public class Socks:Socket
	{
		private Socks(ConnectionInfo info)
			: base(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		{
			connectionInfo = info;
		}

		private readonly ConnectionInfo connectionInfo;
		
		private void ConnectSocks4()
		{
			var request = new List<byte>();
			//  Send socks version number
			request.Add(0x04);
			//SocketWriteByte(0x04);

			//  Send command code
			request.Add(0x01);
			//SocketWriteByte(0x01);

			//  Send port
			//SocketWriteByte((byte)(connectionInfo.Port / 0xFF));
			//SocketWriteByte((byte)(connectionInfo.Port % 0xFF));

			request.AddRange(BitConverter.GetBytes((UInt16)connectionInfo.Port).Reverse());

			//  Send IP
			IPAddress remoteAddress;
			if (!IPAddress.TryParse(connectionInfo.Host, out remoteAddress))
			{
				remoteAddress = Dns.GetHostAddresses(connectionInfo.Host).FirstOrDefault();
			}
			if (remoteAddress == null)
			{
				throw new SocksException(string.Format("Не удается распознать адрес {0}", connectionInfo.Host));
			}
			//SocketWrite(remoteAddress.GetAddressBytes());

			//  Send username
			var username = new ASCIIEncoding().GetBytes(connectionInfo.UserName);
			request.AddRange(username);
			request.Add(0x00);
			//this.SocketWrite(username);
			//this.SocketWriteByte(0x00);
			SocketWrite(request.ToArray());

			//  Read 0
			if (this.SocketReadByte() != 0)
			{
				throw new Exception("SOCKS4: Null is expected.");
			}

			//  Read response code
			var code = this.SocketReadByte();

			switch (code)
			{
				case 0x5a:
					break;
				case 0x5b:
					throw new Exception("SOCKS4: Connection rejected.");
				case 0x5c:
					throw new Exception("SOCKS4: Client is not running identd or not reachable from the server.");
				case 0x5d:
					throw new Exception("SOCKS4: Client's identd could not confirm the user ID string in the request.");
				default:
					throw new Exception("SOCKS4: Not valid response.");
			}

			byte[] dummyBuffer = new byte[4];

			//  Read 2 bytes to be ignored
			SocketRead(2, ref dummyBuffer);

			//  Read 4 bytes to be ignored
			SocketRead(4, ref dummyBuffer);
		}
		private void ConnectSocks5()
		{
			////  Send socks version number
			//SocketWriteByte(0x05);

			////  Send number of supported authentication methods
			//SocketWriteByte(0x02);

			////  Send supported authentication methods
			//SocketWriteByte(0x00); //  No authentication
			//SocketWriteByte(0x02); //  Username/Password

			SocketWrite(new byte[] { 0x05, 0x02, 0x00, 0x02 });

			byte[] recieve = new byte[2];
			SocketRead(2, ref recieve);
			if (recieve[0] != 0x05)
				throw new SocksException(string.Format("Версия SOCKS '{0}' не поддерживается.", recieve[0]));

			var authenticationMethod = recieve[1];
			switch (authenticationMethod)
			{
				case 0x00:
					break;
				case 0x02:
					//  Send version
					SocketWriteByte(0x01);

					var encoding = new ASCIIEncoding();
					var username = encoding.GetBytes(connectionInfo.UserName);
					if (username.Length > byte.MaxValue)
						throw new SocksException("Имя пользователя прокси слишком длинное");

					//  Send username length
					SocketWriteByte((byte)username.Length);

					//  Send username
					SocketWrite(username);

					var password = encoding.GetBytes(connectionInfo.UserPassword);

					if (password.Length > byte.MaxValue)
						throw new SocksException("Пароль прокси слишком длинный");

					//  Send username length
					SocketWriteByte((byte)password.Length);

					//  Send username
					SocketWrite(password);

					var serverVersion = SocketReadByte();

					if (serverVersion != 1)
						throw new SocksException("SOCKS5: Server authentication version is not valid.");

					var statusCode = SocketReadByte();
					if (statusCode != 0)
						throw new SocksException("Неверное имя пользователя или пароль.");

					break;
				case 0xFF:
					throw new Exception("SOCKS5: No acceptable authentication methods were offered.");
			}
			{
				var request = new List<byte>();
				//  Send socks version number
				//SocketWriteByte(0x05);
				request.Add(0x05);

				//  Send command code
				//SocketWriteByte(0x01); //  establish a TCP/IP stream connection
				request.Add(0x01);

				//  Send reserved, must be 0x00
				//SocketWriteByte(0x00);
				request.Add(0x00);

				IPAddress remoteAddress;
				if (!IPAddress.TryParse(connectionInfo.Host, out remoteAddress))
				{
					remoteAddress = null;
				}

				//  Send address type and address
				if (remoteAddress != null)
				{
					if (remoteAddress.AddressFamily == AddressFamily.InterNetwork)
					{
						//SocketWriteByte(0x01);
						request.Add(0x01);
						var address = remoteAddress.GetAddressBytes();
						//SocketWrite(address);
						request.AddRange(address);
					}
					else if (remoteAddress.AddressFamily == AddressFamily.InterNetworkV6)
					{
						var address = remoteAddress.GetAddressBytes();
						//SocketWriteByte(0x04);
						//SocketWrite(address);
						request.Add(0x04);
						request.AddRange(address);
					}
				}
				else
				{
					var address = Encoding.ASCII.GetBytes(connectionInfo.Host);
					if (address.Length > byte.MaxValue)
					{
						throw new SocksException("Адрес удаленного хоста слишком длинный");
					}
					//SocketWriteByte((byte)3);
					//SocketWriteByte((byte)address.Length);
					//SocketWrite(address);
					request.Add(0x03);
					request.Add((byte)address.Length);
					request.AddRange(address);
				}

				//  Send port
				//SocketWriteByte((byte)(connectionInfo.Port / 0xFF));
				//SocketWriteByte((byte)(connectionInfo.Port % 0xFF));
				request.AddRange(BitConverter.GetBytes((UInt16) connectionInfo.Port).Reverse());
				//request.Add((byte)(connectionInfo.Port / 0xFF));
				//request.Add((byte)(connectionInfo.Port % 0xFF));
				SocketWrite(request.ToArray());

				//  Read Server SOCKS5 version
				var response = new byte[4];
				SocketRead(4, ref response);
				//var version = SocketReadByte();
				var version = response[0];
				if (version != 5)
				{
					throw new Exception("SOCKS5: Version 5 is expected.");
				}

				//  Read response code
				//var status =  SocketReadByte();
				var status = response[1];
				switch (status)
				{
					case 0x00:
						break;
					case 0x01:
						throw new Exception("SOCKS5: General failure.");
					case 0x02:
						throw new Exception("SOCKS5: Connection not allowed by ruleset.");
					case 0x03:
						throw new Exception("SOCKS5: Network unreachable.");
					case 0x04:
						throw new Exception("SOCKS5: Host unreachable.");
					case 0x05:
						throw new Exception("SOCKS5: Connection refused by destination host.");
					case 0x06:
						throw new Exception("SOCKS5: TTL expired.");
					case 0x07:
						throw new Exception("SOCKS5: Command not supported or protocol error.");
					case 0x08:
						throw new Exception("SOCKS5: Address type not supported.");
					default:
						throw new Exception("SOCKS4: Not valid response.");
				}

				//  Read 0
				//if (SocketReadByte() != 0)
				if (response[2] != 0)
				{
					throw new Exception("SOCKS5: 0 byte is expected.");
				}

				var addressType = response[3];
				byte[] responseIp = new byte[16];

				switch (addressType)
				{
					case 0x01:
						SocketRead(4, ref responseIp);
						break;
					case 0x03:
						byte length = SocketReadByte();
						var address = new byte[length];
						SocketRead(length, ref address);
						break;
					case 0x04:
						SocketRead(16, ref responseIp);
						break;
					default:
						throw new Exception(string.Format("Address type '{0}' is not supported.", addressType));
				}

				byte[] port = new byte[2];

				//  Read 2 bytes to be ignored
				SocketRead(2, ref port);
			}
		}

		private void ConnectHttp()
		{
			var httpResponseRe = new Regex(@"HTTP/(?<version>\d[.]\d) (?<statusCode>\d{3}) (?<reasonPhrase>.+)$");
			var httpHeaderRe = new Regex(@"(?<fieldName>[^\[\]()<>@,;:\""/?={} \t]+):(?<fieldValue>.+)?");

			var encoding = new ASCIIEncoding();

			SocketWrite(encoding.GetBytes(string.Format("CONNECT {0}:{1} HTTP/1.0\r\n", connectionInfo.Host, connectionInfo.Port)));

			//  Sent proxy authorization is specified
			if (!string.IsNullOrEmpty(connectionInfo.UserName))
			{
				var authorization = string.Format("Proxy-Authorization: Basic {0}\r\n",
												  Convert.ToBase64String(encoding.GetBytes(string.Format("{0}:{1}", connectionInfo.UserName, connectionInfo.UserPassword)))
												  );
				SocketWrite(encoding.GetBytes(authorization));
			}

			SocketWrite(encoding.GetBytes("\r\n"));

			var statusCode = (HttpStatusCode)0;
			var response = string.Empty;
			var contentLength = 0;

			while (statusCode != HttpStatusCode.OK)
			{
				SocketReadLine(ref response);

				var match = httpResponseRe.Match(response);

				if (match.Success)
				{
					statusCode = (HttpStatusCode)int.Parse(match.Result("${statusCode}"));
					continue;
				}

				// continue on parsing message headers coming from the server
				match = httpHeaderRe.Match(response);
				if (match.Success)
				{
					var fieldName = match.Result("${fieldName}");
					if (fieldName.Equals("Content-Length", StringComparison.InvariantCultureIgnoreCase))
					{
						contentLength = int.Parse(match.Result("${fieldValue}"));
					}
					continue;
				}

				//  Read response body if specified
				if (string.IsNullOrEmpty(response) && contentLength > 0)
				{
					var contentBody = new byte[contentLength];
					this.SocketRead(contentLength, ref contentBody);
				}

				switch (statusCode)
				{
					case HttpStatusCode.OK:
						break;
					default:
						throw new Exception(string.Format("HTTP: Status code {0}, \"{1}\"", statusCode, statusCode));
				}
			}
			var resp = new byte[2];
			SocketRead(2, ref resp);
		}
		#region R/W
		private byte SocketReadByte()
		{
			byte[] buffer = new byte[1];

			SocketRead(1, ref buffer);

			return buffer[0];
		}
		private void SocketRead(int length, ref byte[] buffer)
		{
			var offset = 0;
			int receivedTotal = 0;  // how many bytes is already received

			do
			{
				try
				{
					var receivedBytes = Receive(buffer, offset + receivedTotal, length - receivedTotal, SocketFlags.None);
					if (receivedBytes > 0)
					{
						receivedTotal += receivedBytes;
						continue;
					}
					else
					{
						// 2012-09-11: Kenneth_aa
						// When Disconnect or Dispose is called, this throws SshConnectionException(), which...
						// 1 - goes up to ReceiveMessage() 
						// 2 - up again to MessageListener()
						// which is where there is a catch-all exception block so it can notify event listeners.
						// 3 - MessageListener then again calls RaiseError().
						// There the exception is checked for the exception thrown here (ConnectionLost), and if it matches it will not call Session.SendDisconnect().
						//
						// Adding a check for this._isDisconnecting causes ReceiveMessage() to throw SshConnectionException: "Bad packet length {0}".
						//
						throw new Exception("An established connection was aborted by the software in your host machine.");
					}
				}
				catch (SocketException exp)
				{
					if (exp.SocketErrorCode == SocketError.ConnectionAborted)
					{
						buffer = new byte[length];
						this.Disconnect(true);
						return;
					}
					else if (exp.SocketErrorCode == SocketError.WouldBlock ||
					   exp.SocketErrorCode == SocketError.IOPending ||
					   exp.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
					{
						// socket buffer is probably empty, wait and try again
						Thread.Sleep(30);
					}
					else
						throw;  // any serious error occurred
				}
			} while (receivedTotal < length);
		}
		private void SocketWriteByte(byte data)
		{
			this.SocketWrite(new byte[] { data });
		}
		private void SocketWrite(byte[] data)
		{
			int sent = 0;  // how many bytes is already sent
			int length = data.Length;

			do
			{
				try
				{
					sent += Send(data, sent, length - sent, SocketFlags.None);
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.WouldBlock ||
						ex.SocketErrorCode == SocketError.IOPending ||
						ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
					{
						// socket buffer is probably full, wait and try again
						Thread.Sleep(30);
					}
					else
						throw;  // any serious error occurr
				}
			} while (sent < length);
		}
		private void SocketReadLine(ref string response)
		{
			var encoding = new ASCIIEncoding();

			var line = new StringBuilder();
			//  Read data one byte at a time to find end of line and leave any unhandled information in the buffer to be processed later
			var buffer = new List<byte>();

			var data = new byte[1];
			do
			{
				var asyncResult = BeginReceive(data, 0, data.Length, SocketFlags.None, null, null);

				if (!asyncResult.AsyncWaitHandle.WaitOne(1000))
					throw new Exception("Socket read operation has timed out");

				var received = EndReceive(asyncResult);

				//  If zero bytes received then exit
				if (received == 0)
					break;

				buffer.Add(data[0]);
			}
			while (!(buffer.Count > 0 && (buffer[buffer.Count - 1] == 0x0A || buffer[buffer.Count - 1] == 0x00)));

			// Return an empty version string if the buffer consists of a 0x00 character.
			if (buffer.Count > 0 && buffer[buffer.Count - 1] == 0x00)
			{
				response = string.Empty;
			}
			else if (buffer.Count > 1 && buffer[buffer.Count - 2] == 0x0D)
				response = encoding.GetString(buffer.Take(buffer.Count - 2).ToArray());
			else
				response = encoding.GetString(buffer.Take(buffer.Count - 1).ToArray());
		}
		#endregion
		public void Connect()
		{
			if(connectionInfo == null) return;

			switch (connectionInfo.ProxyType)
			{
				case ProxyType.NoProxy:
					Connect(connectionInfo.Host, connectionInfo.Port);
					break;
				case ProxyType.SOCKS4:
					Connect(connectionInfo.ProxyServerHost, connectionInfo.ProxyServerPort);
					ConnectSocks4();
					break;
				case ProxyType.SOCKS5:
					Connect(connectionInfo.ProxyServerHost, connectionInfo.ProxyServerPort);
					ConnectSocks5();
					break;
				case ProxyType.HTTP:
					Connect(connectionInfo.ProxyServerHost, connectionInfo.ProxyServerPort);
					ConnectHttp();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		public static Socks CreateSocks(ConnectionInfo connectionInfo)
		{
			if (connectionInfo == null)
			{
				throw new ArgumentNullException("connectionInfo");
			}

			//IPAddress remoteAddress;

			//if (!IPAddress.TryParse(connectionInfo.Host, out remoteAddress))
			//    remoteAddress = Dns.GetHostAddresses(connectionInfo.Host).FirstOrDefault();

			//if (remoteAddress == null)
			//{
			//    throw new Exception(string.Format("Не удается найти адрес {0}", connectionInfo.Host));
			//}

			return new Socks(connectionInfo);
		}
	}
}
