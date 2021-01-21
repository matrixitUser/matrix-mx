//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    class IdResponse
//    {
//        public byte TubesCount { get; private set; }
//        public string Tube1Name { get; private set; }
//        public string Tube2Name { get; private set; }
//        public string Tube3Name { get; private set; }

//        public byte ContractHour { get; private set; }

//        public DateTime CurrentDate { get; private set; }

//        public IdResponse(byte[] bytes)
//        {
//            TubesCount = bytes[4];

//            Tube1Name = Encoding.ASCII.GetString(bytes, 5, 16);
//            Tube2Name = Encoding.ASCII.GetString(bytes, 22, 16);
//            Tube3Name = Encoding.ASCII.GetString(bytes, 39, 16);

//            CurrentDate = new DateTime(2000 + bytes[58], bytes[56], bytes[57], bytes[59], bytes[60], bytes[61]);
//            ContractHour = bytes[62];
//        }
//    }
//}
