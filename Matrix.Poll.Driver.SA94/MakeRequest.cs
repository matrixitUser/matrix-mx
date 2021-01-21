using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.SA94
{
    public enum Segment
    {
        Service1,
        Service2,
        Hourly1,
        Hourly2,
        Daily1,
        Daily2,
        Abnormal1,
        Abnormal2,
        SegmentCount
    }

    public static class MakeRequest
    {
        public static byte[] DeviceSelect(UInt32 deviceId)
        {
            return new byte[]
            {
            (byte)(0xC0 | ((deviceId >> 14) & 0x3F)),
            (byte)((deviceId >> 7) & 0x7F),
            (byte)(deviceId & 0x7F)
            };
        }

        public static byte[] DeviceUnselect()
        {
            return new byte[] { 0xFF };
        }

        public static byte[] ReadCurrentParameters(byte parameterN)
        {
            return new byte[] { (byte)(0x80 | (parameterN & 0x0F)) };
        }

        // 8 сегментов по 16 Кбайт = 128Кбайт
        public static byte[] ReadStatistics(Segment S, UInt16 a)
        {
            return new byte[] {
                (byte)(0xA0 | ((byte)S & 0x7)),
                (byte)((a >> 7) & 0x7F)
            };
        }

        // 8 сегментов по 64 Кбайт = 512Кбайт
        public static byte[] ReadStatisticsExt(Segment S, UInt16 a)
        {
            return new byte[] {
                0xB7,
                (byte)(0x40 | (((byte)S & 0x7) << 3) | ((a >> 13) & 0x07)),
                (byte)((a >> 7) & 0x3F)
            };
        }

        //
        public static byte[] ReadStatisticsBlockByVer(Segment S, int blockN, bool isExtended)
        {
            UInt16 addr = (UInt16)(blockN * Version.BlockSize);
            return isExtended ? ReadStatisticsExt(S, addr) : ReadStatistics(S, addr);
        }

        public static byte[] ReadNextAddrInStatisticsExt()
        {
            return new byte[] { 0xB8 };
        }

        public static byte[] TimeCorrection(byte hour, byte minute, byte second)
        {
            return new byte[] {
                0xB5,
                0x78,
                Parser.ByteToBCD(hour),
                Parser.ByteToBCD(minute),
                Parser.ByteToBCD(second)
            };
        }

        public static byte[] TimeCorrection(DateTime date)
        {
            return TimeCorrection((byte)date.Hour, (byte)date.Minute, (byte)date.Second);
        }
    }
}
