//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class Response
//    {
//        public byte Type { get; private set; }
//        public int SerialNumber { get; private set; }
//        public byte Command { get; private set; }
//        public int Length { get; private set; }
//        public byte[] Body { get; private set; }

//        public Response(byte[] data)
//        {
//            if (data == null) throw new Exception("пакет не содержит данных");
//            if (data.Length < 11) throw new Exception("длинна пакета меньше минимально допустимой");
//            if (!Crc.Check(data, new GoboyCrcCalculator())) throw new Exception("не сошлась контрольная сумма");

//            Type = data[1];
//            SerialNumber = BitConverter.ToInt16(data, 2);
//            Command = data[6];

//            if (data.Length == 11) throw new Exception(string.Format("ошибка при выполнении команды, код {0:X2}", Command));

//            Length = data[7] + data[8] * 255;
//            Body = data.Skip(9).Take(data.Length - 11).ToArray();
//        }
//    }
//}
