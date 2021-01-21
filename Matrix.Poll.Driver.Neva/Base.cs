using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.Neva
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

        public List<byte> R1 = new List<byte>(Encoding.Default.GetBytes("R1"));
        public List<byte> P1 = new List<byte>(Encoding.Default.GetBytes("P0"));

        public byte ComputeВCC(IList<byte> bytes, int begBCC)
        {
            byte bcc = 0;
            for (int i = begBCC; i < bytes.Count; i++)
            {
                bcc ^= bytes[i];
            }

            return bcc;
        }
        
        byte[] MakePingRequest()
        {
            var address = new List<byte>(Encoding.Default.GetBytes(serial));
            List<byte> Data = ASCIIEncoding.ASCII.GetBytes("/?").ToList();
            List<byte> DataEnd = ASCIIEncoding.ASCII.GetBytes("!\r\n").ToList(); //new List<byte> { VOSKL, CR, LF };
            if (address.Count > 0) Data.AddRange(address);
            Data.AddRange(DataEnd);
            return Data.ToArray();
        }


        ///

        byte[] MakeAskNOptionRequest(byte regim)
        {
            List<byte> Data = new List<byte> { ASK, 0x30, 0x35, regim, CR, LF };
            return Data.ToArray();
        }

        byte[] MakePassRequest(string password = "")
        {
            if ((password == null) || (password == "")) password = "00000000";
            List<byte> data = new List<byte>();
            data.Add(SOH);
            int begВcc = data.Count;
            data.AddRange(ASCIIEncoding.ASCII.GetBytes("P1").ToList());
            data.Add(STX);
            data.AddRange(ASCIIEncoding.ASCII.GetBytes("(" + password + ")").ToList());
            data.Add(ETX);
            data.Add(ComputeВCC(data, begВcc));
            return data.ToArray();
        }

        ///
        
        byte[] MakeDataRequest(string NameParameter)
        {
            var NAMEParameter = new List<byte>(Encoding.Default.GetBytes(NameParameter));
            List<byte> Data = new List<byte> { SOH };
            int begВcc = Data.Count;
            Data.AddRange(R1);
            Data.Add(STX);
            Data.AddRange(NAMEParameter);
            Data.Add(ETX);
            Data.Add(ComputeВCC(Data, begВcc));
            return Data.ToArray();
        }



        private byte[] MakeSessionByeRequest()
        {
            var bytes = new List<byte>
            {
                SOH,
                0x42,
                0x30, //B0
                ETX
            };
            bytes.Add(ComputeВCC(bytes, 1));
            //var crc = Crc.Calc(bytes, 1, 3, new BccCalculator()).CrcData;
            //var crc = CalcCrc(bytes, 1, 3);
            //bytes[4] = crc;
            return bytes.ToArray();
        }

        ///

        dynamic ParsePingResponse(dynamic answer)
        {
            if (!answer.success) return answer;

            string text = answer.text;
            answer.text = new string((text.Take(text.Length - 2).Skip(1) as IEnumerable<char>).ToArray());
            return answer;
        }

        public dynamic ParseValueArray(dynamic answer)
        {
            if (!answer.success) return answer;

            string text = Encoding.Default.GetString(answer.rsp);
            int start = text.IndexOf("(") + 1;
            int end = text.IndexOf(")", start);
            List<string> texts = text.Substring(start, end - start).Split(',').ToList();
            log(string.Format("TEXTS: {0}", string.Join("; ", texts)), 3);
            List<double> values = texts.Select(t => double.Parse(t.Replace(".", ","))).ToList();
            log(string.Format("VALUES: {0}", string.Join("; ", values.Select(d => string.Format("{0:0.###}", d)))), 3);
            answer.texts = texts;
            answer.values = values;
            return answer;
        }

        //     dynamic ParseEndxxResponse(dynamic answer, string nameParameter, DateTime date)
        //     {
        //         if (!answer.success) return answer;

        //         var recs = new List<dynamic>();

        //         var lData = DriverHelper.responceToParameters(nameParameter, answer.rsp, 0);

        //         double summa = System.Double.Parse(lData[0].Replace(".",","));

        //         switch (nameParameter)
        //      {
        //                case "ENDPE": nameParameter=  "Энергия активная потребленная на конец суток";break;
        //                case "ENDPI": nameParameter = "Энергия активная отпущенная на конец суток"; break;
        //                case "ENDQE": nameParameter = "Энергия реактивная потребленная на конец суток"; break;
        //                case "ENDQI": nameParameter = "Энергия реактивная отпущенная на конец суток"; break;
        //      }

        //         recs.Add(MakeDayRecord(nameParameter, (float)summa, "кВт", date));

        //         answer.records = recs;

        //         return answer;
        //}

        //     dynamic ParseGraxxResponse(dynamic answer, string nameParameter, DateTime date, int tAver)
        //     {
        //         if (!answer.success) return answer;

        //         int nDiapasone = (int)(60.0 / tAver); // число дипазонов внутри часа и он же коэфициент на который надо разделить мощности по диапазонам

        //         var recs = new List<dynamic>();

        //         var lData = DriverHelper.responceToParameters(nameParameter, answer.rsp, 0);

        //         double summa = 0;

        //         foreach (var item in lData)
        //         {
        //             try
        //             {
        //                 summa = summa + System.Double.Parse(item.Replace(".", ","));
        //             }
        //             catch (Exception)
        //             {
        //                 summa = -1;
        //             }
        //         }
        //         switch (nameParameter)
        //         {
        //             case "GRAPE": nameParameter = "Мощность активная потребленная"; break;
        //             case "GRAPI": nameParameter = "Мощность активная отпущенная"; break;
        //             case "GRAQE": nameParameter = "Мощность реактивная потребленная"; break;
        //             case "GRAQI": nameParameter = "Мощность реактивная отпущенная"; break;
        //         }

        //         recs.Add(MakeHourRecord(nameParameter, (float)(summa / nDiapasone), "кВт", date));

        //         answer.records = recs;

        //         return answer;
        //     }
    }
}
