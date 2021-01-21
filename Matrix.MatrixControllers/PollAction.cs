using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using Neo4jClient;
using NLog;

namespace Matrix.MatrixControllers
{
    [ActionHandler("poll")]
    class PollAction : IActionState
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly List<byte> buffer = new List<byte>();

        private DriverGhost driver;
        private dynamic tubeId;

        private bool inProccess = false;

        public bool CanChange()
        {
            return !inProccess;
        }

        public void AcceptFrame(MatrixRequest request)
        {
            buffer.AddRange(request.Body);
            logger.Debug("к драйверу {0}", string.Join(",", buffer.Select(b => b.ToString("X2"))));
        }

        public Task<bool> Start(IEnumerable<dynamic> path, dynamic details)
        {
            return Task.Run<bool>(() =>
            {
                try
                {
                    var bus = ServiceLocator.Current.GetInstance<Bus>();
                    inProccess = true;

                    tubeId = path.FirstOrDefault();

                    if (path.Count() > 3)
                    {
                        logger.Debug("путь длиньше обычного, вероятно используется свитч?");
                    }

                    //1 get driver
                    var url = ConfigurationManager.AppSettings["neo4j-url"];
                    var client = new GraphClient(new Uri(url));
                    client.Connect();

                    var d = client.Cypher.Match("(t:Tube {id:{tubeId}})-->(d:Device)<--(dr:Driver)").
                    WithParams(new { tubeId = tubeId }).
                    Return((t,dr) =>new { dr = dr.Node<string>(), t = t.Node<string>() }).Results.FirstOrDefault();
                    if (d == null)
                    {
                        logger.Debug("драйвер не привязан");
                        return false;
                    }
                    var buff = Convert.FromBase64String(d.dr.ToDynamic().driver);
                    var ass = Assembly.Load(buff);
                    var catalog = new AssemblyCatalog(ass);
                    driver = new DriverGhost(catalog);

                    var tube = d.t.ToDynamic();

                    driver.Log = (msg) =>
                    {
                        logger.Debug("драйвер говорит: {0}", msg);
                        dynamic log = new ExpandoObject();
                        log.id = Guid.NewGuid();
                        log.message = msg;
                        log.date = DateTime.Now;
                        log.type = "LogMessage";
                        log.objectId = tubeId;
                        bus.SendRecords(new dynamic[] { log });
                    };
                    driver.Response = () =>
                    {
                        var bytes = buffer.ToArray();
                        buffer.Clear();
                        return bytes;
                    };
                    driver.Request = (bytes) =>
                    {
                        Session.SendFrame(0, bytes);
                        logger.Debug("от драйвера {0}", string.Join(",", bytes.Select(b => b.ToString("X2"))));
                    };
                    driver.Cancel = () =>
                    {
                        return false;
                    };
                    
                    driver.Records = (records) =>
                    {
                        bus.SendRecords(records);
                    };
                    
                    driver.Doing("all", tube);
                }
                catch(Exception ex)
                {

                }
                finally
                {
                    inProccess = false;
                }
                return true;
            });
        }

        public MatrixSession Session { get; set; }
    }

    abstract class ChainLink
    {
        public ChainLink Next { get; private set; }
        public ChainLink Previous { get; private set; }

        public virtual void Prev2Next(byte[] bytes)
        {

        }

        public virtual void Next2Prev(byte[] bytes)
        {

        }

        public ChainLink(ChainLink previous, ChainLink next)
        {
            Previous = previous;
            Next = next;
        }
    }

    class Switch : ChainLink
    {
        public Switch(ChainLink previous, ChainLink next) : base(previous, next) { }

        public override void Next2Prev(byte[] bytes)
        {
            Previous.Next2Prev(bytes);
        }

        public override void Prev2Next(byte[] bytes)
        {
            Next.Prev2Next(bytes);
        }
    }
}
