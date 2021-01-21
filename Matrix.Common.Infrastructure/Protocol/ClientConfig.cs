using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Matrix.Common.Infrastructure.Protocol
{
	class ClientConfig : ConfigurationSection
	{
		private const string remoteAddressKey = "remoteAddress";

		private static Configuration config;

		private ClientConfig()
		{
			
		}

		[ConfigurationProperty(remoteAddressKey, DefaultValue = "localhost:7011", IsRequired = false)]
		public string RemoteAddress
		{
			get
			{
				return (string)this[remoteAddressKey];
			}
			set
			{
				this[remoteAddressKey] = value;

				if (config != null)
					config.Save(ConfigurationSaveMode.Full);
			}
		}

		private static ClientConfig instance;
		public static ClientConfig Instance
		{
			get
			{
				if (instance == null)
				{
					config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
					instance = (ClientConfig)config.GetSection("ClientConfigTransport");
					
					if (instance == null)
					{
						instance = new ClientConfig();
						config.Sections.Add("ClientConfigTransport", instance);
					}
				}
				return instance;
			}
		}
	}
}
