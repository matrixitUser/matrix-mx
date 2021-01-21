//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.Common.Agreements;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    class DataResponse : Response
//    {
//        public byte Group { get; private set; }
//        public byte Channel { get; private set; }
//        public int Number { get; private set; }

//        public double Value { get; private set; }
//        public DateTime Date { get; private set; }

//        public DataType DataType { get; private set; }

//        public DataResponse(byte[] data)
//            : base(data)
//        {
//            Group = data[2];
//            Channel = data[3];
//            Number = Helper.ToInt32(data, 4);

//            char recordType = (char)data[8];
//            var state = data[9];          /* состояние 0....4 */
//            var year = data[10];          /* Временная отметка в качестве местного времени */
//            var month = data[11];
//            var day = data[12];
//            var hour = data[13];
//            var minute = data[14];
//            var second = data[15];

//            Date = new DateTime(2000 + year, month, day, hour, minute, second);

//            Value = 0.0;
//            switch (recordType)
//            {
//                case 'F':
//                    DataType = DataType.floatType;
//                    Value = Helper.ToSingle(data, 16);
//                    break;
//                case 'L':
//                    DataType = DataType.intType;
//                    Value = Helper.ToUInt32(data, 16);
//                    break;
//                case 'T':
//                    break;
//                case 'Z':
//                    DataType = DataType.intType;
//                    Value = Helper.ToInt32(data, 16) * 1000000000 + Helper.ToInt32(data, 20);
//                    break;
//                default:
//                    break;
//            }
//        }

//        public override string ToString()
//        {
//            return string.Format("запись, группа {0}, канал {1}, дата {2:dd.MM.yyyy HH:mm}, значение {3}",Group,Channel,Date, Value);
//        }
//    }
//    enum DataType
//    {
//        unknown,
//        floatType,
//        intType,
//        dateTimeType
//    }
//}
