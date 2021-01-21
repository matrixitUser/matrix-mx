using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;

namespace Matrix.Web.Host.Handlers
{
    class RecordAcceptor
    {    
        private const int CHANNEL_COUNT = 10;
        private readonly static ILog log = LogManager.GetLogger(typeof(RecordAcceptor));

        private readonly List<RecordsBackgroundProccessor> channels = new List<RecordsBackgroundProccessor>();

        public void Save(dynamic newRecord)
        {
            this.Save(new dynamic[] { newRecord });
        }

        Random rnd = new Random();

        public void Save(IEnumerable<DataRecord> newRecords)
        {                        
            channels.ElementAt(rnd.Next(1, CHANNEL_COUNT) - 1).AddPart(newRecords);
        }

        static RecordAcceptor() { }

        private RecordAcceptor()
        {
            channels.AddRange(Enumerable.Range(1, CHANNEL_COUNT).Select(i => new RecordsBackgroundProccessor()));
        }

        private static readonly RecordAcceptor instance = new RecordAcceptor();
        public static RecordAcceptor Instance
        {
            get
            {
                return instance;
            }
        }

        //public void Dispose()
        //{
        //    channels.ForEach(c => c.Dispose());
        //}
    }
}
