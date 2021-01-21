//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.Common.Agreements;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class HourResponse : Response
//    {
//        public DateTime Hour { get; private set; }
//        public List<Data> Hours { get; private set; }

//        public HourResponse(byte[] data)
//            : base(data)
//        {
//            Hours = new List<Data>();
//            Hour = DateTime.MinValue;

//            if (Length != 0)
//            {
//                var timeSeconds = BitConverter.ToUInt32(Body, 0);
//                Hour = new DateTime(1997, 01, 01).AddSeconds(timeSeconds).AddHours(-1); //коррекция

//                var err = Body[4];
//                Hours.Add(new Data("err(суммарный код ошибки за час)", MeasuringUnitType.Unknown, Hour, err));

//                var Qr = BitConverter.ToSingle(Body, 5);
//                Hours.Add(new Data("Qr(расход газа в р.у. за час)", MeasuringUnitType.m3, Hour, Qr));

//                var P = BitConverter.ToSingle(Body, 9);
//                Hours.Add(new Data("P(среднечасовое давление)", MeasuringUnitType.kgs_kvSm, Hour, P));

//                var T = BitConverter.ToSingle(Body, 13);
//                Hours.Add(new Data("T(среднечасовая температура)", MeasuringUnitType.C, Hour, T));

//                var Q = BitConverter.ToSingle(Body, 17);
//                Hours.Add(new Data("Q(расход газа прив. к н.у. за час)", MeasuringUnitType.m3, Hour, Q));

//                var W = BitConverter.ToSingle(Body, 21);
//                Hours.Add(new Data("W(теплота сгорания за час)", MeasuringUnitType.GDj, Hour, W));

//            }
//        }
//    }
//}
