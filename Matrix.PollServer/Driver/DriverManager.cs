using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.PollServer
{
    class DriverManager
    {
        private readonly static ILog log = LogManager.GetLogger(typeof(DriverManager));
        private readonly Dictionary<Guid, AssemblyCatalog> compositionContainers = new Dictionary<Guid, AssemblyCatalog>();

        public void Load()
        {
            try
            {
                var connector = UnityManager.Instance.Resolve<IConnector>();
                
                dynamic file = connector.SendMessage(Helper.BuildMessage("driver-list"));
                if ((file.head.what as string) == "error")
                {
                    log.Error(string.Format("драйверы не загружены. {0}", file.body.message));
                    return;
                }

                foreach (var model in file.body.drivers)
                {
                    Guid id = Guid.Empty;
                    if (!Guid.TryParse((string)model.id, out id))
                    {
                        log.Warn("неудача при обработке драйвера");
                        continue;
                    }
                    byte[] body = Convert.FromBase64String(model.driver);
                    if (!Add(id, body))
                        log.Warn("неудача при добавлении драйвера");
                }
                log.Info(string.Format("загрузка драйверов произведена. {0} шт", compositionContainers.Count));
            }
            catch (Exception ex)
            {
                log.Error(string.Format("ошибка при загрузке драйверов. {0}", ex.Message));
            }
        }

        public bool Remove(Guid id)
        {
            lock (compositionContainers)
            {
                if (compositionContainers.ContainsKey(id))
                {
                    return compositionContainers.Remove(id);
                }
                return false;
            }
        }

        public bool Add(Guid id, byte[] body)
        {
            lock (compositionContainers)
            {
                if (compositionContainers.ContainsKey(id))
                    return false;
                if (body == null) return false;

                try
                {
                    var ass = Assembly.Load(body);
                    var catalog = new AssemblyCatalog(ass);
                    compositionContainers.Add(id, catalog);
                }
                catch (Exception ex)
                {
                    if (ex is BadImageFormatException)
                        log.Error(string.Format("драйвер (id='{0}') имеет не верный формат", id));
                    else
                        log.Error("драйвер не загружен", ex);
                }
                return true;
            }
        }

        public bool Update(Guid id, byte[] newBody)
        {
            Remove(id);
            if (newBody == null) return false;
            return Add(id, newBody);
        }

        public DriverGhost GetDriverGhost(Guid id)
        {
            try
            {
                if (compositionContainers.ContainsKey(id))
                {
                    return new DriverGhost(compositionContainers[id]);
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }
        public DriverGhost GetDriverGhost(string name)
        {
            try
            {
                foreach (var tmp in compositionContainers)
                {
                    if (tmp.Value.Assembly.FullName.ToLower().Contains(name.ToLower()))
                    {
                        return new DriverGhost(tmp.Value);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }
        private DriverManager() { }
        private static DriverManager instance = null;
        public static DriverManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DriverManager();
                }
                return instance;
            }
        }
    }
}
