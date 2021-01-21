//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.Goboy
//{
//    class GoboyCrcCalculator : ICrcCalculator
//    {
//        public int CrcDataLength
//        {
//            get { return 2; }
//        }

//        public Crc Calculate(byte[] buffer, int offset, int length)
//        {
//            Int16 sum = (Int16)buffer.Skip(offset).Take(length).Sum(d => d);
//            return new Crc(new byte[]
//            {
//                Helper.GetLowByte(sum),
//                Helper.GetHighByte(sum)				
				
//            });
//        }
//    }
//}
