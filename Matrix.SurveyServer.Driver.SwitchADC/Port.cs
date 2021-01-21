using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SwitchADC
{
    public partial class Driver
    {
        private dynamic OpenPort(byte na, byte channel, byte start)
        {
            return ParseOpenPort(Send(MakeOpenPortReq(na, channel, start)));
        }

        private dynamic ParseOpenPort(byte[] bytes)
        {
            var port = ParseResp(bytes);
            if (!port.success) return port;

            port.state = bytes[4];

            return port;
        }

        private byte[] MakeOpenPortReq(byte na, byte channel, byte start)
        {
            return MakeReq(na, new byte[]
            {
                channel,
                start
            });
        }

        private dynamic ParseResp(byte[] bytes)
        {
            dynamic resp = new ExpandoObject();
            resp.success = true;

            if (bytes == null || !bytes.Any())
            {
                resp.success = false;
                resp.error = "нет данных";
                return resp;
            }

            if (!CheckCrc(bytes))
            {
                resp.success = false;
                resp.error = "не сошлась КС";
                return resp;
            }

            return resp;
        }

        private byte[] MakeReq(byte na, byte[] body)
        {
            var bytes = new List<byte>();

            bytes.Add(na);
            var len = 1 + 2 + body.Length + 1;
            bytes.Add((byte)(len));
            bytes.Add((byte)(len >> 8));
            bytes.AddRange(body);
            byte crc = CalcCrc(bytes.ToArray());
            bytes.Add(crc);
            return bytes.ToArray(); ;
        }
    }
}
