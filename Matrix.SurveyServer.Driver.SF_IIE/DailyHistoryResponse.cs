//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    class DailyHistoryResponse
//    {
//        private readonly List<Data> records = new List<Data>();
//        public IEnumerable<Data> Records
//        {
//            get
//            {
//                return records;
//            }
//        }

//        public bool HasMore { get; private set; }

//        public DailyHistoryResponse(byte[] bytes)
//        {
//            var channel = bytes[4];
//            var days = bytes[5];
//            var status = bytes[6];

//            HasMore = status == 1;

//            int offset = 7;
//            for (int i = 0; i < days; i++)
//            {
//                var date = new DateTime(2000 + bytes[offset + 2], bytes[offset + 0], bytes[offset + 1]);

//                records.Add(new Data("Q" + channel, Matrix.Common.Agreements.MeasuringUnitType.m3, date, BitConverter.ToSingle(bytes, offset + 3)));
//                records.Add(new Data("E" + channel, Matrix.Common.Agreements.MeasuringUnitType.MDj, date, BitConverter.ToSingle(bytes, offset + 7)));
//                records.Add(new Data("dP" + channel, Matrix.Common.Agreements.MeasuringUnitType.kPa, date, BitConverter.ToSingle(bytes, offset + 11)));
//                records.Add(new Data("P" + channel, Matrix.Common.Agreements.MeasuringUnitType.kPa, date, BitConverter.ToSingle(bytes, offset + 15)));
//                records.Add(new Data("T" + channel, Matrix.Common.Agreements.MeasuringUnitType.C, date, BitConverter.ToSingle(bytes, offset + 19)));
//                records.Add(new Data("Qi" + channel, Matrix.Common.Agreements.MeasuringUnitType.m3, date, BitConverter.ToInt32(bytes, offset + 23)));

//                offset += 27;
//            }
//        }
//    }
//}
