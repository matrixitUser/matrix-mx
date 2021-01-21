//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.HyperFlow
//{
//    class HartCrc : ICrcCalculator
//    {
//        public int CrcDataLength
//        {
//            get { return 1; }
//        }

//        public Crc Calculate(byte[] buffer, int offset, int length)
//        {
//            byte crc = buffer[offset];
//            for (int i = offset + 1; i < length; i++)
//            {
//                crc ^= buffer[i];
//            }
//            return new Crc(new byte[] { crc });
//        }
//    }
//}
