//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class RecordResponse : Response
//    {
//        public IEnumerable<Data> Records { get; private set; }
//        public DateTime Date { get; private set; }

//        public RecordResponse(byte[] data)
//            : base(data)
//        {
//            var records = new List<Data>();

//            if (Body.Length < 20) throw new Exception("длина пакета с записью меньше допустимой");

//            var date = new DateTime(
//                2000 + Body[19 - 1],
//                Body[18 - 1],
//                Body[17 - 1],
//                Body[16 - 1],
//                Body[15 - 1],
//                0
//            );
//            Date = date.AddDays(-1);

//            float vNorm = (float)BitConverter.ToInt32(Body, 0) / 10000f;
//            records.Add(new Data(Glossary.V_work, Matrix.Common.Agreements.MeasuringUnitType.m3, Date, vNorm));

//            float vWork = (float)BitConverter.ToInt32(Body, 4) / 10000f;
//            records.Add(new Data(Glossary.V_norm, Matrix.Common.Agreements.MeasuringUnitType.m3, Date, vWork));

//            float p = (float)BitConverter.ToInt16(Body, 9 - 1) / 10f;
//            records.Add(new Data(Glossary.P, Matrix.Common.Agreements.MeasuringUnitType.kPa, Date, p));

//            float t = (float)BitConverter.ToInt16(Body, 11 - 1) / 100f;
//            records.Add(new Data(Glossary.T, Matrix.Common.Agreements.MeasuringUnitType.C, Date, t));

//            var tOff = BitConverter.ToInt16(Body, 13 - 1);
//            records.Add(new Data(Glossary.NWTime, Matrix.Common.Agreements.MeasuringUnitType.h, Date, tOff));

//            Records = records;
//        }
//    }
//}
