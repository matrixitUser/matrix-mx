using System;

namespace Matrix.Common.Infrastructure.Protocol
{
	public class ConnectionInfo
	{
		public ConnectionInfo(string host, int port)
		{
			if (string.IsNullOrEmpty(host))
			{
				throw new ArgumentNullException("host");
			}
			if (port < 1)
			{
				throw new ArgumentNullException("port");
			}
			Host = host;
			Port = port;

		}

		public ConnectionInfo(string host, int port, string proxyServerHost, int proxyServerPort, ProxyType proxyType)
			: this(host, port)
		{
            if (proxyType != ProxyType.NoProxy)
            {
                if (string.IsNullOrEmpty(proxyServerHost))
                {
                    throw new ArgumentNullException("proxyServerHost");
                }
                if (proxyServerPort < 1)
                {
                    throw new ArgumentNullException("proxyServerPort");
                }
                ProxyServerHost = proxyServerHost;
                ProxyServerPort = proxyServerPort;
            }
		    ProxyType = proxyType;
            Host = host;
            Port = port;
        }

		public string Host { get; private set; }
		public int Port { get; private set; }

		public string ProxyServerHost { get; private set; }
		public int ProxyServerPort { get; private set; }

		public string UserName { get; set; }
		public string UserPassword { get; set; }

		public ProxyType ProxyType { get; private set; }
	}

	public enum ProxyType
	{
		NoProxy,
		SOCKS4,
		SOCKS5,
		HTTP
	}
}
