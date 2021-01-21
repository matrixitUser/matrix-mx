using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrix.Poll.Driver.TEM104
{
    public class SysCon
    {
        public enum SysType
        {
            RashV = 0,
            RashM,
            Magst,
            Podacha,
            Obrat,
            TupikGVS,
            PodpNSO,
            PodrIstoch,
            PodachaPR,
            reserved,
            Otkr,
            GVSsRecerc,
            Istoch
        }

        public static int GetChannelsG(SysType s)
        {
            if ((int)s < 0x08) return 1;
            if ((int)s < 0x0c) return 2;
            return 3;
        }

        public static int GetChannelsPorT(SysType s)
        {
            if ((int)s < 0x01) return 0;
            if ((int)s < 0x03) return 1;
            if ((int)s < 0x0a) return 2;
            return 3;
        }

        SysCon()
        {
            Gprog = new byte[4];
            Gchan = new byte[4];
            Tprog = new byte[4];
            Tchan = new byte[4];
            Pprog = new byte[4];
            Pchan = new byte[4];
        }

        public SysType sysType { get; private set; }
        public byte[] Gprog { get; private set; }
        public byte[] Gchan { get; private set; }
        public byte[] Tprog { get; private set; }
        public byte[] Tchan { get; private set; }
        public byte[] Pprog { get; private set; }
        public byte[] Pchan { get; private set; }


        public static SysCon Parse(byte[] data, int offset)
        {
            if (data == null || data.Length < 0x19 + offset) return null;

            var result = new SysCon();
            result.sysType = (SysType)data[offset + 0x00];
            for (int i = 0; i < 4; i++)
            {
                result.Gprog[i] = data[offset + 0x01 + i];
                result.Gchan[i] = data[offset + 0x05 + i];
                result.Tprog[i] = data[offset + 0x09 + i];
                result.Tchan[i] = data[offset + 0x0d + i];
                result.Pprog[i] = data[offset + 0x11 + i];
                result.Pchan[i] = data[offset + 0x15 + i];
            }

            return result;
        }
    }
}
