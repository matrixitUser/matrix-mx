//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Matrix.SurveyServer.Driver.Common.Crc;

//namespace Matrix.SurveyServer.Driver.SPG761
//{
//    /// <summary>
//    /// контрольные суммы, используемые при работе с счетчиком СПГ761
//    /// </summary>
//    public class CrcSpg : ICrcCalculator
//    {
//        public int CrcDataLength
//        {
//            get { return 2; }
//        }

//        public Crc Calculate(byte[] buffer, int offset, int length)
//        {
//            //var range = buffer.Skip(offset).Take(length).Reverse().ToArray();

//            //var correct = 0;
//            //foreach(var b in range)
//            //{
//            //    if (b == 0x03) break;
//            //    correct++;
//            //}

//            //length -= correct;

//            ushort crc = 0;

//            for (ushort i = (ushort)offset; i < offset + length; i++)
//            {
//                crc = (ushort)(crc ^ (ushort)(buffer[i] << 8));
//                for (ushort j = 0; j < 8; j++)
//                {
//                    if ((ushort)(crc & 0x8000) != 0) crc = (ushort)((crc << 1) ^ 0x1021);
//                    else crc <<= 1;
//                }
//            }

//            Crc spgCrc = new Crc(new byte[] { (byte)(crc >> 8), (byte)(crc & 0x00FF) });

//            //var clear = buffer.Skip(offset).Take(length).Reverse().Take(2).ToArray();
//            //if (clear[0] == 0x57 && clear[1] == 0x08) return new Crc(new byte[] { 0x19, 0x4f });

//            return spgCrc;
//        }
//    }
//}
