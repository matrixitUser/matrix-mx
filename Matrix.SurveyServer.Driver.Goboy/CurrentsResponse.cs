//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class CurrentsResponse : Response
//    {
//        public IEnumerable<Data> Records { get; private set; }

//        public DateTime Date { get; private set; }

//        public CurrentsResponse(byte[] data)
//            : base(data)
//        {
//            if (Body.Length < 25) throw new Exception("длинна пакета меньше допустимой");

//            var date = new DateTime(
//                2000 + Body[5],
//                Body[4],
//                Body[3],
//                Body[2],
//                Body[1],
//                Body[0]
//            );

//            Date = date;

//            var records = new List<Data>();
//            var rate = BitConverter.ToSingle(Body, 6);
//            records.Add(new Data(Glossary.Rate, Matrix.Common.Agreements.MeasuringUnitType.m3, date, rate));

//            var normalRate = BitConverter.ToSingle(Body, 10);
//            records.Add(new Data(Glossary.NormRate, Matrix.Common.Agreements.MeasuringUnitType.m3, date, normalRate));

//            var p = BitConverter.ToSingle(Body, 14);
//            records.Add(new Data(Glossary.P, Matrix.Common.Agreements.MeasuringUnitType.kPa, date, p));

//            var t = BitConverter.ToSingle(Body, 18);
//            records.Add(new Data(Glossary.T, Matrix.Common.Agreements.MeasuringUnitType.C, date, t));

//            var timeOff = BitConverter.ToInt16(Body, 22);
//            records.Add(new Data(Glossary.TimeError, Matrix.Common.Agreements.MeasuringUnitType.h, date, timeOff));

//            var acc = Body[24];
//            records.Add(new Data(Glossary.Acc, Matrix.Common.Agreements.MeasuringUnitType.Unknown, date, acc));

//            Records = records;
//        }
//    }
//}
