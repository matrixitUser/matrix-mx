using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer.Handlers
{
    class DriverHandler : IHandler
    {
        private readonly static ILog log = LogManager.GetLogger(typeof(DriverHandler));

        public bool CanHandle(string what)
        {
            return what.StartsWith("driver");
        }

        public void Handle(dynamic message)
        {
            string what = message.head.what;

            if (what == "driver-update")
            {
                string action = message.body.action;
                dynamic driver = message.body.driver;

                Guid id = Guid.Empty;
                if (!Guid.TryParse((string)driver.id, out id))
                {
                    log.Warn(string.Format("неудача при {0} драйвера", action));
                    return;
                }

                byte[] body = Convert.FromBase64String(driver.driver);
                var name = driver.name;

                switch (action)
                {
                    case "add": DriverManager.Instance.Add(id, body); break;
                    case "remove": DriverManager.Instance.Remove(id); break;
                    case "update":
                        {
                            var result = DriverManager.Instance.Update(id, body);
                            if (result) log.Info(string.Format("драйвер {0} успешно обновлен", name));
                            else log.Warn(string.Format("драйвер {0} не удалось обновить", name));
                            break;
                        }
                    default: break;
                }
            }
        }
    }
}
