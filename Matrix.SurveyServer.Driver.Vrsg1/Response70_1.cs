//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.Common.Agreements;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.Vrsg1
//{
//    class Response70_1 : Response
//    {
//        private readonly List<Data> records = new List<Data>();
//        public IEnumerable<Data> Records { get { return records; } }

//        public bool Finish { get; private set; }

//        public Response70_1(byte[] bytes)
//            : base(bytes)
//        {
//            var channel = bytes[3];
//            var N = bytes[5];

//            Finish = N == 0;

//            var offset = 6;
//            for (var i = 0; i < N; i++)
//            {
//                var minute = bytes[offset + 0];
//                var hour = bytes[offset + 1];
//                var day = bytes[offset + 2];
//                var month = bytes[offset + 3];
//                var year = bytes[offset + 4] + 2000;

//                var date = new DateTime(year, month, day, hour, minute, 0);
//                date = date.AddHours(-1);

//                var vnu = BitConverter.ToUInt32(bytes, offset + 9);
//                records.Add(new Data("Vну" + channel, MeasuringUnitType.nKubM, date, vnu));

//                var vru = BitConverter.ToUInt32(bytes, offset + 13);
//                records.Add(new Data("Vраб" + channel, MeasuringUnitType.m3, date, vru));

//                var qnu = BitConverter.ToUInt32(bytes, offset + 17);
//                records.Add(new Data("Qну" + channel, MeasuringUnitType.nKubM, date, qnu));

//                var qru = BitConverter.ToUInt32(bytes, offset + 21);
//                records.Add(new Data("Qраб" + channel, MeasuringUnitType.m3, date, qru));

//                var p = BitConverter.ToSingle(bytes, offset + 25);
//                records.Add(new Data("P" + channel, MeasuringUnitType.kPa, date, p));

//                var t = BitConverter.ToSingle(bytes, offset + 29);
//                records.Add(new Data("T" + channel, MeasuringUnitType.C, date, t));

//                offset += 33;
//            }
//        }
//    }
//}
