using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Matrix.Web.Host.Data
{
    class NamesCache
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NamesCache));

        private static readonly NamesCache instance = new NamesCache();
        public static NamesCache Instance
        {
            get
            {
                return instance;
            }
        }

        private NamesCache()
        {
        }

        public string Update(dynamic node, Guid userId)
        {
            var dnode = node as IDictionary<string, object>;
            if (!dnode.ContainsKey("id") || !dnode.ContainsKey("type"))
            {
                log.Warn(string.Format("кеш имен не может быть обновлен, нет ИД или типа"));
                return "";
            }

            Guid id = Guid.Parse(node.id.ToString());
            string type = node.type.ToString();

            string name = type;

            switch (type)
            {
                case "Tube":
                    var area = StructureGraph.Instance.GetArea(id, userId);
                    if (area != null)
                        name = ((area as IDictionary<string, object>).ContainsKey("name") ? area.name : "") + " " + ((node as IDictionary<string, object>).ContainsKey("name") ? node.name : "");
                    break;
                case "Area":
                    name = (node as IDictionary<string, object>).ContainsKey("name") ? node.name : "";
                    break;
                case "CsdConnection":
                    name = (node as IDictionary<string, object>).ContainsKey("phone") ? node.phone : "";
                    break;
                case "MatrixConnection":
                    name = (node as IDictionary<string, object>).ContainsKey("imei") ? node.imei : "";
                    break;
                case "LanConnection":
                    name = ((node as IDictionary<string, object>).ContainsKey("host") ? node.host : "") + ":" + ((node as IDictionary<string, object>).ContainsKey("port") ? node.port : "");
                    break;
                case "Modem":
                    name = (node as IDictionary<string, object>).ContainsKey("port") ? node.port : "";
                    break;
            }

            dynamic msg = new ExpandoObject();
            msg.name = name;
            CacheRepository.Instance.Set("name", id, msg);
            //CacheRepository.Instance.SetLocal("name", id, msg);
            return name;
        }
        public string UpdateWithoutRedis(dynamic node, Guid userId)
        {
            Guid id = Guid.Parse(node.id.ToString());
            string type = node.type.ToString();

            string name = type;

            switch (type)
            {
                case "Tube":
                    var area = StructureGraph.Instance.GetArea(id, userId);
                    if (area != null)
                        name = ((area as IDictionary<string, object>).ContainsKey("name") ? area.name : "") + " " + ((node as IDictionary<string, object>).ContainsKey("name") ? node.name : "");
                    break;
                case "Area":
                    name = (node as IDictionary<string, object>).ContainsKey("name") ? node.name : "";
                    break;
                case "CsdConnection":
                    name = (node as IDictionary<string, object>).ContainsKey("phone") ? node.phone : "";
                    break;
                case "MatrixConnection":
                    name = (node as IDictionary<string, object>).ContainsKey("imei") ? node.imei : "";
                    break;
                case "LanConnection":
                    name = ((node as IDictionary<string, object>).ContainsKey("host") ? node.host : "") + ":" + ((node as IDictionary<string, object>).ContainsKey("port") ? node.port : "");
                    break;
                case "Modem":
                    name = (node as IDictionary<string, object>).ContainsKey("port") ? node.port : "";
                    break;
            }
            return name;
        }
        public string GetName(Guid id, Guid userId)
        {
            var name = CacheRepository.Instance.Get("name", id);
            //var name = CacheRepository.Instance.GetLocal("name", id);
            if (name == null)
            {
                var node = StructureGraph.Instance.GetNodeById(id, userId);
                return Update(node, userId);
            }

            return name.name.ToString();
        }
    }
}
