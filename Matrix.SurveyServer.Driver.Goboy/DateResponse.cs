//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class DateResponse : Response
//    {
//        public DateTime Date { get; private set; }

//        public DateResponse(byte[] data)
//            : base(data)
//        {
//            if (Body.Length < 6) throw new Exception("длина пакета с датой не может быть меньше 6");

//            Date = new DateTime(
//                2000 + Body[5],
//                Body[4],
//                Body[3],
//                Body[2],
//                Body[1],
//                Body[0]
//            );
//        }
//    }
//}
