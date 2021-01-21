//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.Common.Agreements;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class DayResponse : Response
//    {
//        public DateTime Day { get; private set; }
//        public List<Data> Days { get; private set; }

//        public DayResponse(byte[] data)
//            : base(data)
//        {
//            Days = new List<Data>();
//            Day = DateTime.MinValue;

//            if (Length != 0)
//            {
//                var timeSeconds = BitConverter.ToUInt32(Body, 0);
//                Day = new DateTime(1997, 01, 01).AddSeconds(timeSeconds).AddDays(-1); //коррекция

//                var err = Body[4];
//                Days.Add(new Data("err(суммарный код ошибки за сутки)", MeasuringUnitType.Unknown, Day, err));

//                var Qr = BitConverter.ToSingle(Body, 5);
//                Days.Add(new Data("Qr(расход газа в р.у. за сутки)", MeasuringUnitType.m3, Day, Qr));

//                var P = BitConverter.ToSingle(Body, 9);
//                Days.Add(new Data("P(среднесуточное давление)", MeasuringUnitType.kgs_kvSm, Day, P));

//                var T = BitConverter.ToSingle(Body, 13);
//                Days.Add(new Data("T(среднесуточная температура)", MeasuringUnitType.C, Day, T));

//                var Q = BitConverter.ToSingle(Body, 17);
//                Days.Add(new Data("Q(расход газа прив. к н.у. за сутки)", MeasuringUnitType.m3, Day, Q));

//                var W = BitConverter.ToSingle(Body, 21);
//                Days.Add(new Data("W(теплота сгорания за сутки)", MeasuringUnitType.GDj, Day, W));

//            }
//        }
        
//    }
//}
