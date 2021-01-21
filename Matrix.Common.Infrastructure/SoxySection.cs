using System;
using System.Configuration;
using Matrix.Common.Infrastructure.Protocol;

namespace Matrix.Common.Infrastructure
{
    public class SoxySection : ConfigurationSection
    {
        public const string SOXY_SECTION = "soxy";
        private const string ProxyServerTypeKey = "proxyServerType";
        private const string ProxyServerAdressKey = "proxyServerAdress";
        private const string ProxyServerPortKey = "proxyServerPort";
        private const string ProxyUserKey = "proxyUser";
        private const string ProxyPasswordKey = "proxyPassword";

        private const string LOGIN = "login";
        [ConfigurationProperty(LOGIN, IsRequired = true, DefaultValue = "admin")]
        public string Login
        {
            get
            {
                return (string)this[LOGIN];
            }
            set
            {
                this[LOGIN] = value;
            }
        }

        private const string PASSWORD = "password";
        [ConfigurationProperty(PASSWORD, IsRequired = true)]
        public string Password
        {
            get
            {
                return (string)this[PASSWORD];
            }
            set
            {
                this[PASSWORD] = value;
            }
        }

        private const string Address = "address";
        [ConfigurationProperty(Address, IsRequired = true, DefaultValue = "localhost")]
        public string ServerAddress
        {
            get
            {
                return (string)this[Address];
            }
            set
            {
                this[Address] = value;
            }
        }

        private const string Port = "port";
        [ConfigurationProperty(Port, IsRequired = true, DefaultValue = 7011)]
        public int ServerPort
        {
            get
            {
                return (int)this[Port];
            }
            set
            {
                this[Port] = value.ToString();
            }
        }
        [ConfigurationProperty(ProxyServerTypeKey, DefaultValue = ProxyType.NoProxy, IsRequired = false)]
        public ProxyType ProxyServerType
        {
            get
            {
                try
                {
                    return (ProxyType)this[ProxyServerTypeKey];
                }
                catch (Exception)
                {
                    return ProxyType.NoProxy;
                }

            }
            set
            {
                this[ProxyServerTypeKey] = value;
            }
        }
        [ConfigurationProperty(ProxyServerAdressKey, DefaultValue = "proxy", IsRequired = false)]
        public string ProxyServerAdress
        {
            get
            {
                return (string)this[ProxyServerAdressKey];
            }
            set
            {
                this[ProxyServerAdressKey] = value;
            }
        }
        [ConfigurationProperty(ProxyServerPortKey, DefaultValue = 7011, IsRequired = false)]
        public int ProxyServerPort
        {
            get
            {
                int port = 0;
                try
                {
                    port = (int)this[ProxyServerPortKey];
                }
                finally
                {                    
                }
                return port;
            }
            set
            {
                this[ProxyServerPortKey] = value;
            }
        }
        [ConfigurationProperty(ProxyUserKey, DefaultValue = "", IsRequired = false)]
        public string ProxyUser
        {
            get
            {
                return (string)this[ProxyUserKey];
            }
            set
            {
                this[ProxyUserKey] = value;
            }
        }
        [ConfigurationProperty(ProxyPasswordKey, DefaultValue = "", IsRequired = false)]
        public string ProxyPassword
        {
            get
            {
                return (string)this[ProxyPasswordKey];
            }
            set
            {
                this[ProxyPasswordKey] = value;
            }
        }

        private static Configuration config;
        public static SoxySection GetSection()
        {
            if (config == null)
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            }

            var section = (SoxySection)config.GetSection(SOXY_SECTION);
            if (section == null)
            {
                section = new SoxySection();
                config.Sections.Add(SOXY_SECTION, section);
                config.Save();
            }
            return section;
        }

        public static void Save()
        {
            if (config == null) return;
            config.Save();
        }
    }
}
