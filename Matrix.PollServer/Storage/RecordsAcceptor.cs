using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.PollServer.Storage
{
    class RecordsAcceptor : IDisposable
    {
        private const int CHANNEL_COUNT = 10;
        private readonly static ILog log = LogManager.GetLogger(typeof(RecordsAcceptor));

        private readonly List<RecordsProccessChannel> channels = new List<RecordsProccessChannel>();

        public void Save(dynamic newRecord)
        {
            this.Save(new dynamic[] { newRecord });
        }

        Random rnd = new Random();
        public void Save(IEnumerable<dynamic> newRecords)
        {
            channels.ElementAt(rnd.Next(1, CHANNEL_COUNT) - 1).Add(newRecords);
        }

        static RecordsAcceptor() { }

        private RecordsAcceptor()
        {
            channels.AddRange(Enumerable.Range(1, CHANNEL_COUNT).Select(i => new RecordsProccessChannel()));
        }

        private static readonly RecordsAcceptor instance = new RecordsAcceptor();
        public static RecordsAcceptor Instance
        {
            get
            {
                return instance;
            }
        }

        public string GetStatus()
        {
            int i = 1;
            StringBuilder text = new StringBuilder();
            foreach (RecordsProccessChannel ch in channels)
            {
                text.AppendFormat("{0}) ", i).AppendLine(ch.GetInfo());
                i++;
            }
            return text.ToString();
        }

        public void Dispose()
        {
            channels.ForEach(c => c.Dispose());
        }
    }
}
