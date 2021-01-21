//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common;
//using Matrix.Common.Agreements;

//namespace Matrix.SurveyServer.Driver.SPG741
//{
//    /// <summary>
//    /// ответ на считывание текущих параметров 1 - 2 труб
//    /// </summary>
//    class CurrentTubeResponse : Response
//    {
//        public List<Data> Currents { get; private set; }
//        /// <summary>
//        /// получение данных
//        /// </summary>
//        /// <param name="data"></param>
//        public CurrentTubeResponse(byte[] data, DateTime date, MeasuringUnits mu)
//            : base(data)
//        {

//            Currents = new List<Data>();

//            //Первая труба
//            int offset = 0;
//            var P1 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var dP1 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var t1 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var Qp1 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var Q1 = Helper.SpgFloatToIEEE(data, offset += 4);

//            Currents.Add(new Data(Glossary.P1, mu.P1, date, P1));
//            Currents.Add(new Data(Glossary.dP1, mu.dP1, date, dP1));
//            Currents.Add(new Data(Glossary.t1, MeasuringUnitType.C, date, t1));
//            Currents.Add(new Data(Glossary.dP1, MeasuringUnitType.m3_h, date, Qp1));
//            Currents.Add(new Data(Glossary.Q1, MeasuringUnitType.m3_h, date, Q1));

//            //Вторая труба			
//            var P2 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var dP2 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var t2 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var Qp2 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var Q2 = Helper.SpgFloatToIEEE(data, offset += 4);

//            Currents.Add(new Data(Glossary.P2, mu.P2, date, P2));
//            Currents.Add(new Data(Glossary.dP2, mu.dP2, date, dP2));
//            Currents.Add(new Data(Glossary.t2, MeasuringUnitType.C, date, t2));
//            Currents.Add(new Data(Glossary.Qp2, MeasuringUnitType.m3_h, date, Qp2));
//            Currents.Add(new Data(Glossary.Q2, MeasuringUnitType.m3_h, date, Q2));

//            //Общие			
//            var dP3 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var Pб = Helper.SpgFloatToIEEE(data, offset += 4);
//            var P3 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var P4 = Helper.SpgFloatToIEEE(data, offset += 4);
//            var t3 = Helper.SpgFloatToIEEE(data, offset += 4);

//            Currents.Add(new Data(Glossary.dP3, mu.dP3, date, dP3));
//            Currents.Add(new Data(Glossary.Pb, mu.Pb, date, Pб));
//            Currents.Add(new Data(Glossary.P3, mu.P3, date, P3));
//            Currents.Add(new Data(Glossary.P4, mu.P4, date, P4));
//            Currents.Add(new Data(Glossary.t3, MeasuringUnitType.C, date, t3));
//        }
//    }

	
//    /// <summary>
//    /// ответ на считывание текущей даты
//    /// </summary>
//    class CurrentTimeResponse : Response
//    {
//        public DateTime Date { get; private set; }

//        public CurrentTimeResponse(byte[] data)
//            : base(data)
//        {

//            int year = Body[0];// data[2];
//            int month =Body[1];// data[3];
//            int day = Body[2];
//            int watch_hh = Body[3];
//            int watch_mm = Body[4];
//            int watch_ss = Body[5];
//            Date = new DateTime(2000 + year, month, day, watch_hh, watch_mm, watch_ss);
//        }
//    }


//    class UnitPressResponse : Response
//    {
//        public byte[] arrayUnitP = new byte[] { };

//        public UnitPressResponse(byte[] data)
//            : base(data)
//        {

//            arrayUnitP = new byte[] { data[3 + 12], data[3 + 28], data[3 + 44], data[3 + 60] };
//            var x = arrayUnitP;
//        }

//    }
//}
