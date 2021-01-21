//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос на построение отчета
//    /// </summary>
//    public class BuildReportRequest : Message
//    {
//        public IEnumerable<Guid> TubeIds { get; private set; }
//        public DateTime Start { get; private set; }
//        public DateTime End { get; private set; }
//        public Guid ReportId { get; private set; }
//        public BuildReportRequest(Guid id, IEnumerable<Guid> tubeIds, DateTime start, DateTime end, Guid reportId)
//            : base(id)
//        {
//            ReportId = reportId;
//            End = end;
//            Start = start;
//            TubeIds = tubeIds.ToList();
//        }

//        public override IEnumerable<Guid> GetEntityIds()
//        {
//            var result = new List<Guid>(new[] { ReportId });
//            if (TubeIds != null)
//                result.AddRange(TubeIds);

//            return result;
//        }

//        public override Message Truncate(IEnumerable<Guid> avalibleEntityIds)
//        {
//            var truncTubes = TubeIds.Where(t => avalibleEntityIds.Contains(t)).ToList();
//            return new BuildReportRequest(Id, truncTubes, Start, End, ReportId);
//        }
//    }
//}
