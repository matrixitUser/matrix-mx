//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// изменение записей (уровень сигнала, заполненность бд и т.п.
//    /// </summary>
//    public class DataRecordChanged : Message
//    {
//        public IEnumerable<DataRecord> Records { get; private set; }

//        public DataRecordChanged(Guid id, IEnumerable<DataRecord> records)
//            : base(id)
//        {
//            Records = records.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            return Records.Select(r => r.ObjectId).Distinct();
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncatedRecords = Records.Where(r => avalibleEntityIds.Contains(r.ObjectId));
//            return new DataRecordChanged(Id, truncatedRecords);
//        }

//        public override string ToString()
//        {
//            return string.Format("изменение записей ({0} шт)", Records.Count());
//        }
//    }
//}
