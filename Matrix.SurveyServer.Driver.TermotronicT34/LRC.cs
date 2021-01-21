using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.TV7
{
    public static class LRC
    {
        public static byte Lrc(byte[] pSrc, int length)
        {
            byte locLrc = 0;
            for (int i = 0; i < length; i++)
                locLrc += pSrc[i];
            return locLrc = (byte)(~locLrc + 1);
        }

        public static bool Check(byte[] buffer)
        {
            bool success = false;
            byte lrsSet = LRC.Lrc(buffer, buffer.Length - 1);

            if(lrsSet == buffer[buffer.Length-1])
            {
                success = true;
            }

            return success;
        }
    }
}
