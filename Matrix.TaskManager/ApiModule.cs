using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Neo4jClient;
using System.Dynamic;
using Newtonsoft.Json;

namespace Matrix.TaskManager
{
    public class ApiModule : NancyModule
    {
        public ApiModule(TaskManager tm)
        {
            Get[""] = (_) =>
            {
                return Response.AsFile(@"ui/index.html");
            };

            Get["poll/{id}/{what}"] = arg =>
            {
                Guid id = Guid.Parse(arg.id);
                string what = arg.what;

                //tm.Push(id, what);
                return "done";
            };

            Post["push"] = (_) =>
            {
                var body = Request.GetBody();
                var idsList = new List<Guid>();
                foreach (var id in body.ids)
                {
                    idsList.Add(Guid.Parse(id));
                }
                tm.Push(idsList.ToArray(), "ha-haa");
                return "ok " + DateTime.Now;
            };

            Get["tasks"] = (_) =>
            {
                return Response.AsJson(tm.Tasks);
            };
        }
    }

    static class NancyPostExtensions
    {
        public static dynamic GetBody(this Request request)
        {
            var len = (int)request.Body.Length;
            var buffer = new byte[len];
            request.Body.Read(buffer, 0, len);
            var json = Encoding.UTF8.GetString(buffer);
            return JsonConvert.DeserializeObject<ExpandoObject>(json);
        }
    }
}
