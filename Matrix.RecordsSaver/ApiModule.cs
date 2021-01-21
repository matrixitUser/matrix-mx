using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using System.Dynamic;
using Newtonsoft.Json;

namespace Matrix.RecordsSaver
{
    public class ApiModule : NancyModule
    {
        public ApiModule(Saver saver)
        {
            Get[""] = (_) =>
            {
                saver.Foo();
                return "haa";
            };

            Post["save"] = (_) =>
            {
                var body = Request.GetBody();
                saver.Push(body.records);
                saver.Save();
                return "ok";
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
