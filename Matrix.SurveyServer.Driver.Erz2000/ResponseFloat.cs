//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Erz2000
//{
//    class ResponseFloat : Response
//    {
//        public float Value { get; private set; }

//        public ResponseFloat(byte[] data)
//            : base(data)
//        {
//            Value = Helper.ToSingle(data, 3);
//        }

//        public override string ToString()
//        {
//            return string.Format("получено число {0}", Value);
//        }
//    }
//}
