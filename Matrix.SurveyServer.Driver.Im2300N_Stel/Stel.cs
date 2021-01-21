using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.Im2300N_Stel
{
    public partial class Driver
    {
        private byte[] MakeStelRangeRequest(byte start, byte end)
        {
            var bytes = new byte[]
            {
                0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x19,0x01,0x03,start,end,0x00,0x00
            };
            var crc = CalcStelCrc(bytes, 0, bytes.Length - 1);
            bytes[0] = 0xff;
            bytes[1] = 0xff;
            bytes[bytes.Length - 1] = crc;
            return bytes;
        }

        private dynamic GetStelVersion()
        {
            return ParseStelVersion(Send(MakeStelVersionRequest()));
        }

        private dynamic ParseStelVersion(byte[] bytes)
        {
            dynamic version = new ExpandoObject();
            version.success = true;
            if (bytes == null || !bytes.Any())
            {
                version.success = false;
                version.error = "данные для разбора не получены";
                return version;
            }

            if (bytes.Length < 10)
            {
                version.success = false;
                version.error = "версия стела не разобрана";
                return version;
            }

            version.version = Encoding.ASCII.GetString(bytes, 8, bytes.Length - 8);
            return version;
        }

        private byte[] MakeStelVersionRequest()
        {
            var bytes = new byte[]
            {
                0xFF,0xFF,0x00,0x00,0x00,0x00,0x00,0x18,0xEA
            };
            return bytes;
        }
    }
}
