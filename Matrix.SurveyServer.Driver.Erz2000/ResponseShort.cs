//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    class ResponseShort : Response
//    {
//        public short Value { get; private set; }
//        public ResponseShort(byte[] data)
//            : base(data)
//        {
//            Value = Helper.ToInt16(data, 3);
//        }

//        public override string ToString()
//        {
//            return string.Format("получено число {0}", Value);
//        }
//    }
//}
