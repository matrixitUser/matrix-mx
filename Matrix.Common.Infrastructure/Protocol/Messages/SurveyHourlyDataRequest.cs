//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос на опрос суточных данных
//    /// </summary>
//    public class SurveyHourlyDataRequest : Message, IInitiator
//    {
//        public DateTime DateStart { get; private set; }
//        public DateTime DateEnd { get; private set; }
//        public IEnumerable<Guid> TubeIds { get; private set; }
//        public bool OnlyHoles { get; private set; }

//        public SurveyHourlyDataRequest(Guid id, IEnumerable<Guid> tubeIds, DateTime dateStart, DateTime dateEnd, bool onlyHoles, Guid initiatorId)
//            : base(id)
//        {
//            InitiatorId = initiatorId;
//            TubeIds = tubeIds.ToList();
//            DateStart = dateStart;
//            DateEnd = dateEnd;
//            OnlyHoles = onlyHoles;
//        }

//        public override string ToString()
//        {
//            return string.Format("запрос на опрос часового архива точек учета ({0} шт) с {1:dd.MM.yy HH:mm} до {2:dd.MM.yy HH:mm} {3}", TubeIds.Count(), DateStart, DateEnd, OnlyHoles ? "только дыры" : "все");
//        }

//        public Guid InitiatorId { get; private set; }
//    }
//}
