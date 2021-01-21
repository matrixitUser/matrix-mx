//using System;

//namespace Matrix.SurveyServer.Driver.SF_IIE
//{
//    public static class ConvertHelper
//    {
//        public static int BinDecToInt(byte binDec)
//        {
//            return (binDec >> 4) * 10 + (binDec & 0x0f);
//        }
//        public static int BinDecToInt32(byte[] binDec, int offset, bool special = false)
//        {
//            var bcdH = binDec[offset + 0];
//            var result = special ? BinDecToInt((byte)(bcdH & 0x0f)) : BinDecToInt(bcdH) * 1000000 +
//                BinDecToInt(binDec[offset + 1]) * 10000 +
//                BinDecToInt(binDec[offset + 2]) * 100 +
//                BinDecToInt(binDec[offset + 3]) * 1;
//            return special ? (int)(result * Math.Pow(10, 3 - BinDecToInt((byte)(bcdH >> 4)))) : result;
//        }
//        public static byte IntToBinDec(int toBCD)
//        {
//            byte result = 0xFF;
//            if (toBCD < 100)
//            {
//                result = 0;
//                result |= (byte)(toBCD % 10);
//                toBCD /= 10;
//                result |= (byte)((toBCD % 10) << 4);
//            }
//            return result;
//        }
//        public static byte ByteLow(int getLow)
//        {
//            return (byte)(getLow & 0xFF);
//        }
//        public static byte ByteHigh(int getHigh)
//        {
//            return (byte)((getHigh >> 8) & 0xFF);
//        }
//    }
//}
