using Nancy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.LogManager
{
    public class ApiModule : NancyModule
    {
        public ApiModule(LogAnalizer log)
        {
            Get[""] = (_) =>
            {
                return Response.AsFile(@"ui/index.html");
            };

            Put["subscribe"] = (_) =>
            {
                var body = Request.GetBody();

                Guid sessionId = Guid.Parse(body.sessionId);

                var ids = new List<Guid>();
                foreach (var id in body.ids)
                {
                    Guid gid = Guid.Parse(id);
                    ids.Add(gid);
                }

                log.Subscribe(sessionId, ids);
                return "ok";
            };

            Put["unsubscribe"] = (_) =>
            {
                var body = Request.GetBody();

                Guid sessionId = Guid.Parse(body.sessionId);

                var ids = new List<Guid>();
                foreach (var id in body.ids)
                {
                    Guid gid = Guid.Parse(id);
                    ids.Add(gid);
                }

                log.Unsubscribe(sessionId, ids);
                return "ok";
            };

            Get["subscribers"] = (_) => {
                return Json(log.Subscribers);
            };

            //Get["updateCache"] = (_) => {
            //    measure.LoadTags();
            //    return "ok";
            //};
        }

        private Response Json(dynamic obj)
        {
            var str = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(str);
            return new Response
            {
                ContentType = "application/json",
                Contents = s => s.Write(bytes, 0, bytes.Length)
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
