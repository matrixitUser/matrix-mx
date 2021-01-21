//using System;
//using System.Collections.Generic;
//using Matrix.Domain.Entities;

//namespace Matrix.Common.Infrastructure.Protocol.Messages
//{
//    /// <summary>
//    /// часовые показания на границах заданного интервала
//    /// </summary>
//    public class HourlyDataBorderResponse : Message
//    {
//        public List<TubeBorder> TubeBorders { get; set; }

//        public HourlyDataBorderResponse()
//            : base(Guid.NewGuid())
//        {
//            TubeBorders = new List<TubeBorder>();
//        }
//    }

//    public class TubeBorder
//    {
//        public Guid TubeId { get; set; }
//        public List<HourlyData> LeftBorder { get; set; }
//        public List<HourlyData> RightBorder { get; set; }
//    }
//}
