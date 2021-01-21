//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.EK270
//{
//    class BccCalculator : ICrcCalculator
//    {
//        public int CrcDataLength
//        {
//            get { return 1; }
//        }

//        public Crc Calculate(byte[] buffer, int offset, int length)
//        {
//            char bcc = Encoding.ASCII.GetChars(new byte[] { buffer[offset] })[0];

//            if (length > 2)
//            {
//                for (int i = offset + 1; i < length + offset; i++)
//                {
//                    bcc ^= Encoding.ASCII.GetChars(new byte[] { buffer[i] })[0];
//                }
//            }

//            return new Crc(new byte[] { Encoding.ASCII.GetBytes(new char[] { bcc })[0] });
//        }
//    }
//}
