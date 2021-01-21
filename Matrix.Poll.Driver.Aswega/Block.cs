using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SA94
{
    class Block
    {
        public DateTime DateTime { get; set; }
        public DateTime Date { get; set; }
        public byte[] Bytes { get; set; }

        public Block(byte[] bytes, int startIndex)
        {
            if (bytes != null) 
            DateTime = DriverHelper.ParseDateTime(bytes, startIndex);
            if (bytes != null && startIndex + 64 <= bytes.Length)
            {
                Bytes = new byte[64];
                Array.Copy(bytes, startIndex, Bytes, 0, 64);
                for (int i = 0; i < Bytes.Length; i++)
                {
                    if (Bytes[i] != 0xFF)
                    {
                        IsValid = true;
                        break;
                    }
                }
            }

            if (bytes != null)
            Date = DriverHelper.ParseDate(bytes, startIndex);
            if (bytes != null && startIndex + 64 <= bytes.Length)
            {
                Bytes = new byte[64];
                Array.Copy(bytes, startIndex, Bytes, 0, 64);
                for (int i = 0; i < Bytes.Length; i++)
                {
                    if (Bytes[i] != 0xFF)
                    {
                        IsValid = true;
                        break;
                    }
                }
            }

        }

        public bool IsValid { get; private set; }
    }
}
