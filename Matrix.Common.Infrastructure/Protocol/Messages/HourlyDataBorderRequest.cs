//using System;
//using System.Collections.Generic;
//using System.Runtime.Serialization;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// запрос граничных значений для часового архива
//    /// нужен для отчетов по квартирным счетчикам, например	
//    /// ---[----++++++-----]---
//    ///	        ^    ^
//    ///	    	возвращаемые данные		
//    /// </summary>
//    public class HourlyDataBorderRequest : Message
//    {
//        public DateTime DateStart { get; set; }
//        public DateTime DateEnd { get; set; }
//        public List<Guid> TubeIds { get; set; }

//        public HourlyDataBorderRequest(List<Guid> tubeIds, DateTime dateStart, DateTime dateEnd)
//            : base(Guid.NewGuid())
//        {
//            DateStart = dateStart;
//            DateEnd = dateEnd;
//            TubeIds = tubeIds;
//        }
//        public HourlyDataBorderRequest()
//            : base(Guid.NewGuid())
//        { }
//    }
//}
