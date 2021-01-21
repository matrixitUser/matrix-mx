using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.MercuryPLCVirtual
{
    internal enum CcWorkMode
    {
        [Description("Обычный")]
        Normal = 0,
        [Description("Master (SR)")]
        MasterSR,
        [Description("Slave (SRT)")]
        SlaveSRT,
        [Description("Slave (SR)")]
        SlaveSR
    }

    internal class CcConfiguration
    {
        public bool Success { get; private set; }
        public byte[] RawData { get; private set; }
        public byte ConfigByte { get; private set; }
        public UInt16 NetSize { get; private set; }
        public bool TransparentMode { get; private set; }
        public bool ZeroTolenace { get; private set; }
        public CcWorkMode Mode { get; private set; }

        public CcConfiguration(byte[] data, int startIndex, int length)
        {
            Success = false;
            if (data.Length < (startIndex + length)) return;
            if (length != 4) return;

            RawData = data.Skip(startIndex).Take(length).ToArray();
            NetSize = BitConverter.ToUInt16(RawData, 1);
            ConfigByte = RawData[3];
            TransparentMode = (ConfigByte & 0x01) > 0;
            ZeroTolenace = (ConfigByte & 0x02) > 0;
            Mode = (CcWorkMode)((ConfigByte & 0x0C) >> 2);
            Success = true;
        }
    }
}
