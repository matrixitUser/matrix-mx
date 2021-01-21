using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Matrix.Web.Host.Data
{
    class SaveHandler : IRecordHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SaveHandler));

        public void Handle(IEnumerable<Domain.Entities.DataRecord> records,Guid userId)
        {
            if (records == null || !records.Any()) return;
            log.Debug(string.Format("начато сохранение {0} записей", records.Count()));
            Cache.Instance.SaveRecords(records);
        }
    }
}
