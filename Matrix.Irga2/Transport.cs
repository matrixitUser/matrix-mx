using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Matrix.Irga2
{
	public partial class Driver
	{
		private byte[] SendWithCrc(byte[] data)
		{
			request(data);
			log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
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

			log(string.Format("пришло {0}", string.Join(",", range.Select(b => b.ToString("X2")))));
			return range.ToArray();
		}

        private byte[] Send(byte[] data)
        {
            request(data);
            log(string.Format("ушло {0}", string.Join(",", data.Select(b => b.ToString("X2")))));
            byte[] buffer = new byte[] { };
            var timeout = 10000;
            var sleep = 100;
            List<byte> range = new List<byte>();
            while ((timeout -= sleep) > 0)
            {
                Thread.Sleep(sleep);
                buffer = response();
                if (!buffer.Any()) continue;

                range.AddRange(buffer);
                break;
            }

            log(string.Format("пришло {0}", string.Join(",", range.Select(b => b.ToString("X2")))));
            return range.ToArray();
        }
    }
}

