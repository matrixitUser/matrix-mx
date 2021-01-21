using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public partial class Driver
    {
        private byte[] MakeAdditionalParametersRequest(byte parameter, BWRI bwri)
        {
            var Data = new List<byte>();
            Data.Add(parameter);
            Data.Add(bwri.GetByte());
            return MakeBaseRequest(0x08, Data);
        }
    }

    class BWRI
    {
        public AdditionalParameterNumber AdditionalParameterNumber { get; private set; }

        public byte Info { get; private set; }

        public BWRI(AdditionalParameterNumber additionalParameterNumber, byte info)
        {
            AdditionalParameterNumber = additionalParameterNumber;
            Info = info;
        }

        public byte GetByte()
        {
            byte left = (byte)((byte)AdditionalParameterNumber << 4 & 0xf0);
            byte right = (byte)(Info & 0x0f);
            return (byte)(left + right);
        }
    }

    enum AdditionalParameterNumber : byte
    {
        /// <summary>
        /// мощность
        /// </summary>
        Power = 0x00,
        /// <summary>
        /// напряжение
        /// </summary>
        Voltage = 0x01,
        /// <summary>
        /// ток
        /// </summary>
        Current = 0x02,
        PowerCoefficient = 0x03,
        Frequency = 0x04,
        FixEnergy = 0x0f
    }
}
