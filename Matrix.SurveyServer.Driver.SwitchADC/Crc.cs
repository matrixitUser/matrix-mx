using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SwitchADC
{
    public partial class Driver
    {
        private byte CalcCrc(byte[] bytes)
        {
            byte sum = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                sum += bytes[i];
            }
            return (byte)(0 - sum);
        }

        private bool CheckCrc(byte[] bytes)
        {
            byte sum = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                sum += bytes[i];
            }
            return sum == 0;
        }
    }
}
