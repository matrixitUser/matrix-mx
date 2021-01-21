using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Matrix.MatrixControllers
{
    [ActionHandler("alarm")]
    class AlarmState : IActionState
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Task<bool> Start(IEnumerable<dynamic> path, dynamic details)
        {
            return Task.Run<bool>(() =>
            {

                //logger.Debug("аларм!");
                //var url = ConfigurationManager.AppSettings["neo4j-url"];
                //var client = new Neo4jClient.GraphClient(new Uri(url));
                //client.Connect();
                //var node = client.Cypher.Match("(m:MatrixConnection {id:{id}})").WithParams(new { id = Session.Id }).Return(m => m.Node<string>()).Results.ToDynamics().FirstOrDefault();
                //var ans = new List<byte>();
                //ans.Add(request.Body[0]);

                //var tm = new List<byte>();
                //foreach (var b in node.tm)
                //{
                //    tm.Add((byte)b);
                //}

                //ans.AddRange(tm);
                //ans.AddRange(BitConverter.GetBytes((float)node.tMin));
                //ans.AddRange(BitConverter.GetBytes((float)node.tMax));
                //ans.AddRange(BitConverter.GetBytes((float)node.tk));
                //ans.AddRange(BitConverter.GetBytes((float)node.t0));
                //ans.AddRange(BitConverter.GetBytes((float)node.pMin));
                //ans.AddRange(BitConverter.GetBytes((float)node.pMax));
                //ans.AddRange(BitConverter.GetBytes((float)node.pk));
                //ans.AddRange(BitConverter.GetBytes((float)node.p0));
                //Session.SendFrame(23, ans.ToArray());

                return true;
            });
        }

        public void AcceptFrame(MatrixRequest request)
        {

        }

        public MatrixSession Session { get; set; }

        public bool CanChange()
        {
            return true;
        }
    }
}
