//using Matrix.Domain.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    public class DataRecordsMessage : Message
//    {
//        public IEnumerable<DataRecord> Records { get; private set; }

//        public DataRecordsMessage(Guid id, IEnumerable<DataRecord> records)
//            : base(id)
//        {
//            Records = records.ToList();
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedRecords = Records.Where(r => avalibleEntityIds.Contains(r.ObjectId));
//            return new DataRecordsResponse(Id, truncatedRecords);
//        }
//    }
//}
