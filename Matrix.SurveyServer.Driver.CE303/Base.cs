using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.CE303
{
    public partial class Driver
    {
        public const byte NAKL = 0x2F;
        public const byte VOPROS = 0x3F;
        public const byte VOSKL = 0x21;
        public const byte CR = 0x0D;
        public const byte LF = 0x0A;
        public const byte ASK = 0x06;
        public const byte SOH = 0x01;
        public const byte ETX = 0x03;
        public const byte STX = 0x02;
        public const byte Z = 0x5A;  //Символ Z
        public const byte B0 = 0xB0;  //Символ Z

        public List<byte> R1 = new List<byte>(Encoding.Default.GetBytes("R1"));
        public List<byte> P1 = new List<byte>(Encoding.Default.GetBytes("P1"));

        public byte ComputeВCC(IList<byte> bytes, int begBCC)
        {
            byte bcc = 0;
            for (int i = begBCC; i < bytes.Count; i++)
            {
                bcc = (byte)(bcc + bytes[i]);
            }

            return (byte)(bcc);
        }

        /// <summary>
        /// Response common parsing
        /// </summary>
        /// <param name="rsp"></param>
        /// <returns></returns>

        dynamic ParseResponse(dynamic answer)
        {
            //dynamic answer = new ExpandoObject();
            //answer.success = false;
            //answer.error = "";

            //if (rsp == null || rsp.Length == 0)
            //{
            //    answer.error = "нет ответа";
            //    return answer;
            //}

            //if (rsp.Length == 1 && rsp[0] == 0x15)
            //{
            //    answer.error = "отрицательный ответ : NAK";
            //    return answer;
            //}

            //if (rsp.Length < 5)
            //{
            //    answer.error = "в кадре ответа не может содежаться менее 5 байт";
            //    return answer;
            //}

            //answer.rsp = rsp;
            //answer.text = Encoding.Default.GetString(rsp);
            //answer.success = true;
            return answer;
        }

        Request MakePingRequest()
        {
            Request request = new Request();
            request.Name = "";

            var address = new List<byte>(Encoding.Default.GetBytes(serial));
            List<byte> Data = new List<byte> { NAKL, VOPROS };
            List<byte> DataEnd = new List<byte> { VOSKL, CR, LF };
            //if (address.Count > 0) Data.AddRange(address);
            Data.AddRange(DataEnd);
            //return Data.ToArray();
            request.bytes = Data.ToArray();
            return request;
        }

        Request MakeSessionStop()
        {
            Request request = new Request();
            request.Name = "";
            List<byte> Data = new List<byte> { SOH };
            int begВcc = Data.Count;
            Data.Add(0x42);//
            Data.Add(0x30);
            //Data.Add(B0);
            Data.Add(ETX);
            Data.Add(ComputeВCC(Data, begВcc));

            request.bytes = Data.ToArray();
            return request;
        }

        ///

        Request MakeAskNOptionRequest(byte regim)
        {
            Request request = new Request();
            request.Name = "";

            List<byte> Data = new List<byte> { ASK, 0x30, Z, regim, CR, LF };
            //return Data.ToArray();
            request.bytes = Data.ToArray();
            return request;
        }

        ///
        Request MakeDataRequestFromBytes(List<byte> bytes)
        {
            Request request = new Request();
            request.Name = "Default";
            request.bytes = bytes.ToArray();
            return request;
        }

        Request MakeDataRequest(string NameParameter)
        {
            Request request = new Request();
            request.Name = NameParameter;

            var NAMEParameter = new List<byte>(Encoding.Default.GetBytes(NameParameter));
            List<byte> Data = new List<byte> { SOH };
            int begВcc = Data.Count;
            Data.AddRange(R1);
            Data.Add(STX);
            Data.AddRange(NAMEParameter);
            Data.Add(ETX);
            Data.Add(ComputeВCC(Data, begВcc));

            //return Data.ToArray();
            request.bytes = Data.ToArray(); 
            return request;
        }


        ///

        dynamic ParseEndxxResponse(dynamic answer, string nameParameter, DateTime date)
        {
            if (!answer.success) return answer;
            var recs = new List<dynamic>();

            var lData = DriverHelper.responceToParameters(nameParameter, answer.rsp, 0);

            double summa = System.Double.Parse(lData[0].Replace(".",","));

            date = date.AddDays(1); //" на конец суток" 23:59:59 => следующие сутки 00:00:00
            switch (nameParameter)
	        {
                   case "ENDPE": nameParameter=  "Энергия активная потребленная";break;
                   case "ENDPI": nameParameter = "Энергия активная отпущенная"; break;
                   case "ENDQE": nameParameter = "Энергия реактивная потребленная"; break;
                   case "ENDQI": nameParameter = "Энергия реактивная отпущенная"; break;
	        }

            recs.Add(MakeDayRecord(nameParameter, (float)summa, "кВт", date));
            recs.Add(MakeDayRecord(nameParameter + " на конец суток", (float)summa, "кВт", date.AddDays(-1)));

            answer.records = recs;

            return answer;
 		}

        dynamic ParseGraxxResponse(dynamic answer, string nameParameter, DateTime date, int tAver)
        {
            if (!answer.success) return answer;

            int nDiapasone = (int)(60.0 / tAver); // число дипазонов внутри часа и он же коэфициент на который надо разделить мощности по диапазонам

            var recs = new List<dynamic>();

            var lData = DriverHelper.responceToParameters(nameParameter, answer.rsp, 0);

            double summa = 0;

            foreach (var item in lData)
            {
                try
                {
                    summa = summa + System.Double.Parse(item.Replace(".", ","));
                }
                catch (Exception)
                {
                    summa = -1;
                }
            }
            switch (nameParameter)
            {
                case "GRAPE": nameParameter = "Мощность активная потребленная"; break;
                case "GRAPI": nameParameter = "Мощность активная отпущенная"; break;
                case "GRAQE": nameParameter = "Мощность реактивная потребленная"; break;
                case "GRAQI": nameParameter = "Мощность реактивная отпущенная"; break;
            }

            recs.Add(MakeHourRecord(nameParameter, (float)(summa / nDiapasone), "кВт", date));

            answer.records = recs;

            return answer;
        }
    }
}
