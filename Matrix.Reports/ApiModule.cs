using Nancy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Reports
{
    public class ApiModule : NancyModule
    {
        public ApiModule()
        {
            Get[""] = (_) =>
            {
                return Response.AsFile(@"ui/index.html");
            };

            Get["build"] = (_) =>
            {
                var cache = new Cache();
                var haa = cache.Get("Day", DateTime.Today.AddDays(-30), DateTime.Today, new Guid[] {
                    Guid.Parse("C800FED2-7C57-4919-96A2-9DCB371EA0B1"),
                    Guid.Parse("A533226D-265E-426D-A2A8-52828F1A6D35"),
                    Guid.Parse("1518BA2C-B083-4F62-8365-E7AEA86BBA33"),
                    Guid.Parse("994AFA6F-38FD-4BA6-A3F9-53868AA8830D"),
                    Guid.Parse("F29E802E-CE21-40AC-9FE2-BD3D484478FA"),
                    Guid.Parse("C6CCC9A9-C59E-4F1A-B1EF-87EE99CDF397"),
                    Guid.Parse("0FB46D2B-C505-4E12-950C-068E758A48D3"),
                    Guid.Parse("58A40FD3-7F0E-4173-BA16-5009A6FA7697"),
                    Guid.Parse("AA924E29-F48C-45A7-8745-909386915F41"),
                    Guid.Parse("E258F6C5-7E24-4C5E-9F18-C67EB6A5F730"),
                    Guid.Parse("107B3125-9B57-469C-BC82-772548BC93A8"),
                    Guid.Parse("0436EC8D-38D3-4FF6-8317-DC05A4257111"),
                    Guid.Parse("2003A952-D7E4-4648-8CFD-FFA150314EF9"),
                    Guid.Parse("EEB27138-9AFE-4508-B4CB-29D82C130A6B")
                });
                return Json(haa);
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
}
