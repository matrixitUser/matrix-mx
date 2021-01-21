using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SwitchADC
{
    public enum ADCcmd : byte
    {
        ADCstop  = 0x00,
        ADCstart = 0x01,
        ADCread  = 0x03,
        ADCstat  = 0x04
    }

    public enum ADCanswer : byte
    {
        ADCstarted = 0x00,
        ADCstoped = 0x01,
        ADCread = 0x03,
        ADCstat = 0x04,
        ADCerror= 0xFF
    }



    public partial class Driver
    {
        private dynamic GetADC(byte na, byte channel, byte cmd, bool is32bit = false)
        {
            var adc = Send(MakeADCReq(na, channel, cmd));
            return is32bit? ParseADC32(adc) : ParseADC(adc);
        }

        private dynamic ParseADC(byte[] bytes)
        {
            var adc = ParseResp(bytes);
            if (!adc.success) return adc;

            adc.state = bytes[5];
            adc.value = BitConverter.ToInt16(bytes, 6);

            return adc;
        }

        private dynamic ParseADC32(byte[] bytes)
        {
            var adc = ParseResp(bytes);
            if (!adc.success) return adc;

            adc.state = bytes[5];
            adc.value = BitConverter.ToUInt32(bytes, 6);

            return adc;
        }

        private byte[] MakeADCReq(byte na, byte channel, byte cmd)
        {
            return MakeReq(na, new byte[]
            {
                channel,
                cmd
            });
        }
    }
}
