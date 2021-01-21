using System;
using System.Dynamic;
using System.Text;
using Nancy;
using Newtonsoft.Json;

namespace Matrix.Spotter
{
    public class ApiModule : NancyModule
    {
        public ApiModule(ObjectManager om, Bus bus)
        {
            Get[""] = (_) => {
                return Response.AsFile(@"ui/index.html");
            };

            Get["objects"] = (_) =>
            {
                return Json(om.GetAll());
            };

            Get["data/{id:guid}/{start:datetime(dd-MM-yyyy)}/{end:datetime(dd-MM-yyyy)}"] = (arg) => {
                Guid id = arg.id;
                DateTime start = arg.start;
                DateTime end = arg.end;

                return Json(om.GetData(id, start, end));                
            };

            Post[""] = (_) =>
            {
                try
                {
                    var body = Request.GetBody();
                    string id = body.deviceId;
                    string base64image = body.image;

                    var obj = om.Save(id);

                    dynamic record = new ExpandoObject();
                    record.id = Guid.NewGuid();
                    record.type = "Spotter";
                    record.date = DateTime.Now;
                    record.objectId = obj.id;
                    record.image = base64image;

                    bus.SendRecords(new dynamic[] { record });

                    dynamic answer = new ExpandoObject();
                    answer.frequencyHours = 24;
                    return Json(answer);
                }
                catch (Exception ex)
                {
                    dynamic answer = new ExpandoObject();
                    answer.error = ex.Message;
                    return Json(answer);
                }
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
