//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class DataRecordsResponse : Message
//    {
//        public IEnumerable<DataRecord> Records { get; private set; }

//        public DataRecordsResponse(Guid id, IEnumerable<DataRecord> records)
//            : base(id)
//        {
//            Records = records.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return Records.Select(d => d.ObjectId);
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedRecords = Records.Where(r => avalibleEntityIds.Contains(r.ObjectId));
//            return new DataRecordsResponse(Id, truncatedRecords);
//        }

//        public override string ToString()
//        {
//            return string.Format("записи ({0} шт)", Records.Count());
//        }
//    }
//}
