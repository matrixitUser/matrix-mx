using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Neo4jClient;
using SuperSocket.SocketBase;

namespace Matrix.MatrixControllers
{
    public class ApiModule : NancyModule
    {
        public ApiModule(MatrixSocketServer sos)
        {
            Get[""] = (_) =>
            {
                return Response.AsFile("ui/index.html");
            };

            Get["sessions"] = (_) =>
            {
                return Response.AsJson(sos.GetAllSessions().Select(s =>
                {
                    dynamic mock = new ExpandoObject();
                    mock.sid = s.SessionID;
                    mock.imei = s.Imei;
                    mock.lastActive = s.LastActiveTime;
                    mock.ip = s.LocalEndPoint.Address.ToString();
                    return mock;
                }));
            };

            Get["poll/{matrixId}/{tubeId}"] = (arg) =>
            {
                Guid matrixId = Guid.Parse(arg.matrixId);
                Guid tubeId = Guid.Parse(arg.tubeId);

                return "удалось ";// +(pm.Poll(matrixId, tubeId).Result.Success ? "да" : "нет");
            };
        }
    }
}
