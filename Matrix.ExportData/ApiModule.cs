using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Newtonsoft.Json;

namespace Matrix.ExportData
{
    public class ApiModule : NancyModule
    {
        public ApiModule(Pivoter pvt, ObjectManager om)
        {
            Post["data"] = (_) =>
            {
                var body = Request.GetBody();

                DateTime start = body.start;
                DateTime end = body.end;

                string type = body.type;

                var ids = (body.ids as IEnumerable<object>).Select(s => Guid.Parse(s.ToString())).ToArray();

                var rcs = pvt.Pivot(ids, start, end, type);

                return Json(rcs);
            };

            Get["objects"] = (_) =>
            {
                var objs = om.GetObjects();
                return Json(objs);
            };
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
