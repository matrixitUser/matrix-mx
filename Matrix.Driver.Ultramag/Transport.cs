using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Matrix.Driver.Ultramag
{
    public partial class Driver
    {
        private byte[] SendWithCrc(byte[] data)
        {
            request(data);
            //log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            byte[] buffer = new byte[] { };
            var timeout = 10000;
            var sleep = 100;
            List<byte> range = new List<byte>();
            while ((timeout -= sleep) > 0 && !CheckCrc16(range.ToArray()))
            {
                Thread.Sleep(sleep);
                buffer = response();
                if (!buffer.Any()) continue;

                range.AddRange(buffer);
            }

            //log(string.Format("пришло {0}", string.Join(",", range.Select(b => b.ToString("X2")))));
            return range.ToArray();
        }
    }
}
