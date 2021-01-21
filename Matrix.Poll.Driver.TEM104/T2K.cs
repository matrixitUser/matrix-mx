using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Matrix.Poll.Driver.TEM104
{
    public class T2K
    {
        public enum TypeG
        {
            [Description("Частотные")]
            Freq = 0,
            [Description("Импульсные")]
            Pulse = 1
        }
        public enum TypeT
        {
            [Description("Среднеарифметические")]
            SA = 0,
            [Description("Средневзвешенные")]
            SV = 1
        }

        T2K()
        {
            Type_g = new TypeG[12];
            Type_t = new TypeT[12];

            Diam = new int[4];
            G_max = new float[4];
            G_pcnt_max = new byte[4];
            G_pcnt_min = new byte[4];

            F_max = new float[2];
            Weight = new float[2];

            SysConN = new SysCon[4];
        }

        public byte Systems { get; private set; }
        public TypeG[] Type_g { get; private set; }
        public TypeT[] Type_t { get; private set; }

        public int Net_num { get; private set; }
        public int Number { get; private set; }

        public int[] Diam { get; private set; }
        public float[] G_max { get; private set; }
        public byte[] G_pcnt_max { get; private set; }
        public byte[] G_pcnt_min { get; private set; }

        public float[] F_max { get; private set; }
        public float[] Weight { get; private set; }

        public SysInt SysInt_copy { get; private set; }
        public SysCon[] SysConN { get; private set; }

        public static T2K Parse(byte[] data, int offset)
        {
            if (data == null || data.Length < 0x2FF + offset) return null;

            var result = new T2K();

            result.Systems = data[offset + 0];
            for (int i = 0; i < 12; i++)
            {
                result.Type_g[i] = (TypeG)data[offset + 0x01 + i];
                result.Type_t[i] = (TypeT)data[offset + 0x0c + i];
            }
            result.Net_num = BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x78, 4), 0);
            result.Number = BitConverter.ToInt32(ConvertHelper.GetReversed(data, offset + 0x7c, 4), 0);

            for (int i = 0; i < 4; i++)
            {
                result.Diam[i] = BitConverter.ToInt16(ConvertHelper.GetReversed(data, offset + 0xc4 + i * 2, 2), 0);
                result.G_max[i] = BitConverter.ToSingle(ConvertHelper.GetReversed(data, offset + 0xcc + i * 4, 4), 0);
                result.G_pcnt_max[i] = data[offset + 0xdc + i];
                result.G_pcnt_min[i] = data[offset + 0xe0 + i];
            }

            for (int i = 0; i < 2; i++)
            {
                result.F_max[i] = BitConverter.ToSingle(ConvertHelper.GetReversed(data, offset + 0xe4 + i * 4, 4), 0);
                result.Weight[i] = BitConverter.ToSingle(ConvertHelper.GetReversed(data, offset + 0xec + i * 4, 4), 0);//??? 74? EC?
            }

            result.SysInt_copy = SysInt.Parse(data, offset + 0x200);
            for (int i = 0; i < 4; i++)
            {
                result.SysConN[i] = SysCon.Parse(data, offset + 0x600 + i * 0x20);
            }

            return result;
        }
    }
}
